namespace Blazeditor.Application.Models
{
    public class BaseEntity(string name, string? description)
    {
        public int Id { get; private set; } = GenerateId();
        public string Name { get; set; } = name;
        public string? Description { get; set; } = description;
        private static int _lastId = 0;
        public static int GenerateId()
        {
            return ++_lastId;
        }
    }
}