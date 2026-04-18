using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using CsvHelper;
using CsvHelper.Configuration;
using cffview.Models;
using cffview.Models.DTOs;
using Serilog;

namespace cffview.Services;

public interface IGtfsService
{
    Task<bool> LoadGtfsDataAsync();
    Task<bool> UpdateGtfsDataAsync();
    List<Stop> GetAllStops();
    List<Departure> GetDeparturesForStop(string stopId, int limit = 3);
    DateTime LastUpdate { get; }
    bool IsLoaded { get; }
}

public class GtfsService : IGtfsService
{
    private readonly string _gtfsUrl = "https://data.opentransportdata.swiss/dataset/timetable-2026-gtfs2020/resource/98be98ad-3b23-43d2-bc22-6122c5870333/download/gtfs_fp2026.zip";
    private readonly string _dataFolder;
    private readonly ILogger _logger = Log.ForContext<GtfsService>();

    private List<StopRecordDto> _stops = new();
    private List<RouteRecordDto> _routes = new();
    private List<TripRecordDto> _trips = new();
    private List<StopTimeRecordDto> _stopTimes = new();
    private List<CalendarRecordDto> _calendars = new();

    public DateTime LastUpdate { get; private set; }
    public bool IsLoaded { get; private set; }

    public GtfsService()
    {
        _dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        Directory.CreateDirectory(_dataFolder);
    }

    public async Task<bool> LoadGtfsDataAsync()
    {
        var zipPath = Path.Combine(_dataFolder, "gtfs.zip");
        
        if (!File.Exists(zipPath))
        {
            _logger.Information("GTFS data not found locally");
            return await UpdateGtfsDataAsync();
        }

        try
        {
            await ParseGtfsFilesAsync(zipPath);
            LastUpdate = File.GetLastWriteTime(zipPath);
            IsLoaded = true;
            _logger.Information("GTFS loaded: {Stops} stops", _stops.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load GTFS");
            return false;
        }
    }

    public async Task<bool> UpdateGtfsDataAsync()
    {
        var zipPath = Path.Combine(_dataFolder, "gtfs.zip");
        
        try
        {
            _logger.Information("Downloading GTFS from {Url}", _gtfsUrl);
            
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.Add("User-Agent", "CFFView/1.0");
            
            var response = await client.GetAsync(_gtfsUrl);
            response.EnsureSuccessStatusCode();
            
            await using var fs = new FileStream(zipPath, FileMode.Create);
            await response.Content.CopyToAsync(fs);
            
            await ParseGtfsFilesAsync(zipPath);
            LastUpdate = DateTime.Now;
            IsLoaded = true;
            
            _logger.Information("GTFS updated: {Stops} stops", _stops.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update GTFS");
            
            if (File.Exists(zipPath))
            {
                _logger.Warning("Using existing GTFS data");
                await ParseGtfsFilesAsync(zipPath);
                IsLoaded = true;
                return true;
            }
            
            return false;
        }
    }

    private async Task ParseGtfsFilesAsync(string zipPath)
    {
        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(zipPath);
            
            _stops = ReadCsv<StopRecordDto>(archive, "stops.txt");
            _routes = ReadCsv<RouteRecordDto>(archive, "routes.txt");
            _trips = ReadCsv<TripRecordDto>(archive, "trips.txt");
            _stopTimes = ReadCsv<StopTimeRecordDto>(archive, "stop_times.txt");
            _calendars = ReadCsv<CalendarRecordDto>(archive, "calendar.txt").ToList();
            
            _logger.Information("GTFS parsed: {Stops} stops, {Routes} routes", _stops.Count, _routes.Count);
        });
    }

    private static List<T> ReadCsv<T>(ZipArchive archive, string fileName) where T : class
    {
        var entry = archive.GetEntry(fileName);
        if (entry == null) return new List<T>();
        
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return csv.GetRecords<T>().ToList();
    }

    public List<Stop> GetAllStops()
    {
        return _stops.Select(s => new Stop
        {
            Id = s.StopId,
            Name = s.StopName,
            Description = s.StopDesc,
            Latitude = s.StopLat,
            Longitude = s.StopLon
        }).OrderBy(s => s.Name).ToList();
    }

    public List<Departure> GetDeparturesForStop(string stopId, int limit = 3)
    {
        var now = DateTime.Now;
        
        var tripIds = _stopTimes
            .Where(st => st.StopId == stopId)
            .OrderBy(st => st.DepartureTime)
            .Take(50)
            .Select(st => st.TripId)
            .Distinct()
            .ToList();
        
        var departures = new List<Departure>();
        
        foreach (var tripId in tripIds.Take(limit))
        {
            var trip = _trips.FirstOrDefault(t => t.TripId == tripId);
            var route = _routes.FirstOrDefault(r => r.RouteId == trip?.RouteId);
            var stopTime = _stopTimes.FirstOrDefault(st => st.TripId == tripId && st.StopId == stopId);
            
            if (trip == null || route == null || stopTime == null) continue;
            
            var timeParts = stopTime.DepartureTime.Split(':');
            if (timeParts.Length < 2) continue;
            
            if (int.TryParse(timeParts[0], out var hours) && int.TryParse(timeParts[1], out var minutes))
            {
                var scheduledTime = now.Date.AddHours(hours).AddMinutes(minutes);
                if (scheduledTime < now) scheduledTime = scheduledTime.AddDays(1);
                
                departures.Add(new Departure
                {
                    Id = tripId,
                    StopId = stopId,
                    Line = new Line
                    {
                        Id = route.RouteId,
                        ShortName = route.RouteShortName,
                        LongName = route.RouteLongName,
                        Color = route.RouteColor ?? "#EE1C25"
                    },
                    ScheduledTime = scheduledTime,
                    Destination = trip.TripHeadSign,
                    Status = DepartureStatus.Scheduled
                });
            }
        }
        
        return departures.OrderBy(d => d.ScheduledTime).Take(limit).ToList();
    }
}