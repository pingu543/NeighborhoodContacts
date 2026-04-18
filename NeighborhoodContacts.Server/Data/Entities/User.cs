namespace NeighborhoodContacts.Server.Data.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        public Property? Property { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string? ContactNumber { get; set; }
        public string? ContactEmail { get; set; }
        public string? AboutMe { get; set; }
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime Created { get; set; }
    }
}
