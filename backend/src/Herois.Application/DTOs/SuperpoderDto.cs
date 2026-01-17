namespace Herois.Application.DTOs;

// OBS: No desafio, a coluna se chama "Superpoder", mas no contrato da API e no frontend
// usamos "Nome" para ficar mais semântico e consistente.
public record SuperpoderDto(int Id, string Nome, string Descricao);
