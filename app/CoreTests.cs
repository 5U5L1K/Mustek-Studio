using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using PdfSharp.Pdf.IO;

namespace MustekStudio
{
    public static class CoreTests
    {
        static void Check(bool condition, string message)
        { if (!condition) throw new Exception("FAILED: " + message); Console.WriteLine("PASS: " + message); }
        static void Fixture(string path, Color color, string text)
        {
            using (Bitmap image = new Bitmap(300, 450))
            {
                image.SetResolution(150, 150);
                using (Graphics g = Graphics.FromImage(image))
                using (Font font = new Font("Arial", 25, FontStyle.Bold))
                {
                    g.Clear(color); g.FillRectangle(Brushes.White, 25, 70, 250, 300);
                    g.DrawString(text, font, Brushes.Black, 35, 130);
                    g.FillRectangle(Brushes.Black, 10, 10, 30, 30);
                    g.FillRectangle(Brushes.Yellow, 260, 410, 30, 30);
                }
                image.Save(path, ImageFormat.Png);
            }
        }
        static string Hash(string path)
        { using (SHA256 hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(path))); }
        static void DumpControls(System.Windows.Forms.Control control, System.Text.StringBuilder text)
        {
            if (!String.IsNullOrEmpty(control.Text)) text.AppendLine(control.GetType().Name + " " + control.Text.Replace("\n", " ") + " " + control.Bounds);
            foreach (System.Windows.Forms.Control child in control.Controls) DumpControls(child, text);
        }
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                string root = args[0], output = args[1]; Directory.CreateDirectory(output);
                string red = Path.Combine(output, "original-red.png"), blue = Path.Combine(output, "original-blue.png");
                Fixture(red, Color.FromArgb(225, 45, 60), "PAGE 1\nRED"); Fixture(blue, Color.FromArgb(30, 90, 210), "PAGE 2\nBLUE");
                string redHash = Hash(red), blueHash = Hash(blue);
                DocumentStore store = new DocumentStore(Path.Combine(output, "state"));
                store.AddFile(red, 150); store.AddFile(blue, 150);
                Check(store.Move(1, -1) == 0, "reorder pages"); store.Rotate(0);
                store.State.ScanDpi = 300; store.Save();
                DocumentStore restored = new DocumentStore(store.DirectoryPath);
                Check(restored.State.Pages.Count == 2 && restored.State.Pages[0].Rotation == 90 && restored.State.ScanDpi == 300, "restore document and settings");
                string png = Path.Combine(output, "rotated-blue.png");
                DocumentExporter.SavePng(restored, restored.State.Pages[0], png);
                using (Image image = Image.FromFile(png))
                    Check(image.Width == 450 && image.Height == 300 && Math.Abs(image.HorizontalResolution - 150) < 1, "PNG rotation, dimensions and DPI");
                string pdf = Path.Combine(output, "two-pages-a4.pdf");
                DocumentExporter.SavePdf(restored, restored.State.Pages.ToList(), pdf, true);
                using (var document = PdfReader.Open(pdf, PdfDocumentOpenMode.ReadOnly))
                {
                    Check(document.PageCount == 2, "multipage PDF count");
                    Check(document.Pages[0].Width.Point > document.Pages[0].Height.Point && document.Pages[1].Width.Point < document.Pages[1].Height.Point, "PDF rotated and portrait page orientations");
                }
                DocumentExporter.SavePdf(restored, restored.State.Pages.ToList(), Path.Combine(output, "two-pages-native.pdf"), false);
                using (var document = PdfReader.Open(Path.Combine(output, "two-pages-native.pdf"), PdfDocumentOpenMode.ReadOnly))
                    Check(Math.Abs(document.Pages[0].Width.Point - 216) < .1 && Math.Abs(document.Pages[0].Height.Point - 144) < .1, "native PDF physical dimensions reflect DPI");
                Check(Hash(red) == redHash && Hash(blue) == blueHash, "imports, rotation and export preserve original files");
                string existing = Path.Combine(output, "atomic.txt"); File.WriteAllText(existing, "original");
                try { FileTools.AtomicWrite(existing, delegate(string temp) { File.WriteAllText(temp, "partial"); throw new IOException("simulated write failure"); }); }
                catch (IOException) { }
                Check(File.ReadAllText(existing) == "original", "failed export preserves existing destination");
                int count = restored.State.Pages.Count;
                try { restored.AddFile(Path.Combine(output, "missing.png"), 150); } catch (FileNotFoundException) { } catch (OutOfMemoryException) { }
                Check(restored.State.Pages.Count == count, "failed import leaves document intact");
                if (args.Length > 2)
                {
                    DocumentStore actual = new DocumentStore(Path.Combine(output, "actual-state"));
                    actual.AddFile(args[2], 150); actual.AddFile(args[2], 150);
                    DocumentExporter.SavePdf(actual, actual.State.Pages.ToList(), Path.Combine(output, "actual-scans.pdf"), true);
                    Console.WriteLine("PASS: two actual scanned pages exported");
                }
                File.WriteAllText(Path.Combine(output, "result.txt"), "All core tests passed.\r\n" + output);
                using (MainWindow window = new MainWindow(root, restored.DirectoryPath))
                {
                    window.ShowInTaskbar = false;
                    window.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
                    window.Location = new Point(-20000, -20000);
                    window.Show(); System.Windows.Forms.Application.DoEvents();
                    window.PerformLayout();
                    System.Text.StringBuilder layout = new System.Text.StringBuilder(); DumpControls(window, layout);
                    File.WriteAllText(Path.Combine(output, "layout.txt"), layout.ToString());
                    using (Bitmap snapshot = new Bitmap(window.Width, window.Height))
                    {
                        window.DrawToBitmap(snapshot, new Rectangle(Point.Empty, snapshot.Size));
                        snapshot.Save(Path.Combine(output, "interface.png"), ImageFormat.Png);
                    }
                }
                Console.WriteLine("PASS: application interface rendered from test document");
                Console.WriteLine("All core tests passed: " + output); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
