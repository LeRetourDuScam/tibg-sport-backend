namespace TIBG.Models
{
    public class UserFeedback
    {
        public int Id { get; set; }
        public required string Rating { get; set; }
        public string? Comment { get; set; }
        public required string Sport { get; set; }
        public int Score { get; set; }
        public string? UserId { get; set; }
        public string? Context { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
