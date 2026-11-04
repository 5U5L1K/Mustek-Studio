using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MustekStudio
{
    public static class AppearanceTests
    {
        static IEnumerable<Control> Walk(Control control)
        {
            yield return control;
            foreach (Control child in control.Controls)
                foreach (Control nested in Walk(child)) yield return nested;
        }
        static void Check(bool condition, string name)
        { if (!condition) throw new Exception("FAILED: " + name); Console.WriteLine("PASS: " + name); }
        static void Render(Form form, string file)
        {
            form.ShowInTaskbar = false; form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000);
            form.Show(); Application.DoEvents(); form.PerformLayout();
            using (Bitmap image = new Bitmap(form.Width, form.Height))
            { form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(file, ImageFormat.Png); }
        }
        static bool Cyrillic(string text) { return text.Any(c => c >= 0x400 && c <= 0x4ff); }
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                string root = args[0], output = args[1]; Directory.CreateDirectory(output);
                string source = Path.Combine(output, "fixture.png");
                using (Bitmap image = new Bitmap(300, 450))
                using (Graphics g = Graphics.FromImage(image))
                using (Font font = new Font("Arial", 28))
                {
                    g.Clear(Color.White); g.FillRectangle(Brushes.MediumPurple, 20, 20, 260, 12);
                    g.DrawString("SCAN\nPREVIEW", font, Brushes.Black, 25, 100);
                    image.Save(source, ImageFormat.Png);
                }
                DocumentStore store = new DocumentStore(Path.Combine(output, "state"));
                store.AddFile(source, 300); store.AddFile(source, 150); store.Rotate(1);
                store.State.Mode = "Gray"; store.State.Format = "PNG"; store.State.ScanDpi = 600; store.Save();
                string[] files = store.State.Pages.Select(p => p.FileName).ToArray();
                List<string> snapshots = new List<string>();
                using (MainWindow window = new MainWindow(root, store.DirectoryPath))
                {
                    window.ShowInTaskbar = false; window.StartPosition = FormStartPosition.Manual; window.Location = new Point(-20000, -20000); window.Show();
                    PreviewCanvas canvas = Walk(window).OfType<PreviewCanvas>().Single(); canvas.ActualSize();
                    Size imageSize = canvas.ImageSize;
                    foreach (string language in new string[] { "ru", "en" })
                    foreach (string theme in new string[] { "light", "dark" })
                    {
                        window.ApplyPreferences(language, theme); Application.DoEvents();
                        string combination = language + "-" + theme;
                        Check(canvas.ImageSize == imageSize && Walk(window).OfType<Label>().Any(l => l.Text == "100 %"), "preserve image and zoom " + combination);
                        DocumentStore saved = new DocumentStore(store.DirectoryPath);
                        Check(saved.State.Language == language && saved.State.Theme == theme, "preferences saved " + combination);
                        Check(UiText.ReadSavedLanguage(store.DirectoryPath) == language, "startup language " + combination);
                        Check(saved.State.Pages.Select(p => p.FileName).SequenceEqual(files) && saved.State.Pages[1].Rotation == 90 && saved.State.Pages[0].Dpi == 300 && saved.State.ScanDpi == 600 && saved.State.Mode == "Gray" && saved.State.Format == "PNG", "preserve document and scan settings " + combination);
                        Button scan = Walk(window).OfType<Button>().Single(b => b.Text == UiText.Get("Сканировать страницу"));
                        Check(scan.BackColor == ThemeColors.For(theme).Accent, "correct primary button color " + combination);
                        Check(Walk(window).OfType<ComboBox>().Single(c => c.AccessibleName == UiText.Get("Цветовой режим")).SelectedIndex == 1, "preserve dropdown values " + combination);
                        if (language == "en")
                        {
                            string[] untranslated = Walk(window).Where(c => Cyrillic(c.Text)).Select(c => c.GetType().Name + ": " + c.Text).ToArray();
                            Check(untranslated.Length == 0, "all interface captions translated: " + String.Join(", ", untranslated));
                            Check(!Cyrillic(Walk(window).OfType<ComboBox>().SelectMany(c => c.Items.Cast<object>()).Aggregate("", (a, b) => a + b)), "all dropdown items translated");
                        }
                        string name = "main-" + combination + ".png"; Render(window, Path.Combine(output, name)); snapshots.Add(name);
                        using (SettingsDialog dialog = new SettingsDialog(language, theme))
                        {
                            Check(dialog.SelectedLanguage == language && dialog.SelectedTheme == theme, "settings selection " + combination);
                            Check(((Button)dialog.AcceptButton).BackColor == ThemeColors.For(theme).Accent, "settings save color " + combination);
                            name = "settings-" + combination + ".png"; Render(dialog, Path.Combine(output, name)); snapshots.Add(name);
                        }
                    }
                    using (SettingsDialog dialog = new SettingsDialog("en", "dark"))
                    {
                        ComboBox languageBox = Walk(dialog).OfType<ComboBox>().Single(c => c.Items.Contains("English"));
                        languageBox.SelectedIndex = 0;
                        Check(dialog.Text == "Настройки" && dialog.SelectedTheme == "dark", "settings live language change retains theme");
                        ComboBox themeBox = Walk(dialog).OfType<ComboBox>().Single(c => c.Items.Contains("Тёмная"));
                        themeBox.SelectedIndex = 0;
                        Check(dialog.BackColor == ThemeColors.For("light").Background, "settings live theme preview");
                        dialog.DialogResult = DialogResult.Cancel;
                    }
                    Check(new DocumentStore(store.DirectoryPath).State.Language == "en" && new DocumentStore(store.DirectoryPath).State.Theme == "dark", "cancel leaves saved preferences unchanged");
                }
                using (MainWindow restored = new MainWindow(root, store.DirectoryPath))
                {
                    Check(restored.Text == "Mustek Studio — document scanning" && restored.BackColor == ThemeColors.For("dark").Background, "language and theme restored after restart");
                }
                string legacy = Path.Combine(output, "legacy"); Directory.CreateDirectory(legacy);
                File.WriteAllText(Path.Combine(legacy, "document.xml"), "<DocumentState><ScanDpi>150</ScanDpi><Pages /></DocumentState>");
                DocumentStore old = new DocumentStore(legacy);
                Check(old.State.Language == "ru" && old.State.Theme == "light", "old documents retain default language and theme");
                old.State.Language = "en"; old.State.Theme = "dark"; old.Save();
                using (MainWindow empty = new MainWindow(root, legacy))
                {
                    Render(empty, Path.Combine(output, "empty-en-dark.png"));
                    var operation = new System.Threading.Tasks.TaskCompletionSource<bool>();
                    var work = typeof(MainWindow).GetMethod("Work", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var running = (System.Threading.Tasks.Task)work.Invoke(empty, new object[] {
                        "Scanning page · 1200 dpi", new Func<System.Threading.Tasks.Task>(() => operation.Task) });
                    Application.DoEvents();
                    Check(Walk(empty).OfType<ComboBox>().All(c => !c.Enabled), "scan controls locked during operation");
                    ListView pageViewer = Walk(empty).OfType<ListView>().Single();
                    Check(pageViewer.BackColor == ThemeColors.For("dark").Surface && pageViewer.Enabled, "page viewer retains dark background while scanning");
                    Render(empty, Path.Combine(output, "busy-marquee-en-dark.png"));
                    ThemedProgressBar indicator = Walk(empty).OfType<ThemedProgressBar>().Single();
                    indicator.Style = ProgressBarStyle.Continuous; indicator.Value = 37;
                    Render(empty, Path.Combine(output, "busy-progress-en-dark.png"));
                    using (Bitmap bar = new Bitmap(indicator.Width, indicator.Height))
                    {
                        indicator.DrawToBitmap(bar, new Rectangle(Point.Empty, bar.Size));
                        Check(bar.GetPixel(4, bar.Height / 2).ToArgb() == ThemeColors.For("dark").Accent.ToArgb() &&
                              bar.GetPixel(bar.Width - 4, bar.Height / 2).ToArgb() == ThemeColors.For("dark").Surface.ToArgb(),
                              "progress fill purple and track dark");
                    }
                    foreach (ComboBox combo in Walk(empty).OfType<ComboBox>())
                    using (Bitmap image = new Bitmap(combo.Width, combo.Height))
                    {
                        combo.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                        Check(image.GetPixel(combo.Width - 25, image.Height / 2).ToArgb() == ThemeColors.For("dark").Surface.ToArgb(),
                              "disabled dropdown retains dark fill: " + combo.AccessibleName);
                    }
                    operation.SetResult(true);
                    DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                    while (!running.IsCompleted && DateTime.UtcNow < deadline) Application.DoEvents();
                    Check(running.IsCompleted && !indicator.Visible && Walk(empty).OfType<Button>().Single(b => b.Text == "Preview").Enabled,
                          "idle controls restored after operation");
                }
                UiText.Language = "en";
                try { new ScannerEngine(root, output).Scan("unused.png", 123, "Color", false, null); throw new Exception("Expected validation error"); }
                catch (ArgumentException error) { Check(error.Message == "Unsupported resolution.", "scanner validation messages localized"); }
                File.WriteAllText(Path.Combine(output, "result.txt"), "Appearance checks passed.\r\n" + String.Join("\r\n", snapshots), Encoding.UTF8);
                Console.WriteLine("All appearance tests passed: " + output); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
