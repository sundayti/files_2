namespace Domain.ValueObjects;

/// <summary>
/// Идентификатор файла (GUID).
/// </summary>
public sealed record FileId
{
    public Guid Value { get; }

    private FileId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("FileId не может быть пустым GUID.", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Создаёт новый уникальный FileId.
    /// </summary>
    public static FileId New() =>
        new FileId(Guid.NewGuid());

    /// <summary>
    /// Создаёт FileId из уже существующего GUID (например, при чтении из БД).
    /// </summary>
    public static FileId From(Guid value) =>
        new FileId(value);

    public override string ToString() => Value.ToString();
}