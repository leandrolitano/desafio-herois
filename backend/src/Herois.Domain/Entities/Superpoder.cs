namespace Herois.Domain.Entities;

public class Superpoder
{
    public int Id { get; set; }

    // Nome do superpoder.
    // OBS: no desafio a coluna se chama "Superpoder". Em C# não é permitido
    // ter um membro com o mesmo nome do tipo (CS0542), então usamos "Nome" e
    // mapeamos para a coluna correta no AppDbContext.
    public string Nome { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public List<HeroiSuperpoder> HeroisSuperpoderes { get; set; } = new();
}
