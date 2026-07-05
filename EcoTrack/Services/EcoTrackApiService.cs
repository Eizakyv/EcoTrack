using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EcoTrack.Models;

namespace EcoTrack.Services
{
    public class EcoTrackApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api-ecotrack-chb1.onrender.com";

        public EcoTrackApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add
            (
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
            );
        }

        // Sends a location check request without device or user identification.
        // Returns the status response or null if deserialization fails.
        public async Task<LocationStatus?> CheckLocationAsync(double latitude, double longitude)
        {
            try
            {
                var data = new { latitude, longitude };
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/check", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<LocationStatus>(responseJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en EcoTrackApiService: {ex.Message}");
                throw;
            }
        }

        // Authenticates a user with username and SHA‑256 password hash.
        // Returns the login response (success flag, user data, etc.) or null if deserialization fails.
        public async Task<LoginResponse?> LoginAsync(string username, string passwordHash)
        {
            try
            {
                var data = new { username, password_hash = passwordHash };
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/login", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                // Even if the status code is not success, the server may return a structured error.
                // Deserialize and return the response (which may be null).
                return JsonSerializer.Deserialize<LoginResponse>(responseJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en LoginAsync: {ex.Message}");
                throw;
            }
        }

        // Sends a location check with device and user identification.
        // The username may be empty or null for unauthenticated devices.
        // Returns the status response or null if deserialization fails.
        public async Task<LocationStatus?> CheckLocationAsyncWithDevice(double latitude, double longitude, string deviceId, string username)
        {
            try
            {
                var data = new
                {
                    latitude,
                    longitude,
                    device_id = deviceId,
                    username
                };
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/check", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<LocationStatus>(responseJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en EcoTrackApiService: {ex.Message}");
                throw;
            }
        }
    }
}