using System.Runtime.InteropServices;

namespace MiniShell.Runtime;

public static class TerminalMode
{
    private static IntPtr _originalMode;
    private static bool _isRawMode;

    public static void EnableRawMode()
    {
        if (_isRawMode) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var handle = GetStdHandle(-10); // STD_INPUT_HANDLE
            GetConsoleMode(handle, out var mode);
            _originalMode = mode;

            const uint ENABLE_ECHO_INPUT = 0x0004;
            const uint ENABLE_LINE_INPUT = 0x0002;
            const uint ENABLE_PROCESSED_INPUT = 0x0001;

            var newModeValue = (uint)(long)mode & ~(ENABLE_ECHO_INPUT | ENABLE_LINE_INPUT | ENABLE_PROCESSED_INPUT);
            SetConsoleMode(handle, (IntPtr)newModeValue);
        }
        else
        {
            // Unix/Linux/Mac
            tcgetattr(0, out var original);
            _originalMode = original;

            // Disable canonical mode and echo
            var rawValue = (long)original & ~(ICANON_VALUE | ECHO_VALUE);

            tcsetattr(0, TCSAFLUSH, (IntPtr)rawValue);
        }

        _isRawMode = true;
    }

    public static void DisableRawMode()
    {
        if (!_isRawMode) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var handle = GetStdHandle(-10);
            SetConsoleMode(handle, _originalMode);
        }
        else
        {
            tcsetattr(0, TCSAFLUSH, _originalMode);
        }

        _isRawMode = false;
    }

    // Windows P/Invoke
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out IntPtr lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, IntPtr dwMode);

    // Unix P/Invoke
    [DllImport("libc", SetLastError = true)]
    private static extern int tcgetattr(int fd, out IntPtr termios);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcsetattr(int fd, int optional_actions, IntPtr termios);

    private const int TCSAFLUSH = 2;
    private const long ICANON_VALUE = 0x00000002;
    private const long ECHO_VALUE = 0x00000008;
}
