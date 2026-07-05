using System.Globalization;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

using EcoTrack.Models;
using EcoTrack.Services;
using EcoTrack.Helpers;
using CommunityToolkit.Mvvm.Messaging;

#if ANDROID
using EcoTrack.Platforms.Android;
#endif

namespace EcoTrack.Views;

public partial class MainPage : ContentPage
{
    // ============================================================
    //  CONSTANTS
    // ============================================================
    private const string ApiBaseUrl = "https://api-ecotrack-chb1.onrender.com";

    private static class PreferencesKeys
    {
        public const string DeviceId = "DeviceId";
        public const string IsAuthenticated = "IsAuthenticated";
        public const string Username = "Username";
        public const string UserRole = "UserRole";
        public const string UserId = "UserId";
        public const string UseSimulation = "UseSimulation";
        public const string SelectedProfileIndex = "SelectedProfileIndex";
    }

    // ============================================================
    //  PRIVATE FIELDS
    // ============================================================
    private readonly EcoTrackApiService _apiService;
    private readonly string _deviceId;

    private bool _isMapReady;
    private bool _isAuthenticated;
    private bool _isTrackingEnabled = true;
    private bool _isInitializing = true;

    private int _selectedProfileIndex;
    private IDispatcherTimer _userLocationsTimer = null!;

    private string _currentStatus = "idle";
    private string _currentTrailName = string.Empty;
    private double? _currentTrailDistance;
    private double? _currentPowerLineDistance;

    private double _lastLat = 8.9879;
    private double _lastLon = -79.5460;

    private string _currentUsername = string.Empty;
    private string _currentUserRole = string.Empty;

    // ============================================================
    //  SIMULATION ROUTES
    // ============================================================
    private static readonly IReadOnlyList<Location> PerfectWalkerRoute =
    [
        new(8.987911, -79.546003),
        new(8.987960, -79.545918),
        new(8.988122, -79.545792),
        new(8.988207, -79.545642),
        new(8.988382, -79.545473),
        new(8.988543, -79.545292),
        new(8.988677, -79.545179),
        new(8.98822, -79.545105)
    ];

    private static readonly IReadOnlyList<Location> ZigzagWalkerRoute =
    [
        new(8.986964, -79.547784),
        new(8.986911, -79.547942),
        new(8.986837, -79.547832),
        new(8.986635, -79.547772),
        new(8.986463, -79.547891)
    ];

    private static readonly IReadOnlyList<Location> PermanentlyLostRoute =
    [
        new(8.989535, -79.543150),
        new(8.989233, -79.542516),
        new(8.989037, -79.543370)
    ];

    private static readonly IReadOnlyList<Location> HighVoltageRoute =
    [
        new(8.990757, -79.540911),
        new(8.991041, -79.541085),
        new(8.991449, -79.541227),
        new(8.991884, -79.541257)
    ];

    private static readonly IReadOnlyList<Location> InsideResearchZoneRoute =
    [
        new(8.9945, -79.5430),
        new(8.9949, -79.5434),
        new(8.9940, -79.5425),
        new(8.9945, -79.5430)
    ];

    private static readonly IReadOnlyList<Location> OutsideParkRoute =
    [
        new(8.9700, -79.5400),
        new(8.9650, -79.5450),
        new(8.9700, -79.5500),
        new(8.9750, -79.5400)
    ];

    private readonly Dictionary<int, IReadOnlyList<Location>> _routeMap = new()
    {
        [0] = PerfectWalkerRoute,
        [1] = ZigzagWalkerRoute,
        [2] = PermanentlyLostRoute,
        [3] = HighVoltageRoute,
        [4] = InsideResearchZoneRoute,
        [5] = OutsideParkRoute
    };

    private IReadOnlyList<Location> _selectedRoute = PerfectWalkerRoute;

