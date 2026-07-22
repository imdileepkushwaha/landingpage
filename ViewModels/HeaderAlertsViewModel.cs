namespace SoftflipSolutions.ViewModels;

public class HeaderAlertItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string TimeLabel { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-bell";
    public string Accent { get; set; } = "cyan";
    public DateTime CreatedAt { get; set; }
}

public class HeaderAlertsViewModel
{
    public List<HeaderAlertItem> Notifications { get; set; } = [];
    public List<HeaderAlertItem> Messages { get; set; } = [];
    public int NotificationCount => Notifications.Count;
    public int MessageCount => Messages.Count;
}
