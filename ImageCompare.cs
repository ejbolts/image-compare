using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ImageCompare
{
    static class Program
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
        [STAThread] static void Main(string[] args)
        {
            SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CompareForm(args));
        }
    }

    public sealed class CompareForm : Form
    {
        readonly CompareCanvas canvas = new CompareCanvas();
        readonly Label beforeInfo = new Label(), afterInfo = new Label(), status = new Label(), percent = new Label();
        readonly Button swap;
        readonly CheckBox verticalCheck;
        readonly Icon appIcon;
        string beforeName = "No image selected", afterName = "No image selected";
        readonly Color muted = Color.FromArgb(155, 166, 186);

        public CompareForm(string[] args)
        {
            Text = "Image Compare"; BackColor = Color.FromArgb(20, 24, 33); ForeColor = Color.White;
            using (var stream = typeof(CompareForm).Assembly.GetManifestResourceStream("ImageCompare.ico"))
                if (stream != null) appIcon = new Icon(stream, new Size(32, 32));
            if (appIcon != null) { Icon = appIcon; ShowIcon = true; }
            Font = new Font("Segoe UI", 10); ClientSize = new Size(1100, 760); MinimumSize = new Size(720, 520);
            StartPosition = FormStartPosition.CenterScreen; KeyPreview = true;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 5 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            Controls.Add(layout);
            var heading = new Panel { Dock = DockStyle.Fill };
            heading.Controls.Add(new Label { Text = "Image Compare", Font = new Font("Segoe UI Semibold", 23), AutoSize = true, Location = new Point(0, 0) });
            heading.Controls.Add(new Label { Text = "Two images. One simple comparison.", ForeColor = muted, AutoSize = true, Location = new Point(3, 44) });
            layout.Controls.Add(heading, 0, 0);
            var pickers = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0) };
            pickers.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); pickers.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pickers.Controls.Add(Picker(true, beforeInfo), 0, 0); pickers.Controls.Add(Picker(false, afterInfo), 1, 0);
            layout.Controls.Add(pickers, 0, 1);
            canvas.Dock = DockStyle.Fill; canvas.Margin = new Padding(0, 10, 0, 0);
            canvas.PositionChanged += delegate { percent.Text = Math.Round(canvas.Position * 100) + "%"; };
            canvas.FilesDropped += DropFiles;
            layout.Controls.Add(canvas, 0, 2);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0), Margin = new Padding(0), WrapContents = false };
            swap = MakeButton("Swap images", delegate { canvas.Swap(); var name = beforeName; beforeName = afterName; afterName = name; UpdateInfo(); });
            actions.Controls.Add(swap);
            actions.Controls.Add(MakeButton("Center slider", delegate { canvas.Position = .5; canvas.Focus(); }));
            actions.Controls.Add(MakeButton("Try demo", delegate { Demo(); }));
            verticalCheck = new CheckBox
            {
                Text = "Vertical comparison",
                AutoSize = true,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(12, 7, 8, 0)
            };
            verticalCheck.CheckedChanged += delegate
            {
                canvas.IsVertical = verticalCheck.Checked;
                canvas.Focus();
            };
            actions.Controls.Add(verticalCheck);
            percent.Text = "50%"; percent.ForeColor = Color.FromArgb(157, 206, 255); percent.AutoSize = true; percent.Margin = new Padding(14, 8, 0, 0); actions.Controls.Add(percent);
            layout.Controls.Add(actions, 0, 3);
            status.Dock = DockStyle.Fill; status.ForeColor = muted; status.TextAlign = ContentAlignment.MiddleLeft; status.AutoEllipsis = true; status.Margin = new Padding(0);
            layout.Controls.Add(status, 0, 4);
            KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.V && !e.Control && !e.Alt)
                {
                    verticalCheck.Checked = !verticalCheck.Checked;
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            UpdateInfo();
            Shown += delegate { if (args.Length > 0) LoadFile(args[0], true); if (args.Length > 1) LoadFile(args[1], false); };
        }

        Panel Picker(bool before, Label info)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(before ? 0 : 8, 8, before ? 8 : 0, 0), BackColor = Color.FromArgb(31, 37, 49), AllowDrop = true };
            var button = MakeButton(before ? "Open before…" : "Open after…", delegate { PickFile(before); });
            button.Location = new Point(12, 10); button.Width = 140;
            info.ForeColor = muted; info.AutoEllipsis = true; info.Location = new Point(14, 51); info.Height = 22;
            info.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            panel.Controls.Add(button); panel.Controls.Add(info);
            panel.Resize += delegate { info.Width = Math.Max(10, panel.Width - 28); };
            panel.DragEnter += AcceptDrag;
            panel.DragDrop += delegate(object s, DragEventArgs e) { var files = (string[])e.Data.GetData(DataFormats.FileDrop); if (files.Length > 0) LoadFile(files[0], before); };
            return panel;
        }
        static void AcceptDrag(object sender, DragEventArgs e) { e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; }
        Button MakeButton(string text, EventHandler click)
        {
            var b = new Button { Text = text, AutoSize = false, Size = new Size(132, 34), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(43, 53, 70), ForeColor = Color.White, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) };
            b.FlatAppearance.BorderColor = Color.FromArgb(67, 81, 103); b.FlatAppearance.MouseOverBackColor = Color.FromArgb(59, 75, 98); b.Click += click;
            return b;
        }
        void PickFile(bool before)
        {
            using (var dialog = new OpenFileDialog { Title = before ? "Choose before image" : "Choose after image", Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files|*.*", CheckFileExists = true })
                if (dialog.ShowDialog(this) == DialogResult.OK) LoadFile(dialog.FileName, before);
        }
        public static Bitmap ReadImage(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var original = Image.FromStream(stream, true, true))
            {
                // Apply camera orientation before making an independent, unlocked copy.
                try {
                    if (Array.IndexOf(original.PropertyIdList, 0x112) >= 0) {
                        int orientation = BitConverter.ToUInt16(original.GetPropertyItem(0x112).Value, 0);
                        RotateFlipType[] rotations = { RotateFlipType.RotateNoneFlipNone, RotateFlipType.RotateNoneFlipNone, RotateFlipType.RotateNoneFlipX, RotateFlipType.Rotate180FlipNone, RotateFlipType.Rotate180FlipX, RotateFlipType.Rotate90FlipX, RotateFlipType.Rotate90FlipNone, RotateFlipType.Rotate270FlipX, RotateFlipType.Rotate270FlipNone };
                        if (orientation >= 1 && orientation <= 8) original.RotateFlip(rotations[orientation]);
                    }
                } catch (ArgumentException) { }
                return new Bitmap(original);
            }
        }
        void LoadFile(string path, bool before)
        {
            try {
                var bitmap = ReadImage(path);
                canvas.SetImage(bitmap, before);
                if (before) beforeName = Path.GetFileName(path); else afterName = Path.GetFileName(path);
                UpdateInfo(); canvas.Focus();
            } catch (Exception ex) {
                if (!(ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is OutOfMemoryException || ex is System.Runtime.InteropServices.ExternalException)) throw;
                MessageBox.Show(this, "This image could not be opened. Choose a valid PNG, JPEG, BMP, GIF, or TIFF file.", "Unable to open image", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        void DropFiles(string[] files, bool before)
        {
            if (files.Length >= 2) { LoadFile(files[0], true); LoadFile(files[1], false); }
            else if (files.Length == 1) LoadFile(files[0], canvas.Before == null || (canvas.After != null && before));
        }
        void UpdateInfo()
        {
            beforeInfo.Text = Describe(beforeName, canvas.Before); afterInfo.Text = Describe(afterName, canvas.After);
            swap.Enabled = canvas.Before != null && canvas.After != null;
            if (canvas.Before == null || canvas.After == null) status.Text = "Open two images or drop them here  •  Everything stays on your device";
            else if (canvas.Before.Size != canvas.After.Size) status.Text = "Different sizes: centered at the same scale, without stretching  •  Drag to compare";
            else status.Text = "Drag to compare  •  Arrow keys to adjust  •  Home / End to reveal either image";
        }
        static string Describe(string name, Image img) { return img == null ? name : name + "  ·  " + img.Width + " × " + img.Height; }
        void Demo()
        {
            canvas.SetImage(DemoImage(false), true); canvas.SetImage(DemoImage(true), false);
            beforeName = "Demo · original"; afterName = "Demo · updated"; canvas.Position = .5; UpdateInfo(); canvas.Focus();
        }
        public static Bitmap DemoImage(bool after)
        {
            var b = new Bitmap(1200, 700);
            using (var g = Graphics.FromImage(b)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var sky = new LinearGradientBrush(new Rectangle(0, 0, 1200, 700), after ? Color.FromArgb(105, 172, 205) : Color.FromArgb(182, 192, 198), after ? Color.FromArgb(244, 217, 166) : Color.FromArgb(227, 226, 215), 90)) g.FillRectangle(sky, 0, 0, 1200, 700);
                using (var sun = new SolidBrush(after ? Color.FromArgb(255, 236, 183) : Color.FromArgb(230, 227, 212))) g.FillEllipse(sun, 890, 90, 110, 110);
                using (var hill = new SolidBrush(after ? Color.FromArgb(79, 125, 122) : Color.FromArgb(133, 148, 142))) g.FillPolygon(hill, new[] { new Point(0, 410), new Point(190, 230), new Point(410, 440), new Point(760, 280), new Point(1200, 440), new Point(1200, 700), new Point(0, 700) });
                using (var lawn = new SolidBrush(after ? Color.FromArgb(54, 102, 81) : Color.FromArgb(133, 143, 114))) g.FillRectangle(lawn, 0, 485, 1200, 215);
                using (var house = new SolidBrush(after ? Color.FromArgb(246, 229, 199) : Color.FromArgb(197, 192, 175))) g.FillRectangle(house, 300, 290, 580, 260);
                using (var roof = new SolidBrush(after ? Color.FromArgb(47, 66, 70) : Color.FromArgb(103, 108, 106))) g.FillPolygon(roof, new[] { new Point(265, 300), new Point(590, 180), new Point(915, 300) });
                using (var windows = new SolidBrush(after ? Color.FromArgb(62, 117, 139) : Color.FromArgb(123, 141, 146))) { g.FillRectangle(windows, 350, 345, 135, 125); g.FillRectangle(windows, 690, 345, 135, 125); g.FillRectangle(windows, 545, 345, 90, 205); }
                using (var line = new Pen(Color.FromArgb(233, 224, 204), 7)) { g.DrawLine(line, 417, 345, 417, 470); g.DrawLine(line, 350, 405, 485, 405); g.DrawLine(line, 757, 345, 757, 470); g.DrawLine(line, 690, 405, 825, 405); }
                using (var path = new SolidBrush(after ? Color.FromArgb(202, 186, 161) : Color.FromArgb(170, 168, 153))) g.FillPolygon(path, new[] { new Point(545, 550), new Point(635, 550), new Point(790, 700), new Point(410, 700) });
                if (after) using (var plants = new SolidBrush(Color.FromArgb(32, 78, 58))) { g.FillEllipse(plants, 240, 470, 140, 105); g.FillEllipse(plants, 785, 465, 160, 110); }
            }
            return b;
        }
        protected override void Dispose(bool disposing) { if (disposing) canvas.Dispose(); base.Dispose(disposing); if (disposing && appIcon != null) appIcon.Dispose(); }
    }

    public sealed class CompareCanvas : Control
    {
        public Bitmap Before { get; private set; }
        public Bitmap After { get; private set; }
        double position = .5;
        bool isVertical;
        bool dragging;
        public event Action<string[], bool> FilesDropped;
        public event EventHandler PositionChanged;
        public bool IsVertical
        {
            get { return isVertical; }
            set
            {
                if (isVertical == value) return;
                isVertical = value;
                Cursor = isVertical ? Cursors.HSplit : Cursors.VSplit;
                UpdateAccessibility();
                Invalidate();
            }
        }
        public double Position {
            get { return position; }
            set {
                position = Math.Max(0, Math.Min(1, value));
                UpdateAccessibility();
                Invalidate();
                if (PositionChanged != null) PositionChanged(this, EventArgs.Empty);
            }
        }
        void UpdateAccessibility()
        {
            AccessibleDescription = string.Format("Comparison divider at {0} percent. Drag {1}, or use arrow keys, Home or End.",
                Math.Round(position * 100), isVertical ? "up and down" : "side to side");
        }
        public CompareCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true; AllowDrop = true; BackColor = Color.FromArgb(13, 17, 24); Cursor = Cursors.VSplit;
            AccessibleName = "Image comparison slider"; AccessibleRole = AccessibleRole.Slider;
            UpdateAccessibility();
            DragEnter += delegate(object s, DragEventArgs e) { e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; };
            DragDrop += delegate(object s, DragEventArgs e) {
                if (FilesDropped != null) {
                    var pt = PointToClient(new Point(e.X, e.Y));
                    bool isBefore = isVertical ? (pt.Y < Height / 2) : (pt.X < Width / 2);
                    FilesDropped((string[])e.Data.GetData(DataFormats.FileDrop), isBefore);
                }
            };
        }
        public void SetImage(Bitmap img, bool before) { if (before) { if (Before != null) Before.Dispose(); Before = img; } else { if (After != null) After.Dispose(); After = img; } Invalidate(); }
        public void Swap() { var img = Before; Before = After; After = img; Invalidate(); }
        public RectangleF Frame()
        {
            int w = Math.Max(Before == null ? 0 : Before.Width, After == null ? 0 : After.Width), h = Math.Max(Before == null ? 0 : Before.Height, After == null ? 0 : After.Height);
            if (w == 0 || h == 0) return ClientRectangle;
            float scale = Math.Min((float)Math.Max(1, Width - 32) / w, (float)Math.Max(1, Height - 32) / h);
            return new RectangleF((Width - w * scale) / 2, (Height - h * scale) / 2, w * scale, h * scale);
        }
        void DrawImage(Graphics g, Bitmap img, RectangleF frame)
        {
            if (img == null) return;
            int w = Math.Max(Before == null ? 0 : Before.Width, After == null ? 0 : After.Width), h = Math.Max(Before == null ? 0 : Before.Height, After == null ? 0 : After.Height);
            float scale = frame.Width / w;
            g.DrawImage(img, new RectangleF(frame.X + (w - img.Width) * scale / 2, frame.Y + (h - img.Height) * scale / 2, img.Width * scale, img.Height * scale));
        }
        void Checker(Graphics g, RectangleF r)
        {
            var save = g.Save(); g.SetClip(r, CombineMode.Intersect);
            using (var a = new SolidBrush(Color.FromArgb(35, 40, 50))) using (var b = new SolidBrush(Color.FromArgb(42, 48, 60)))
                for (int y = (int)r.Top; y < r.Bottom; y += 16) for (int x = (int)r.Left; x < r.Right; x += 16) g.FillRectangle((((x - (int)r.Left) / 16 + (y - (int)r.Top) / 16) % 2 == 0) ? a : b, x, y, 16, 16);
            g.Restore(save);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            if (Before == null && After == null) {
                using (var f = new Font("Segoe UI Semibold", 19)) using (var small = new Font("Segoe UI", 11)) using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }) {
                    g.DrawString("Drop two images here", f, Brushes.White, new RectangleF(0, Height / 2 - 45, Width, 40), sf);
                    using (var brush = new SolidBrush(Color.FromArgb(155, 166, 186))) g.DrawString("Or open a before and after image above", small, brush, new RectangleF(0, Height / 2 + 2, Width, 35), sf);
                }
            } else {
                var frame = Frame(); Checker(g, frame);
                if (Before == null || After == null) DrawImage(g, Before ?? After, frame);
                else {
                    DrawImage(g, After, frame);
                    if (isVertical) {
                        float y = frame.Top + frame.Height * (float)position;
                        var save = g.Save();
                        g.SetClip(new RectangleF(frame.Left, frame.Top, frame.Width, frame.Height * (float)position));
                        Checker(g, frame); DrawImage(g, Before, frame);
                        g.Restore(save);
                        using (var shadow = new Pen(Color.FromArgb(80, 0, 0, 0), 5)) g.DrawLine(shadow, frame.Left, y, frame.Right, y);
                        using (var pen = new Pen(Color.White, 2)) g.DrawLine(pen, frame.Left, y, frame.Right, y);
                        float cx = frame.Left + frame.Width / 2;
                        g.FillEllipse(Brushes.White, cx - 21, y - 21, 42, 42);
                        using (var pen = new Pen(Color.FromArgb(35, 48, 66), 2)) {
                            g.DrawLines(pen, new[] { new PointF(cx - 5, y - 6), new PointF(cx, y - 11), new PointF(cx + 5, y - 6) });
                            g.DrawLines(pen, new[] { new PointF(cx - 5, y + 6), new PointF(cx, y + 11), new PointF(cx + 5, y + 6) });
                        }
                    } else {
                        float x = frame.Left + frame.Width * (float)position;
                        var save = g.Save();
                        g.SetClip(new RectangleF(frame.Left, frame.Top, frame.Width * (float)position, frame.Height));
                        Checker(g, frame); DrawImage(g, Before, frame);
                        g.Restore(save);
                        using (var shadow = new Pen(Color.FromArgb(80, 0, 0, 0), 5)) g.DrawLine(shadow, x, frame.Top, x, frame.Bottom);
                        using (var pen = new Pen(Color.White, 2)) g.DrawLine(pen, x, frame.Top, x, frame.Bottom);
                        float cy = frame.Top + frame.Height / 2;
                        g.FillEllipse(Brushes.White, x - 21, cy - 21, 42, 42);
                        using (var pen = new Pen(Color.FromArgb(35, 48, 66), 2)) {
                            g.DrawLines(pen, new[] { new PointF(x - 6, cy - 5), new PointF(x - 11, cy), new PointF(x - 6, cy + 5) });
                            g.DrawLines(pen, new[] { new PointF(x + 6, cy - 5), new PointF(x + 11, cy), new PointF(x + 6, cy + 5) });
                        }
                    }
                }
                Badge(g, Before == null ? "AFTER" : "BEFORE", 14, 14);
                if (Before != null && After != null) {
                    if (isVertical) Badge(g, "AFTER", 14, Height - 40);
                    else Badge(g, "AFTER", Width - 89, 14);
                }
            }
            using (var pen = new Pen(Focused ? Color.FromArgb(114, 170, 226) : Color.FromArgb(49, 58, 73))) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
        void Badge(Graphics g, string text, int x, int y) { using (var b = new SolidBrush(Color.FromArgb(215, 25, 32, 44))) g.FillRectangle(b, x, y, 75, 26); using (var f = new Font("Segoe UI Semibold", 8)) using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }) g.DrawString(text, f, Brushes.White, new Rectangle(x, y, 75, 26), sf); }
        void MoveDivider(Point pt)
        {
            var frame = Frame();
            if (isVertical)
                Position = (pt.Y - frame.Top) / Math.Max(1f, frame.Height);
            else
                Position = (pt.X - frame.Left) / Math.Max(1f, frame.Width);
        }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); Focus(); if (e.Button == MouseButtons.Left && Before != null && After != null) { dragging = true; Capture = true; MoveDivider(e.Location); } }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (dragging) MoveDivider(e.Location); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); if (e.Button == MouseButtons.Left) { dragging = false; Capture = false; } }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) dragging = false; }
        protected override bool IsInputKey(Keys keyData) { var key = keyData & Keys.KeyCode; return key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down || key == Keys.Home || key == Keys.End || base.IsInputKey(keyData); }
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (Before == null || After == null) return;
            double step = e.Shift ? .1 : .01;
            switch (e.KeyCode) {
                case Keys.Left: case Keys.Up: Position -= step; break;
                case Keys.Right: case Keys.Down: Position += step; break;
                case Keys.Home: Position = 0; break;
                case Keys.End: Position = 1; break;
                default: return;
            }
            e.Handled = true; e.SuppressKeyPress = true;
        }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void Dispose(bool disposing) { if (disposing) { if (Before != null) Before.Dispose(); if (After != null) After.Dispose(); Before = After = null; } base.Dispose(disposing); }
    }
}
