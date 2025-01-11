using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class MicaForm : Form
{
    // Import the DwmSetWindowAttribute function from dwmapi.dll
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    // Import RtlGetVersion for accurate OS version detection
    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int RtlGetVersion(out OSVERSIONINFOEX versionInfo);

    // Constants for DwmSetWindowAttribute
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // Enable dark mode
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;     // Set the backdrop type
    private const int DWMSBT_MAINWINDOW = 2;              // Mica effect
    private const int DWMSBT_TRANSIENTWINDOW = 3;         // Acrylic effect

    // Structure for OS version information
    [StructLayout(LayoutKind.Sequential)]
    private struct OSVERSIONINFOEX
    {
        public int dwOSVersionInfoSize;
        public int dwMajorVersion;
        public int dwMinorVersion;
        public int dwBuildNumber;
        public int dwPlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szCSDVersion;
        public ushort wServicePackMajor;
        public ushort wServicePackMinor;
        public ushort wSuiteMask;
        public byte wProductType;
        public byte wReserved;
    }

    public MicaForm()
    {
        // Enable Mica effect when the form is created
        this.Load += MicaForm_Load;
    }

    private void MicaForm_Load(object sender, EventArgs e)
    {
        ApplyMicaEffect(this.Handle);
    }

    private static (int Major, int Build) GetActualOSVersion()
    {
        OSVERSIONINFOEX osInfo = new OSVERSIONINFOEX();
        osInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

        if (RtlGetVersion(out osInfo) == 0) // Success
        {
            return (osInfo.dwMajorVersion, osInfo.dwBuildNumber);
        }

        // Fallback to Environment.OSVersion if RtlGetVersion fails
        var osVersion = Environment.OSVersion;
        return (osVersion.Version.Major, osVersion.Version.Build);
    }

    private void ApplyMicaEffect(IntPtr hwnd)
    {
        var (osMajor, osBuild) = GetActualOSVersion();

        // MessageBox.Show($"Detected OS: Major={osMajor}, Build={osBuild}", "OS Version Info");

        if (osMajor >= 10 && osBuild >= 22000) // Windows 11
        {
            // Enable dark mode
            int darkMode = 1;
            int result = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            if (result != 0)
            {
                //MessageBox.Show($"Failed to enable dark mode. Error code: {result}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Apply the Mica effect
            int backdropType = DWMSBT_MAINWINDOW;
            result = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
            if (result != 0)
            {
                //MessageBox.Show($"Failed to apply Mica effect. Error code: {result}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            //MessageBox.Show("Mica effect is only supported on Windows 11 and later.", "Unsupported OS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}