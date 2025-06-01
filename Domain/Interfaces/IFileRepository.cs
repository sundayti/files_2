using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Interfaces;

/// <summary>
/// Репозиторий для хранения и получения метаданных о файлах.
/// </summary>
public interface IFileRepository
{
    /// <summary>
    /// Получает метаданные файла по его идентификатору.
    /// </summary>
    Task<FileRecord?> GetByIdAsync(FileId id);
    
    /// <summary>
    /// Добавляет новую запись о файле в хранилище.
    /// </summary>
    Task AddAsync(FileRecord file);
}
