using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext для работы с таблицей файлов. 
/// Отвечает за соединение с PostgreSQL и маппинг сущности FileRecord.
/// </summary>
public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Набор записей о файлах (таблица "files").
    /// </summary>
    public DbSet<FileRecord> Files { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Настраиваем маппинг сущности FileRecord → таблица "files"
        modelBuilder.Entity<FileRecord>(entity =>
        {
            entity.ToTable("files"); // имя таблицы

            // Primary Key: Id (value object FileId)
            // Используем ValueConverter, чтобы EF умел сохранять Guid из FileId.Value и обратно.
            var fileIdConverter = new ValueConverter<FileId, Guid>(
                v => v.Value,
                v => FileId.From(v)
            );
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Id)
                  .HasConversion(fileIdConverter)
                  .HasColumnName("id")
                  .IsRequired();

            // Имя файла
            entity.Property(f => f.Name)
                  .HasColumnName("name")
                  .HasMaxLength(255)
                  .IsRequired();

            // Локация (ключ) в MinIO
            var fileLocationConverter = new ValueConverter<FileLocation, string>(
                v => v.Value,
                v => new FileLocation(v)
            );
            entity.Property(f => f.Location)
                  .HasConversion(fileLocationConverter)
                  .HasColumnName("location")
                  .HasMaxLength(1024)
                  .IsRequired();

            // Если понадобится поле ContentType или CreatedAt, можно добавить здесь:
            // entity.Property(f => f.ContentType) ...
            // entity.Property(f => f.CreatedAt) ...
        });
    }
}