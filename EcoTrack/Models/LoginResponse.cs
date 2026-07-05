using System.Text.Json.Serialization;

namespace EcoTrack.Models
{
    // Response from the /login endpoint.
    // Contains success flag, a message, and the user's information if successful.
    public class LoginResponse
    {
        [JsonPropertyName("success")]
        public bool Success
        {
            get;
            set;
        }

        [JsonPropertyName("message")]
        public string Message
        {
            get;
            set;
        }
        = string.Empty;

        // User information is only present when login succeeds.
        [JsonPropertyName("user")]
        public UserInfo? User
        {
            get;
            set;
        }
    }

    // Detailed user information returned after successful login.
    public class UserInfo
    {
        [JsonPropertyName("id")]
        public int Id
        {
            get;
            set;
        }

        [JsonPropertyName("username")]
        public string Username
        {
            get;
            set;
        }
        = string.Empty;

        [JsonPropertyName("role")]
        public string Role
        {
            get;
            set;
        }
        = string.Empty;
    }
}