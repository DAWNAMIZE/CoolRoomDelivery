namespace ColdRoom.Core.Models
{
    public sealed class User
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public UserRole Role { get; set; }
        public string DefaultStationId { get; set; }
        public string CreatedAt { get; set; }
    }
}
