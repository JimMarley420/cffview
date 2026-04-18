using System.Net.Http;
using System.Text.Json;
using cffview.Models;
using cffview.Models.DTOs;
using Serilog;

namespace cffview.Services;

public interface ITransportApiService
{
    Task<ApiResponse<List<Departure>>> GetDeparturesAsync(string stationId, int limit = 3);
    Task<ApiResponse<List<Stop>>> SearchStationsAsync(string query);
    Task<bool> CheckConnectivityAsync();
}

public class TransportApiService : ITransportApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://transport.opendata.ch/v1";
    private readonly ILogger _logger = Log.ForContext<TransportApiService>();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public TransportApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CFFView/1.0");
    }

    public async Task<bool> CheckConnectivityAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/locations?query=test&limit=1");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ApiResponse<List<Stop>>> SearchStationsAsync(string query)
    {
        try
        {
            var url = $"{_baseUrl}/locations?query={Uri.EscapeDataString(query)}&limit=10";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse<List<Stop>>
                {
                    Success = false,
                    ErrorMessage = $"API error: {response.StatusCode}"
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LocationSearchResponse>(json, JsonOptions);

            var locations = result?.Stations ?? result?.Locations ?? new List<LocationDto>();

            var stops = locations
                .Where(l => !string.IsNullOrEmpty(l.Name))
                .Select(l => new Stop
                {
                    Id = l.Id ?? "",
                    Name = l.Name ?? "",
                    Description = l.Icon ?? "station",
                    Latitude = l.Coordinate?.Y ?? 0,
                    Longitude = l.Coordinate?.X ?? 0
                })
                .ToList();

            _logger.Information("Found {Count} stations for query '{Query}'", stops.Count, query);
            return new ApiResponse<List<Stop>> { Success = true, Data = stops };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Search stations error for '{Query}'", query);
            return new ApiResponse<List<Stop>>
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ApiResponse<List<Departure>>> GetDeparturesAsync(string stationId, int limit = 3)
    {
        try
        {
            var url = $"{_baseUrl}/stationboard?station={Uri.EscapeDataString(stationId)}&limit={limit}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning("Stationboard failed: {StatusCode}", response.StatusCode);
                return new ApiResponse<List<Departure>>
                {
                    Success = false,
                    ErrorMessage = $"API error: {response.StatusCode}"
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StationBoardResponse>(json, JsonOptions);

            if (result?.Stationboard == null || !result.Stationboard.Any())
            {
                return new ApiResponse<List<Departure>> { Success = true, Data = new List<Departure>() };
            }

            var departures = result.Stationboard
                .GroupBy(s => s.DepartureTimestamp)
                .Select(g => g.First())
                .Take(limit)
                .Select(s => new Departure
            {
                Id = s.Number ?? s.Name ?? Guid.NewGuid().ToString(),
                StopId = stationId,
                StopName = s.Stop?.Station?.Name ?? result.Station?.Name ?? "",
                Line = new Line
                {
                    ShortName = s.Number ?? "",
                    LongName = s.Name ?? "",
                    Color = GetCategoryColor(s.Category)
                },
                ScheduledTime = ParseDateTime(s.Departure),
                RealTime = s.DepartureTimestamp > 0 ? ParseTimestamp(s.DepartureTimestamp) : null,
                DelayMinutes = s.Delay ?? 0,
                Destination = s.To ?? "",
                Platform = s.Platform ?? "",
                Operator = s.Operator ?? "",
                Status = s.Delay > 0 ? DepartureStatus.Delayed :
                         s.Category is "IC" or "EC" or "IR" or "RE" ? DepartureStatus.RealTime :
                         DepartureStatus.Scheduled
            }).ToList();

            _logger.Information("Retrieved {Count} departures for {Station}", departures.Count, stationId);
            return new ApiResponse<List<Departure>> { Success = true, Data = departures };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Get departures error for {Station}", stationId);
            return new ApiResponse<List<Departure>>
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static DateTime ParseDateTime(string? timestamp)
    {
        if (string.IsNullOrEmpty(timestamp)) return DateTime.Now;
        try { return DateTime.Parse(timestamp); }
        catch { return DateTime.Now; }
    }

    private static DateTime ParseTimestamp(long timestamp)
    {
        return timestamp > 0 ? DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime : DateTime.Now;
    }

    private static string GetCategoryColor(string? category)
    {
        return category switch
        {
            "IC" or "EC" => "#004D95",
            "IR" or "RE" => "#0065B8",
            "S" => "#6F9E4A",
            "R" => "#F7941D",
            "TRAM" or "T" => "#E6332A",
            "BUS" or "B" => "#F3941D",
            _ => "#EE1C25"
        };
    }
}