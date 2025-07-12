namespace Blazeditor.Application.Models
{
    public class BaseEntity
    {
        public BaseEntity() { }
        public BaseEntity(string name, string? description)
        {
            Id = GenerateId();
            Name = name;
            Description = description;
        }
        public int Id { get; set; } = GenerateId();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        private static int _lastId = 0;
        public static int GenerateId()
        {
            return ++_lastId;
        }
    }
}