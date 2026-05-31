using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using cffview.Models;
using cffview.Services;
using Serilog;
using System.Windows.Media;

namespace cffview.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITransportApiService _apiService;
    private readonly IGtfsService _gtfsService;
    private readonly IDatabaseService _databaseService;
    private readonly ILogger _logger = Log.ForContext<MainViewModel>();
    private readonly DispatcherTimer _refreshTimer;
    private CancellationTokenSource? _searchCts;
    private bool _suppressSearch;

    [ObservableProperty] private ObservableCollection<Stop> _searchResults = new();
    [ObservableProperty] private ObservableCollection<Departure> _previewDepartures = new();
    [ObservableProperty] private ObservableCollection<FavoriteViewModel> _favorites = new();
    [ObservableProperty] private string _searchQuery = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreview))]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private Stop? _selectedStop;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _isLoading;

    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _statusMessage = "Prêt";
    [ObservableProperty] private bool _showSearchResults;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreview))]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _previewHasDepartures;

    [ObservableProperty] private string _lastRefreshed = "--:--";
    [ObservableProperty] private bool _isDarkMode;

    public bool ShowPreview => SelectedStop != null && PreviewHasDepartures;
    public bool ShowEmptyState => !Favorites.Any() && !ShowPreview && !IsLoading;

    public MainViewModel(
        ITransportApiService apiService,
        IGtfsService gtfsService,
        IDatabaseService databaseService)
    {
        _apiService = apiService;
        _gtfsService = gtfsService;
        _databaseService = databaseService;

        Favorites.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowEmptyState));

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _refreshTimer.Tick += async (_, _) => await RefreshAsync();
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Initialisation...";
        try
        {
            await _databaseService.InitializeAsync();

            // Restore saved theme
            var settings = await _databaseService.GetSettingsAsync();
            IsDarkMode = settings.IsDarkMode;
            ThemeManager.Apply(IsDarkMode);

            var isOnline = await _apiService.CheckConnectivityAsync();
            IsOffline = !isOnline;
            if (!isOnline)
                await _gtfsService.LoadGtfsDataAsync();
            await LoadFavoritesAsync();
            LastRefreshed = DateTime.Now.ToString("HH:mm");
            StatusMessage = IsOffline ? "Mode hors-ligne" : "En ligne";
            _refreshTimer.Start();
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
        var favs = await _databaseService.GetFavoritesAsync();
        var vms = favs.Select(f => new FavoriteViewModel(f, _apiService, _gtfsService, _databaseService)).ToList();
        Favorites.Clear();
        foreach (var vm in vms)
            Favorites.Add(vm);
        await Task.WhenAll(vms.Select(vm => vm.LoadDeparturesAsync()));
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        await SearchInternalAsync(_searchCts.Token);
    }

    private async Task SearchInternalAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 2)
        {
            ShowSearchResults = false;
            return;
        }
        try
        {
            var response = await _apiService.SearchStationsAsync(SearchQuery);
            if (ct.IsCancellationRequested) return;
            if (response.Success && response.Data != null)
            {
                SearchResults.Clear();
                foreach (var stop in response.Data)
                    SearchResults.Add(stop);
                ShowSearchResults = SearchResults.Any();
            }
            else
            {
                ShowSearchResults = false;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Search error");
        }
    }

    [RelayCommand]
    private async Task SelectStopAsync(Stop stop)
    {
        _suppressSearch = true;
        SelectedStop = stop;
        ShowSearchResults = false;
        SearchQuery = stop.Name;
        _suppressSearch = false;
        await LoadPreviewAsync(stop.Id);
    }

    private async Task LoadPreviewAsync(string stopId)
    {
        IsLoading = true;
        PreviewHasDepartures = false;
        PreviewDepartures.Clear();
        try
        {
            var response = await _apiService.GetDeparturesAsync(stopId, 10);
            var deps = response.Success && response.Data != null
                ? Deduplicate(response.Data)
                : Deduplicate(_gtfsService.GetDeparturesForStop(stopId, 6));
            foreach (var dep in deps)
                PreviewDepartures.Add(dep);
            PreviewHasDepartures = PreviewDepartures.Any();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Preview load error for {StopId}", stopId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddFavoriteAsync()
    {
        if (SelectedStop == null) return;
        if (Favorites.Any(f => f.StopId == SelectedStop.Id)) return;
        try
        {
            var favorite = await _databaseService.AddFavoriteAsync(SelectedStop);
            var vm = new FavoriteViewModel(favorite, _apiService, _gtfsService, _databaseService);
            Favorites.Add(vm);
            await vm.LoadDeparturesAsync();
            _suppressSearch = true;
            SelectedStop = null;
            PreviewDepartures.Clear();
            PreviewHasDepartures = false;
            SearchQuery = string.Empty;
            ShowSearchResults = false;
            _suppressSearch = false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Add favorite error");
        }
    }

    [RelayCommand]
    private void DismissPreview()
    {
        _suppressSearch = true;
        SelectedStop = null;
        PreviewDepartures.Clear();
        PreviewHasDepartures = false;
        SearchQuery = string.Empty;
        ShowSearchResults = false;
        _suppressSearch = false;
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
        if (!Favorites.Any()) return;
        StatusMessage = "Actualisation...";
        await Task.WhenAll(Favorites.Select(f => f.LoadDeparturesAsync()));
        LastRefreshed = DateTime.Now.ToString("HH:mm");
        StatusMessage = IsOffline ? "Mode hors-ligne" : "En ligne";
    }

    [RelayCommand]
    private async Task ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        ThemeManager.Apply(IsDarkMode);
        await _databaseService.SaveSettingsAsync(new UserSettings { IsDarkMode = IsDarkMode });
    }

    private static IEnumerable<Departure> Deduplicate(IEnumerable<Departure> deps)
    {
        var seen = new HashSet<string>();
        foreach (var dep in deps)
            if (seen.Add($"{dep.Line.ShortName}:{dep.DisplayTime:HHmm}"))
                yield return dep;
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (_suppressSearch) return;
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
            _ = SearchInternalAsync(_searchCts.Token);
        else
            ShowSearchResults = false;
    }
}

