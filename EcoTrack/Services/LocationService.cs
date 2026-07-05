using EcoTrack.Models;
using CommunityToolkit.Mvvm.Messaging;

namespace EcoTrack.Services;

public class LocationService : IDisposable
{
    private readonly EcoTrackApiService _apiService = new();
    private readonly string _deviceId;
    private string _username;
    private System.Timers.Timer? _timer;
    private bool _isRunning;

    private bool _simulationEnabled;
    private IReadOnlyList<Location>? _simulationRoute;
    private int _simulationIndex;

    public event EventHandler<LocationStatus>? LocationUpdated;

    public LocationService(string deviceId, string username)
    {
        _deviceId = deviceId;
        _username = username;

        WeakReferenceMessenger.Default.Register<UserChangedMessage>(this, (recipient, message) =>
        {
            _username = message.NewUsername;
            System.Diagnostics.Debug.WriteLine($"📍 [LocationService] Identidad actualizada reactivamente a: '{_username}'");
        });
    }

    public void UpdateUsername(string username) => _username = username;

    public void SetSimulationMode(bool enabled, IReadOnlyList<Location>? route = null)
    {
        _simulationEnabled = enabled;
        _simulationRoute = route;
        _simulationIndex = 0;
    }

    public void Start()
    {
        if (_isRunning) return;

        _timer = new System.Timers.Timer(1500);
        _timer.Elapsed += async (s, e) => await SendLocationAsync();
        _timer.AutoReset = true;
        _timer.Start();
        _isRunning = true;

        System.Diagnostics.Debug.WriteLine("📍 LocationService iniciado.");
    }

    public void Stop()
    {
        if (!_isRunning) return;

        if (_timer != null)
        {
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }
        _isRunning = false;

        System.Diagnostics.Debug.WriteLine("📍 LocationService detenido.");
    }

    private async Task SendLocationAsync()
    {
        try
        {
            double lat;
            double lon;

            if (_simulationEnabled && _simulationRoute != null && _simulationRoute.Count > 0)
            {
                var loc = _simulationRoute[_simulationIndex % _simulationRoute.Count];
                lat = loc.Latitude;
                lon = loc.Longitude;
                _simulationIndex++;
            }
            else
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5))
                {
                    RequestFullAccuracy = true
                };

                var location = await Geolocation.Default.GetLocationAsync(request);
                if (location == null) return;

                lat = location.Latitude;
                lon = location.Longitude;
            }

            var result = await _apiService.CheckLocationAsyncWithDevice(lat, lon, _deviceId, _username);

            if (result != null)
            {
                LocationUpdated?.Invoke(this, result);
                System.Diagnostics.Debug.WriteLine($"📍 Enviado: {lat}, {lon} con Usuario: '{_username}' → {result.Status}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en LocationService: {ex.Message}");
        }
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<UserChangedMessage>(this);
        Stop();
    }
}