    // ============================================================
    //  CONSTRUCTOR
    // ============================================================
    public MainPage()
    {
        InitializeComponent();
        _apiService = new EcoTrackApiService();

        // Generate or retrieve persistent device ID.
        _deviceId = Preferences.Get(PreferencesKeys.DeviceId, string.Empty);
        if (string.IsNullOrEmpty(_deviceId))
        {
            _deviceId = Guid.NewGuid().ToString();
            Preferences.Set(PreferencesKeys.DeviceId, _deviceId);
        }

        // Register for location update messages from the foreground service.
        WeakReferenceMessenger.Default.Register<LocationUpdatedMessage>(this, (r, m) =>
        {
            OnLocationUpdated(m.Status);
        });

        // Start loading map asynchronously.
        _ = LoadMapAsync();

        // Full‑screen on Android.
        EnableFullScreen();

        // WebView navigation event to load layers when map is ready.
        EcoMapView.Navigated += OnMapNavigated;

        // Restore session from preferences.
        _isAuthenticated = Preferences.Get(PreferencesKeys.IsAuthenticated, false);
        if (_isAuthenticated)
        {
            _currentUsername = Preferences.Get(PreferencesKeys.Username, string.Empty);
            _currentUserRole = Preferences.Get(PreferencesKeys.UserRole, "user");
        }

        // Restore selected simulation profile.
        int savedIndex = Preferences.Get(PreferencesKeys.SelectedProfileIndex, 0);
        _selectedProfileIndex = Math.Clamp(savedIndex, 0, 5);
        SelectProfile(_selectedProfileIndex);
        UpdateSelectedRoute();

        // Initial UI setup.
        UpdateAuthenticationUI();
        ConfigureGpsSimulation();
        SetNotificationIdle();
        UpdateTrackingSwitches(_isTrackingEnabled);

        // Start fetching other users' locations if admin/guard.
        if (_isAuthenticated && (_currentUserRole == "admin" || _currentUserRole == "guard"))
        {
            StartUserLocationsTimer();
        }
    }

    // ============================================================
    //  LIFECYCLE
    // ============================================================
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Suppress event triggers while setting initial state.
        _isInitializing = true;

        // Temporarily disable switches to avoid event cascade.
        if (TrackingSwitch != null)
        {
            TrackingSwitch.IsToggled = false;
        }

        if (TrackingSwitchLogin != null)
        {
            TrackingSwitchLogin.IsToggled = false;
        }

        // Ask for notification and location permissions.
        var notifyStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (notifyStatus != PermissionStatus.Granted)
        {
            notifyStatus = await Permissions.RequestAsync<Permissions.PostNotifications>();
        }

        var locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (locationStatus != PermissionStatus.Granted)
        {
            locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        // Start tracking only if both permissions are granted.
        if (locationStatus == PermissionStatus.Granted && notifyStatus == PermissionStatus.Granted)
        {
            System.Diagnostics.Debug.WriteLine("🎯 All required permissions granted successfully.");

            if (TrackingSwitch != null)
            {
                TrackingSwitch.IsToggled = true;
            }

            if (TrackingSwitchLogin != null)
            {
                TrackingSwitchLogin.IsToggled = true;
            }

            StartLocationService();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ Permissions denied or incomplete.");
            await DisplayAlertAsync(
                "Permissions Required",
                "EcoTrack needs access to your location and notifications to safely track trails in the background.",
                "Got it"
            );
        }

        // Allow user interactions now.
        _isInitializing = false;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Optionally unregister messenger if needed (but not required).
    }

    // ============================================================
    //  PROFILE SELECTION
    // ============================================================
    private void SelectProfile(int index)
    {
        ProfileOption1.IsChecked = false;
        ProfileOption2.IsChecked = false;
        ProfileOption3.IsChecked = false;
        ProfileOption4.IsChecked = false;
        ProfileOption5.IsChecked = false;
        ProfileOption6.IsChecked = false;

        switch (index)
        {
            case 0:
                {
                    ProfileOption1.IsChecked = true;
                    break;
                }
            case 1:
                {
                    ProfileOption2.IsChecked = true;
                    break;
                }
            case 2:
                {
                    ProfileOption3.IsChecked = true;
                    break;
                }
            case 3:
                {
                    ProfileOption4.IsChecked = true;
                    break;
                }
            case 4:
                {
                    ProfileOption5.IsChecked = true;
                    break;
                }
            case 5:
                {
                    ProfileOption6.IsChecked = true;
                    break;
                }
            default:
                {
                    ProfileOption1.IsChecked = true;
                    break;
                }
        }

        UpdateProfileButtonText(index);
    }

    private void UpdateProfileButtonText(int index)
    {
        string[] names =
        [
            "Perfect Trail Walker",
            "Zigzag In/Out Tracker",
            "Permanently Lost / Off-Trail",
            "High Voltage Risk Walker",
            "Within the research zone",
            "Outside the park"
        ];

        if (index >= 0 && index < names.Length)
        {
            ProfileMenuButton.Text = names[index];
        }
        else
        {
            ProfileMenuButton.Text = names[0];
        }
    }

