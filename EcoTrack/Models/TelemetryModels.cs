namespace EcoTrack.Models;

// Simplified telemetry response used for quick status updates.
// All fields are required and always provided by the server.
public sealed class TelemetryResponse
{
    // Current safety status: "seguro", "advertencia", "peligro", etc.
    public required string Status
    {
        get;
        init;
    }

    // Human-readable description of the current situation.
    public required string Message
    {
        get;
        init;
    }

    // Distance to the nearest trail in meters.
    public double DistanceTrail
    {
        get;
        init;
    }

    // Distance to the nearest high‑voltage power line in meters.
    public double DistanceTension
    {
        get;
        init;
    }
}