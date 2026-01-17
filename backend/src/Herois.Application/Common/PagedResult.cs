namespace Herois.Application.Common;

/// <summary>
/// Estrutura simples de paginação para respostas de listagem.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize
);