public record LineChip(string ShortName, string Color, bool IsSelected);

public partial class FavoriteViewModel : ObservableObject
{
    private readonly ITransportApiService _apiService;
    private readonly IGtfsService _gtfsService;
    private readonly IDatabaseService _databaseService;

    [ObservableProperty] private int _id;
    [ObservableProperty] private string _stopId = string.Empty;
    [ObservableProperty] private string _stopName = string.Empty;
    [ObservableProperty] private ObservableCollection<Departure> _departures = new();
    [ObservableProperty] private ObservableCollection<Departure> _visibleDepartures = new();
    [ObservableProperty] private ObservableCollection<LineChip> _lineChips = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNoDepartures))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpandLabel))]
    private bool _isExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLineFilter))]
    private string _lineFilter = string.Empty;

    public bool HasDepartures => Departures.Any();
    public bool ShowNoDepartures => !IsLoading && !HasDepartures;
    public string ExpandLabel => IsExpanded ? "▲ Moins" : "▼ Plus";
    public bool HasLineFilter => !string.IsNullOrWhiteSpace(LineFilter);

    public FavoriteViewModel(Favorite favorite, ITransportApiService apiService, IGtfsService gtfsService, IDatabaseService databaseService)
    {
        _apiService = apiService;
        _gtfsService = gtfsService;
        _databaseService = databaseService;
        Id = favorite.Id;
        StopId = favorite.StopId;
        StopName = favorite.StopName;
        LineFilter = favorite.LineFilter ?? string.Empty;
        Departures.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasDepartures));
            OnPropertyChanged(nameof(ShowNoDepartures));
            RefreshAll();
        };
    }

    private void RefreshAll()
    {
        RefreshChips();
        RefreshVisible();
    }

    private void RefreshChips()
    {
        LineChips.Clear();
        var seen = new HashSet<string>();
        foreach (var dep in Departures)
        {
            if (seen.Add(dep.Line.ShortName))
                LineChips.Add(new LineChip(dep.Line.ShortName, dep.Line.Color,
                    dep.Line.ShortName == LineFilter));
        }
    }

    private void RefreshVisible()
    {
        VisibleDepartures.Clear();
        var source = string.IsNullOrWhiteSpace(LineFilter)
            ? Departures
            : Departures.Where(d => d.Line.ShortName.Equals(LineFilter.Trim(), StringComparison.OrdinalIgnoreCase));
        var limit = IsExpanded ? int.MaxValue : 5;
        foreach (var dep in source.Take(limit))
            VisibleDepartures.Add(dep);
    }

    partial void OnIsExpandedChanged(bool value) => RefreshVisible();

    partial void OnLineFilterChanged(string value)
    {
        RefreshAll();
        _ = _databaseService.UpdateLineFilterAsync(Id, string.IsNullOrWhiteSpace(value) ? null : value);
    }

    [RelayCommand]
    private void SelectLine(string lineName)
    {
        LineFilter = LineFilter == lineName ? string.Empty : lineName;
    }

    public async Task LoadDeparturesAsync()
    {
        IsLoading = true;
        try
        {
            var response = await _apiService.GetDeparturesAsync(StopId, 10);
            Departures.Clear();
            var source = response.Success && response.Data != null
                ? response.Data
                : _gtfsService.GetDeparturesForStop(StopId, 6);
            var seen = new HashSet<string>();
            foreach (var dep in source)
                if (seen.Add($"{dep.Line.ShortName}:{dep.DisplayTime:HHmm}"))
                    Departures.Add(dep);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Departures load failed for {StopId}, trying GTFS", StopId);
            try
            {
                Departures.Clear();
                foreach (var dep in _gtfsService.GetDeparturesForStop(StopId, 6))
                    Departures.Add(dep);
            }
            catch { /* best effort */ }
        }
        finally
        {
            IsLoading = false;
            RefreshVisible();
        }
    }

    [RelayCommand]
    private async Task RefreshDeparturesAsync() => await LoadDeparturesAsync();

    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }
}
