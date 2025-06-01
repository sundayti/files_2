using MediatR;

namespace Application.Queries;

public record GetFileQuery(Guid FileId) : IRequest<DTOs.FileDto>;
