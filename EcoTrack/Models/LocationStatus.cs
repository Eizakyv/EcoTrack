using System.Text.Json.Serialization;

namespace EcoTrack.Models
{
    // Represents the full response from the /check endpoint.
    // All properties are initialized to avoid nullability warnings.
    public class LocationStatus
    {
        [JsonPropertyName("status")]
        public string Status
        {
            get;
            set;
        }
        = string.Empty;

        [JsonPropertyName("message")]
        public string Message
        {
            get;
            set;
        }
        = string.Empty;

        [JsonPropertyName("location")]
        public Position Position
        {
            get;
            set;
        }
        = new Position();

        [JsonPropertyName("trail")]
        public Trail Trail
        {
            get;
            set;
        }
        = new Trail();

        [JsonPropertyName("powerLine")]
        public PowerLine PowerLine
        {
            get;
            set;
        }
        = new PowerLine();

        [JsonPropertyName("park")]
        public Park Park
        {
            get;
            set;
        }
        = new Park();

        [JsonPropertyName("researchZone")]
        public ResearchZone ResearchZone
        {
            get;
            set;
        }
        = new ResearchZone();
    }

    // GPS coordinates of the user.
    public class Position
    {
        [JsonPropertyName("latitude")]
        public double Latitude
        {
            get;
            set;
        }

        [JsonPropertyName("longitude")]
        public double Longitude
        {
            get;
            set;
        }
    }

    // Information about the nearest trail.
    // Name can be null if no trail is found within the search radius.
    public class Trail
    {
        [JsonPropertyName("name")]
        public string? Name
        {
            get;
            set;
        }

        [JsonPropertyName("distance_meters")]
        public double? DistanceMeters
        {
            get;
            set;
        }
    }

    // Distance to the nearest power line.
    public class PowerLine
    {
        [JsonPropertyName("distance_meters")]
        public double? DistanceMeters
        {
            get;
            set;
        }
    }

    // Park boundary information.
    public class Park
    {
        [JsonPropertyName("distance_meters")]
        public double? DistanceMeters
        {
            get;
            set;
        }

        [JsonPropertyName("inside")]
        public bool Inside
        {
            get;
            set;
        }
    }

    // Research zone (1‑ha plot) information.
    public class ResearchZone
    {
        [JsonPropertyName("distance_meters")]
        public double? DistanceMeters
        {
            get;
            set;
        }

        [JsonPropertyName("inside")]
        public bool Inside
        {
            get;
            set;
        }
    }
}