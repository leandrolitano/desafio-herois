using FluentValidation;
using Herois.Api.Errors;
using Herois.Api.Middleware;
using Herois.Application.Behaviors;
using Herois.Domain.Entities;
using Herois.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MediatR (CQRS)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Herois.Application.Herois.CreateHeroCommand).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

// FluentValidation (validators no projeto Application)
builder.Services.AddValidatorsFromAssembly(typeof(Herois.Application.Herois.CreateHeroCommand).Assembly);

// Cache simples (para endpoints de consulta como superpoderes)
builder.Services.AddMemoryCache();

// ProblemDetails + ExceptionHandler
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        if (ctx.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var cid) && cid is string correlationId)
        {
            ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
        }
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var connectionString = builder.Configuration.GetConnectionString("SqlServer")
    ?? throw new InvalidOperationException("Connection string SqlServer not found.");

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("dev", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db");

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("dev");

// Inicializa banco (migrations, se existirem; senao EnsureCreated)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (db.Database.GetMigrations().Any())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();



    // Hotfix de compatibilidade: caso o banco ja exista de versoes anteriores (sem RowVersion),
    // adiciona a coluna automaticamente no SQL Server (evita "Invalid column name 'RowVersion'").
    if (db.Database.IsSqlServer())
    {
        try
        {
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Herois' AND COLUMN_NAME = 'RowVersion'";
            var countObj = cmd.ExecuteScalar();
            var count = countObj is int i ? i : Convert.ToInt32(countObj);

            if (count == 0)
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE Herois ADD RowVersion rowversion;");
            }
        }
        catch
        {
            // Se falhar por qualquer motivo, nao impede a subida. Em dev, basta recriar o container do DB.
        }
    }

    // Seed de superpoderes (idempotente) - ampliamos o catálogo para suportar o seed de heróis.
    // (Em C# o campo se chama "Nome", mas na tabela a coluna é "Superpoder".)
    var superpoderesSeed = new List<Superpoder>
    {
        new() { Nome = "Forca", Descricao = "Forca fisica acima do normal" },
        new() { Nome = "Resistencia", Descricao = "Alta resistencia fisica" },
        new() { Nome = "Agilidade", Descricao = "Reflexos e mobilidade acima da media" },
        new() { Nome = "Velocidade", Descricao = "Movimenta-se muito rapido" },
        new() { Nome = "Voo", Descricao = "Capacidade de voar" },
        new() { Nome = "Invisibilidade", Descricao = "Pode ficar invisivel" },
        new() { Nome = "Tecnologia", Descricao = "Uso de tecnologia/armaduras/dispositivos" },
        new() { Nome = "Energia", Descricao = "Manipulacao/projecao de energia" },
        new() { Nome = "Magia", Descricao = "Feiticos, artefatos e energia mistica" },
        new() { Nome = "Telepatia", Descricao = "Habilidades psiquicas/telepatia" },
        new() { Nome = "Regeneracao", Descricao = "Cura acelerada / regeneracao" },
        new() { Nome = "Metamorfose", Descricao = "Pode alterar o proprio corpo/forma" },
        new() { Nome = "Artes Marciais", Descricao = "Combate corpo a corpo e tecnicas" },
        new() { Nome = "Faseamento", Descricao = "Pode atravessar materia (intangibilidade)" }
    };

    var existentes = db.Superpoderes.Select(s => s.Nome).ToHashSet();
    var faltantes = superpoderesSeed.Where(s => !existentes.Contains(s.Nome)).ToList();
    if (faltantes.Count > 0)
    {
        db.Superpoderes.AddRange(faltantes);
        db.SaveChanges();
    }

    // Seed de 100 heróis (apenas em Development, ou se SEED_MARVEL_HEROES=true)
    // Mantém os testes de integração (ambiente "Testing") livres de seed.
    var seedMarvelHeroes = app.Environment.IsDevelopment()
        || string.Equals(Environment.GetEnvironmentVariable("SEED_MARVEL_HEROES"), "true", StringComparison.OrdinalIgnoreCase);

    if (seedMarvelHeroes && !db.Herois.Any())
    {
        // Recarrega superpoderes após seed.
        var superpoderes = db.Superpoderes.ToList();

        Superpoder? FindPower(string nome) =>
            superpoderes.FirstOrDefault(p => string.Equals(p.Nome, nome, StringComparison.OrdinalIgnoreCase));

        void AddHero(List<Heroi> list, int index, MarvelHeroSeed seed)
        {
            // Valores default só para demo (o desafio não exige fidelidade de biografia).
            var dataNascimento = new DateTime(1970, 1, 1).AddDays(index * 30);
            var altura = 1.65 + (index % 50) * 0.01; // 1.65m a ~2.14m
            var peso = 60 + (index % 80);           // 60kg a 139kg

            var heroi = new Heroi
            {
                NomeHeroi = seed.NomeHeroi,
                Nome = seed.Nome,
                DataNascimento = dataNascimento,
                Altura = Math.Round(altura, 2),
                Peso = peso
            };

            foreach (var powerName in seed.Poderes.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var power = FindPower(powerName);
                if (power is null) continue;
                heroi.HeroisSuperpoderes.Add(new HeroiSuperpoder { Superpoder = power });
            }

            // Segurança: garante pelo menos 1 poder para não quebrar o UX no frontend.
            if (heroi.HeroisSuperpoderes.Count == 0)
            {
                var fallback = FindPower("Agilidade") ?? FindPower("Forca");
                if (fallback is not null)
                    heroi.HeroisSuperpoderes.Add(new HeroiSuperpoder { Superpoder = fallback });
            }

            list.Add(heroi);
        }

        var marvelHeroes = new List<Heroi>();
        var seeds = MarvelHeroSeed.List;
        for (var i = 0; i < seeds.Count; i++)
            AddHero(marvelHeroes, i, seeds[i]);

        db.Herois.AddRange(marvelHeroes);
        db.SaveChanges();
    }
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program { }

