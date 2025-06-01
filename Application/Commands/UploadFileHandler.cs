using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands;

/// <summary>
/// Обработчик UploadFileCommand.
/// 1) Читает поток в массив байт.
/// 2) Сохраняет массив в MinIO, получает FileLocation.
/// 3) Создаёт FileRecord и добавляет его в репозиторий.
/// 4) Возвращает новый FileId.
/// </summary>
public class UploadFileHandler : IRequestHandler<UploadFileCommand, UploadFileResult>
{
    private readonly IFileRepository _fileRepository;
    private readonly IStorageClient _storageClient;

    public UploadFileHandler(
        IFileRepository fileRepository,
        IStorageClient storageClient)
    {
        _fileRepository = fileRepository;
        _storageClient = storageClient;
    }

    public async Task<UploadFileResult> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        // 1) Читаем содержимое потока в массив байт
        byte[] fileBytes;
        await using (var ms = new MemoryStream())
        {
            await request.ContentStream.CopyToAsync(ms, cancellationToken);
            fileBytes = ms.ToArray();
        }

        // 2) Сохраняем в MinIO (без проверки хэша)
        FileLocation location = await _storageClient.SaveAsync(fileBytes, request.FileName, cancellationToken);

        // 3) Создаём доменный объект и сохраняем в БД
        var fileRecord = new FileRecord(request.FileName, location);
        await _fileRepository.AddAsync(fileRecord);

        // 4) Возвращаем новый FileId
        return new UploadFileResult(fileRecord.Id.Value);
    }
}