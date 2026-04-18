using System.IO;
using System.Text.Json;
using cffview.Models;
using Serilog;

namespace cffview.Services;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<List<Favorite>> GetFavoritesAsync();
    Task<Favorite> AddFavoriteAsync(Stop stop, string? lineId = null, string? lineName = null);
    Task RemoveFavoriteAsync(int id);
    Task UpdateFavoriteOrderAsync(List<Favorite> favorites);
}

public class DatabaseService : IDatabaseService
{
    private readonly string _dataPath;
    private readonly ILogger _logger = Log.ForContext<DatabaseService>();
    private List<Favorite> _favorites = new();
    private int _nextId = 1;

    public DatabaseService() : this(null) { }

    public DatabaseService(string? customPath)
    {
        if (customPath != null)
        {
            _dataPath = customPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbFolder = Path.Combine(appData, "CFFView");
            Directory.CreateDirectory(dbFolder);
            _dataPath = Path.Combine(dbFolder, "favorites.json");
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            if (File.Exists(_dataPath))
            {
                var json = await File.ReadAllTextAsync(_dataPath);
                _favorites = JsonSerializer.Deserialize<List<Favorite>>(json) ?? new();
                _nextId = _favorites.Any() ? _favorites.Max(f => f.Id) + 1 : 1;
            }
            _logger.Information("Database initialized at {Path}", _dataPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Database initialization failed");
            _favorites = new();
        }
    }

    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_favorites, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_dataPath, json);
    }

    public Task<List<Favorite>> GetFavoritesAsync()
    {
        return Task.FromResult(_favorites.OrderBy(f => f.DisplayOrder).ToList());
    }

    public async Task<Favorite> AddFavoriteAsync(Stop stop, string? lineId = null, string? lineName = null)
    {
        var maxOrder = _favorites.Any() ? _favorites.Max(f => f.DisplayOrder) : 0;
        
        var favorite = new Favorite
        {
            Id = _nextId++,
            StopId = stop.Id,
            StopName = stop.Name,
            LineId = lineId,
            LineName = lineName,
            DisplayOrder = maxOrder + 1,
            CreatedAt = DateTime.Now
        };
        
        _favorites.Add(favorite);
        await SaveAsync();
        
        _logger.Information("Added favorite: {StopName}", stop.Name);
        return favorite;
    }

    public async Task RemoveFavoriteAsync(int id)
    {
        var favorite = _favorites.FirstOrDefault(f => f.Id == id);
        if (favorite != null)
        {
            _favorites.Remove(favorite);
            await SaveAsync();
            _logger.Information("Removed favorite: {StopName}", favorite.StopName);
        }
    }

    public async Task UpdateFavoriteOrderAsync(List<Favorite> favorites)
    {
        _favorites = favorites;
        await SaveAsync();
    }
}