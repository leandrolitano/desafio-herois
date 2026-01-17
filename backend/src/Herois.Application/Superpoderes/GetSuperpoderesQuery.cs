using Herois.Application.Common;
using Herois.Application.DTOs;
using MediatR;

namespace Herois.Application.Superpoderes;

public record GetSuperpoderesQuery() : IRequest<Result<List<SuperpoderDto>>>;
