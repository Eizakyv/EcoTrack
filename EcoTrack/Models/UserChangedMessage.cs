namespace EcoTrack.Models
{
    public class UserChangedMessage
    {
        public string NewUsername { get; set; } = string.Empty;
        public string NewRole { get; set; } = string.Empty;
    }
}