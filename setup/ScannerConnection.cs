using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace MustekConnection
{
    public sealed class ScannerStatus
    {
        public bool Found;
        public bool Ready;
        public string Service = "";
        public uint Problem;
        public string Name = "";
    }
    public static class UsbScanner
    {
        [StructLayout(LayoutKind.Sequential)] struct DeviceInfo
        { public uint Size; public Guid ClassGuid; public uint DevInst; public UIntPtr Reserved; }
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr SetupDiGetClassDevsW(IntPtr guid, string enumerator, IntPtr parent, uint flags);
        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr devices, uint index, ref DeviceInfo info);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SetupDiGetDeviceRegistryPropertyW(IntPtr devices, ref DeviceInfo info, uint property,
            out uint type, byte[] buffer, uint size, out uint required);
        [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr devices);
        [DllImport("cfgmgr32.dll")] static extern uint CM_Get_DevNode_Status(out uint status, out uint problem, uint devInst, uint flags);
        static string Property(IntPtr devices, ref DeviceInfo info, uint property)
        {
            uint type, needed; byte[] buffer = new byte[8192];
            if (!SetupDiGetDeviceRegistryPropertyW(devices, ref info, property, out type, buffer, (uint)buffer.Length, out needed)) return "";
            return Encoding.Unicode.GetString(buffer, 0, (int)Math.Min(needed, buffer.Length)).TrimEnd('\0');
        }
        public static ScannerStatus Read()
        {
            ScannerStatus result = new ScannerStatus();
            IntPtr devices = SetupDiGetClassDevsW(IntPtr.Zero, "USB", IntPtr.Zero, 0x6);
            if (devices == new IntPtr(-1)) throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                for (uint index = 0; ; index++)
                {
                    DeviceInfo info = new DeviceInfo(); info.Size = (uint)Marshal.SizeOf(typeof(DeviceInfo));
                    if (!SetupDiEnumDeviceInfo(devices, index, ref info)) break;
                    string[] ids = Property(devices, ref info, 1).Split('\0');
                    if (!ids.Any(id => id.Equals("USB\\VID_05D8&PID_4002", StringComparison.OrdinalIgnoreCase) ||
                                       id.StartsWith("USB\\VID_05D8&PID_4002&", StringComparison.OrdinalIgnoreCase))) continue;
                    ScannerStatus match = new ScannerStatus { Found = true, Service = Property(devices, ref info, 4), Name = Property(devices, ref info, 0) };
                    uint status, problem;
                    uint error = CM_Get_DevNode_Status(out status, out problem, info.DevInst, 0);
                    match.Problem = problem;
                    match.Ready = error == 0 && problem == 0 && (status & 8) != 0 && match.Service.Equals("WinUSB", StringComparison.OrdinalIgnoreCase);
                    if (match.Ready) return match;
                    result = match;
                }
            }
            finally { SetupDiDestroyDeviceInfoList(devices); }
            return result;
        }
    }
    internal static class Program
    {
        internal const string ZadigHash = "4ecaa95df3da3621486a043aef8b3050b8bafe7c901402871e816229ef82039b";
        internal static string DirectoryPath { get { return AppDomain.CurrentDomain.BaseDirectory; } }
        internal static string AppPath { get { return Path.Combine(DirectoryPath, "MustekStudio.exe"); } }
        internal static string ToolPath { get { return Path.Combine(DirectoryPath, "tools", "zadig-2.9.exe"); } }
        internal static bool English;
        internal static string T(string ru, string en) { return English ? en : ru; }
        internal static string Hash(string path)
        { using (SHA256 hash = SHA256.Create()) using (FileStream stream = File.OpenRead(path)) return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        internal static void LaunchApplication()
        {
            string document = Path.Combine(DirectoryPath, "MustekStudio.Data", "app-data", "document.xml");
            if (!File.Exists(document))
            {
                XmlDocument defaults = new XmlDocument();
                Assembly app = Assembly.LoadFrom(AppPath);
                using (Stream resource = app.GetManifestResourceStream("Portable.defaults.xml")) defaults.Load(resource);
                XmlNode language = defaults.DocumentElement.SelectSingleNode("Language");
                if (language != null) language.InnerText = English ? "en" : "ru";
                Directory.CreateDirectory(Path.GetDirectoryName(document));
                try { using (FileStream stream = new FileStream(document, FileMode.CreateNew)) defaults.Save(stream); }
                catch (IOException) { if (!File.Exists(document)) throw; }
            }
            Process.Start(new ProcessStartInfo(AppPath) { UseShellExecute = true, WorkingDirectory = DirectoryPath });
        }
        [STAThread]
        static int Main(string[] args)
        {
            English = args.Contains("en");
            try
            {
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                if (args.Length >= 2 && args[0] == "--diagnose")
                {
                    ScannerStatus status = UsbScanner.Read();
                    File.WriteAllText(args[1], "Found=" + status.Found + "\r\nReady=" + status.Ready + "\r\nService=" + status.Service +
                        "\r\nProblem=" + status.Problem + "\r\nZadigHashValid=" + (Hash(ToolPath) == ZadigHash), Encoding.UTF8);
                    return Hash(ToolPath) == ZadigHash ? 0 : 1;
                }
                if (args.Contains("--finish") && UsbScanner.Read().Ready) { LaunchApplication(); return 0; }
                using (ConnectionWindow form = new ConnectionWindow())
                {
                    int renderIndex = Array.IndexOf(args, "--render-check");
                    if (renderIndex >= 0 && renderIndex + 1 < args.Length)
                    {
                        form.ShowInTaskbar = false; form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000);
                        form.Show(); Application.DoEvents();
                        using (Bitmap image = new Bitmap(form.Width, form.Height))
                        { form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(args[renderIndex + 1], System.Drawing.Imaging.ImageFormat.Png); }
                    }
                    else Application.Run(form);
                }
                return 0;
            }
            catch (Exception error)
            {
                if (args.Length >= 2 && args[0] == "--diagnose") File.WriteAllText(args[1], error.ToString());
                else MessageBox.Show(error.Message, "Mustek Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }
    public sealed class ConnectionWindow : Form
    {
        readonly Label status = new Label();
        readonly Button driver = new Button();
        readonly Timer refresh = new Timer();
        public ConnectionWindow()
        {
            Text = Program.T("Mustek Studio — подключение сканера", "Mustek Studio — scanner connection");
            Font = new Font("Segoe UI", 10); ClientSize = new Size(690, 490); MinimumSize = new Size(690, 490);
            BackColor = Color.FromArgb(245, 247, 250); ForeColor = Color.FromArgb(31, 45, 61); StartPosition = FormStartPosition.CenterScreen;
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 5 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); Controls.Add(layout);
            Label title = new Label { Text = "Mustek 1200 UB Plus", Font = new Font("Segoe UI", 19, FontStyle.Bold), Dock = DockStyle.Fill };
            status.Dock = DockStyle.Fill; status.Padding = new Padding(0, 8, 0, 0);
            Label instructions = new Label { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0), Text = Program.T(
                "1. Подключите сканер к USB.\n\n2. Если WinUSB ещё не установлен, нажмите «Настроить WinUSB». Подтвердите запрос прав администратора.\n\n3. В Zadig выберите устройство с USB ID 05D8:4002, справа — WinUSB, затем нажмите Install Driver.\n\n4. Дождитесь завершения. Вернитесь в это окно: статус обновится автоматически.",
                "1. Connect the scanner to USB.\n\n2. If WinUSB is not installed, click Configure WinUSB and approve the administrator prompt.\n\n3. In Zadig, select USB ID 05D8:4002, choose WinUSB on the right, then click Install Driver.\n\n4. Wait for completion. Return to this window; the status updates automatically.") };
            Label note = new Label { Text = Program.T("Выбирайте в Zadig только сканер с ID 05D8:4002.", "In Zadig, select only the scanner with ID 05D8:4002."), Dock = DockStyle.Fill, ForeColor = Color.FromArgb(92, 108, 126), Padding = new Padding(0, 8, 0, 0) };
            FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            Button launch = new Button { Text = Program.T("Открыть Mustek Studio", "Open Mustek Studio"), Width = 205, Height = 36, BackColor = Color.FromArgb(124, 58, 237), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            launch.FlatAppearance.BorderSize = 0; launch.Click += delegate { try { Program.LaunchApplication(); Close(); } catch (Exception e) { ShowError(e); } };
            driver.Text = Program.T("Настроить WinUSB", "Configure WinUSB"); driver.Width = 180; driver.Height = 36;
            driver.Click += delegate
            {
                try
                {
                    if (Program.Hash(Program.ToolPath) != Program.ZadigHash) throw new IOException(Program.T("Контрольная сумма Zadig не совпадает. Запустите установщик повторно.", "Zadig checksum mismatch. Run Setup again."));
                    string destination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MustekStudio", "Driver");
                    Directory.CreateDirectory(destination);
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(Program.ToolPath), "zadig.ini"),
                        "[general]\r\nexit_on_success=true\r\n[device]\r\nlist_all=true\r\ninclude_hubs=false\r\n[driver]\r\ndefault_driver=0\r\ndefault_dir=\"" + destination.Replace("\\", "\\\\") + "\"\r\n", new UTF8Encoding(false));
                    Process.Start(new ProcessStartInfo(Program.ToolPath) { UseShellExecute = true, Verb = "runas", WorkingDirectory = Path.GetDirectoryName(Program.ToolPath) });
                }
                catch (Win32Exception error) { if (error.NativeErrorCode != 1223) ShowError(error); }
                catch (Exception error) { ShowError(error); }
            };
            actions.Controls.Add(launch); actions.Controls.Add(driver);
            layout.Controls.Add(title, 0, 0); layout.Controls.Add(status, 0, 1); layout.Controls.Add(instructions, 0, 2); layout.Controls.Add(note, 0, 3); layout.Controls.Add(actions, 0, 4);
            refresh.Interval = 2000; refresh.Tick += delegate { UpdateStatus(); }; Shown += delegate { UpdateStatus(); refresh.Start(); };
        }
        void UpdateStatus()
        {
            try
            {
                ScannerStatus state = UsbScanner.Read();
                status.Text = state.Ready ? Program.T("Сканер подключён. WinUSB установлен — можно начинать работу.", "Scanner connected. WinUSB is installed — ready to scan.") :
                    !state.Found ? Program.T("Сканер пока не обнаружен. Подключите его к USB.", "Scanner not detected yet. Connect it to USB.") :
                    Program.T("Сканер обнаружен. Требуется настроить WinUSB.", "Scanner detected. WinUSB configuration is required.");
                status.ForeColor = state.Ready ? Color.FromArgb(16, 112, 118) : Color.FromArgb(120, 85, 20);
                driver.Enabled = state.Found && !state.Ready;
            }
            catch (Exception error) { status.Text = error.Message; driver.Enabled = false; }
        }
        void ShowError(Exception error) { MessageBox.Show(this, error.Message, "Mustek Studio", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        protected override void Dispose(bool disposing) { if (disposing) refresh.Dispose(); base.Dispose(disposing); }
    }
}
