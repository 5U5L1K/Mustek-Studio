using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MustekStudio
{
    internal static class Program
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
        [STAThread]
        static void Main(string[] args)
        {
            SetProcessDPIAware();
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
#if PORTABLE
            try
            {
                PortablePackage.Initialize();
                if (args.Contains("--portable-check"))
                {
                    PortableChecks.Run(args.Contains("--probe-scanner")); return;
                }
                Run(args, PortablePackage.EngineRoot, PortablePackage.DataRoot);
            }
            catch (Exception error)
            {
                PortablePackage.WriteFailure(error);
                if (args.Contains("--portable-check")) Environment.ExitCode = 1;
                else MessageBox.Show(error.Message, "Mustek Studio Portable", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#else
            string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
            Run(args, root, root);
#endif
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void Run(string[] args, string root, string userRoot)
        {
            string data = Path.Combine(userRoot, "app-data");
            if (args.Length == 2 && args[0] == "--data-dir") data = Path.GetFullPath(args[1]);
            UiText.Language = UiText.ReadSavedLanguage(data);
            bool first;
            using (Mutex single = new Mutex(true, "Local\\MustekStudio-" + data.ToUpperInvariant().GetHashCode().ToString("X8"), out first))
            {
                if (!first) { MessageBox.Show(UiText.Get("Приложение уже запущено. Переключитесь на окно Mustek Studio."), "Mustek Studio"); return; }
                try { Application.Run(new MainWindow(root, data, Path.Combine(userRoot, "scans"))); }
                catch (Exception ex) { MessageBox.Show(ex.Message, UiText.Get("Не удалось открыть Mustek Studio"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
                finally { single.ReleaseMutex(); }
            }
        }
    }

    public sealed class PreviewCanvas : Control
    {
        Image image;
        double zoom;
        PointF pan;
        Point dragStart;
        PointF dragPan;
        public Action<string> ZoomChanged;
        public Color EmptyTitleColor = Color.FromArgb(69, 85, 105);
        public Color EmptyHintColor = Color.FromArgb(108, 122, 139);
        public Size ImageSize { get { return image == null ? Size.Empty : image.Size; } }
        public PreviewCanvas()
        {
            DoubleBuffered = true; BackColor = Color.FromArgb(236, 240, 244);
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true); TabStop = true;
        }
        public void SetImage(Image next)
        {
            Image previous = image; image = next; if (previous != null) previous.Dispose(); Fit();
        }
        double FitScale { get { return image == null ? 1 : Math.Max(.01, Math.Min((Width - 48.0) / image.Width, (Height - 48.0) / image.Height)); } }
        double ScaleValue { get { return zoom == 0 ? FitScale : zoom; } }
        public void Fit() { zoom = 0; pan = PointF.Empty; Invalidate(); NotifyZoom(); }
        public void ActualSize() { zoom = 1; pan = PointF.Empty; Invalidate(); NotifyZoom(); }
        public void ChangeZoom(double multiplier)
        { zoom = Math.Max(.03, Math.Min(8, ScaleValue * multiplier)); Invalidate(); NotifyZoom(); }
        void NotifyZoom() { if (ZoomChanged != null) ZoomChanged(image == null ? "" : Math.Round(ScaleValue * 100) + " %"); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); NotifyZoom(); }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); ChangeZoom(e.Delta > 0 ? 1.2 : 1 / 1.2); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); Focus();
            if (e.Button == MouseButtons.Left && image != null) { Capture = true; dragStart = e.Location; dragPan = pan; Cursor = Cursors.SizeAll; }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Capture) { pan = new PointF(dragPan.X + e.X - dragStart.X, dragPan.Y + e.Y - dragStart.Y); Invalidate(); }
        }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); Capture = false; Cursor = Cursors.Default; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g = e.Graphics;
            if (image == null)
            {
                Rectangle area = new Rectangle(24, Math.Max(20, Height / 2 - 60), Math.Max(10, Width - 48), 52);
                using (Font title = new Font("Segoe UI", 18, FontStyle.Regular))
                    TextRenderer.DrawText(g, UiText.Get("Документ начинается со скана"), title, area, EmptyTitleColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                area.Y += 64; area.Height = 60;
                TextRenderer.DrawText(g, UiText.Get("Проверьте лист через предпросмотр, затем сканируйте страницу.\nМожно также перетащить сюда готовые изображения."), Font, area, EmptyHintColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
                return;
            }
            double scale = ScaleValue;
            float w = (float)(image.Width * scale), h = (float)(image.Height * scale);
            float x = (Width - w) / 2 + pan.X, y = (Height - h) / 2 + pan.Y;
            using (Brush shadow = new SolidBrush(Color.FromArgb(45, 50, 65, 80))) g.FillRectangle(shadow, x + 5, y + 6, w, h);
            g.FillRectangle(Brushes.White, x, y, w, h);
            g.InterpolationMode = scale < 1 ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(image, new RectangleF(x, y, w, h));
        }
        protected override void Dispose(bool disposing) { if (disposing && image != null) image.Dispose(); base.Dispose(disposing); }
    }

    public sealed class MainWindow : Form
    {
        readonly string root, data;
        readonly DocumentStore store;
        readonly ScannerEngine engine;
        readonly List<Control> interactive = new List<Control>();
        readonly HashSet<Button> primaryButtons = new HashSet<Button>();
        readonly HashSet<Control> mutedLabels = new HashSet<Control>();
        readonly Dictionary<Control, string> textKeys = new Dictionary<Control, string>();
        readonly Dictionary<Control, string> accessibleKeys = new Dictionary<Control, string>();
        readonly Dictionary<ComboBox, string[]> itemKeys = new Dictionary<ComboBox, string[]>();
        ThemeColors colors = ThemeColors.For("light");
        readonly Dictionary<string, Image> thumbCache = new Dictionary<string, Image>();
        readonly PreviewCanvas canvas = new PreviewCanvas();
        readonly ListView pages = new ListView();
        readonly ImageList thumbnails = new ImageList();
        readonly Label status = new Label(), imageTitle = new Label(), counter = new Label(), zoomLabel = new Label();
        readonly ThemedProgressBar progress = new ThemedProgressBar();
        readonly Icon applicationIcon;
        readonly ComboBox scanDpi = new ThemedComboBox(), previewDpi = new ThemedComboBox(), mode = new ThemedComboBox();
        readonly ComboBox format = new ThemedComboBox(), scope = new ThemedComboBox(), paper = new ThemedComboBox();
        readonly Button scan = new ThemedButton(), preview = new ThemedButton(), save = new ThemedButton();
        readonly Button rotate = new ThemedButton(), up = new ThemedButton(), down = new ThemedButton(), remove = new ThemedButton();
        readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer();
        bool busy, refreshing, showingPreview;
        int displayedPreviewDpi;
        DateTime started;
        string busyText, lastOutputDirectory;
        readonly Color ink = Color.FromArgb(31, 45, 61), accent = Color.FromArgb(16, 112, 118);

        public MainWindow(string projectRoot, string dataDirectory) : this(projectRoot, dataDirectory, null) { }
        public MainWindow(string projectRoot, string dataDirectory, string outputDirectory)
        {
            root = projectRoot; data = dataDirectory;
            store = new DocumentStore(data);
            store.State.Language = store.State.Language == "en" ? "en" : "ru";
            store.State.Theme = store.State.Theme == "dark" ? "dark" : "light";
            UiText.Language = store.State.Language;
            engine = new ScannerEngine(root, Path.Combine(data, "logs"));
            Directory.CreateDirectory(Path.Combine(data, "cache"));
            lastOutputDirectory = outputDirectory ?? Path.Combine(root, "scans"); Directory.CreateDirectory(lastOutputDirectory);
            Text = UiText.Get("Mustek Studio — сканирование документов");
            using (Stream iconStream = typeof(MainWindow).Assembly.GetManifestResourceStream("MustekStudio.AppIcon"))
                if (iconStream != null)
                    using (Icon loadedIcon = new Icon(iconStream))
                    {
                        applicationIcon = (Icon)loadedIcon.Clone(); Icon = applicationIcon;
                    }
            Font = new Font("Segoe UI", 9.5F); BackColor = Color.FromArgb(245, 247, 250); ForeColor = ink;
            AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(1200, 800); MinimumSize = new Size(1000, 730);
            StartPosition = FormStartPosition.CenterScreen;
            BuildInterface(); LoadSettings(); CaptureTextKeys(this); ApplyAppearance(); RefreshPages(store.State.Pages.Count - 1);
            clock.Interval = 500; clock.Tick += delegate { if (busy) status.Text = busyText + " · " + (int)(DateTime.Now - started).TotalSeconds + UiText.Get(" с"); };
            FormClosing += OnClosing;
            Shown += delegate { if (store.State.Pages.Count > 0) SetStatus(UiText.Get("Восстановлен документ: ") + store.State.Pages.Count + UiText.Get(" стр.")); };
        }

        Label LabelFor(string text)
        { return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = Color.FromArgb(92, 108, 126), Margin = new Padding(0, 0, 0, 4) }; }
        void SetupCombo(ComboBox combo, string name, params string[] items)
        {
            combo.Name = name; combo.AccessibleName = name; combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Dock = DockStyle.Fill; combo.Items.AddRange(items); combo.SelectedIndex = 0;
            combo.Margin = new Padding(0, 2, 0, 4); interactive.Add(combo);
            ThemeColors.StyleCombo(combo, () => colors);
        }
        void SetupButton(Button button, string text, bool primary)
        {
            button.Text = text; button.AccessibleName = text; button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(211, 221, 230);
            button.BackColor = primary ? accent : Color.White; button.ForeColor = primary ? Color.White : ink;
            button.Cursor = Cursors.Hand; button.Margin = new Padding(0, 4, 0, 4); interactive.Add(button);
            if (primary) primaryButtons.Add(button);
        }
        Button SmallButton(string text, EventHandler action)
        {
            Button button = new ThemedButton(); SetupButton(button, text, false);
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink; button.AutoSize = true; button.Padding = new Padding(5, 0, 5, 0);
            button.MinimumSize = new Size(27, 29); button.Dock = DockStyle.None; button.Height = 29;
            button.Margin = new Padding(0, 1, 6, 1); button.Click += action; return button;
        }
        void BuildInterface()
        {
            TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 12), ColumnCount = 2, RowCount = 3 };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280)); shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 66)); shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            Controls.Add(shell);
            Panel header = new Panel { Dock = DockStyle.Fill };
            Label brand = new Label { Text = "Mustek Studio", Font = new Font("Segoe UI", 23, FontStyle.Bold), ForeColor = ink, AutoSize = true, Location = new Point(0, 0) };
            Label subtitle = new Label { Text = UiText.Get("1200 UB Plus  /  Сканирование документов"), ForeColor = Color.FromArgb(98, 115, 134), AutoSize = true, Location = new Point(3, 43) };
            header.Controls.Add(brand); header.Controls.Add(subtitle); shell.Controls.Add(header, 0, 0); shell.SetColumnSpan(header, 2);
            Button preferences = new ThemedButton(); SetupButton(preferences, UiText.Get("Настройки…"), false);
            preferences.Dock = DockStyle.None; preferences.Size = new Size(150, 34); preferences.Top = 9;
            header.Controls.Add(preferences);
            header.Resize += delegate { preferences.Left = Math.Max(360, header.ClientSize.Width - preferences.Width); };
            preferences.Left = Math.Max(360, header.ClientSize.Width - preferences.Width);
            preferences.Click += delegate
            {
                using (SettingsDialog dialog = new SettingsDialog(store.State.Language, store.State.Theme))
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                        Edit(delegate { ApplyPreferences(dialog.SelectedLanguage, dialog.SelectedTheme); });
            };

            Panel settingsScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
            TableLayoutPanel settings = new TableLayoutPanel { Dock = DockStyle.Top, Height = 654, ColumnCount = 1, RowCount = 20, Padding = new Padding(0, 4, 20, 0), Margin = Padding.Empty };
            int[] heights = { 24, 34, 24, 34, 24, 34, 45, 45, 30, 26, 34, 24, 34, 24, 34, 46, 44, 34, 40 };
            foreach (int height in heights) settings.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            settings.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); settingsScroll.Controls.Add(settings); shell.Controls.Add(settingsScroll, 0, 1);
            SetupCombo(previewDpi, UiText.Get("Разрешение предпросмотра"), "50 dpi", "75 dpi", "150 dpi", "300 dpi", "600 dpi", "1200 dpi");
            SetupCombo(scanDpi, UiText.Get("Разрешение сканирования"), "50 dpi", "75 dpi", "150 dpi", "300 dpi", "600 dpi", "1200 dpi");
            SetupCombo(mode, UiText.Get("Цветовой режим"), UiText.Get("Цветной"), UiText.Get("Оттенки серого"));
            settings.Controls.Add(LabelFor(UiText.Get("РАЗРЕШЕНИЕ ПРЕДПРОСМОТРА")), 0, 0); settings.Controls.Add(previewDpi, 0, 1);
            settings.Controls.Add(LabelFor(UiText.Get("РАЗРЕШЕНИЕ СКАНИРОВАНИЯ")), 0, 2); settings.Controls.Add(scanDpi, 0, 3);
            settings.Controls.Add(LabelFor(UiText.Get("ЦВЕТОВОЙ РЕЖИМ")), 0, 4); settings.Controls.Add(mode, 0, 5);
            SetupButton(preview, UiText.Get("Предпросмотр"), false); SetupButton(scan, UiText.Get("Сканировать страницу"), true);
            preview.Click += async delegate { await CapturePage(true); }; scan.Click += async delegate { await CapturePage(false); };
            settings.Controls.Add(preview, 0, 6); settings.Controls.Add(scan, 0, 7);
            Label hint = new Label { Text = UiText.Get("Предпросмотр не добавляется в PDF."), ForeColor = Color.FromArgb(106, 120, 137), Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5F), Padding = new Padding(0, 4, 0, 0) };
            settings.Controls.Add(hint, 0, 8);
            settings.Controls.Add(LabelFor(UiText.Get("СОХРАНЕНИЕ")), 0, 9);
            SetupCombo(format, UiText.Get("Формат файла"), "PDF", "PNG"); SetupCombo(scope, UiText.Get("Страницы для сохранения"), UiText.Get("Все страницы"), UiText.Get("Выбранная страница"));
            SetupCombo(paper, UiText.Get("Размер страницы PDF"), UiText.Get("A4 · поля 5 мм"), UiText.Get("По размеру скана"));
            settings.Controls.Add(format, 0, 10); settings.Controls.Add(LabelFor(UiText.Get("СТРАНИЦЫ")), 0, 11); settings.Controls.Add(scope, 0, 12);
            settings.Controls.Add(LabelFor(UiText.Get("РАЗМЕР СТРАНИЦЫ PDF")), 0, 13); settings.Controls.Add(paper, 0, 14);
            SetupButton(save, UiText.Get("Сохранить PDF"), true); save.Click += async delegate { await ExportDocument(); }; settings.Controls.Add(save, 0, 15);
            format.SelectedIndexChanged += delegate { UpdateButtons(); };
            Button import = new ThemedButton(); SetupButton(import, UiText.Get("Добавить изображения…"), false);
            import.Click += async delegate
            {
                using (OpenFileDialog dialog = new OpenFileDialog { Title = UiText.Get("Добавить страницы из изображений"), Multiselect = true, Filter = UiText.Get("Изображения|*.png;*.jpg;*.jpeg;*.bmp") })
                    if (dialog.ShowDialog(this) == DialogResult.OK) await ImportFiles(dialog.FileNames);
            };
            settings.Controls.Add(import, 0, 16);
            Button check = new ThemedButton(); SetupButton(check, UiText.Get("Проверить сканер"), false);
            check.Click += async delegate { await Work(UiText.Get("Проверка подключения"), async delegate { string name = await Task.Run(() => engine.FindDevice()); SetStatus(UiText.Get("Сканер подключён · ") + name); }); };
            settings.Controls.Add(check, 0, 17);
            Button folder = new ThemedButton(); SetupButton(folder, UiText.Get("Открыть папку сканов"), false);
            folder.Click += delegate { System.Diagnostics.Process.Start("explorer.exe", ScannerEngine.Quote(lastOutputDirectory)); }; settings.Controls.Add(folder, 0, 18);

            TableLayoutPanel right = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, ColumnCount = 1, RowCount = 4 };
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 148)); shell.Controls.Add(right, 1, 1);
            TableLayoutPanel imageHeader = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            imageHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            imageHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); imageHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
            imageTitle.Dock = DockStyle.Fill; imageTitle.Margin = Padding.Empty; imageTitle.Text = UiText.Get("Область просмотра"); imageTitle.TextAlign = ContentAlignment.MiddleLeft; imageTitle.AutoEllipsis = true;
            imageHeader.Controls.Add(imageTitle, 0, 0);
            FlowLayoutPanel zoomControls = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            Button fit = SmallButton(UiText.Get("Вписать"), delegate { canvas.Fit(); }); Button actual = SmallButton("100%", delegate { canvas.ActualSize(); });
            Button plus = SmallButton("+", delegate { canvas.ChangeZoom(1.25); }); Button minus = SmallButton("−", delegate { canvas.ChangeZoom(.8); });
            foreach (Button b in new Button[] { plus, minus, actual, fit }) interactive.Remove(b);
            zoomLabel.AutoSize = false; zoomLabel.Size = new Size(45, 28); zoomLabel.TextAlign = ContentAlignment.MiddleCenter;
            zoomControls.Controls.Add(fit); zoomControls.Controls.Add(actual); zoomControls.Controls.Add(plus); zoomControls.Controls.Add(minus); zoomControls.Controls.Add(zoomLabel);
            imageHeader.Controls.Add(zoomControls, 1, 0); right.Controls.Add(imageHeader, 0, 0);
            canvas.Dock = DockStyle.Fill; canvas.Margin = Padding.Empty; canvas.Font = Font;
            canvas.ZoomChanged = delegate(string value) { zoomLabel.Text = value; };
            canvas.AllowDrop = true; canvas.DragEnter += delegate(object sender, DragEventArgs e) { e.Effect = !busy && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; };
            canvas.DragDrop += async delegate(object sender, DragEventArgs e) { if (!busy) await ImportFiles((string[])e.Data.GetData(DataFormats.FileDrop)); };
            right.Controls.Add(canvas, 0, 1);
            FlowLayoutPanel editing = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
            counter.AutoSize = false; counter.Size = new Size(136, 28); counter.TextAlign = ContentAlignment.MiddleLeft; editing.Controls.Add(counter);
            foreach (Button b in new Button[] { up, down, rotate, remove }) { SetupButton(b, "", false); b.Dock = DockStyle.None; b.Height = 29; b.AutoSize = true; b.Margin = new Padding(0, 0, 6, 0); editing.Controls.Add(b); }
            up.Text = UiText.Get("← Раньше"); down.Text = UiText.Get("Позже →"); rotate.Text = UiText.Get("Повернуть 90°"); remove.Text = UiText.Get("Убрать");
            foreach (Button b in new Button[] { up, down, rotate, remove }) b.AccessibleName = b.Text;
            up.Click += delegate { Edit(delegate { RefreshPages(store.Move(SelectedIndex, -1)); }); };
            down.Click += delegate { Edit(delegate { RefreshPages(store.Move(SelectedIndex, 1)); }); };
            rotate.Click += delegate { Edit(delegate { int i = SelectedIndex; store.Rotate(i); RefreshPages(i); }); };
            remove.Click += delegate { Edit(delegate { int i = SelectedIndex; store.Remove(i); RefreshPages(Math.Min(i, store.State.Pages.Count - 1)); SetStatus(UiText.Get("Страница убрана из документа. Исходный файл сохранён.")); }); };
            right.Controls.Add(editing, 0, 2);
            thumbnails.ImageSize = new Size(104, 96); thumbnails.ColorDepth = ColorDepth.Depth32Bit;
            pages.Dock = DockStyle.Fill; pages.View = View.LargeIcon; pages.MultiSelect = false; pages.HideSelection = false;
            pages.BorderStyle = BorderStyle.None; pages.BackColor = Color.White; pages.LargeImageList = thumbnails; pages.Margin = Padding.Empty;
            pages.SelectedIndexChanged += delegate { if (!refreshing && SelectedIndex >= 0) ShowSelectedPage(); UpdateButtons(); };
            right.Controls.Add(pages, 0, 3);
            TableLayoutPanel footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = Padding.Empty };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            status.Dock = DockStyle.Fill; status.TextAlign = ContentAlignment.MiddleLeft; status.AutoEllipsis = true; status.Text = UiText.Get("Готово · добавьте страницу или выполните предпросмотр");
            progress.Dock = DockStyle.Fill; progress.Margin = new Padding(5, 12, 0, 4); progress.Visible = false;
            progress.AccessibleName = UiText.Get("Ход выполнения");
            AttributionLabel attribution = new AttributionLabel {
                Name = "attribution", Text = UiText.Get("@Навайбкодил 5U5L1K"),
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5F), ForeColor = ink,
                Margin = new Padding(12, 0, 0, 0) };
            footer.Controls.Add(status, 0, 0); footer.Controls.Add(progress, 1, 0); footer.Controls.Add(attribution, 2, 0);
            shell.Controls.Add(footer, 0, 2); shell.SetColumnSpan(footer, 2);
        }

        int SelectedIndex { get { return pages.SelectedIndices.Count == 0 ? -1 : pages.SelectedIndices[0]; } }
        void CaptureTextKeys(Control control)
        {
            if ((control is Form || control is Label || control is Button) && !String.IsNullOrEmpty(control.Text))
                textKeys[control] = UiText.KeyFor(control.Text);
            if (!String.IsNullOrEmpty(control.AccessibleName)) accessibleKeys[control] = UiText.KeyFor(control.AccessibleName);
            ComboBox combo = control as ComboBox;
            if (combo != null) itemKeys[combo] = combo.Items.Cast<object>().Select(v => UiText.KeyFor(v.ToString())).ToArray();
            if (control is Label && control.ForeColor != ink) mutedLabels.Add(control);
            foreach (Control child in control.Controls) CaptureTextKeys(child);
        }
        void ApplyAppearance()
        {
            SuspendLayout();
            try
            {
                UiText.Language = store.State.Language;
                foreach (KeyValuePair<Control, string> entry in textKeys) entry.Key.Text = UiText.Get(entry.Value);
                foreach (KeyValuePair<Control, string> entry in accessibleKeys) entry.Key.AccessibleName = UiText.Get(entry.Value);
                foreach (KeyValuePair<ComboBox, string[]> entry in itemKeys)
                {
                    int index = entry.Key.SelectedIndex;
                    entry.Key.BeginUpdate();
                    for (int i = 0; i < entry.Value.Length; i++) entry.Key.Items[i] = UiText.Get(entry.Value[i]);
                    entry.Key.SelectedIndex = index; entry.Key.EndUpdate();
                }
                colors = ThemeColors.For(store.State.Theme); colors.Apply(this, primaryButtons, mutedLabels);
                UpdateButtons(); UpdateImageTitle(); canvas.Invalidate();
            }
            finally { ResumeLayout(true); }
        }
        public void ApplyPreferences(string language, string theme)
        {
            if (busy) throw new InvalidOperationException(UiText.Get("Дождитесь завершения операции перед изменением настроек."));
            if (language != "ru" && language != "en") throw new ArgumentException("Unsupported language");
            if (theme != "light" && theme != "dark") throw new ArgumentException("Unsupported theme");
            string oldLanguage = store.State.Language, oldTheme = store.State.Theme;
            store.State.Language = language; store.State.Theme = theme;
            try { SaveSettings(); }
            catch { store.State.Language = oldLanguage; store.State.Theme = oldTheme; throw; }
            ApplyAppearance(); SetStatus(UiText.Get("Настройки сохранены."));
        }
        void UpdateImageTitle()
        {
            if (showingPreview)
                imageTitle.Text = UiText.Get("Предпросмотр · ") + displayedPreviewDpi + UiText.Get(" dpi · не добавлен в документ");
            else if (SelectedIndex >= 0 && canvas.ImageSize != Size.Empty)
                imageTitle.Text = String.Format(UiText.Get("Страница {0} · {1} dpi · {2} × {3}"), SelectedIndex + 1,
                    store.State.Pages[SelectedIndex].Dpi, canvas.ImageSize.Width, canvas.ImageSize.Height);
            else imageTitle.Text = UiText.Get("Область просмотра");
        }
        int ReadDpi(ComboBox combo) { return Int32.Parse(combo.Text.Split(' ')[0], CultureInfo.InvariantCulture); }
        void LoadSettings()
        {
            string scanValue = store.State.ScanDpi + " dpi", previewValue = store.State.PreviewDpi + " dpi";
            scanDpi.SelectedItem = scanDpi.Items.Contains(scanValue) ? scanValue : "150 dpi";
            previewDpi.SelectedItem = previewDpi.Items.Contains(previewValue) ? previewValue : "75 dpi";
            mode.SelectedIndex = store.State.Mode == "Gray" ? 1 : 0;
            format.SelectedIndex = store.State.Format == "PNG" ? 1 : 0; paper.SelectedIndex = store.State.A4 ? 0 : 1;
        }
        void SaveSettings()
        {
            store.State.ScanDpi = ReadDpi(scanDpi); store.State.PreviewDpi = ReadDpi(previewDpi);
            store.State.Mode = mode.SelectedIndex == 0 ? "Color" : "Gray"; store.State.Format = format.Text; store.State.A4 = paper.SelectedIndex == 0; store.Save();
        }
        void SetStatus(string text) { status.Text = text; }
        void UpdateButtons()
        {
            bool selected = SelectedIndex >= 0;
            rotate.Enabled = remove.Enabled = !busy && selected;
            up.Enabled = !busy && selected && SelectedIndex > 0;
            down.Enabled = !busy && selected && SelectedIndex < store.State.Pages.Count - 1;
            save.Enabled = !busy && store.State.Pages.Count > 0;
            save.Text = format.SelectedIndex == 1 ? UiText.Get("Сохранить PNG…") : UiText.Get("Сохранить PDF…");
            paper.Enabled = !busy && format.SelectedIndex == 0;
            counter.Text = UiText.Get("Страниц: ") + store.State.Pages.Count;
        }
        void RefreshPages(int selected)
        {
            refreshing = true; pages.BeginUpdate(); pages.Items.Clear(); thumbnails.Images.Clear();
            try
            {
                for (int i = 0; i < store.State.Pages.Count; i++)
                {
                    PageRecord record = store.State.Pages[i]; string key = record.FileName + ":" + record.Rotation;
                    Image thumb;
                    if (!thumbCache.TryGetValue(key, out thumb))
                    {
                        Bitmap bitmap = new Bitmap(104, 96, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        using (Bitmap source = store.ReadPage(record))
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.Clear(Color.Transparent); g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            double scale = Math.Min(94.0 / source.Width, 88.0 / source.Height);
                            int w = (int)(source.Width * scale), h = (int)(source.Height * scale);
                            g.DrawImage(source, (104 - w) / 2, (96 - h) / 2, w, h);
                        }
                        thumb = bitmap; thumbCache.Add(key, thumb);
                    }
                    thumbnails.Images.Add(thumb);
                    pages.Items.Add(new ListViewItem(String.Format("{0:00} · {1} dpi", i + 1, record.Dpi), i) { ToolTipText = record.Label });
                }
                if (selected >= 0 && selected < pages.Items.Count) { pages.Items[selected].Selected = true; pages.Items[selected].Focused = true; pages.EnsureVisible(selected); }
            }
            finally { pages.EndUpdate(); refreshing = false; }
            if (selected >= 0 && selected < pages.Items.Count) ShowSelectedPage();
            else if (!showingPreview) { canvas.SetImage(null); imageTitle.Text = UiText.Get("Область просмотра"); }
            UpdateButtons();
        }
        void ShowSelectedPage()
        {
            int index = SelectedIndex; if (index < 0) return;
            PageRecord page = store.State.Pages[index]; Bitmap image = store.ReadPage(page); showingPreview = false;
            imageTitle.Text = String.Format(UiText.Get("Страница {0} · {1} dpi · {2} × {3}"), index + 1, page.Dpi, image.Width, image.Height);
            canvas.SetImage(image);
        }
        void Edit(Action action) { if (busy) return; try { action(); } catch (Exception e) { ShowError(e); } }
        void ShowError(Exception ex)
        {
            SetStatus(UiText.Get("Ошибка · подробности в сообщении"));
            try { File.AppendAllText(Path.Combine(data, "application.log"), DateTime.Now + " " + ex + Environment.NewLine); } catch { }
            MessageBox.Show(this, ex.Message, "Mustek Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        async Task Work(string text, Func<Task> action)
        {
            if (busy) return; busy = true; busyText = text; started = DateTime.Now;
            foreach (Control control in interactive) control.Enabled = false;
            // Keep the page viewer readable while document-editing buttons
            // are disabled. A disabled native ListView uses a system-white fill.
            progress.Visible = true; progress.Style = ProgressBarStyle.Marquee; SetStatus(text); clock.Start();
            try { SaveSettings(); await action(); }
            catch (Exception ex) { ShowError(ex); }
            finally
            {
                busy = false; clock.Stop(); progress.Visible = false;
                foreach (Control control in interactive) control.Enabled = true;
                pages.Enabled = true; UpdateButtons();
            }
        }
        void ReportProgress(int value)
        {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke((Action)delegate { if (busy) { progress.Style = ProgressBarStyle.Continuous; progress.Value = value; } });
        }
        async Task CapturePage(bool isPreview)
        {
            int dpi = ReadDpi(isPreview ? previewDpi : scanDpi); string color = mode.SelectedIndex == 0 ? "Color" : "Gray";
            await Work((isPreview ? UiText.Get("Предпросмотр") : UiText.Get("Сканирование страницы")) + " · " + dpi + " dpi", async delegate
            {
                string incoming = Path.Combine(data, "cache", Guid.NewGuid().ToString("N") + ".png");
                await Task.Run(() => engine.Scan(incoming, dpi, color, isPreview, ReportProgress));
                if (isPreview)
                {
                    pages.SelectedIndices.Clear(); showingPreview = true; displayedPreviewDpi = dpi;
                    Bitmap image = FileTools.ReadImage(incoming); canvas.SetImage(image);
                    imageTitle.Text = UiText.Get("Предпросмотр · ") + dpi + UiText.Get(" dpi · не добавлен в документ");
                    SetStatus(UiText.Get("Предпросмотр готов. Для добавления страницы нажмите «Сканировать страницу»."));
                }
                else
                {
                    PageRecord record = await Task.Run(() => store.AddFile(incoming, dpi));
                    record.Label = UiText.Get("Скан ") + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"); store.Save();
                    RefreshPages(store.State.Pages.Count - 1); SetStatus(UiText.Get("Страница добавлена. Замените лист и сканируйте следующую."));
                }
                File.Delete(incoming);
            });
        }
        async Task ImportFiles(string[] files)
        {
            if (files == null || files.Length == 0) return;
            await Work(UiText.Get("Добавление изображений"), async delegate
            {
                try
                {
                    await Task.Run(delegate
                    {
                        foreach (string file in files)
                        {
                            string ext = Path.GetExtension(file).ToLowerInvariant();
                            if (!(new string[] { ".png", ".jpg", ".jpeg", ".bmp" }).Contains(ext))
                                throw new IOException(UiText.Get("Поддерживаются PNG, JPEG и BMP: ") + Path.GetFileName(file));
                            store.AddFile(file, 0);
                        }
                    });
                    SetStatus(UiText.Get("Изображения добавлены: ") + files.Length);
                }
                finally { RefreshPages(store.State.Pages.Count - 1); }
            });
        }
        async Task ExportDocument()
        {
            List<PageRecord> selected = scope.SelectedIndex == 1 && SelectedIndex >= 0
                ? new List<PageRecord> { store.State.Pages[SelectedIndex] } : store.State.Pages.ToList();
            if (scope.SelectedIndex == 1 && SelectedIndex < 0) { MessageBox.Show(this, UiText.Get("Выберите страницу внизу окна.")); return; }
            if (selected.Count == 0) return;
            bool pdf = format.SelectedIndex == 0, a4 = paper.SelectedIndex == 0;
            string output;
            if (pdf || selected.Count == 1)
            {
                using (SaveFileDialog dialog = new SaveFileDialog { Title = pdf ? UiText.Get("Сохранить документ PDF") : UiText.Get("Сохранить страницу PNG"), InitialDirectory = lastOutputDirectory,
                    Filter = pdf ? UiText.Get("Документ PDF|*.pdf") : UiText.Get("Изображение PNG|*.png"), DefaultExt = pdf ? "pdf" : "png", AddExtension = true,
                    FileName = UiText.Get("Документ-") + DateTime.Now.ToString("yyyyMMdd-HHmmss") + (pdf ? ".pdf" : ".png") })
                { if (dialog.ShowDialog(this) != DialogResult.OK) return; output = dialog.FileName; }
                lastOutputDirectory = Path.GetDirectoryName(output);
            }
            else
            {
                using (FolderBrowserDialog dialog = new FolderBrowserDialog { Description = UiText.Get("Выберите папку: каждая страница будет отдельным PNG"), SelectedPath = lastOutputDirectory })
                { if (dialog.ShowDialog(this) != DialogResult.OK) return; output = dialog.SelectedPath; }
                lastOutputDirectory = output;
            }
            await Work(UiText.Get("Сохранение ") + (pdf ? "PDF" : "PNG"), async delegate
            {
                await Task.Run(delegate
                {
                    if (pdf) DocumentExporter.SavePdf(store, selected, output, a4);
                    else if (selected.Count == 1) DocumentExporter.SavePng(store, selected[0], output);
                    else
                    {
                        string prefix = UiText.Get("Скан-") + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-";
                        for (int i = 0; i < selected.Count; i++)
                        {
                            string path = Path.Combine(output, prefix + (i + 1).ToString("D3") + ".png");
                            if (File.Exists(path)) throw new IOException(UiText.Get("Файл уже существует: ") + path);
                            DocumentExporter.SavePng(store, selected[i], path);
                        }
                    }
                });
                SetStatus(UiText.Get("Сохранено: ") + selected.Count + UiText.Get(" стр. · ") + output);
            });
        }
        void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (busy)
            {
                e.Cancel = true; MessageBox.Show(this, UiText.Get("Дождитесь завершения текущей операции. Страницы сохраняются автоматически."), UiText.Get("Сканирование выполняется")); return;
            }
            try { SaveSettings(); }
            catch (Exception ex) { e.Cancel = true; ShowError(ex); }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                clock.Dispose(); thumbnails.Dispose(); foreach (Image thumb in thumbCache.Values) thumb.Dispose();
            }
            base.Dispose(disposing);
            if (disposing && applicationIcon != null) applicationIcon.Dispose();
        }
    }
}
