using Microsoft.AspNetCore.Html;

namespace RescateAcademico.ViewModels;

/// <summary>
/// Model for the shared <c>_Hero.cshtml</c> partial — the signature shader
/// hero band used on the most important role pages (Profesor, Autoridad, Admin).
/// </summary>
public class HeroBand
{
    public string? Eyebrow { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Lead { get; set; }
    /// <summary>Raw HTML rendered in the action row (buttons).</summary>
    public string? ActionsHtml { get; set; }
    /// <summary>Raw HTML rendered in the right-hand aside (e.g. a stat ring).</summary>
    public IHtmlContent? AsideHtml { get; set; }
    /// <summary>Turn the WebGL shader on/off. CSS gradient still renders if false.</summary>
    public bool Shader { get; set; } = true;
}
