using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using cffview.Models;
using cffview.Services;
using Serilog;

namespace cffview.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITransportApiService _apiService;
    private readonly IGtfsService _gtfsService;
    private readonly IDatabaseService _databaseService;
    private readonly ILogger _logger = Log.ForContext<MainViewModel>();

    [ObservableProperty]
    private ObservableCollection<Stop> _searchResults = new();

    [ObservableProperty]
    private ObservableCollection<Departure> _departures = new();

    [ObservableProperty]
    private ObservableCollection<FavoriteViewModel> _favorites = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private Stop? _selectedStop;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isOffline;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _statusMessage = "Prêt";

    [ObservableProperty]
    private bool _showSearchResults;

    [ObservableProperty]
    private bool _showDepartures;

    public MainViewModel(
        ITransportApiService apiService,
        IGtfsService gtfsService,
        IDatabaseService databaseService)
    {
        _apiService = apiService;
        _gtfsService = gtfsService;
        _databaseService = databaseService;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Initialisation...";

        try
        {
            await _databaseService.InitializeAsync();
            
            var isOnline = await _apiService.CheckConnectivityAsync();
            IsOffline = !isOnline;
            
            if (isOnline)
            {
                StatusMessage = "Connecté - Chargement des favoris...";
            }
            else
            {
                await _gtfsService.LoadGtfsDataAsync();
                StatusMessage = "Mode hors-ligne - Données locales";
            }

            await LoadFavoritesAsync();
            StatusMessage = IsOffline ? "Hors-ligne" : "Prêt";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Initialization error");
            HasError = true;
            ErrorMessage = "Erreur d'initialisation";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadFavoritesAsync()
    {
        try
        {
            var favs = await _databaseService.GetFavoritesAsync();
            Favorites.Clear();

            foreach (var fav in favs)
            {
                var vm = new FavoriteViewModel(fav, _apiService, _gtfsService);
                await vm.LoadDeparturesAsync();
                Favorites.Add(vm);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load favorites");
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 2)
        {
            ShowSearchResults = false;
            return;
        }

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var response = await _apiService.SearchStationsAsync(SearchQuery);
            
            if (response.Success && response.Data != null)
            {
                SearchResults.Clear();
                foreach (var stop in response.Data)
                {
                    SearchResults.Add(stop);
                }
                ShowSearchResults = SearchResults.Any();
            }
            else
            {
                ShowSearchResults = false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Search error");
            HasError = true;
            ErrorMessage = "Erreur de recherche";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectStopAsync(Stop stop)
    {
        SelectedStop = stop;
        ShowSearchResults = false;
        SearchQuery = stop.Name;
        
        await LoadDeparturesForStopAsync(stop.Id);
    }

    private async Task LoadDeparturesForStopAsync(string stopId)
    {
        IsLoading = true;
        ShowDepartures = false;
        Departures.Clear();

        try
        {
            var response = await _apiService.GetDeparturesAsync(stopId, 10);
            
            if (response.Success && response.Data != null)
            {
                var seen = new HashSet<string>();
                foreach (var dep in response.Data)
                {
                    var key = $"{dep.Line.ShortName}:{dep.DisplayTime:HHmm}";
                    if (seen.Contains(key)) continue;
                    seen.Add(key);
                    Departures.Add(dep);
                }
                ShowDepartures = Departures.Any();
            }
            else
            {
                var offlineDeps = _gtfsService.GetDeparturesForStop(stopId, 10);
                var seen = new HashSet<string>();
                foreach (var dep in offlineDeps)
                {
                    var key = $"{dep.Line.ShortName}:{dep.DisplayTime:HHmm}";
                    if (seen.Contains(key)) continue;
                    seen.Add(key);
                    Departures.Add(dep);
                }
                ShowDepartures = Departures.Any();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Departure load error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddFavoriteAsync()
    {
        _logger.Information("AddFavoriteCommand called, SelectedStop: {Stop}", SelectedStop?.Name);
        
        if (SelectedStop == null)
        {
            _logger.Warning("AddFavorite called but SelectedStop is null");
            return;
        }

        try
        {
            var favorite = await _databaseService.AddFavoriteAsync(SelectedStop);
            _logger.Information("Favorite added: {StopName}", favorite.StopName);
            
            var vm = new FavoriteViewModel(favorite, _apiService, _gtfsService);
            Favorites.Add(vm);
            
            await vm.LoadDeparturesAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Add favorite error");
        }
    }

    [RelayCommand]
    private async Task RemoveFavoriteAsync(FavoriteViewModel favorite)
    {
        try
        {
            await _databaseService.RemoveFavoriteAsync(favorite.Id);
            Favorites.Remove(favorite);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Remove favorite error");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        foreach (var fav in Favorites)
        {
            await fav.LoadDeparturesAsync();
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
        {
            _ = SearchAsync();
        }
        else
        {
            ShowSearchResults = false;
        }
    }
}

public partial class FavoriteViewModel : ObservableObject
{
    private readonly ITransportApiService _apiService;
    private readonly IGtfsService _gtfsService;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _stopId = string.Empty;

    [ObservableProperty]
    private string _stopName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Departure> _departures = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isExpanded;

    public FavoriteViewModel(Favorite favorite, ITransportApiService apiService, IGtfsService gtfsService)
    {
        _apiService = apiService;
        _gtfsService = gtfsService;
        
        Id = favorite.Id;
        StopId = favorite.StopId;
        StopName = favorite.StopName;
    }

    public async Task LoadDeparturesAsync()
    {
        IsLoading = true;
        
        try
        {
            var response = await _apiService.GetDeparturesAsync(StopId, 10);
            
            Departures.Clear();
            
            if (response.Success && response.Data != null)
            {
                var seen = new HashSet<string>();
                foreach (var dep in response.Data)
                {
                    var key = $"{dep.Line.ShortName}:{dep.DisplayTime:HHmm}";
                    if (seen.Contains(key)) continue;
                    seen.Add(key);
                    Departures.Add(dep);
                }
            }
            else
            {
                var offlineDeps = _gtfsService.GetDeparturesForStop(StopId, 10);
                var seen = new HashSet<string>();
                foreach (var dep in offlineDeps)
                {
                    var key = $"{dep.Line.ShortName}:{dep.DisplayTime:HHmm}";
                    if (seen.Contains(key)) continue;
                    seen.Add(key);
                    Departures.Add(dep);
                }
            }
        }
        catch
        {
            var offlineDeps = _gtfsService.GetDeparturesForStop(StopId, 3);
            Departures.Clear();
            foreach (var dep in offlineDeps)
            {
                Departures.Add(dep);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    [RelayCommand]
    private async Task RefreshDeparturesAsync()
    {
        await LoadDeparturesAsync();
    }
}