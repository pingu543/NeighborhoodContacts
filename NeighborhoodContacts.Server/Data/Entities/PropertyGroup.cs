namespace NeighborhoodContacts.Server.Data.Entities
{
    public class PropertyGroup
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Property> Properties { get; set; } = new();
    }
}