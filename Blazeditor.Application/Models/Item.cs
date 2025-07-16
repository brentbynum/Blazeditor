namespace Blazeditor.Application.Models;

public class Item(string name, string description) : BaseEntity(name, description)
{
    public int Quality { get; set; } = 1;
}