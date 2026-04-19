namespace NeighborhoodContacts.Server.Data.Entities
{
    public class Property
    {
        public Guid Id { get; set; }
        public string Address { get; set; } = string.Empty;
        public Guid PropertyGroupId { get; set; }
        public PropertyGroup? PropertyGroup { get; set; }
        public List<User> Users { get; set; } = [];
    }
}