    private void UpdateSelectedRoute()
    {
        if (_routeMap.TryGetValue(_selectedProfileIndex, out IReadOnlyList<Location>? route))
        {
            _selectedRoute = route;
        }
        else
        {
            _selectedRoute = PerfectWalkerRoute;
        }

        // Update the service simulation mode if on Android.
#if ANDROID
        bool useSimulation = Preferences.Get(PreferencesKeys.UseSimulation, false);
        if (useSimulation && _currentUserRole == "admin")
        {
            LocationForegroundService.SetSimulationMode(true, _selectedRoute);
        }
        else
        {
            LocationForegroundService.SetSimulationMode(false);
        }
#endif
    }

    // ============================================================
    //  MAP LOADING
    // ============================================================
    private async Task LoadMapAsync()
    {
        try
        {
            using Stream stream = await FileSystem.OpenAppPackageFileAsync("map.html");
            using StreamReader reader = new(stream);
            string htmlContent = await reader.ReadToEndAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                EcoMapView.Source = new HtmlWebViewSource
                {
                    Html = htmlContent
                };
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo cargar el mapa: {ex.Message}", "OK");
        }
    }

    private async void OnMapNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (_isMapReady)
        {
            return;
        }

        try
        {
            _isMapReady = true;
            await LoadAllLayersAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading layers: {ex.Message}");
        }
    }

    private async Task LoadAllLayersAsync()
    {
        var layers = new Dictionary<string, string>
        {
            ["senderos"] = "senderos.geojson",
            ["lineas_tension"] = "lineas_alta_tension.geojson",
            ["limites"] = "limites_pnm.geojson",
            ["parcela"] = "parcela_1ha_pnm.geojson",
            ["pluma"] = "pluma_grua_pnm.geojson",
            ["pois"] = "puntos_de_interes.geojson"
        };

        foreach (KeyValuePair<string, string> layer in layers)
        {
            await LoadGeoJsonLayerAsync(layer.Key, layer.Value);
        }
    }

    private async Task LoadGeoJsonLayerAsync(string layerName, string fileName)
    {
        try
        {
            using Stream stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            using StreamReader reader = new(stream);
            string geojson = await reader.ReadToEndAsync();

            // Escape JSON to embed in JavaScript.
            string escaped = geojson
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");

            await EcoMapView.EvaluateJavaScriptAsync($"loadLayer('{layerName}', '{escaped}');");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading {layerName}: {ex.Message}");
        }
    }

    private static void EnableFullScreen()
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        if (activity == null)
        {
            return;
        }

        var window = activity.Window;
        if (window == null)
        {
            return;
        }

        window.SetFlags
        (
            Android.Views.WindowManagerFlags.Fullscreen,
            Android.Views.WindowManagerFlags.Fullscreen
        );
