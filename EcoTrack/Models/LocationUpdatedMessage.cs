using EcoTrack.Models;

namespace EcoTrack.Models;

// Message sent via WeakReferenceMessenger when a new location status is received.
public class LocationUpdatedMessage
{
    public LocationStatus Status
    {
        get; set;
    }
    = new LocationStatus();
}