namespace NeighborhoodContacts.Server.Data.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        public Property? Property { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string? ContactNumber { get; set; }
        public string? ContactEmail { get; set; }
        public string? SelfIntroduction { get; set; }
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime Created { get; set; }
    }
}
