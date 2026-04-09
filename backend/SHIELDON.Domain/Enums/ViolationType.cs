namespace SHIELDON.Domain.Enums;

/// <summary>
/// Classifies the type of exam violation detected by the Anti-Cheating Engine.
/// </summary>
public enum ViolationType
{
    // Minor severity
    AbnormalMouseActivity,

    // Medium severity
    ClipboardCopy,
    ClipboardPaste,
    RestrictedShortcut,
    WindowResize,
    WindowMinimize,
    SplitScreen,

    // Critical severity
    FullScreenExit,
    TabSwitch,
    FocusLoss,
    BrowserClose
}
