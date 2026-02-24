using System.Collections.ObjectModel;
using Shiny.Locations;
using Shiny.Notifications;
using Shiny.Spatial.Geofencing;

namespace Sample.Maui;

public partial class MainPage : ContentPage
{
    readonly ISpatialGeofenceManager geofences;
    readonly INotificationManager notifications;
    readonly ObservableCollection<string> history = new();
    bool isListening;

    public MainPage(ISpatialGeofenceManager geofences, INotificationManager notifications)
    {
        InitializeComponent();
        this.geofences = geofences;
        this.notifications = notifications;
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
            await this.notifications.RequestAccess();
            
            if (isListening)
            {
                await this.geofences.Stop();
                isListening = false;
                ToggleButton.Text = "Start Geofencing";
            }
            else
            {
                await this.geofences.Start();
                isListening = true;
                ToggleButton.Text = "Stop Geofencing";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Geofencing", $"Geofencing error: {ex.Message}", "OK");
        }
    }
}
