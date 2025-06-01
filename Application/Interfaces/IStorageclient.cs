using Domain.ValueObjects;

namespace Application.Interfaces;

/// <summary>
/// Абстракция над внешним хранилищем (MinIO).
/// </summary>
public interface IStorageClient
{
    /// <summary>
    /// Сохраняет массив байт в MinIO, возвращает локацию в бакете.
    /// </summary>
    Task<FileLocation> SaveAsync(byte[] content, string fileName, CancellationToken cancellationToken);

    /// <summary>
    /// Читает файл по локации (ключу в MinIO) и возвращает массив байт.
    /// </summary>
    Task<byte[]> GetAsync(FileLocation location, CancellationToken cancellationToken);
}