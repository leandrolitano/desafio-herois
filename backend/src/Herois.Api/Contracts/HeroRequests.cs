namespace Herois.Api.Contracts;

public record CreateHeroRequest(
    string Nome,
    string NomeHeroi,
    DateTime DataNascimento,
    double Altura,
    double Peso,
    List<int> SuperpoderIds
);

public record UpdateHeroRequest(
    string Nome,
    string NomeHeroi,
    DateTime DataNascimento,
    double Altura,
    double Peso,
    List<int> SuperpoderIds,
    string RowVersion
);