// Seed de 100 heróis "principais" (curadoria para demo). Para evitar depender de uma lista única
// de terceiros, mantemos uma seleção ampla de personagens icônicos de várias "famílias" (Avengers,
// X-Men, FF, etc.). Ajuste livremente conforme a necessidade do desafio.
internal sealed record MarvelHeroSeed(string NomeHeroi, string Nome, string[] Poderes)
{
    public static IReadOnlyList<MarvelHeroSeed> List { get; } = new List<MarvelHeroSeed>
    {
        new("Spider-Man", "Peter Parker", new[] { "Agilidade", "Forca" }),
        new("Wolverine", "Logan", new[] { "Regeneracao", "Forca" }),
        new("Captain America", "Steve Rogers", new[] { "Resistencia", "Artes Marciais" }),
        new("Iron Man", "Tony Stark", new[] { "Tecnologia", "Voo" }),
        new("Thor", "Thor Odinson", new[] { "Forca", "Voo", "Energia" }),
        new("Hulk", "Bruce Banner", new[] { "Forca", "Resistencia", "Regeneracao" }),
        new("Black Widow", "Natasha Romanoff", new[] { "Artes Marciais", "Agilidade" }),
        new("Hawkeye", "Clint Barton", new[] { "Agilidade" }),
        new("Doctor Strange", "Stephen Strange", new[] { "Magia" }),
        new("Black Panther", "T'Challa", new[] { "Agilidade", "Artes Marciais" }),
        new("Scarlet Witch", "Wanda Maximoff", new[] { "Magia", "Energia" }),
        new("Vision", "Vision", new[] { "Voo", "Energia" }),
        new("Ant-Man (Scott Lang)", "Scott Lang", new[] { "Tecnologia" }),
        new("Wasp (Janet van Dyne)", "Janet van Dyne", new[] { "Voo", "Tecnologia" }),
        new("Captain Marvel", "Carol Danvers", new[] { "Voo", "Energia" }),
        new("Ms. Marvel", "Kamala Khan", new[] { "Metamorfose" }),
        new("Daredevil", "Matt Murdock", new[] { "Agilidade", "Artes Marciais" }),
        new("Punisher", "Frank Castle", new[] { "Artes Marciais" }),
        new("Luke Cage", "Luke Cage", new[] { "Resistencia", "Forca" }),
        new("Iron Fist", "Danny Rand", new[] { "Artes Marciais", "Energia" }),
        new("Jessica Jones", "Jessica Jones", new[] { "Forca" }),
        new("Moon Knight", "Marc Spector", new[] { "Artes Marciais", "Resistencia" }),
        new("Ghost Rider", "Johnny Blaze", new[] { "Energia" }),
        new("Blade", "Eric Brooks", new[] { "Regeneracao", "Agilidade" }),
        new("Deadpool", "Wade Wilson", new[] { "Regeneracao", "Agilidade" }),
        new("Silver Surfer", "Norrin Radd", new[] { "Voo", "Energia" }),
        new("Mister Fantastic", "Reed Richards", new[] { "Metamorfose" }),
        new("Invisible Woman", "Sue Storm", new[] { "Invisibilidade", "Energia" }),
        new("Human Torch", "Johnny Storm", new[] { "Voo", "Energia" }),
        new("The Thing", "Ben Grimm", new[] { "Forca", "Resistencia" }),
        new("Storm", "Ororo Munroe", new[] { "Energia", "Voo" }),
        new("Cyclops", "Scott Summers", new[] { "Energia" }),
        new("Jean Grey", "Jean Grey", new[] { "Telepatia", "Energia" }),
        new("Professor X", "Charles Xavier", new[] { "Telepatia" }),
        new("Beast", "Hank McCoy", new[] { "Agilidade", "Forca" }),
        new("Iceman", "Bobby Drake", new[] { "Energia" }),
        new("Rogue", "Anna Marie", new[] { "Forca", "Energia" }),
        new("Gambit", "Remy LeBeau", new[] { "Energia", "Agilidade" }),
        new("Nightcrawler", "Kurt Wagner", new[] { "Agilidade" }),
        new("Colossus", "Piotr Rasputin", new[] { "Forca", "Resistencia" }),
        new("Kitty Pryde", "Kitty Pryde", new[] { "Faseamento" }),
        new("Emma Frost", "Emma Frost", new[] { "Telepatia" }),
        new("Psylocke", "Betsy Braddock", new[] { "Telepatia", "Artes Marciais" }),
        new("Jubilee", "Jubilation Lee", new[] { "Energia" }),
        new("Cable", "Nathan Summers", new[] { "Tecnologia", "Energia" }),
        new("Bishop", "Lucas Bishop", new[] { "Energia" }),
        new("X-23", "Laura Kinney", new[] { "Regeneracao", "Agilidade" }),
        new("Domino", "Neena Thurman", new[] { "Agilidade" }),
        new("Archangel", "Warren Worthington III", new[] { "Voo" }),
        new("Magik", "Illyana Rasputin", new[] { "Magia" }),
        new("Nova (Richard Rider)", "Richard Rider", new[] { "Voo", "Energia" }),
        new("Nova (Sam Alexander)", "Sam Alexander", new[] { "Voo", "Energia" }),
        new("Nick Fury", "Nick Fury", new[] { "Tecnologia" }),
        new("War Machine", "James Rhodes", new[] { "Tecnologia", "Voo" }),
        new("Falcon", "Sam Wilson", new[] { "Voo" }),
        new("Winter Soldier", "Bucky Barnes", new[] { "Artes Marciais" }),
        new("Spider-Woman", "Jessica Drew", new[] { "Agilidade" }),
        new("She-Hulk", "Jennifer Walters", new[] { "Forca", "Resistencia" }),
        new("Shang-Chi", "Shang-Chi", new[] { "Artes Marciais", "Agilidade" }),
        new("Squirrel Girl", "Doreen Green", new[] { "Forca" }),
        new("Star-Lord", "Peter Quill", new[] { "Tecnologia" }),
        new("Gamora", "Gamora", new[] { "Artes Marciais", "Agilidade" }),
        new("Drax", "Drax", new[] { "Forca" }),
        new("Rocket Raccoon", "Rocket Raccoon", new[] { "Tecnologia" }),
        new("Groot", "Groot", new[] { "Resistencia" }),
        new("Mantis", "Mantis", new[] { "Telepatia" }),
        new("Adam Warlock", "Adam Warlock", new[] { "Energia" }),
        new("Quasar", "Wendell Vaughn", new[] { "Energia" }),
        new("Spectrum", "Monica Rambeau", new[] { "Energia", "Voo" }),
        new("Valkyrie", "Brunnhilde", new[] { "Forca" }),
        new("Namor", "Namor McKenzie", new[] { "Forca", "Voo" }),
        new("Captain Britain", "Brian Braddock", new[] { "Forca", "Voo" }),
        new("America Chavez", "America Chavez", new[] { "Forca" }),
        new("Wiccan", "Billy Kaplan", new[] { "Magia" }),
        new("Hulkling", "Teddy Altman", new[] { "Metamorfose", "Forca" }),
        new("Kate Bishop", "Kate Bishop", new[] { "Agilidade" }),
        new("Patriot", "Eli Bradley", new[] { "Resistencia" }),
        new("Speed", "Tommy Shepherd", new[] { "Velocidade" }),
        new("Stature", "Cassie Lang", new[] { "Tecnologia" }),
        new("Ironheart", "Riri Williams", new[] { "Tecnologia", "Voo" }),
        new("Spider-Man (Miles Morales)", "Miles Morales", new[] { "Agilidade", "Invisibilidade" }),
        new("Spider-Gwen", "Gwen Stacy", new[] { "Agilidade" }),
        new("Silk", "Cindy Moon", new[] { "Agilidade" }),
        new("Spider-Man 2099", "Miguel O'Hara", new[] { "Agilidade" }),
        new("Scarlet Spider (Ben Reilly)", "Ben Reilly", new[] { "Agilidade" }),
        new("Scarlet Spider (Kaine)", "Kaine Parker", new[] { "Agilidade", "Regeneracao" }),
        new("Moon Girl", "Lunella Lafayette", new[] { "Tecnologia" }),
        new("Doctor Voodoo", "Jericho Drumm", new[] { "Magia" }),
        new("Cloak", "Tyrone Johnson", new[] { "Invisibilidade" }),
        new("Dagger", "Tandy Bowen", new[] { "Energia" }),
        new("Quake", "Daisy Johnson", new[] { "Energia" }),
        new("Mockingbird", "Bobbi Morse", new[] { "Artes Marciais" }),
        new("Misty Knight", "Misty Knight", new[] { "Artes Marciais" }),
        new("Colleen Wing", "Colleen Wing", new[] { "Artes Marciais" }),
        new("Songbird", "Melissa Gold", new[] { "Energia", "Voo" }),
        new("Hercules", "Hercules", new[] { "Forca" }),
        new("Sentry", "Robert Reynolds", new[] { "Forca", "Energia" }),
        new("Blue Marvel", "Adam Brashear", new[] { "Energia" }),
        new("Black Knight", "Dane Whitman", new[] { "Artes Marciais" }),
        new("Agent Venom", "Flash Thompson", new[] { "Agilidade", "Regeneracao" })
    };
}
