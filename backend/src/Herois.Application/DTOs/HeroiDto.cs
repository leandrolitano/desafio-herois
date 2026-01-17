namespace Herois.Application.DTOs;

public record HeroiDto(
    int Id,
    string Nome,
    string NomeHeroi,
    DateTime DataNascimento,
    double Altura,
    double Peso,
    string RowVersion,
    List<SuperpoderDto> Superpoderes
);
