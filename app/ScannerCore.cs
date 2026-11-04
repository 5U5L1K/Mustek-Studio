using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace MustekStudio
{
    public class PageRecord
    {
        public string FileName { get; set; }
        public int Dpi { get; set; }
        public int Rotation { get; set; }
        public string Label { get; set; }
    }

    public class DocumentState
    {
        public int ScanDpi = 150;
        public int PreviewDpi = 75;
        public string Mode = "Color";
        public string Format = "PDF";
        public bool A4 = true;
        public string Language = "ru";
        public string Theme = "light";
        public List<PageRecord> Pages = new List<PageRecord>();
    }

    public static class FileTools
    {
        public static void AtomicWrite(string path, Action<string> write)
        {
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                write(temp);
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        public static Bitmap ReadImage(string path)
        {
            using (Image image = Image.FromFile(path))
            {
                Bitmap result = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
                float dx = image.HorizontalResolution, dy = image.VerticalResolution;
                result.SetResolution(dx >= 20 && dx <= 2400 ? dx : 150,
                                     dy >= 20 && dy <= 2400 ? dy : 150);
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.Clear(Color.White);
                    g.DrawImageUnscaled(image, 0, 0);
                }
                return result;
            }
        }

        public static void CheckPng(string path)
        {
            using (Image image = Image.FromFile(path))
            {
                if (image.RawFormat.Guid != ImageFormat.Png.Guid || image.Width < 1 || image.Height < 1)
                    throw new IOException(UiText.Get("Сканер не создал корректное PNG-изображение."));
            }
        }
    }

    public sealed class DocumentStore
    {
        public string DirectoryPath { get; private set; }
        public DocumentState State { get; private set; }
        public string ManifestPath { get { return Path.Combine(DirectoryPath, "document.xml"); } }
        public DocumentStore(string directory)
        {
            DirectoryPath = Path.GetFullPath(directory);
            Directory.CreateDirectory(DirectoryPath);
            Directory.CreateDirectory(Path.Combine(DirectoryPath, "pages"));
            State = new DocumentState();
            if (File.Exists(ManifestPath))
            {
                using (FileStream stream = File.OpenRead(ManifestPath))
                    State = (DocumentState)new XmlSerializer(typeof(DocumentState)).Deserialize(stream);
                if (State.Pages == null) State.Pages = new List<PageRecord>();
                foreach (PageRecord page in State.Pages)
                {
                    if (page.Dpi < 20 || page.Dpi > 2400 || page.Rotation < 0 || page.Rotation > 270 || page.Rotation % 90 != 0)
                        throw new IOException(UiText.Get("Некорректные параметры сохранённой страницы."));
                    if (!File.Exists(PagePath(page)))
                        throw new IOException(UiText.Get("Не найдена сохранённая страница: ") + page.FileName);
                }
            }
        }
        public string PagePath(PageRecord page)
        {
            if (String.IsNullOrEmpty(page.FileName) || Path.GetFileName(page.FileName) != page.FileName)
                throw new IOException(UiText.Get("Некорректное имя страницы в сохранённом документе."));
            return Path.Combine(DirectoryPath, "pages", page.FileName);
        }
        public void Save()
        {
            FileTools.AtomicWrite(ManifestPath, delegate(string path)
            {
                using (FileStream stream = File.Create(path))
                    new XmlSerializer(typeof(DocumentState)).Serialize(stream, State);
            });
        }
        public PageRecord AddFile(string file, int dpi)
        {
            PageRecord page = new PageRecord { FileName = Guid.NewGuid().ToString("N") + ".png", Dpi = dpi,
                                               Rotation = 0, Label = Path.GetFileName(file) };
            using (Bitmap image = FileTools.ReadImage(file))
            {
                if (dpi < 20 || dpi > 2400)
                    page.Dpi = (int)Math.Round(image.HorizontalResolution);
                if (page.Dpi < 20 || page.Dpi > 2400) page.Dpi = 150;
                image.SetResolution(page.Dpi, page.Dpi);
                FileTools.AtomicWrite(PagePath(page), delegate(string path) { image.Save(path, ImageFormat.Png); });
            }
            State.Pages.Add(page);
            try { Save(); }
            catch { State.Pages.Remove(page); throw; }
            return page;
        }
        public Bitmap ReadPage(PageRecord page)
        {
            Bitmap image = FileTools.ReadImage(PagePath(page));
            if (page.Rotation == 90) image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            if (page.Rotation == 180) image.RotateFlip(RotateFlipType.Rotate180FlipNone);
            if (page.Rotation == 270) image.RotateFlip(RotateFlipType.Rotate270FlipNone);
            image.SetResolution(page.Dpi, page.Dpi);
            return image;
        }
        public int Move(int index, int delta)
        {
            int next = index + delta;
            if (index < 0 || index >= State.Pages.Count || next < 0 || next >= State.Pages.Count) return index;
            PageRecord page = State.Pages[index];
            State.Pages.RemoveAt(index); State.Pages.Insert(next, page); Save(); return next;
        }
        public void Rotate(int index)
        {
            if (index < 0 || index >= State.Pages.Count) return;
            State.Pages[index].Rotation = (State.Pages[index].Rotation + 90) % 360;
            Save();
        }
        public void Remove(int index)
        {
            if (index < 0 || index >= State.Pages.Count) return;
            State.Pages.RemoveAt(index); Save();
            // Keep cached image files so removal never deletes an original or recovery copy.
        }
    }

    public static class DocumentExporter
    {
        public static void SavePng(DocumentStore store, PageRecord page, string output)
        {
            using (Bitmap bitmap = store.ReadPage(page))
                FileTools.AtomicWrite(output, delegate(string path) { bitmap.Save(path, ImageFormat.Png); });
            FileTools.CheckPng(output);
        }
        public static void SavePdf(DocumentStore store, IList<PageRecord> pages, string output, bool a4)
        {
            if (pages.Count == 0) throw new InvalidOperationException(UiText.Get("Добавьте хотя бы одну страницу."));
            FileTools.AtomicWrite(output, delegate(string temp)
            {
                using (PdfDocument pdf = new PdfDocument())
                {
                    pdf.Info.Title = Path.GetFileNameWithoutExtension(output);
                    pdf.Info.Creator = "Mustek Studio";
                    pdf.Options.CompressContentStreams = true;
                    foreach (PageRecord record in pages)
                    {
                        using (XImage image = XImage.FromFile(store.PagePath(record)))
                        {
                            bool sideways = record.Rotation % 180 != 0;
                            double width = sideways ? image.PixelHeight : image.PixelWidth;
                            double height = sideways ? image.PixelWidth : image.PixelHeight;
                            PdfPage page = pdf.AddPage();
                            double pw = width * 72 / record.Dpi, ph = height * 72 / record.Dpi;
                            if (a4) { pw = width > height ? 841.8898 : 595.2756; ph = width > height ? 595.2756 : 841.8898; }
                            page.Width = XUnit.FromPoint(pw); page.Height = XUnit.FromPoint(ph);
                            double margin = a4 ? 14.1732 : 0;
                            double scale = Math.Min((pw - 2 * margin) / width, (ph - 2 * margin) / height);
                            using (XGraphics g = XGraphics.FromPdfPage(page))
                            {
                                g.TranslateTransform(pw / 2, ph / 2);
                                g.RotateTransform(record.Rotation);
                                g.DrawImage(image, -image.PixelWidth * scale / 2, -image.PixelHeight * scale / 2,
                                            image.PixelWidth * scale, image.PixelHeight * scale);
                            }
                        }
                    }
                    pdf.Save(temp);
                }
            });
        }
    }

    public sealed class ScannerEngine
    {
        readonly string root;
        readonly string logs;
        public ScannerEngine(string projectRoot, string logDirectory) { root = projectRoot; logs = logDirectory; }
        public static string Quote(string value)
        {
            if (value.IndexOf('\0') >= 0) throw new ArgumentException("NUL in argument");
            StringBuilder result = new StringBuilder("\""); int slashes = 0;
            foreach (char ch in value)
            {
                if (ch == '\\') { slashes++; continue; }
                if (ch == '"') { result.Append('\\', slashes * 2 + 1); result.Append(ch); }
                else { result.Append('\\', slashes); result.Append(ch); }
                slashes = 0;
            }
            result.Append('\\', slashes * 2); result.Append('"'); return result.ToString();
        }
        void Configure()
        {
#if PORTABLE
            // The portable backend's data directory is patched to ".".
            // Relative ASCII paths let the native module work even when the
            // EXE and its data directory have Unicode names.
            string firmware = Path.Combine(root, "sane", "gt68xx", "SBfw.usb");
            string firmwareSetting = "SBfw.usb";
#else
            string firmware = Path.Combine(root, "firmware", "SBfw.usb");
            string firmwareSetting = "\\\\?\\" + firmware;
#endif
            if (!File.Exists(firmware)) throw new IOException(UiText.Get("Не найдена прошивка SBfw.usb. Сохраните приложение в папке проекта."));
            string config = Path.Combine(root, "config"); Directory.CreateDirectory(config);
            File.WriteAllLines(Path.Combine(config, "gt68xx.conf"), new string[] {
                "# Mustek Studio", "usb 0x05d8 0x4002", "override \"mustek-scanexpress-1200-ub-plus\"",
                "firmware \"" + firmwareSetting + "\"" }, new UTF8Encoding(false));
        }
        string Run(string[] args, Action<int> progress)
        {
            string exe = Path.Combine(root, "runtime", "ucrt64", "bin", "scanimage.exe");
            if (!File.Exists(exe)) throw new IOException(UiText.Get("Не найден модуль сканирования: ") + exe);
            Configure(); Directory.CreateDirectory(logs);
            ProcessStartInfo info = new ProcessStartInfo(exe, String.Join(" ", args.Select(Quote)));
            info.WorkingDirectory = root; info.UseShellExecute = false; info.CreateNoWindow = true;
            info.RedirectStandardOutput = true; info.RedirectStandardError = true;
            info.EnvironmentVariables["PATH"] = Path.GetDirectoryName(exe) + ";" + Environment.GetEnvironmentVariable("PATH");
#if PORTABLE
            info.EnvironmentVariables["SANE_CONFIG_DIR"] = "config";
#else
            info.EnvironmentVariables["SANE_CONFIG_DIR"] = Path.Combine(root, "config");
#endif
            foreach (string key in info.EnvironmentVariables.Keys.Cast<string>().Where(k => k.StartsWith("SANE_DEBUG_", StringComparison.OrdinalIgnoreCase)).ToArray())
                info.EnvironmentVariables.Remove(key);
            StringBuilder output = new StringBuilder(), error = new StringBuilder();
            using (Process process = new Process())
            {
                process.StartInfo = info;
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                { if (e.Data != null) lock (output) output.AppendLine(e.Data); };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data == null) return;
                    lock (error) error.AppendLine(e.Data);
                    Match match = Regex.Match(e.Data, @"Progress:\s*([\d.]+)%");
                    double value;
                    if (match.Success && Double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && progress != null)
                        progress(Math.Max(0, Math.Min(100, (int)value)));
                };
                process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine(); process.WaitForExit();
                string log = Path.Combine(logs, "scan-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".log");
                File.WriteAllText(log, info.Arguments + Environment.NewLine + "Exit: " + process.ExitCode + Environment.NewLine + output + error, Encoding.UTF8);
                if (process.ExitCode != 0)
                    throw new IOException(UiText.Get("Не удалось выполнить сканирование (код ") + process.ExitCode + ").\n" +
                        (error.Length > 1500 ? error.ToString().Substring(error.Length - 1500) : error.ToString()) + UiText.Get("\nЖурнал: ") + log);
            }
            return output.ToString().Trim();
        }
        public string FindDevice()
        {
            string[] names = Run(new string[] { "--formatted-device-list=%d%n" }, null)
                .Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (names.Length == 0) throw new IOException(UiText.Get("Сканер не найден. Проверьте USB, питание и установку WinUSB для устройства 05D8:4002."));
            if (names.Length != 1) throw new IOException(UiText.Get("Найдено несколько сканеров. Подключите один Mustek 1200 UB Plus."));
            return names[0].Trim();
        }
        public string ProbeDevice()
        {
            string options = Run(new string[] { "--device-name=" + FindDevice(), "--help" }, null);
            if (!options.Contains("Options specific to device")) throw new IOException("The scanner could not be opened.");
            return options;
        }
        public void Scan(string output, int dpi, string mode, bool preview, Action<int> progress)
        {
            if (!(new int[] { 50, 75, 150, 300, 600, 1200 }).Contains(dpi)) throw new ArgumentException(UiText.Get("Неподдерживаемое разрешение."));
            if (mode != "Color" && mode != "Gray") throw new ArgumentException(UiText.Get("Неподдерживаемый цветовой режим."));
            if (File.Exists(output)) throw new IOException(UiText.Get("Файл сканирования уже существует."));
            string device = FindDevice();
            string nativeOutput = output;
#if PORTABLE
            Uri relative = new Uri(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar)
                .MakeRelativeUri(new Uri(Path.GetFullPath(output)));
            if (!relative.IsAbsoluteUri) nativeOutput = Uri.UnescapeDataString(relative.ToString()).Replace('/', Path.DirectorySeparatorChar);
#endif
            List<string> args = new List<string> { "--device-name=" + device, "--mode=" + mode,
                "--resolution=" + dpi, "--depth=8", "--format=png", "--output-file=" + nativeOutput, "--progress" };
            if (preview) args.Add("--preview=yes");
            Run(args.ToArray(), progress); FileTools.CheckPng(output);
        }
    }
}
