using MediatR;


namespace Application.Commands;

/// <summary>
/// Команда на загрузку файла (сохранение в MinIO + запись в БД).
/// </summary>
public record UploadFileCommand(Stream ContentStream, string FileName) : IRequest<DTOs.UploadFileResult>;
