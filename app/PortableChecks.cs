using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace MustekStudio
{
    internal static class PortableChecks
    {
        public static void Run(bool probeScanner)
        {
            string directory = Path.Combine(PortablePackage.DataRoot, "checks"); Directory.CreateDirectory(directory);
            StringBuilder result = new StringBuilder();
            result.AppendLine("Package: " + PortablePackage.Version);
            result.AppendLine("Managed PDF dependency embedded: " + (typeof(PdfDocument).Assembly.Location.Length == 0));
            if (typeof(PdfDocument).Assembly.Location.Length != 0) throw new IOException("PDF library was loaded externally.");
            string exe = Path.Combine(PortablePackage.EngineRoot, "runtime", "ucrt64", "bin", "scanimage.exe");
            ProcessStartInfo info = new ProcessStartInfo(exe, "--version") {
                WorkingDirectory = PortablePackage.EngineRoot, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true };
            info.EnvironmentVariables["PATH"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32");
            using (Process process = Process.Start(info))
            {
                string output = process.StandardOutput.ReadToEnd(), error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new IOException("Native scanner dependency check failed: " + process.ExitCode + " " + error);
                result.AppendLine("Native scanner: " + output.Trim());
            }
            string fixture = Path.Combine(directory, "fixture.png");
            using (Bitmap image = new Bitmap(300, 450))
            using (Graphics graphics = Graphics.FromImage(image))
            using (Font font = new Font("Arial", 20))
            {
                image.SetResolution(150, 150); graphics.Clear(Color.White);
                graphics.DrawString("PORTABLE\nSCANNER", font, Brushes.Black, 35, 100); image.Save(fixture, ImageFormat.Png);
            }
            string stateDirectory = Path.Combine(directory, "state-" + Guid.NewGuid().ToString("N"));
            DocumentStore store = new DocumentStore(stateDirectory);
            store.AddFile(fixture, 150); store.AddFile(fixture, 300); store.Rotate(1);
            store.State.Language = "en"; store.State.Theme = "dark"; store.Save();
            string pdf = Path.Combine(directory, "portable-test.pdf");
            DocumentExporter.SavePdf(store, store.State.Pages.ToList(), pdf, true);
            using (PdfDocument document = PdfReader.Open(pdf, PdfDocumentOpenMode.ReadOnly))
                if (document.PageCount != 2) throw new IOException("Portable PDF export failed.");
            result.AppendLine("PDF export: 2 pages, passed");
            DocumentStore restored = new DocumentStore(stateDirectory);
            if (restored.State.Language != "en" || restored.State.Theme != "dark" || restored.State.Pages.Count != 2) throw new IOException("Settings persistence failed.");
            result.AppendLine("Document and settings persistence: passed");
            using (MainWindow window = new MainWindow(PortablePackage.EngineRoot, stateDirectory, Path.Combine(directory, "scans")))
            {
                window.ShowInTaskbar = false; window.StartPosition = FormStartPosition.Manual; window.Location = new Point(-20000, -20000);
                window.Show(); Application.DoEvents(); window.PerformLayout();
                using (Bitmap screenshot = new Bitmap(window.Width, window.Height))
                {
                    window.DrawToBitmap(screenshot, new Rectangle(Point.Empty, screenshot.Size));
                    screenshot.Save(Path.Combine(directory, "portable-interface.png"), ImageFormat.Png);
                }
            }
            result.AppendLine("GUI startup and render: passed");
            if (probeScanner)
            {
                ScannerEngine engine = new ScannerEngine(PortablePackage.EngineRoot, Path.Combine(directory, "logs"));
                string options = engine.ProbeDevice();
                File.WriteAllText(Path.Combine(directory, "scanner-options.txt"), options, Encoding.UTF8);
                result.AppendLine("Connected scanner open and firmware lookup: passed");
            }
            File.WriteAllText(Path.Combine(PortablePackage.DataRoot, "portable-check.txt"), result.ToString(), Encoding.UTF8);
        }
    }
}
