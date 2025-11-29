using System.Runtime.InteropServices;

namespace MiniShell.Runtime;

public static class TerminalMode
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Termios
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;
        public byte c_line;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] c_cc;
        public uint c_ispeed;
        public uint c_ospeed;
    }

    private static Termios _originalTermios;
    private static bool _isRawMode;

    public static void EnableRawMode()
    {
        if (_isRawMode) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var handle = GetStdHandle(-10); // STD_INPUT_HANDLE
            GetConsoleMode(handle, out var mode);

            const uint ENABLE_ECHO_INPUT = 0x0004;
            const uint ENABLE_LINE_INPUT = 0x0002;
            const uint ENABLE_PROCESSED_INPUT = 0x0001;

            var newMode = mode & ~(ENABLE_ECHO_INPUT | ENABLE_LINE_INPUT | ENABLE_PROCESSED_INPUT);
            SetConsoleMode(handle, newMode);
        }
        else
        {
            // Unix/Linux/Mac
            tcgetattr(0, ref _originalTermios);

            var raw = _originalTermios;

            // Disable canonical mode and echo
            raw.c_lflag &= ~(ICANON | ECHO);

            // Set VMIN and VTIME for non-canonical mode
            raw.c_cc[VMIN] = 1;
            raw.c_cc[VTIME] = 0;

            tcsetattr(0, TCSAFLUSH, ref raw);
        }

        _isRawMode = true;
    }

    public static void DisableRawMode()
    {
        if (!_isRawMode) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows restore would need the original mode stored
        }
        else
        {
            tcsetattr(0, TCSAFLUSH, ref _originalTermios);
        }

        _isRawMode = false;
    }

    // Windows P/Invoke
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    // Unix P/Invoke
    [DllImport("libc", SetLastError = true)]
    private static extern int tcgetattr(int fd, ref Termios termios);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcsetattr(int fd, int optional_actions, ref Termios termios);

    private const int TCSAFLUSH = 2;
    private const uint ICANON = 0x00000002;
    private const uint ECHO = 0x00000008;
    private const int VMIN = 6;
    private const int VTIME = 5;
}
