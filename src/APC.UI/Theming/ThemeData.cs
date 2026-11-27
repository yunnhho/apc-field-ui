using System.Runtime.Serialization;

namespace APC.UI.Theming
{
    /// <summary>theme.json mapping. JSON keys are camelCase.</summary>
    [DataContract]
    public sealed class ThemeData
    {
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "fontFamily")] public string FontFamily { get; set; }
        [DataMember(Name = "fontSize")] public int FontSize { get; set; }
        [DataMember(Name = "colors")] public ThemeColors Colors { get; set; }
        [DataMember(Name = "status")] public StatusColors Status { get; set; }
        [DataMember(Name = "buttonSlice")] public int[] ButtonSlice { get; set; }
        [DataMember(Name = "scan")] public ScanSettings Scan { get; set; }
        [DataMember(Name = "logo")] public string Logo { get; set; }
    }

    [DataContract]
    public sealed class ThemeColors
    {
        /// <summary>Gradient [start, end]</summary>
        [DataMember(Name = "background")] public string[] Background { get; set; }
        [DataMember(Name = "header")] public string[] Header { get; set; }
        /// <summary>Window title bar (light tone because the logo sits on it)</summary>
        [DataMember(Name = "titleBar")] public string[] TitleBar { get; set; }
        [DataMember(Name = "gridHeader")] public string[] GridHeader { get; set; }
        [DataMember(Name = "border")] public string Border { get; set; }
        [DataMember(Name = "text")] public string Text { get; set; }
        [DataMember(Name = "textMuted")] public string TextMuted { get; set; }
        [DataMember(Name = "textInverse")] public string TextInverse { get; set; }
        [DataMember(Name = "gridLine")] public string GridLine { get; set; }
        [DataMember(Name = "gridSelected")] public string GridSelected { get; set; }
        /// <summary>Panel and card background gradient [start, end]. White if missing.</summary>
        [DataMember(Name = "panel")] public string[] Panel { get; set; }
        /// <summary>Input field border. border if missing.</summary>
        [DataMember(Name = "borderStrong")] public string BorderStrong { get; set; }
        /// <summary>Brand color. Used only for the function name in the window title and the group/section title bars (kept apart from alarm red). text if missing.</summary>
        [DataMember(Name = "brand")] public string Brand { get; set; }
    }

    [DataContract]
    public sealed class StatusColors
    {
        [DataMember(Name = "run")] public string Run { get; set; }
        [DataMember(Name = "warn")] public string Warn { get; set; }
        [DataMember(Name = "alarm")] public string Alarm { get; set; }
        [DataMember(Name = "stop")] public string Stop { get; set; }
        [DataMember(Name = "info")] public string Info { get; set; }
    }

    [DataContract]
    public sealed class ScanSettings
    {
        [DataMember(Name = "maxKeyIntervalMs")] public int MaxKeyIntervalMs { get; set; }
        [DataMember(Name = "minLength")] public int MinLength { get; set; }
    }
}
