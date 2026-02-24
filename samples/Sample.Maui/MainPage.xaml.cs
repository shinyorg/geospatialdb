using System.Collections.ObjectModel;
using Shiny.Locations;
using Shiny.Spatial.Gps;

namespace Sample.Maui;

public partial class MainPage : ContentPage
{
    readonly IGpsManager gpsManager;
    readonly ObservableCollection<string> history = new();
    bool isListening;

    public MainPage(IGpsManager gpsManager)
    {
        InitializeComponent();
        this.gpsManager = gpsManager;
        HistoryList.ItemsSource = history;

        SampleGeofenceDelegate.RegionChanged += OnRegionChanged;
    }

    void OnRegionChanged(object? sender, SpatialRegionChange change)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var previousName = change.PreviousRegion?.Properties.GetValueOrDefault("name")?.ToString() ?? "None";
            var currentName = change.CurrentRegion?.Properties.GetValueOrDefault("name")?.ToString() ?? "None";

            PreviousStateLabel.Text = previousName;
            CurrentStateLabel.Text = currentName;

            history.Insert(0, $"{DateTime.Now:HH:mm:ss} - {previousName} -> {currentName}");
        });
    }

    async void OnToggleClicked(object? sender, EventArgs e)
    {
        try
        {
            if (isListening)
            {
                await gpsManager.StopListener();
                isListening = false;
                ToggleButton.Text = "Start GPS";
            }
            else
            {
                await gpsManager.StartListener(new GpsRequest());
                isListening = true;
                ToggleButton.Text = "Stop GPS";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("GPS", $"GPS error: {ex.Message}", "OK");
        }
    }
}
