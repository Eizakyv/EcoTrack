using Android.Content;

namespace EcoTrack.Platforms.Android;

// Helper class to start and stop the foreground location service.
// Encapsulates version-specific logic for Android API levels.
public static class LocationServiceHelper
{
    // Starts the foreground service with the provided device ID and username.
    // Uses StartForegroundService on Android 8.0+ (API 26+) and StartService on older versions.
    public static void StartForegroundService(string deviceId, string username)
    {
        // Get the current Android activity context
        var activity = Platform.CurrentActivity;
        if (activity == null)
        {
            return;
        }

        // Create an intent targeting the foreground service
        var intent = new Intent(activity, typeof(LocationForegroundService));
        intent.PutExtra("deviceId", deviceId);
        intent.PutExtra("username", username);

        // Use the appropriate start method based on Android version
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            activity.StartForegroundService(intent);
        }
        else
        {
            activity.StartService(intent);
        }
    }

    // Stops the foreground service.
    // Delegates to LocationForegroundService.StopService which handles cleanup.
    public static void StopForegroundService()
    {
        // Get the current Android activity context
        var context = Platform.CurrentActivity;
        if (context != null)
        {
            LocationForegroundService.StopService(context);
        }
    }
}