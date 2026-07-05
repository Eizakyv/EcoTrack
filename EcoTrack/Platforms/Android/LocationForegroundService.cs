using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using CommunityToolkit.Mvvm.Messaging;
using EcoTrack.Models;
using EcoTrack.Services;

namespace EcoTrack.Platforms.Android;

[Service(ForegroundServiceType = ForegroundService.TypeLocation, Enabled = true, Exported = false)]
public class LocationForegroundService : Service
{
    private const int NotificationId = 1001;
    private const string ChannelId = "location_channel";
    private const string ChannelName = "EcoTrack Updates";
    private const string ActionStopService = "ACTION_STOP_SERVICE";

    private static LocationService? _locationService;
    private static bool _isSubscribed;
    private static EventHandler<LocationStatus>? _locationHandler;

    // Pending simulation settings to apply before the service starts.
    private static bool _pendingSimulationEnabled;
    private static IReadOnlyList<Location>? _pendingSimulationRoute;

    // Sets simulation mode for the service (called from MainPage).
    public static void SetSimulationMode(bool enabled, IReadOnlyList<Location>? route = null)
    {
        _pendingSimulationEnabled = enabled;
        _pendingSimulationRoute = route;

        // If the service is already running, apply directly.
        if (_locationService != null)
        {
            _locationService.SetSimulationMode(enabled, route);
        }
    }

    // Returns the shared LocationService instance (for MainPage to subscribe).
    public static LocationService? GetLocationService() => _locationService;

    // Static event handler for location updates.
    private static void OnLocationUpdatedStatic(object? sender, LocationStatus result)
    {
        // Update the notification.
        UpdateNotificationStatic(result);

        // Send the location status to the UI via the messenger.
        WeakReferenceMessenger.Default.Send(new LocationUpdatedMessage { Status = result });
    }

    private static void UpdateNotificationStatic(LocationStatus result)
    {
        var context = global::Android.App.Application.Context;
        if (context == null) return;

        string statusText = GetStatusTextStatic(result.Status);
        string messageText = BuildMessageTextStatic(result);

        var intent = new Intent(context, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

        PendingIntentFlags pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }

        var pendingIntent = PendingIntent.GetActivity(context, 0, intent, pendingIntentFlags);

        var builder = new NotificationCompat.Builder(context, ChannelId)!;
        builder.SetContentTitle(statusText);
        builder.SetContentText(messageText);
        builder.SetSmallIcon(global::Android.Resource.Drawable.SymDefAppIcon);
        builder.SetContentIntent(pendingIntent);
        builder.SetOngoing(true);
        builder.SetPriority(NotificationCompat.PriorityDefault);
        builder.SetVisibility(NotificationCompat.VisibilityPublic);

        var stopIntent = new Intent(context, typeof(LocationForegroundService));
        stopIntent.SetAction(ActionStopService);
        var stopPendingIntent = PendingIntent.GetService(context, 1, stopIntent, pendingIntentFlags);
        int stopIcon = global::Android.Resource.Drawable.IcMenuCloseClearCancel;
        builder.AddAction(stopIcon, "Cerrar", stopPendingIntent);

        var notification = builder.Build()!;
        var manager = (NotificationManager?)context.GetSystemService(NotificationService);
        manager?.Notify(NotificationId, notification);
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == ActionStopService)
        {
            StopForegroundServiceOnly();
            return StartCommandResult.NotSticky;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            CreateNotificationChannel();
        }

        Notification initialNotification = BuildNotification("⏳ ESPERANDO...", "Iniciando servicio de rastreo...");

