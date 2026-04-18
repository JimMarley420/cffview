using System.Collections.ObjectModel;
using cffview.Models;
using cffview.Services;
using cffview.ViewModels;
using Moq;
using Xunit;

namespace cffview.tests;

public class MainViewModelTests
{
    private readonly Mock<ITransportApiService> _mockApiService;
    private readonly Mock<IGtfsService> _mockGtfsService;
    private readonly Mock<IDatabaseService> _mockDbService;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _mockApiService = new Mock<ITransportApiService>();
        _mockGtfsService = new Mock<IGtfsService>();
        _mockDbService = new Mock<IDatabaseService>();
        
        _viewModel = new MainViewModel(
            _mockApiService.Object,
            _mockGtfsService.Object,
            _mockDbService.Object);
    }

    [Fact]
    public void MainViewModel_InitialState_HasEmptyCollections()
    {
        Assert.NotNull(_viewModel.SearchResults);
        Assert.NotNull(_viewModel.Departures);
        Assert.NotNull(_viewModel.Favorites);
        Assert.Empty(_viewModel.SearchResults);
    }

    [Fact]
    public void MainViewModel_InitialState_IsNotLoading()
    {
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public void MainViewModel_InitialState_HasDefaultStatus()
    {
        Assert.Equal("Prêt", _viewModel.StatusMessage);
    }

    [Fact]
    public void MainViewModel_InitialState_HasReadyStatus()
    {
        var service = new TransportApiService(new HttpClient());
        var vm = new MainViewModel(service, new GtfsService(), new DatabaseService());
        
        Assert.Equal("Prêt", vm.StatusMessage);
    }

    [Fact]
    public void InitializeAsync_WhenOnline_SetsIsOfflineFalse()
    {
        _mockApiService.Setup(x => x.CheckConnectivityAsync())
            .ReturnsAsync(true);
        _mockDbService.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);
        _mockDbService.Setup(x => x.GetFavoritesAsync())
            .ReturnsAsync(new List<Favorite>());

        _ = _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsOffline);
    }

    [Fact]
    public void InitializeAsync_WhenOffline_SetsIsOfflineTrue()
    {
        _mockApiService.Setup(x => x.CheckConnectivityAsync())
            .ReturnsAsync(false);
        _mockDbService.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);
        _mockDbService.Setup(x => x.GetFavoritesAsync())
            .ReturnsAsync(new List<Favorite>());

        _ = _viewModel.InitializeAsync();

        Assert.True(_viewModel.IsOffline);
    }

    [Fact]
    public void SearchQuery_Changed_TriggersPropertyChanged()
    {
        bool called = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.SearchQuery))
                called = true;
        };

        _viewModel.SearchQuery = "Lausanne";

        Assert.True(called);
    }

    [Fact]
    public async Task SearchAsync_WithShortQuery_DoesNotSearch()
    {
        _viewModel.SearchQuery = "A";

        await _viewModel.SearchCommand.ExecuteAsync(null);

        _mockApiService.Verify(x => x.SearchStationsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_CallsApi()
    {
        _viewModel.SearchQuery = "Lausanne";
        _mockApiService.Setup(x => x.SearchStationsAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApiResponse<List<Stop>>
            {
                Success = true,
                Data = new List<Stop> { new Stop { Id = "1", Name = "Lausanne" } }
            });

        await _viewModel.SearchCommand.ExecuteAsync(null);

        _mockApiService.Verify(x => x.SearchStationsAsync("Lausanne"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AddFavoriteAsync_WithNullStop_DoesNotAdd()
    {
        _viewModel.SelectedStop = null;

        await _viewModel.AddFavoriteCommand.ExecuteAsync(null);

        _mockDbService.Verify(x => x.AddFavoriteAsync(It.IsAny<Stop>()), Times.Never);
    }

    [Fact]
    public async Task AddFavoriteAsync_WithStop_AddsToFavorites()
    {
        _viewModel.SelectedStop = new Stop { Id = "8501120", Name = "Lausanne" };
        _mockDbService.Setup(x => x.AddFavoriteAsync(It.IsAny<Stop>()))
            .ReturnsAsync(new Favorite { Id = 1, StopId = "8501120", StopName = "Lausanne" });

        await _viewModel.AddFavoriteCommand.ExecuteAsync(null);

        _mockDbService.Verify(x => x.AddFavoriteAsync(It.IsAny<Stop>()), Times.Once);
    }
}

public class FavoriteViewModelTests
{
    [Fact]
    public void FavoriteViewModel_InitialState_HasEmptyDepartures()
    {
        var mockApi = new Mock<ITransportApiService>();
        var mockGtfs = new Mock<IGtfsService>();
        
        var favorite = new Favorite { Id = 1, StopId = "8501120", StopName = "Lausanne" };
        var vm = new FavoriteViewModel(favorite, mockApi.Object, mockGtfs.Object);

        Assert.NotNull(vm.Departures);
        Assert.Empty(vm.Departures);
    }

    [Fact]
    public void FavoriteViewModel_SetsStopInfo()
    {
        var mockApi = new Mock<ITransportApiService>();
        var mockGtfs = new Mock<IGtfsService>();
        
        var favorite = new Favorite { Id = 1, StopId = "8501120", StopName = "Lausanne" };
        var vm = new FavoriteViewModel(favorite, mockApi.Object, mockGtfs.Object);

        Assert.Equal("8501120", vm.StopId);
        Assert.Equal("Lausanne", vm.StopName);
        Assert.Equal(1, vm.Id);
    }
}