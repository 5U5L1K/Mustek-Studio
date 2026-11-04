using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MustekStudio
{
    public static class UiText
    {
        public static string Language = "ru";
        public static string ReadSavedLanguage(string dataDirectory)
        {
            try
            {
                string path = System.IO.Path.Combine(dataDirectory, "document.xml");
                if (!System.IO.File.Exists(path)) return "ru";
                using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(path,
                    new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit }))
                    while (reader.Read())
                        if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "Language")
                            return reader.ReadElementContentAsString() == "en" ? "en" : "ru";
            }
            catch (System.IO.IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (System.Xml.XmlException) { }
            return "ru";
        }
        static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            {"Приложение уже запущено. Переключитесь на окно Mustek Studio.", "The application is already running. Switch to the Mustek Studio window."},
            {"Не удалось открыть Mustek Studio", "Could not open Mustek Studio"},
            {"Документ начинается со скана", "Your document starts with a scan"},
            {"Проверьте лист через предпросмотр, затем сканируйте страницу.\nМожно также перетащить сюда готовые изображения.", "Preview the sheet, then scan a page.\nYou can also drop existing images here."},
            {"Mustek Studio — сканирование документов", "Mustek Studio — document scanning"},
            {" с", " s"}, {"Восстановлен документ: ", "Restored document: "}, {" стр.", " pages"},
            {"1200 UB Plus  /  Сканирование документов", "1200 UB Plus  /  Document scanning"},
            {"Разрешение предпросмотра", "Preview resolution"}, {"Разрешение сканирования", "Scan resolution"},
            {"Цветовой режим", "Color mode"}, {"Цветной", "Color"}, {"Оттенки серого", "Grayscale"},
            {"РАЗРЕШЕНИЕ ПРЕДПРОСМОТРА", "PREVIEW RESOLUTION"}, {"РАЗРЕШЕНИЕ СКАНИРОВАНИЯ", "SCAN RESOLUTION"},
            {"ЦВЕТОВОЙ РЕЖИМ", "COLOR MODE"}, {"Предпросмотр", "Preview"}, {"Сканировать страницу", "Scan page"},
            {"Предпросмотр не добавляется в PDF.", "Preview is not added to the PDF."},
            {"СОХРАНЕНИЕ", "EXPORT"}, {"Формат файла", "File format"}, {"Страницы для сохранения", "Pages to export"},
            {"Все страницы", "All pages"}, {"Выбранная страница", "Selected page"},
            {"Размер страницы PDF", "PDF page size"}, {"A4 · поля 5 мм", "A4 · 5 mm margins"},
            {"По размеру скана", "Original scan size"}, {"СТРАНИЦЫ", "PAGES"},
            {"РАЗМЕР СТРАНИЦЫ PDF", "PDF PAGE SIZE"}, {"Сохранить PDF", "Save PDF"},
            {"Добавить изображения…", "Add images…"}, {"Добавить страницы из изображений", "Add pages from images"},
            {"Изображения|*.png;*.jpg;*.jpeg;*.bmp", "Images|*.png;*.jpg;*.jpeg;*.bmp"},
            {"Проверить сканер", "Check scanner"}, {"Проверка подключения", "Checking connection"},
            {"Сканер подключён · ", "Scanner connected · "}, {"Открыть папку сканов", "Open scans folder"},
            {"Область просмотра", "Preview area"}, {"Вписать", "Fit"}, {"← Раньше", "← Earlier"},
            {"Позже →", "Later →"}, {"Повернуть 90°", "Rotate 90°"}, {"Убрать", "Remove"},
            {"Страница убрана из документа. Исходный файл сохранён.", "Page removed from the document. The original file is preserved."},
            {"Готово · добавьте страницу или выполните предпросмотр", "Ready · add a page or start a preview"},
            {"@Навайбкодил 5U5L1K", "@Vibe-coded by 5U5L1K"},
            {"Сохранить PNG…", "Save PNG…"}, {"Сохранить PDF…", "Save PDF…"}, {"Страниц: ", "Pages: "},
            {"Страница {0} · {1} dpi · {2} × {3}", "Page {0} · {1} dpi · {2} × {3}"},
            {"Ошибка · подробности в сообщении", "Error · see the message for details"},
            {"Сканирование страницы", "Scanning page"}, {"Предпросмотр · ", "Preview · "},
            {" dpi · не добавлен в документ", " dpi · not added to document"},
            {"Предпросмотр готов. Для добавления страницы нажмите «Сканировать страницу».", "Preview ready. Click Scan page to add it to the document."},
            {"Скан ", "Scan "}, {"Страница добавлена. Замените лист и сканируйте следующую.", "Page added. Replace the sheet and scan the next page."},
            {"Добавление изображений", "Adding images"}, {"Поддерживаются PNG, JPEG и BMP: ", "Supported formats are PNG, JPEG and BMP: "},
            {"Изображения добавлены: ", "Images added: "}, {"Выберите страницу внизу окна.", "Select a page at the bottom of the window."},
            {"Сохранить документ PDF", "Save PDF document"}, {"Сохранить страницу PNG", "Save PNG page"},
            {"Документ PDF|*.pdf", "PDF document|*.pdf"}, {"Изображение PNG|*.png", "PNG image|*.png"},
            {"Документ-", "Document-"}, {"Выберите папку: каждая страница будет отдельным PNG", "Choose a folder: each page will be a separate PNG"},
            {"Сохранение ", "Saving "}, {"Скан-", "Scan-"}, {"Файл уже существует: ", "File already exists: "},
            {"Сохранено: ", "Saved: "}, {" стр. · ", " pages · "},
            {"Дождитесь завершения текущей операции. Страницы сохраняются автоматически.", "Wait for the current operation to finish. Pages are saved automatically."},
            {"Сканирование выполняется", "Scanning in progress"},
            {"Сканер не создал корректное PNG-изображение.", "The scanner did not produce a valid PNG image."},
            {"Некорректные параметры сохранённой страницы.", "Invalid saved page settings."},
            {"Не найдена сохранённая страница: ", "Saved page not found: "},
            {"Некорректное имя страницы в сохранённом документе.", "Invalid page filename in the saved document."},
            {"Добавьте хотя бы одну страницу.", "Add at least one page."},
            {"Не найдена прошивка SBfw.usb. Сохраните приложение в папке проекта.", "SBfw.usb firmware not found. Keep the application in the project folder."},
            {"Не найден модуль сканирования: ", "Scanner module not found: "},
            {"Не удалось выполнить сканирование (код ", "Scanning failed (code "}, {"\nЖурнал: ", "\nLog: "},
            {"Сканер не найден. Проверьте USB, питание и установку WinUSB для устройства 05D8:4002.", "Scanner not found. Check USB, power, and WinUSB for device 05D8:4002."},
            {"Найдено несколько сканеров. Подключите один Mustek 1200 UB Plus.", "Multiple scanners found. Connect one Mustek 1200 UB Plus."},
            {"Неподдерживаемое разрешение.", "Unsupported resolution."}, {"Неподдерживаемый цветовой режим.", "Unsupported color mode."},
            {"Файл сканирования уже существует.", "The scan output file already exists."},
            {"Настройки…", "Settings…"}, {"Настройки", "Settings"}, {"ЯЗЫК ИНТЕРФЕЙСА", "INTERFACE LANGUAGE"},
            {"ТЕМА ОФОРМЛЕНИЯ", "APPEARANCE"}, {"Светлая", "Light"}, {"Тёмная", "Dark"},
            {"Настройки применяются без перезапуска приложения.", "Settings take effect without restarting the application."},
            {"Отмена", "Cancel"}, {"Сохранить", "Save"}, {"Настройки сохранены.", "Settings saved."},
            {"Дождитесь завершения операции перед изменением настроек.", "Wait for the current operation before changing settings."},
            {"Ход выполнения", "Progress"}, {"Выполняется", "In progress"}
        };
        public static string Get(string russian) { return Get(russian, Language); }
        public static string Get(string russian, string language)
        {
            string value; return language == "en" && English.TryGetValue(russian, out value) ? value : russian;
        }
        public static string KeyFor(string value)
        {
            if (English.ContainsKey(value)) return value;
            foreach (KeyValuePair<string, string> entry in English) if (entry.Value == value) return entry.Key;
            return value;
        }
    }

    public sealed class ThemeColors
    {
        public bool IsDark;
        public Color Background, Surface, Canvas, Text, Muted, Border, Hover, Accent, AccentHover, Selection;
        public static ThemeColors For(string name)
        {
            return name == "dark" ? new ThemeColors {
                IsDark = true,
                Background = Color.FromArgb(23, 21, 31), Surface = Color.FromArgb(37, 33, 48),
                Canvas = Color.FromArgb(16, 15, 23), Text = Color.FromArgb(241, 237, 249),
                Muted = Color.FromArgb(181, 173, 198), Border = Color.FromArgb(81, 72, 95),
                Hover = Color.FromArgb(52, 44, 67), Accent = Color.FromArgb(124, 58, 237),
                AccentHover = Color.FromArgb(139, 92, 246), Selection = Color.FromArgb(76, 29, 149)
            } : new ThemeColors {
                Background = Color.FromArgb(245, 247, 250), Surface = Color.White,
                Canvas = Color.FromArgb(236, 240, 244), Text = Color.FromArgb(31, 45, 61),
                Muted = Color.FromArgb(92, 108, 126), Border = Color.FromArgb(211, 221, 230),
                Hover = Color.FromArgb(231, 238, 243), Accent = Color.FromArgb(16, 112, 118),
                AccentHover = Color.FromArgb(20, 130, 136), Selection = Color.FromArgb(16, 112, 118)
            };
        }
        public void Apply(Control control, HashSet<Button> primary, HashSet<Control> muted)
        {
            control.BackColor = Background; control.ForeColor = Text;
            Button button = control as Button;
            if (button != null)
            {
                bool main = primary.Contains(button);
                ThemedButton themed = button as ThemedButton;
                if (themed != null) themed.DisabledTextColor = Muted;
                button.UseVisualStyleBackColor = false;
                button.BackColor = main ? Accent : Surface; button.ForeColor = main ? Color.White : Text;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.MouseOverBackColor = main ? AccentHover : Hover;
                button.FlatAppearance.MouseDownBackColor = main ? Selection : Hover;
            }
            else if (control is ComboBox || control is ListView)
            {
                control.BackColor = Surface;
                ThemedComboBox combo = control as ThemedComboBox;
                if (combo != null) combo.Palette = this;
            }
            else if (control is Label && muted.Contains(control)) control.ForeColor = Muted;
            PreviewCanvas canvas = control as PreviewCanvas;
            if (canvas != null) { canvas.BackColor = Canvas; canvas.EmptyTitleColor = Text; canvas.EmptyHintColor = Muted; }
            ThemedProgressBar progress = control as ThemedProgressBar;
            if (progress != null) progress.Palette = this;
            foreach (Control child in control.Controls) Apply(child, primary, muted);
            control.Invalidate();
        }
        public static void StyleCombo(ComboBox combo, Func<ThemeColors> colors)
        {
            combo.FlatStyle = FlatStyle.Flat; combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = Math.Max(20, combo.Font.Height + 5);
            combo.DrawItem += delegate(object sender, DrawItemEventArgs e)
            {
                ThemeColors theme = colors(); bool selected = (e.State & DrawItemState.Selected) != 0;
                Color background = selected ? theme.Selection : theme.Surface;
                Color text = !combo.Enabled ? theme.Muted : selected ? Color.White : theme.Text;
                using (Brush brush = new SolidBrush(background)) e.Graphics.FillRectangle(brush, e.Bounds);
                string value = e.Index >= 0 ? combo.Items[e.Index].ToString() : combo.Text;
                Rectangle area = e.Bounds; area.X += 5; area.Width -= 8;
                TextRenderer.DrawText(e.Graphics, value, combo.Font, area, text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                if ((e.State & DrawItemState.Focus) != 0 && !theme.IsDark) e.DrawFocusRectangle();
            };
        }
    }

    public sealed class AttributionLabel : Label
    {
        public AttributionLabel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoSize = false; TabStop = false; AccessibleRole = AccessibleRole.StaticText;
            TextAlign = ContentAlignment.MiddleRight;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            // GDI+ blends the text with the current theme background at 45% opacity.
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using (Brush brush = new SolidBrush(Color.FromArgb(115, ForeColor)))
            using (StringFormat format = new StringFormat {
                Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                e.Graphics.DrawString(Text, Font, brush, ClientRectangle, format);
        }
    }

    public sealed class ThemedComboBox : ComboBox
    {
        public ThemeColors Palette;
        bool hovering;
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovering = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovering = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (Palette == null || !Palette.IsDark || Width < 2 || Height < 2) return;
            // The native flat ComboBox paints its frame and arrow in system
            // colors even with owner-drawn items. Overlay only that chrome;
            // keep native dropdown, keyboard and accessibility behavior.
            if (message.Msg == 0x000F)
            {
                using (Graphics graphics = Graphics.FromHwnd(Handle)) DrawChrome(graphics);
            }
            else if ((message.Msg == 0x0317 || message.Msg == 0x0318) && message.WParam != IntPtr.Zero)
            {
                using (Graphics graphics = Graphics.FromHdc(message.WParam)) DrawChrome(graphics);
            }
        }
        void DrawChrome(Graphics graphics)
        {
            int width = Math.Min(Width - 2, SystemInformation.VerticalScrollBarWidth + 1);
            Rectangle arrow = new Rectangle(Width - width - 1, 1, width, Height - 2);
            if (!Enabled)
            {
                Rectangle value = new Rectangle(1, 1, Math.Max(0, arrow.Left - 1), Height - 2);
                using (Brush background = new SolidBrush(Palette.Surface)) graphics.FillRectangle(background, value);
                value.X += 5; value.Width = Math.Max(0, value.Width - 8);
                TextRenderer.DrawText(graphics, Text, Font, value, Palette.Muted,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }
            // The system also adds an inset white frame around the value.
            using (Pen inset = new Pen(Palette.Surface))
                graphics.DrawRectangle(inset, 1, 1, Math.Max(0, arrow.Left - 2), Math.Max(0, Height - 3));
            using (Brush background = new SolidBrush(hovering && Enabled ? Palette.Hover : Palette.Surface))
                graphics.FillRectangle(background, arrow);
            using (Pen separator = new Pen(Palette.Border)) graphics.DrawLine(separator, arrow.Left, 1, arrow.Left, Height - 2);
            int x = arrow.Left + arrow.Width / 2, y = Height / 2;
            using (Brush foreground = new SolidBrush(Enabled ? Palette.Muted : Palette.Border))
                graphics.FillPolygon(foreground, new Point[] { new Point(x - 3, y - 1), new Point(x + 3, y - 1), new Point(x, y + 2) });
            using (Pen border = new Pen(Focused && Enabled ? Palette.Accent : Palette.Border))
                graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }
    }

    public sealed class ThemedProgressBar : Control
    {
        public ThemeColors Palette;
        readonly Timer animation = new Timer();
        int value, phase;
        ProgressBarStyle style = ProgressBarStyle.Continuous;
        public int Value
        {
            get { return value; }
            set { this.value = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }
        public ProgressBarStyle Style
        {
            get { return style; }
            set { style = value; phase = 0; UpdateAnimation(); Invalidate(); }
        }
        public ThemedProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = false; animation.Interval = 35;
            animation.Tick += delegate { phase = (phase + 3) % 140; Invalidate(); };
        }
        void UpdateAnimation()
        {
            if (animation == null) return;
            if (Visible && style == ProgressBarStyle.Marquee) animation.Start(); else animation.Stop();
        }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); UpdateAnimation(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            ThemeColors colors = Palette ?? ThemeColors.For("light");
            Rectangle track = new Rectangle(1, 1, Math.Max(0, Width - 2), Math.Max(0, Height - 2));
            using (Brush background = new SolidBrush(colors.Surface)) e.Graphics.FillRectangle(background, ClientRectangle);
            Rectangle filled;
            if (style == ProgressBarStyle.Marquee)
                filled = Rectangle.Intersect(track, new Rectangle(track.Left + (phase - 40) * track.Width / 100, track.Top, Math.Max(1, track.Width * 40 / 100), track.Height));
            else filled = new Rectangle(track.Left, track.Top, track.Width * value / 100, track.Height);
            if (filled.Width > 0 && filled.Height > 0)
                using (Brush accent = new SolidBrush(colors.Accent)) e.Graphics.FillRectangle(accent, filled);
            if (Width > 1 && Height > 1)
                using (Pen border = new Pen(colors.Border)) e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }
        protected override AccessibleObject CreateAccessibilityInstance() { return new ProgressAccessibility(this); }
        sealed class ProgressAccessibility : ControlAccessibleObject
        {
            readonly ThemedProgressBar progress;
            public ProgressAccessibility(ThemedProgressBar owner) : base(owner) { progress = owner; }
            public override AccessibleRole Role { get { return AccessibleRole.ProgressBar; } }
            public override string Value
            {
                get { return progress.Style == ProgressBarStyle.Marquee ? UiText.Get("Выполняется") : progress.Value + "%"; }
            }
        }
        protected override void Dispose(bool disposing) { if (disposing) animation.Dispose(); base.Dispose(disposing); }
    }

    public sealed class ThemedButton : Button
    {
        public Color DisabledTextColor = Color.Gray;
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled) { base.OnPaint(e); return; }
            using (Brush background = new SolidBrush(BackColor)) e.Graphics.FillRectangle(background, ClientRectangle);
            if (FlatAppearance.BorderSize > 0)
                using (Pen border = new Pen(FlatAppearance.BorderColor))
                    e.Graphics.DrawRectangle(border, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, DisabledTextColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
    }

    public sealed class SettingsDialog : Form
    {
        readonly ComboBox language = new ThemedComboBox(), theme = new ThemedComboBox();
        readonly Label heading = new Label(), languageLabel = new Label(), themeLabel = new Label(), hint = new Label();
        readonly Button save = new ThemedButton(), cancel = new ThemedButton();
        ThemeColors colors;
        bool updating;
        public string SelectedLanguage { get { return language.SelectedIndex == 1 ? "en" : "ru"; } }
        public string SelectedTheme { get { return theme.SelectedIndex == 1 ? "dark" : "light"; } }

        public SettingsDialog(string selectedLanguage, string selectedTheme)
        {
            Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(440, 342); FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; StartPosition = FormStartPosition.CenterParent;
            colors = ThemeColors.For(selectedTheme);
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 20), ColumnCount = 1, RowCount = 7 };
            foreach (int height in new int[] { 44, 28, 36, 28, 36, 64, 44 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            Controls.Add(layout);
            heading.Font = new Font("Segoe UI", 18, FontStyle.Bold); heading.Dock = DockStyle.Fill;
            languageLabel.Dock = themeLabel.Dock = hint.Dock = DockStyle.Fill;
            languageLabel.TextAlign = themeLabel.TextAlign = ContentAlignment.BottomLeft;
            language.Dock = theme.Dock = DockStyle.Fill;
            language.DropDownStyle = theme.DropDownStyle = ComboBoxStyle.DropDownList;
            language.Items.AddRange(new string[] { "Русский", "English" });
            theme.Items.AddRange(new string[] { "", "" });
            language.SelectedIndex = selectedLanguage == "en" ? 1 : 0; theme.SelectedIndex = selectedTheme == "dark" ? 1 : 0;
            ThemeColors.StyleCombo(language, () => colors); ThemeColors.StyleCombo(theme, () => colors);
            hint.Padding = new Padding(0, 12, 0, 0); hint.Font = new Font("Segoe UI", 9);
            FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            foreach (Button button in new Button[] { save, cancel })
            {
                button.Size = new Size(120, 34); button.Margin = new Padding(8, 4, 0, 0); button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = button == save ? 0 : 1; actions.Controls.Add(button);
            }
            save.DialogResult = DialogResult.OK; cancel.DialogResult = DialogResult.Cancel;
            AcceptButton = save; CancelButton = cancel;
            layout.Controls.Add(heading, 0, 0); layout.Controls.Add(languageLabel, 0, 1); layout.Controls.Add(language, 0, 2);
            layout.Controls.Add(themeLabel, 0, 3); layout.Controls.Add(theme, 0, 4); layout.Controls.Add(hint, 0, 5); layout.Controls.Add(actions, 0, 6);
            language.SelectedIndexChanged += delegate { RefreshAppearance(); };
            theme.SelectedIndexChanged += delegate { RefreshAppearance(); };
            RefreshAppearance();
        }
        void RefreshAppearance()
        {
            if (updating) return; updating = true;
            try
            {
                string lang = SelectedLanguage; int index = theme.SelectedIndex;
                Text = heading.Text = UiText.Get("Настройки", lang);
                languageLabel.Text = UiText.Get("ЯЗЫК ИНТЕРФЕЙСА", lang); language.AccessibleName = languageLabel.Text;
                themeLabel.Text = UiText.Get("ТЕМА ОФОРМЛЕНИЯ", lang); theme.AccessibleName = themeLabel.Text;
                theme.Items[0] = UiText.Get("Светлая", lang); theme.Items[1] = UiText.Get("Тёмная", lang); theme.SelectedIndex = index;
                hint.Text = UiText.Get("Настройки применяются без перезапуска приложения.", lang);
                save.Text = UiText.Get("Сохранить", lang); cancel.Text = UiText.Get("Отмена", lang);
                colors = ThemeColors.For(SelectedTheme);
                colors.Apply(this, new HashSet<Button> { save }, new HashSet<Control> { languageLabel, themeLabel, hint });
            }
            finally { updating = false; }
        }
    }
}