        // Start foreground with the notification.
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            StartForeground(NotificationId, initialNotification, ForegroundService.TypeLocation);
        }
        else if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            StartForeground(NotificationId, initialNotification, ForegroundService.TypeLocation);
        }
        else
        {
            StartForeground(NotificationId, initialNotification);
        }

        // Create the LocationService if it doesn't exist.
        if (_locationService == null)
        {
            string deviceId = intent?.GetStringExtra("deviceId") ?? string.Empty;
            string username = intent?.GetStringExtra("username") ?? string.Empty;
            _locationService = new LocationService(deviceId, username);

            // Apply pending simulation settings before starting.
            _locationService.SetSimulationMode(_pendingSimulationEnabled, _pendingSimulationRoute);

            _locationService.Start();

            _locationHandler = OnLocationUpdatedStatic;
            _locationService.LocationUpdated += _locationHandler;
            _isSubscribed = true;
        }
        else
        {
            string username = intent?.GetStringExtra("username") ?? string.Empty;
            _locationService.UpdateUsername(username);
            _locationService.Start(); // Ensure it is running.
        }

        return StartCommandResult.Sticky;
    }

    public override void OnTaskRemoved(Intent? rootIntent)
    {
        // If the app is removed from recent tasks, stop the service.
        System.Diagnostics.Debug.WriteLine("🗑️ App removed from recent tasks. Stopping service...");
        StopForegroundServiceOnly();
        base.OnTaskRemoved(rootIntent);
    }

    private Notification BuildNotification(string title, string text)
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

        PendingIntentFlags pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }

        var pendingIntent = PendingIntent.GetActivity(this, 0, intent, pendingIntentFlags);

        var builder = new NotificationCompat.Builder(this, ChannelId)!;
        builder.SetContentTitle(title);
        builder.SetContentText(text);
        builder.SetSmallIcon(global::Android.Resource.Drawable.SymDefAppIcon);
        builder.SetContentIntent(pendingIntent);
        builder.SetOngoing(true);
        builder.SetPriority(NotificationCompat.PriorityDefault);

        var stopIntent = new Intent(this, typeof(LocationForegroundService));
        stopIntent.SetAction(ActionStopService);
        var stopPendingIntent = PendingIntent.GetService(this, 1, stopIntent, pendingIntentFlags);
        builder.AddAction(global::Android.Resource.Drawable.IcMenuCloseClearCancel, "Cerrar", stopPendingIntent);

        return builder.Build()!;
    }

    private void StopForegroundServiceOnly()
    {
        if (_locationService != null)
        {
            _locationService.Stop();
        }

        if (_locationService != null && _isSubscribed && _locationHandler != null)
        {
            _locationService.LocationUpdated -= _locationHandler;
            _isSubscribed = false;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(24))
        {
            StopForeground(StopForegroundFlags.Remove);
        }
        else
        {
            StopForeground(true);
        }

        StopSelf();
    }

    public static void StopService(Context context)
    {
        if (_locationService != null && _isSubscribed && _locationHandler != null)
        {
            _locationService.LocationUpdated -= _locationHandler;
            _isSubscribed = false;
        }

        var intent = new Intent(context, typeof(LocationForegroundService));
        context.StopService(intent);
    }

    public override void OnDestroy()
    {
        if (_locationService != null && _isSubscribed && _locationHandler != null)
        {
            _locationService.LocationUpdated -= _locationHandler;
            _isSubscribed = false;
        }
        base.OnDestroy();
    }

    private void CreateNotificationChannel()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low)
            {
                LockscreenVisibility = NotificationVisibility.Public
            };
            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.CreateNotificationChannel(channel);
        }
    }

    private static string GetStatusTextStatic(string status) => status switch
    {
        "seguro" => "✅ SEGURO",
        "advertencia" => "⚠️ ADVERTENCIA",
        "peligro" => "❌ PELIGRO",
        "error" => "⚠️ ERROR",
        _ => "⏳ ESPERANDO..."
    };

    private static string BuildMessageTextStatic(LocationStatus result)
    {
        string message = result.Message;
        if (result.Status == "seguro") return "Se encuentra dentro del sendero";

        if (result.Status == "advertencia")
        {
            if (message.Contains("Fuera del parque"))
                return result.Park?.DistanceMeters.HasValue == true ? $"Fuera del parque ({result.Park.DistanceMeters.Value:F2} m)" : "Fuera del parque";
            if (message.Contains("Dentro de zona de investigación")) return "Dentro de zona de investigación";
            if (message.Contains("Dentro de pluma grúa")) return "Dentro de pluma grúa";

            return result.Trail?.DistanceMeters.HasValue == true ? $"Fuera del sendero ({result.Trail.DistanceMeters.Value:F2} m)" : "Fuera del sendero";
        }
        if (result.Status == "peligro")
        {
            return result.PowerLine?.DistanceMeters.HasValue == true ? $"Línea de alta tensión a {result.PowerLine.DistanceMeters.Value:F2} m" : "Cerca de línea de alta tensión";
        }
        return message;
    }
}