#endif
    }

    // ============================================================
    //  NOTIFICATION UI
    // ============================================================
    private void SetNotificationIdle()
    {
        NotificationIcon.Text = "⏳";
        NotificationStatus.Text = "ESPERANDO...";

        NotificationMessage.FormattedText = new FormattedString
        {
            Spans =
            {
                new Span
                {
                    Text = "Esperando ubicación...",
                    TextColor = Colors.Gray
                }
            }
        };

        NotificationTrail.Text = "Sendero: --";
        NotificationTrailDistance.Text = "Distancia al sendero: -- m";
        NotificationBar.Stroke = Color.FromArgb("#D1D5DB");

        _currentStatus = "idle";
        _currentTrailName = string.Empty;
        _currentTrailDistance = null;
        _currentPowerLineDistance = null;
    }

    private void UpdateNotification(
        string status,
        string message,
        string trailName,
        double? trailDistance,
        double? powerLineDistance = null,
        double? parkDistance = null)
    {
        // Avoid redundant updates.
        bool isSameStatus = status == _currentStatus;
        bool isSameTrail = trailName == _currentTrailName;
        bool isSameTrailDist = Math.Abs((trailDistance ?? 0) - (_currentTrailDistance ?? 0)) < 0.01;
        bool isSamePowerDist = Math.Abs((powerLineDistance ?? 0) - (_currentPowerLineDistance ?? 0)) < 0.01;

        if (isSameStatus && isSameTrail && isSameTrailDist && isSamePowerDist)
        {
            return;
        }

        _currentStatus = status;
        _currentTrailName = trailName;
        _currentTrailDistance = trailDistance;
        _currentPowerLineDistance = powerLineDistance;

        (string icon, string borderHex, string statusText) = GetNotificationStyle(status);

        string displayTrailLabel = "Sendero: --";
        string displayDistance = "Distancia al sendero: -- m";
        bool showTrailInfo = true;

        // Customize text for "outside park" or "research zone" warnings.
        if (status == "advertencia" && message.Contains("Fuera del parque"))
        {
            displayTrailLabel = "Parque";
            displayDistance = parkDistance.HasValue
                ? $"Distancia al parque: {parkDistance.Value:F2} m"
                : "Distancia al parque: --";
        }
        else if (status == "advertencia" && message.Contains("Dentro de zona de investigación"))
        {
            showTrailInfo = false;
        }
        else
        {
            displayTrailLabel = string.IsNullOrEmpty(trailName)
                ? "Sendero: --"
                : $"Sendero: {trailName}";

            displayDistance = trailDistance.HasValue
                ? $"Distancia al sendero: {trailDistance.Value:F2} m"
                : "Distancia al sendero: -- m";
        }

        string displayMessage = (status == "advertencia" && message.Contains("Fuera del parque"))
            ? "Fuera del parque"
            : message;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            NotificationIcon.Text = icon;
            NotificationStatus.Text = statusText;

            NotificationTrail.IsVisible = showTrailInfo;
            NotificationTrailDistance.IsVisible = showTrailInfo;

            if (showTrailInfo)
            {
                NotificationTrail.Text = displayTrailLabel;
                NotificationTrailDistance.Text = displayDistance;
            }

            NotificationBar.Stroke = Color.FromArgb(borderHex);
            NotificationMessage.FormattedText = BuildNotificationMessage(status, powerLineDistance, displayMessage);
        });
    }

    private static (string icon, string borderHex, string statusText) GetNotificationStyle(string status)
    {
        switch (status)
        {
            case "seguro":
                {
                    return ("✅", "#22C55E", "SEGURO");
                }
            case "advertencia":
                {
                    return ("⚠️", "#F59E0B", "ADVERTENCIA");
                }
            case "peligro":
                {
                    return ("❌", "#EF4444", "PELIGRO");
                }
            case "error":
                {
                    return ("⚠️", "#EF4444", "ERROR");
                }
            default:
                {
                    return ("⏳", "#D1D5DB", "ESPERANDO...");
                }
        }
    }

    private static FormattedString BuildNotificationMessage(string status, double? powerLineDistance, string fallbackMessage)
    {
        var formatted = new FormattedString();

        switch (status)
        {
            case "peligro":
                {
                    formatted.Spans.Add(
                        new Span
                        {
                            Text = "Cerca de línea de alta tensión",
                            TextColor = Colors.Black
                        }
                    );

                    if (powerLineDistance.HasValue)
                    {
                        formatted.Spans.Add(
                            new Span
                            {
                                Text = $" ({powerLineDistance.Value:F2} m)",
                                TextColor = Colors.Red,
                                FontAttributes = FontAttributes.Bold
                            }
                        );
                    }
                    else
                    {
                        formatted.Spans.Add(
                            new Span
                            {
                                Text = " (distancia desconocida)",
                                TextColor = Colors.Red
                            }
                        );
                    }
                    break;
                }
            case "seguro":
                {
                    formatted.Spans.Add(
                        new Span
                        {
                            Text = "Se encuentra dentro del sendero",
                            TextColor = Colors.Black
                        }
                    );
                    break;
                }
            case "error":
                {
                    formatted.Spans.Add(
                        new Span
                        {
                            Text = "Error de conexión con el servidor",
                            TextColor = Colors.Red
                        }
                    );
                    break;
                }
            default:
                {
                    formatted.Spans.Add(
                        new Span
                        {
                            Text = fallbackMessage,
                            TextColor = Colors.Black
                        }
                    );
                    break;
                }
        }

        return formatted;
    }

    // ============================================================
    //  LOCATION SERVICE CONTROL
    // ============================================================
    // Starts the location service (foreground service on Android).
    private void StartLocationService()
    {
#if ANDROID
        // Set the simulation mode before starting the service.
        bool useSimulation = Preferences.Get(PreferencesKeys.UseSimulation, false);
        if (useSimulation && _currentUserRole == "admin")
        {
            LocationForegroundService.SetSimulationMode(true, _selectedRoute);
        }
        else
        {
            LocationForegroundService.SetSimulationMode(false);
        }

        // Start the foreground service.
        LocationServiceHelper.StartForegroundService(_deviceId, _currentUsername);
#endif
    }

    // Stops the location service (foreground service on Android).
    private void StopLocationService()
    {
#if ANDROID
        // Stop the foreground service (this also stops its internal LocationService).
        LocationForegroundService.StopService(Android.App.Application.Context);
#endif
    }

    // Handles location updates (called via messenger).
    private void OnLocationUpdated(LocationStatus result)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateNotification(
                result.Status,
                result.Message,
                result.Trail?.Name ?? string.Empty,
                result.Trail?.DistanceMeters,
                result.PowerLine?.DistanceMeters,
                result.Park?.DistanceMeters
            );

            _ = UpdateMapCircleColor(result.Status);
            _ = UpdateMapLocation(result.Position.Latitude, result.Position.Longitude);
        });
    }

    // ============================================================
    //  MAP UPDATES
    // ============================================================
    private async Task UpdateMapLocation(double lat, double lon)
    {
        if (!_isMapReady)
        {
            return;
        }

        string latText = lat.ToString(CultureInfo.InvariantCulture);
        string lonText = lon.ToString(CultureInfo.InvariantCulture);

        await EcoMapView.EvaluateJavaScriptAsync($"updatePosition({latText}, {lonText})");

        if (_isTrackingEnabled)
        {
            await EcoMapView.EvaluateJavaScriptAsync($"setMapView({latText}, {lonText})");
        }

        _lastLat = lat;
        _lastLon = lon;
    }

    private async Task UpdateMapCircleColor(string status)
    {
        if (!_isMapReady)
        {
            return;
        }

        string color;

        switch (status)
        {
            case "seguro":
                {
                    color = "#22C55E";
                    break;
                }
            case "advertencia":
                {
                    color = "#F59E0B";
                    break;
                }
            case "peligro":
                {
                    color = "#EF4444";
                    break;
                }
            default:
                {
                    color = "#808080";
                    break;
                }
        }

        await EcoMapView.EvaluateJavaScriptAsync($"updateCircleColor('{color}')");
    }

    private async Task CenterMapAtLastPosition()
    {
        if (!_isMapReady)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500);

                if (_isMapReady)
                {
                    break;
                }
            }

            if (!_isMapReady)
            {
                return;
            }
        }

        string latText = _lastLat.ToString(CultureInfo.InvariantCulture);
        string lonText = _lastLon.ToString(CultureInfo.InvariantCulture);
        await EcoMapView.EvaluateJavaScriptAsync($"setMapView({latText}, {lonText})");
    }

    // ============================================================
    //  TRACKING SWITCH
    // ============================================================
    private async void OnTrackingToggled(object sender, ToggledEventArgs e)
    {
        // Ignore automatic changes during init.
        if (_isInitializing)
        {
            return;
        }

        if (e.Value) // Enable tracking: start service and center map.
        {
            UpdateTrackingSwitches(true);

            // Verify permissions are still granted.
            var locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            var notifyStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();

            if (locationStatus == PermissionStatus.Granted && notifyStatus == PermissionStatus.Granted)
            {
                StartLocationService();
                await CenterMapAtLastPosition();
            }
            else
            {
                // Revert switch to avoid inconsistent state.
                if (sender is Switch toggleSwitch)
                {
                    _isInitializing = true;
                    toggleSwitch.IsToggled = false;
                    _isInitializing = false;
                }

                await DisplayAlertAsync(
                    "Permisos Faltantes",
                    "No se puede activar el seguimiento sin permisos de ubicación y notificaciones.",
                    "OK"
                );
            }
        }
        else // Disable auto‑centering but keep location sending active.
        {
            UpdateTrackingSwitches(false);
        }
    }

    private void UpdateTrackingSwitches(bool value)
    {
        _isTrackingEnabled = value;

        if (TrackingSwitch != null)
        {
            TrackingSwitch.IsToggled = value;
        }

        if (TrackingSwitchLogin != null)
        {
            TrackingSwitchLogin.IsToggled = value;
        }
    }

    // ============================================================
    //  MAIN MENU
    // ============================================================
    private async void OnMenuButtonClicked(object sender, EventArgs e)
    {
        if (LoginOverlay.IsVisible)
        {
            await CloseLoginAsync();
            return;
        }

        if (MenuOverlay.IsVisible)
        {
            await CloseMenuAsync();
            return;
        }

        if (_isAuthenticated)
        {
            await OpenLoginMenuAsync();
        }
        else
        {
            await OpenMenuAsync();
        }
    }

    private async Task OpenMenuAsync()
    {
        MenuOverlay.IsVisible = true;
        MenuPanel.Opacity = 0;

        await MenuOverlay.FadeToAsync(1, 150);
        await MenuPanel.FadeToAsync(1, 150);

        UpdateTrackingSwitches(_isTrackingEnabled);
    }

    private async void OnCloseMenuClicked(object sender, EventArgs e)
    {
        await CloseMenuAsync();
    }

    private async void OnCloseMenuTapped(object sender, TappedEventArgs e)
    {
        await CloseMenuAsync();
    }

    private async Task CloseMenuAsync()
    {
        if (!MenuOverlay.IsVisible)
        {
            return;
        }

        await MenuPanel.FadeToAsync(0, 150);
        await MenuOverlay.FadeToAsync(0, 150);
        MenuOverlay.IsVisible = false;
    }

    private async void OnLoginButtonClicked(object sender, EventArgs e)
    {
        await CloseMenuAsync();
        await OpenLoginMenuAsync();
    }

    // ============================================================
    //  LOGIN MENU
    // ============================================================
    private async Task OpenLoginMenuAsync()
    {
        UpdateAuthenticationUI();

        if (!_isAuthenticated)
        {
            ClearLoginFields();
        }

        LoginOverlay.IsVisible = true;
        LoginPanel.Opacity = 0;

        await LoginOverlay.FadeToAsync(1, 150);
        await LoginPanel.FadeToAsync(1, 150);

        UpdateTrackingSwitches(_isTrackingEnabled);
    }

    private async void OnCloseLoginClicked(object sender, EventArgs e)
    {
        await CloseLoginAsync();
    }

    private async void OnCloseLoginTapped(object sender, TappedEventArgs e)
    {
        await CloseLoginAsync();
    }

    private async Task CloseLoginAsync()
    {
        if (!LoginOverlay.IsVisible)
        {
            return;
        }

        if (!_isAuthenticated)
        {
            ClearLoginFields();
        }

        await LoginPanel.FadeToAsync(0, 150);
        await LoginOverlay.FadeToAsync(0, 150);
        LoginOverlay.IsVisible = false;
    }

    // ============================================================
    //  AUTHENTICATION
    // ============================================================
    private void UpdateAuthenticationUI()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            bool isAuth = _isAuthenticated;
            bool isAdmin = _currentUserRole == "admin";
            bool isGuard = _currentUserRole == "guard";

            LoginSection.IsVisible = !isAuth;
            AuthenticatedSection.IsVisible = isAuth;

            if (isAuth)
            {
                UserRoleLabel.Text = $"Rol: {_currentUserRole}";
            }
            else
            {
                UserRoleLabel.Text = "Rol: not assigned";
            }

            if (GpsModeSwitch != null)
            {
                GpsModeSwitch.IsEnabled = isAuth && (isAdmin || isGuard);
                if (!GpsModeSwitch.IsEnabled)
                {
                    GpsModeSwitch.IsToggled = false;
                }
            }

            if (SimulationControls != null)
            {
                bool useSimulation = Preferences.Get(PreferencesKeys.UseSimulation, false);
                SimulationControls.IsVisible = isAuth && useSimulation && isAdmin;
            }
        });
    }

    private async void OnLoginSubmitClicked(object sender, EventArgs e)
    {
        string username = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await ShowErrorDialogAsync("Por favor, completa todos los campos.");
            return;
        }

        try
        {
            LoginResponse? result = await _apiService.LoginAsync(
                username,
                SecurityHelper.ComputeSha256Hash(password)
            );

            if (result != null && result.Success)
            {
                await HandleSuccessfulLogin(
                    username,
                    result.User?.Role ?? "user",
                    result.User?.Id ?? 0
                );
            }
            else
            {
                await ShowErrorDialogAsync(result?.Message ?? "Error de autenticación");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync($"Error de conexión: {ex.Message}");
        }
    }

    private async Task HandleSuccessfulLogin(string username, string role, int userId)
    {
        _isAuthenticated = true;
        _currentUsername = username;
        _currentUserRole = role;

        Preferences.Set(PreferencesKeys.IsAuthenticated, true);
        Preferences.Set(PreferencesKeys.UserRole, role);
        Preferences.Set(PreferencesKeys.Username, username);
        Preferences.Set(PreferencesKeys.UserId, userId);

        WeakReferenceMessenger.Default.Send(new UserChangedMessage
        {
            NewUsername = username,
            NewRole = role
        });

        UpdateAuthenticationUI();
        ConfigureGpsSimulation();
        ClearLoginFields();

        await CloseLoginAsync();

        await ShowWelcomeDialogAsync($"Has iniciado sesión como {username} (rol: {role})");

        if (role == "admin" || role == "guard")
        {
            StartUserLocationsTimer();
        }

        SetNotificationIdle();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        _isAuthenticated = false;
        _currentUsername = string.Empty;
        _currentUserRole = string.Empty;

        Preferences.Set(PreferencesKeys.IsAuthenticated, false);
        Preferences.Set(PreferencesKeys.UserRole, string.Empty);
        Preferences.Set(PreferencesKeys.Username, string.Empty);
        Preferences.Set(PreferencesKeys.UserId, string.Empty);
        Preferences.Set(PreferencesKeys.UseSimulation, false);

        WeakReferenceMessenger.Default.Send(new UserChangedMessage
        {
            NewUsername = string.Empty,
            NewRole = string.Empty
        });

        _userLocationsTimer?.Stop();

        if (_isMapReady)
        {
            await EcoMapView.EvaluateJavaScriptAsync("userLayerGroup.clearLayers()");
        }

#if ANDROID
        LocationForegroundService.SetSimulationMode(false);
#endif

        UpdateAuthenticationUI();
        ConfigureGpsSimulation();
        ClearLoginFields();

        await CloseLoginAsync();
        await OpenMenuAsync();

        SetNotificationIdle();
        await UpdateMapCircleColor("desconocido");
    }

    private void ConfigureGpsSimulation()
    {
        bool isAdmin = _currentUserRole == "admin";
        bool isGuard = _currentUserRole == "guard";
        bool isAuth = _isAuthenticated;

        GpsModeSwitch.IsEnabled = isAuth && (isAdmin || isGuard);

        if (!isAuth || !(isAdmin || isGuard))
        {
            GpsModeSwitch.IsToggled = false;
            SimulationControls.IsVisible = false;
            return;
        }

        bool useSimulation = Preferences.Get(PreferencesKeys.UseSimulation, false);
        GpsModeSwitch.IsToggled = useSimulation;
        SimulationControls.IsVisible = useSimulation && isAdmin;

        // Update the service simulation mode if on Android.
#if ANDROID
        if (useSimulation && isAdmin)
        {
            LocationForegroundService.SetSimulationMode(true, _selectedRoute);
        }
        else
        {
            LocationForegroundService.SetSimulationMode(false);
        }
#endif
    }

    private void ClearLoginFields()
    {
        EmailEntry.Text = string.Empty;
        PasswordEntry.Text = string.Empty;
    }

    // ============================================================
    //  SIMULATION CONTROLS
    // ============================================================
    private void OnGpsModeToggled(object sender, ToggledEventArgs e)
    {
        if (!_isAuthenticated || _currentUserRole != "admin")
        {
            GpsModeSwitch.IsToggled = false;
            return;
        }

        Preferences.Set(PreferencesKeys.UseSimulation, e.Value);
        SimulationControls.IsVisible = e.Value;

#if ANDROID
        if (e.Value)
        {
            LocationForegroundService.SetSimulationMode(true, _selectedRoute);
        }
        else
        {
            LocationForegroundService.SetSimulationMode(false);
        }
#endif
    }

    private async void OnProfileMenuButtonClicked(object sender, EventArgs e)
    {
        if (_currentUserRole == "admin")
        {
            await OpenProfileMenuAsync();
        }
    }

    private async Task OpenProfileMenuAsync()
    {
        ProfileMenuOverlay.IsVisible = true;
        ProfileMenuPanel.Opacity = 0;

        await ProfileMenuOverlay.FadeToAsync(1, 150);
        await ProfileMenuPanel.FadeToAsync(1, 150);
    }

    private async void OnCloseProfileMenuTapped(object sender, TappedEventArgs e)
    {
        await CloseProfileMenuAsync();
    }

    private async void OnCloseProfileMenuClicked(object sender, EventArgs e)
    {
        await CloseProfileMenuAsync();
    }

    private async Task CloseProfileMenuAsync()
    {
        if (!ProfileMenuOverlay.IsVisible)
        {
            return;
        }

        await ProfileMenuPanel.FadeToAsync(0, 150);
        await ProfileMenuOverlay.FadeToAsync(0, 150);
        ProfileMenuOverlay.IsVisible = false;
    }

    private void OnProfileOptionCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value || sender is not RadioButton selected)
        {
            return;
        }

        int index = -1;

        if (selected == ProfileOption1)
        {
            index = 0;
        }
        else if (selected == ProfileOption2)
        {
            index = 1;
        }
        else if (selected == ProfileOption3)
        {
            index = 2;
        }
        else if (selected == ProfileOption4)
        {
            index = 3;
        }
        else if (selected == ProfileOption5)
        {
            index = 4;
        }
        else if (selected == ProfileOption6)
        {
            index = 5;
        }

        if (index >= 0)
        {
            _selectedProfileIndex = index;
            Preferences.Set(PreferencesKeys.SelectedProfileIndex, index);

            UpdateProfileButtonText(index);
            UpdateSelectedRoute();

            // Refresh service if simulation is active.
#if ANDROID
            bool useSimulation = Preferences.Get(PreferencesKeys.UseSimulation, false);
            if (useSimulation && (_currentUserRole == "admin" || _currentUserRole == "guard"))
            {
                LocationForegroundService.SetSimulationMode(true, _selectedRoute);
            }
            else
            {
                LocationForegroundService.SetSimulationMode(false);
            }
#endif

            _ = CloseProfileMenuAsync();
        }
    }

    // ============================================================
    //  DIALOGS
    // ============================================================
    private async Task ShowErrorDialogAsync(string message)
    {
        ErrorMessageLabel.Text = message;
        ErrorDialog.IsVisible = true;
        ErrorDialog.Opacity = 0;

        await ErrorDialog.FadeToAsync(1, 150);
    }

    private async void OnErrorDialogAcceptClicked(object sender, EventArgs e)
    {
        await CloseErrorDialogAsync();
    }

    private async void OnErrorDialogBackgroundTapped(object sender, TappedEventArgs e)
    {
        await CloseErrorDialogAsync();
    }

    private async Task CloseErrorDialogAsync()
    {
        if (!ErrorDialog.IsVisible)
        {
            return;
        }

        await ErrorDialog.FadeToAsync(0, 150);
        ErrorDialog.IsVisible = false;
    }

    private async Task ShowWelcomeDialogAsync(string message)
    {
        WelcomeMessageLabel.Text = message;
        WelcomeDialog.IsVisible = true;
        WelcomeDialog.Opacity = 0;

        await WelcomeDialog.FadeToAsync(1, 150);
    }

    private async void OnWelcomeDialogAcceptClicked(object sender, EventArgs e)
    {
        await CloseWelcomeDialogAsync();
    }

    private async void OnWelcomeDialogBackgroundTapped(object sender, TappedEventArgs e)
    {
        await CloseWelcomeDialogAsync();
    }

    private async Task CloseWelcomeDialogAsync()
    {
        if (!WelcomeDialog.IsVisible)
        {
            return;
        }

        await WelcomeDialog.FadeToAsync(0, 150);
        WelcomeDialog.IsVisible = false;
    }

    // ============================================================
    //  USER LOCATIONS (ADMIN/GUARD)
    // ============================================================
    private void StartUserLocationsTimer()
    {
        _userLocationsTimer?.Stop();
        _userLocationsTimer = Dispatcher.CreateTimer();
        _userLocationsTimer.Interval = TimeSpan.FromSeconds(2);

        _userLocationsTimer.Tick += async (s, args) =>
        {
            await FetchAllUserLocations();
        };

        _userLocationsTimer.Start();

        _ = FetchAllUserLocations();
    }

    private async Task FetchAllUserLocations()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Username", _currentUsername);
            client.DefaultRequestHeaders.Add("X-DeviceId", _deviceId);

            HttpResponseMessage response = await client.GetAsync($"{ApiBaseUrl}/users/locations");

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                string json = await response.Content.ReadAsStringAsync();
                UserLocationsResponse? result = JsonSerializer.Deserialize<UserLocationsResponse>(json, options);
                List<UserLocation> users = result?.Users ?? [];

                await DrawUserLocations(users);
            }
            else
            {
                await DrawUserLocations([]);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error fetching user locations: {ex.Message}");
            await DrawUserLocations([]);
        }
    }

    private async Task DrawUserLocations(List<UserLocation> users)
    {
        if (!_isMapReady)
        {
            return;
        }

        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        string json = JsonSerializer.Serialize(users, options);
        string escapedJson = json.Replace("'", "\\'").Replace("\"", "\\\"");
        await EcoMapView.EvaluateJavaScriptAsync($"loadUserLocations('{escapedJson}')");
    }
}