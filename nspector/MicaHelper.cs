using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
// Program.cs STAThread = MicaHelper.EnableDarkMode();
// InitComp = MicaHelper.ApplyMicaEffect(this);
// MessageBox.Show = MicaHelper.CustomMessageBox.Show
//
// Replace win32 folder picker:
// string folderPath = MicaHelper.PickFolder();
// if (!string.IsNullOrEmpty(folderPath))
// {
//     MessageBox.Show($"Selected Folder: {folderPath}");
// }

public static class MicaHelper
{
    #region DWM & Dark Mode APIs

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int RtlGetVersion(out OSVERSIONINFOEX versionInfo);

    [DllImport("uxtheme.dll", SetLastError = true, EntryPoint = "#135")]
    private static extern int SetPreferredAppMode(int appMode);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string subIdList);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetForegroundWindow();

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMSBT_TABBEDWINDOW = 4;
    private const int ALLOW_DARK_MODE = 1;

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

    private static void ApplyDarkThemeToControls(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            control.BackColor = Color.FromArgb(32, 32, 32);
            control.ForeColor = Color.White;

            if (control is Button button)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = Color.FromArgb(64, 64, 64);
                button.ForeColor = Color.White;
            }
            else if (control is TextBox textBox)
            {
                textBox.BackColor = Color.FromArgb(50, 50, 50);
                textBox.ForeColor = Color.White;
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }

            // Apply recursively for nested controls
            if (control.HasChildren)
            {
                ApplyDarkThemeToControls(control);
            }
        }
    }


    public static void ApplyMicaEffect(Form form)
    {
        if (form == null) throw new ArgumentNullException(nameof(form));
        var (osMajor, osBuild) = GetActualOSVersion();
        IntPtr hwnd = form.Handle;

        if (osMajor >= 10 && osBuild >= 22000)
        {
            int darkMode = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            int backdropType = DWMSBT_TABBEDWINDOW;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
        }
        form.BackColor = Color.FromArgb(32, 32, 32);
        ApplyDarkThemeToControls(form);
    }

    public static void EnableDarkMode() => SetPreferredAppMode(ALLOW_DARK_MODE);

    private static (int Major, int Build) GetActualOSVersion()
    {
        OSVERSIONINFOEX osInfo = new OSVERSIONINFOEX { dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX)) };
        return RtlGetVersion(out osInfo) == 0 ? (osInfo.dwMajorVersion, osInfo.dwBuildNumber) : (Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Build);
    }

    #endregion

    #region Custom MessageBox

    public class CustomMessageBox : Form
    {
        public CustomMessageBox(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            Width = 350;
            Height = 150;
            MicaHelper.ApplyMicaEffect(this);

            Label lblMessage = new Label { Text = message, Dock = DockStyle.Fill, Padding = new Padding(10), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter };
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Padding = new Padding(10), AutoSize = true };
            if (buttons == MessageBoxButtons.OK || buttons == MessageBoxButtons.OKCancel) buttonPanel.Controls.Add(CreateButton("OK", DialogResult.OK));
            if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel) buttonPanel.Controls.Add(CreateButton("Yes", DialogResult.Yes));
            if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel) buttonPanel.Controls.Add(CreateButton("No", DialogResult.No));
            if (buttons == MessageBoxButtons.OKCancel || buttons == MessageBoxButtons.YesNoCancel) buttonPanel.Controls.Add(CreateButton("Cancel", DialogResult.Cancel));
            PictureBox iconBox = new PictureBox { Size = new Size(40, 40), Image = GetIconImage(icon), SizeMode = PictureBoxSizeMode.StretchImage };
            TableLayoutPanel mainPanel = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, Dock = DockStyle.Fill };
            mainPanel.Controls.Add(iconBox, 0, 0);
            mainPanel.Controls.Add(lblMessage, 1, 0);
            mainPanel.Controls.Add(buttonPanel, 1, 1);
            Controls.Add(mainPanel);
        }

        private Button CreateButton(string text, DialogResult result) => new Button { Text = text, DialogResult = result, AutoSize = true, BackColor = Color.FromArgb(64, 64, 64), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        private Image GetIconImage(MessageBoxIcon icon) => icon switch { MessageBoxIcon.Information => SystemIcons.Information.ToBitmap(), MessageBoxIcon.Warning => SystemIcons.Warning.ToBitmap(), MessageBoxIcon.Error => SystemIcons.Error.ToBitmap(), MessageBoxIcon.Question => SystemIcons.Question.ToBitmap(), _ => null };
        public static DialogResult Show(string message, string title = "Message", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None) => new CustomMessageBox(message, title, buttons, icon).ShowDialog();
    }

    #endregion

    #region Modern Folder Picker

    public static string PickFolder()
    {
        var dialog = (IFileOpenDialog)new FileOpenDialog();
        dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_NOCHANGEDIR);
        dialog.SetTitle("Select a Folder");

        int hr = dialog.Show(IntPtr.Zero); // Store result separately
        if (hr == 0) // S_OK
        {
            dialog.GetResult(out IShellItem shellItem); // Call separately
            shellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out IntPtr pszPath); // Now it's valid

            string selectedPath = Marshal.PtrToStringAuto(pszPath); // Convert pointer to string
            Marshal.FreeCoTaskMem(pszPath); // Free allocated memory
            return selectedPath;
        }

        return null; // User canceled or error occurred
    }


    #endregion

    #region COM Interfaces & Constants

    [ComImport]
    [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig] int Show(IntPtr parent);
        void SetFileTypes(); // Not needed
        void SetFileTypeIndex(); // Not needed
        void GetFileTypeIndex(); // Not needed
        void Advise(); // Not needed
        void Unadvise(); // Not needed
        void SetOptions(FOS fos);
        void GetOptions(out FOS fos);
        void SetDefaultFolder(); // Not needed
        void SetFolder(); // Not needed
        void GetFolder(); // Not needed
        void GetCurrentSelection(); // Not needed
        void SetFileName(); // Not needed
        void GetFileName(); // Not needed
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
        void SetOkButtonLabel(); // Not needed
        void SetFileNameLabel(); // Not needed
        void GetResult(out IShellItem ppsi);
        void AddPlace(); // Not needed
        void SetDefaultExtension(); // Not needed
        void Close(); // Not needed
        void SetClientGuid(); // Not needed
        void ClearClientData(); // Not needed
        void SetFilter(); // Not needed
        void GetResults(); // Not needed
        void GetSelectedItems(); // Not needed
    }

    [ComImport]
    [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
    private class FileOpenDialog { }

    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(); // Not needed
        void GetParent(); // Not needed
        void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
        void GetAttributes(); // Not needed
        void Compare(); // Not needed
    }

    private enum SIGDN : uint
    {
        SIGDN_FILESYSPATH = 0x80058000,
    }

    [Flags]
    private enum FOS : uint
    {
        FOS_PICKFOLDERS = 0x00000020,
        FOS_FORCEFILESYSTEM = 0x00000040,
        FOS_NOCHANGEDIR = 0x00000008,
    }

    #endregion

}
