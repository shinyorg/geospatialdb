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

    public MainPage(ISpatialGeofenceManager geofences, INotificationManager notifications)
    {
        InitializeComponent();
        this.geofences = geofences;
        this.notifications = notifications;
        HistoryList.ItemsSource = history;

        SampleGeofenceDelegate.RegionChanged += OnRegionChanged;
        this.SetButtonText();
    }

    void OnRegionChanged(object? sender, SpatialRegionChange change)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var regionName = change.Region.Properties.GetValueOrDefault("name")?.ToString() ?? "Unknown";
            var action = change.Entered ? "Entered" : "Exited";

            CurrentStateLabel.Text = $"{action}: {regionName}";

            history.Insert(0, $"{DateTime.Now:HH:mm:ss} - {action}: {regionName}");
        });
    }

    async void OnGetCurrentClicked(object? sender, EventArgs e)
    {
        try
        {
            var results = await this.geofences.GetCurrent();
            var parts = results.Select(r =>
            {
                var name = r.Region?.Properties.GetValueOrDefault("name")?.ToString() ?? "None";
                return $"{r.TableName}: {name}";
            });
            GetCurrentLabel.Text = string.Join(", ", parts);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Get Current", $"Error: {ex.Message}", "OK");
        }
    }

    async void OnToggleClicked(object? sender, EventArgs e)
    {
        try
        {
            await this.notifications.RequestAccess();

            if (this.geofences.IsStarted)
                await this.geofences.Stop();
            else
                await this.geofences.Start();
            
            this.SetButtonText();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Geofencing", $"Geofencing error: {ex.Message}", "OK");
        }
    }

    void SetButtonText()
    {
        ToggleButton.Text = this.geofences.IsStarted ? "Stop Geofencing" : "Start Geofencing";
    }
}
