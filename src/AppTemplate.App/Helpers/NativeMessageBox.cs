using System.Runtime.InteropServices;

namespace AkariToolbox.Helpers;

/// <summary>
/// Blocking Win32 message box (user32). Used from headless safe-boot flows and
/// from the Tweaks layer where WPF MessageBox used to live. No XamlRoot needed.
/// </summary>
public static class NativeMessageBox
{
    private const uint MB_OK = 0x00000000;
    private const uint MB_YESNO = 0x00000004;
    private const uint MB_ICONWARNING = 0x00000030;
    private const uint MB_ICONQUESTION = 0x00000020;
    private const uint MB_ICONINFORMATION = 0x00000040;

    private const int IDYES = 6;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    public static void Show(string message, string title) =>
        MessageBoxW(IntPtr.Zero, message, title, MB_OK | MB_ICONINFORMATION);

    public static void ShowWarning(string message, string title) =>
        MessageBoxW(IntPtr.Zero, message, title, MB_OK | MB_ICONWARNING);

    public static bool Confirm(string message, string title) =>
        MessageBoxW(IntPtr.Zero, message, title, MB_YESNO | MB_ICONQUESTION) == IDYES;

    public static bool ConfirmWarning(string message, string title) =>
        MessageBoxW(IntPtr.Zero, message, title, MB_YESNO | MB_ICONWARNING) == IDYES;
}
