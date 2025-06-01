using Domain.ValueObjects;

namespace Domain.Entities;

public class FileRecord
{
    public FileId Id { get; private set; }
    public string Name { get; private set; }
    public FileLocation Location { get; private set; }

    private FileRecord() { }

    public FileRecord(string name, FileLocation location)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name не может быть пустым.", nameof(name));

        Id = FileId.New();
        Name = name;
        Location = location;
    }
}


