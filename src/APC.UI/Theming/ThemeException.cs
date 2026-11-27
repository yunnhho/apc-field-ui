using System;

namespace APC.UI.Theming
{
    /// <summary>theme.json cannot be read or breaks a rule. The message includes the file path and field name.</summary>
    [Serializable]
    public sealed class ThemeException : Exception
    {
        public ThemeException(string message) : base(message) { }
        public ThemeException(string message, Exception inner) : base(message, inner) { }
    }
}
