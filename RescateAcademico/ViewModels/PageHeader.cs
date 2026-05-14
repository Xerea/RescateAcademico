using Microsoft.AspNetCore.Html;

namespace RescateAcademico.ViewModels;

/// <summary>
/// Model for the shared <c>_PageHeader.cshtml</c> partial. Provides a consistent
/// header on every page: title, optional subtitle, icon, back-link, and an
/// optional HTML chunk for primary actions.
/// </summary>
public class PageHeader
{
    public string Title { get; set; } = string.Empty;
    /// <summary>Plain text subtitle. Razor encodes this value in the shared partial.</summary>
    public string? Subtitle { get; set; }
    /// <summary>Trusted, pre-encoded rich subtitle markup for intentional icon/emphasis cases.</summary>
    public IHtmlContent? SubtitleHtml { get; set; }
    public string? Icon { get; set; }
    /// <summary>accent (default) | primary | muted</summary>
    public string? IconVariant { get; set; }
    public string? BackUrl { get; set; }
    public string? BackLabel { get; set; }
    /// <summary>Trusted internal HTML rendered in the right-aligned actions slot.</summary>
    public string? ActionsHtml { get; set; }
}
