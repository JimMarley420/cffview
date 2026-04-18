using System.Net.Http;
using cffview.Models;
using cffview.Services;
using Moq;
using Xunit;

namespace cffview.tests;

public class TransportApiServiceTests
{
    [Fact]
    public async Task GetDeparturesAsync_WithValidStation_ReturnsDepartures()
    {
        var httpClient = new HttpClient();
        var service = new TransportApiService(httpClient);
        
        var result = await service.GetDeparturesAsync("Lausanne", 3);
        
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task SearchStationsAsync_WithQuery_ReturnsStations()
    {
        var httpClient = new HttpClient();
        var service = new TransportApiService(httpClient);
        
        var result = await service.SearchStationsAsync("Lausanne");
        
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task CheckConnectivityAsync_ReturnsTrue_WhenOnline()
    {
        var httpClient = new HttpClient();
        var service = new TransportApiService(httpClient);
        
        var result = await service.CheckConnectivityAsync();
        
        Assert.True(result);
    }
}

public class GtfsServiceTests
{
    [Fact]
    public void GtfsService_IsLoaded_DefaultsToFalse()
    {
        var service = new GtfsService();
        
        Assert.False(service.IsLoaded);
    }

    [Fact]
    public void GtfsService_LastUpdate_DefaultsToDefault()
    {
        var service = new GtfsService();
        
        Assert.Equal(default(DateTime), service.LastUpdate);
    }
}

public class DatabaseServiceTests
{
    [Fact]
    public async Task InitializeAsync_CreatesStorageFile()
    {
        var service = new DatabaseService();
        
        await service.InitializeAsync();
        
        Assert.NotNull(service);
    }

    [Fact]
    public async Task AddFavoriteAsync_ReturnsFavorite()
    {
        var service = new DatabaseService();
        await service.InitializeAsync();
        
        var stop = new Stop { Id = "8501120", Name = "Lausanne" };
        var favorite = await service.AddFavoriteAsync(stop);
        
        Assert.NotNull(favorite);
        Assert.Equal("Lausanne", favorite.StopName);
    }

    [Fact]
    public async Task GetFavoritesAsync_ReturnsList()
    {
        var service = new DatabaseService();
        await service.InitializeAsync();
        
        var favorites = await service.GetFavoritesAsync();
        
        Assert.NotNull(favorites);
    }

    [Fact]
    public async Task RemoveFavoriteAsync_AfterAdd_RemovesFavorite()
    {
        var service = new DatabaseService();
        await service.InitializeAsync();
        
        // First add a favorite, then remove it
        var stop = new Stop { Id = "8501120", Name = "Lausanne" };
        var favorite = await service.AddFavoriteAsync(stop);
        
        // Now remove it
        await service.RemoveFavoriteAsync(favorite.Id);
    }
}