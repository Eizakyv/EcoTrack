using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EcoTrack.Models
{
    // Response from the /users/locations endpoint.
    // Contains a list of user locations within the park.
    public class UserLocationsResponse
    {
        [JsonPropertyName("users")]
        public List<UserLocation> Users
        {
            get;
            set;
        }
        = [];
    }

    // Represents a single user's location data displayed on the map.
    // Properties marked as nullable correspond to fields that the server may omit.
    public class UserLocation
    {
        [JsonPropertyName("display_name")]
        public string DisplayName
        {
            get;
            set;
        }
        = string.Empty;

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

        [JsonPropertyName("status")]
        public string Status
        {
            get;
            set;
        }
        = string.Empty;

        // Trail name is optional (null if the user is not on a known trail).
        [JsonPropertyName("trail_name")]
        public string? TrailName
        {
            get;
            set;
        }

        // Distance to the trail in meters; may be null if no trail is nearby.
        [JsonPropertyName("distance_meters")]
        public double? DistanceMeters
        {
            get;
            set;
        }

        // User role (admin, guard, user, or null for unauthenticated).
        [JsonPropertyName("role")]
        public string? Role
        {
            get;
            set;
        }

        // Distance to the nearest power line; only present if in danger.
        [JsonPropertyName("power_line_distance")]
        public double? PowerLineDistance
        {
            get;
            set;
        }
    }
}