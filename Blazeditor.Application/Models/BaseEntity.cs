namespace Blazeditor.Application.Models
{
    public class BaseEntity()
    {
        public int Id { get; private set; } = GenerateId();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        private static int _lastId = 0;
        public static int GenerateId()
        {
            return ++_lastId;
        }
    }
}