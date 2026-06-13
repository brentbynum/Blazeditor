namespace Blazeditor.Application.Models;

public class BaseEntity
{
    public BaseEntity() { }
    public BaseEntity(string name, string? description)
    {
        Name = name;
        Description = description;
    }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}