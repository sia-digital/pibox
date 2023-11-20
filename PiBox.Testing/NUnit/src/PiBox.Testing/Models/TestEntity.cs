namespace PiBox.Testing.Models
{
    public record BaseTestEntity(Guid Id, string Name, DateTime CreationDate, BaseSubTestEntity SubTestEntity = null)
    {
        public Guid Id { get; set; } = Id;
        public DateTime CreationDate { get; set; } = CreationDate;

        public BaseSubTestEntity SubTestEntity { get; set; } = SubTestEntity;
    }

    public record BaseSubTestEntity(Guid Id, string SubNode)
    {
        public Guid Id { get; set; } = Id;
        public string SubNode { get; set; } = SubNode;
    }
}
