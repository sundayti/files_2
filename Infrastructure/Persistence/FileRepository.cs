using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence;

namespace Infrastructure.Services;

/// <summary>
/// Реализация IFileRepository на основе Entity Framework Core / PostgreSQL.
/// </summary>
public class FileRepository : IFileRepository
{
    private readonly FileDbContext _dbContext;

    public FileRepository(FileDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Получить метаданные файла по его FileId.
    /// </summary>
    /// <param name="id">Value Object FileId</param>
    /// <returns>FileRecord или null, если не найден</returns>
    public async Task<FileRecord?> GetByIdAsync(FileId id)
    {
        // Здесь используем FindAsync, который сам развернёт значение FileId → Guid через конвертер.
        return await _dbContext.Files.FindAsync(id);
    }

    /// <summary>
    /// Сохранить новую запись о файле (метаданные) в базу.
    /// </summary>
    public async Task AddAsync(FileRecord file)
    {
        await _dbContext.Files.AddAsync(file);
        await _dbContext.SaveChangesAsync();
    }
}