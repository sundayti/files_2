namespace Application.DTOs;

/// <summary>
/// DTO для передачи файла клиенту (скачивание).
/// </summary>
public record FileDto
{
    /// <summary>Сырые байты файла.</summary>
    public byte[] Content { get; init; } = [];

    /// <summary>Имя файла (например, "report.pdf").</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>МIME-тип (например, "application/pdf").</summary>
    public string ContentType { get; init; } = string.Empty;
}
