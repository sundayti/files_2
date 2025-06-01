namespace Application.DTOs;

/// <summary>
/// Результат команды UploadFileCommand — возвращает сгенерированный FileId.
/// </summary>
public record UploadFileResult(Guid FileId);
