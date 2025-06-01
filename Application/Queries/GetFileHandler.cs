using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;

namespace Application.Queries;

/// <summary>
/// Обработчик GetFileQuery:
/// 1) Получает FileRecord из репозитория.
/// 2) Если нет записи — бросает исключение.
/// 3) По Location запрашивает байты из MinIO.
/// 4) Возвращает FileDto с содержимым и метаданными.
/// </summary>
public class GetFileHandler(
    IFileRepository fileRepository,
    IStorageClient storageClient)
    : IRequestHandler<GetFileQuery, FileDto>
{
    public async Task<FileDto> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        // 1) Преобразуем Guid в FileId
        var fileId = FileId.From(request.FileId);

        // 2) Читаем метаданные из БД
        var record = await fileRepository.GetByIdAsync(fileId);
        if (record is null)
            throw new KeyNotFoundException($"Файл с Id = {request.FileId} не найден.");

        // 3) Получаем содержимое из MinIO
        byte[] contentBytes = await storageClient.GetAsync(record.Location, cancellationToken);

        // 4) Формируем и возвращаем DTO
        return new FileDto
        {
            Content = contentBytes,
            FileName = record.Name,
            ContentType = record.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? "application/pdf"
                : "application/octet-stream"
        };
    }
}