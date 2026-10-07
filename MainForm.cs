using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace LolKey;

public class MainForm : Form
{
    // Controls
    private Button btnHeaderLangIcon = null!;
    private Button btnToggleViet = null!;
    private ComboBox cbCharset = null!;
    private ComboBox cbMethod = null!;
    private RadioButton rbCtrlShift = null!;
    private RadioButton rbAltZ = null!;
    private CheckBox chkSpellCheck = null!;
    private CheckBox chkModernTone = null!;
    private CheckBox chkFreeMarking = null!;

    private Button btnToggleToxic = null!;
    private CheckBox chkSound = null!;
    private Label lblRescuedCount = null!;
    private Label lblLastRescue = null!;

    private CheckBox chkStartup = null!;
    private CheckBox chkShowOnStart = null!;

    private Button btnOpenDict = null!;
    private Button btnResetDefault = null!;
    private Button btnMinimizeToTray = null!;
    private Button btnExit = null!;

    // Tray Icon & Menu
    private NotifyIcon trayIcon = null!;
    private ContextMenuStrip trayMenu = null!;
    private ToolStripMenuItem miToggleViet = null!;
    private ToolStripMenuItem miMethodTelex = null!;
    private ToolStripMenuItem miMethodVni = null!;
    private ToolStripMenuItem miMethodSimple = null!;
    private ToolStripMenuItem miMethodViqr = null!;
    private ToolStripMenuItem miToggleToxic = null!;
    private ToolStripMenuItem miSound = null!;

    private bool reallyExit = false;

    public MainForm()
    {
        InitializeComponents();
        UpdateUIState();

        // Subscribe to engine events
        KeyboardEngine.OnWordRescued += HandleWordRescued;
        KeyboardEngine.OnStatusChanged += HandleStatusChanged;

        if (!ConfigManager.Current.ShowWindowOnStartup)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }
    }

    private void InitializeComponents()
    {
        this.Text = "Anti Ăn Toxic (AUT) - Bộ gõ Tiếng Việt & Chống Toxic v1.0 Pro";
        this.Size = new Size(530, 685);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        this.BackColor = Color.FromArgb(248, 250, 252);
        this.ForeColor = Color.FromArgb(15, 23, 42);

        // Header Panel
        Panel headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 74,
            BackColor = Color.FromArgb(15, 23, 42)
        };

        Label lblTitle = new Label
        {
            Text = "ANTI ĂN TOXIC (AUT)  •  BỘ GÕ TIẾNG VIỆT & CHỐNG TOXIC",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 211, 153),
            Location = new Point(16, 14),
            AutoSize = true
        };

        Label lblSubtitle = new Label
        {
            Text = "Lõi gõ tiếng Việt UniKey (Tác giả: Phạm Kim Long) • Hóa giải từ nóng trong game",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(16, 40),
            AutoSize = true
        };

        // Header Language Icon Button (UniKey-style quick switch [V] / [E])
        btnHeaderLangIcon = new Button
        {
            Size = new Size(48, 48),
            Location = new Point(452, 13),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(220, 38, 38),
            Text = "V",
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btnHeaderLangIcon.FlatAppearance.BorderSize = 0;
        btnHeaderLangIcon.Click += (s, e) =>
        {
            KeyboardEngine.ToggleVietnamese(playBeep: true);
        };

        ToolTip tt = new ToolTip();
        tt.SetToolTip(btnHeaderLangIcon, "Bấm vào icon để chuyển nhanh [V] ⮂ [E] như UniKey");

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblSubtitle);
        headerPanel.Controls.Add(btnHeaderLangIcon);
        this.Controls.Add(headerPanel);

        // ==========================================
        // 1. VIETNAMESE TYPING SECTION
        // ==========================================
        GroupBox gbViet = new GroupBox
        {
            Text = " Điều khiển Bộ gõ Tiếng Việt ",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(16, 84),
            Size = new Size(482, 215)
        };

        // Big Toggle Button: [V] / [E]
        btnToggleViet = new Button
        {
            Location = new Point(16, 24),
            Size = new Size(450, 36),
            Font = new Font("Segoe UI", 9.8f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnToggleViet.FlatAppearance.BorderSize = 0;
        btnToggleViet.Click += (s, e) =>
        {
            KeyboardEngine.ToggleVietnamese(playBeep: true);
        };
        gbViet.Controls.Add(btnToggleViet);

        // Bảng mã Dropdown
        Label lblCharset = new Label
        {
            Text = "Bảng mã:",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(16, 70),
            Size = new Size(80, 24)
        };
        cbCharset = new ComboBox
        {
            Location = new Point(100, 67),
            Size = new Size(180, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular)
        };
        cbCharset.Items.AddRange(new object[] {
            "Unicode (Dựng sẵn)",
            "Unicode (Tổ hợp)",
            "TCVN3 (ABC)",
            "VNI Windows"
        });
        cbCharset.SelectedIndex = (int)ConfigManager.Current.EncodingCharset;
        cbCharset.SelectedIndexChanged += (s, e) =>
        {
            ConfigManager.Current.EncodingCharset = (Charset)cbCharset.SelectedIndex;
            ConfigManager.Save();
        };
        gbViet.Controls.Add(lblCharset);
        gbViet.Controls.Add(cbCharset);

        // Kiểu gõ Dropdown (Telex, VNI, Simple Telex, VIQR)
        Label lblMethod = new Label
        {
            Text = "Kiểu gõ:",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(292, 70),
            Size = new Size(60, 24)
        };
        cbMethod = new ComboBox
        {
            Location = new Point(356, 67),
            Size = new Size(110, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        cbMethod.Items.AddRange(new object[] {
            "Telex",
            "VNI",
            "Simple Telex",
            "VIQR"
        });
        cbMethod.SelectedIndex = (int)ConfigManager.Current.Method;
        cbMethod.SelectedIndexChanged += (s, e) =>
        {
            ConfigManager.Current.Method = (InputMethod)cbMethod.SelectedIndex;
            ConfigManager.Save();
            UpdateUIState();
        };
        gbViet.Controls.Add(lblMethod);
        gbViet.Controls.Add(cbMethod);

        // Phím chuyển đổi [Ctrl+Shift] vs [Alt+Z]
        Label lblSwitch = new Label
        {
            Text = "Phím chuyển:",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(16, 105),
            Size = new Size(90, 24)
        };
        rbCtrlShift = new RadioButton
        {
            Text = "Ctrl + Shift",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(110, 103),
            Size = new Size(100, 24),
            Checked = (ConfigManager.Current.SwitchKey == SwitchKeyMode.CtrlShift)
        };
        rbCtrlShift.CheckedChanged += (s, e) =>
        {
            if (rbCtrlShift.Checked)
            {
                ConfigManager.Current.SwitchKey = SwitchKeyMode.CtrlShift;
                ConfigManager.Save();
            }
        };

        rbAltZ = new RadioButton
        {
            Text = "Alt + Z",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(220, 103),
            Size = new Size(80, 24),
            Checked = (ConfigManager.Current.SwitchKey == SwitchKeyMode.AltZ)
        };
        rbAltZ.CheckedChanged += (s, e) =>
        {
            if (rbAltZ.Checked)
            {
                ConfigManager.Current.SwitchKey = SwitchKeyMode.AltZ;
                ConfigManager.Save();
            }
        };
        gbViet.Controls.Add(lblSwitch);
        gbViet.Controls.Add(rbCtrlShift);
        gbViet.Controls.Add(rbAltZ);

        // Checkboxes: SpellCheck & Modern Tone
        chkSpellCheck = new CheckBox
        {
            Text = "Bật kiểm tra chính tả (tự khôi phục từ tiếng Anh: pass, fast, test...)",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(16, 134),
            Size = new Size(450, 24),
            Checked = ConfigManager.Current.SpellCheck
        };
        chkSpellCheck.CheckedChanged += (s, e) =>
        {
            ConfigManager.Current.SpellCheck = chkSpellCheck.Checked;
            ConfigManager.Save();
        };
        gbViet.Controls.Add(chkSpellCheck);

        chkModernTone = new CheckBox
        {
            Text = "Đặt dấu theo chuẩn mới (hòa, thúy thay vì hoà, thuý)",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(16, 160),
            Size = new Size(450, 24),
            Checked = ConfigManager.Current.ModernTonePlacement
        };
        chkModernTone.CheckedChanged += (s, e) =>
        {
            ConfigManager.Current.ModernTonePlacement = chkModernTone.Checked;
            ConfigManager.Save();
        };
        gbViet.Controls.Add(chkModernTone);

        chkFreeMarking = new CheckBox
        {
            Text = "Cho phép bỏ dấu tự do ở cuối từ (toans ➔ toán, did ➔ đi)",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(16, 186),
            Size = new Size(450, 24),
            Checked = ConfigManager.Current.FreeMarking
        };
        chkFreeMarking.CheckedChanged += (s, e) =>
        {
            ConfigManager.Current.FreeMarking = chkFreeMarking.Checked;
            ConfigManager.Save();
        };
        gbViet.Controls.Add(chkFreeMarking);

        this.Controls.Add(gbViet);

        // ==========================================
        // 2. ANTI-TOXIC SECTION (Clean & Simple)
        // ==========================================
        GroupBox gbToxic = new GroupBox
        {
            Text = " Bảo vệ Chống Toxic (Tự động hóa giải từ nóng) ",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(16, 305),
            Size = new Size(482, 105)
        };

        btnToggleToxic = new Button
        {
            Location = new Point(16, 24),
            Size = new Size(450, 36),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnToggleToxic.FlatAppearance.BorderSize = 0;
        btnToggleToxic.Click += (s, e) =>
        {
            ConfigManager.Current.IsEnabled = !ConfigManager.Current.IsEnabled;
            ConfigManager.Save();
            KeyboardEngine.PlayBeep(ConfigManager.Current.IsEnabled ? 1000 : 500);
            UpdateUIState();
        };
        gbToxic.Controls.Add(btnToggleToxic);

        chkSound = new CheckBox
        {
            Text = "Bật âm thanh nhắc nhở vui tai khi hóa giải từ nóng",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(16, 68),
            Size = new Size(450, 24),
            Checked = ConfigManager.Current.SoundEnabled
        };
        chkSound.CheckedChanged += (s, e) =>
        {
            ConfigManager.Current.SoundEnabled = chkSound.Checked;
            ConfigManager.Save();
        };
        gbToxic.Controls.Add(chkSound);

        this.Controls.Add(gbToxic);

        // ==========================================
        // 3. STATS & RESCUE LOG
        // ==========================================
        GroupBox gbStats = new GroupBox
        {
            Text = " Nhật ký & Thống kê cứu nguy ",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(16, 418),
            Size = new Size(482, 80)
        };

        lblRescuedCount = new Label
        {
            Text = $"🛡️ Đã cứu bạn khỏi bị report/ban: {ConfigManager.Current.RescuedCount} lần",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(16, 185, 129),
            Location = new Point(16, 22),
            Size = new Size(450, 24)
        };

        lblLastRescue = new Label
        {
            Text = "Trạng thái: Đang sẵn sàng gõ tiếng Việt và bảo vệ bạn trong game!",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(16, 48),
            Size = new Size(450, 24)
        };

        gbStats.Controls.Add(lblRescuedCount);
        gbStats.Controls.Add(lblLastRescue);
        this.Controls.Add(gbStats);

        // ==========================================
        // 4. SYSTEM OPTIONS
        // ==========================================
        GroupBox gbSystem = new GroupBox
        {
            Text = " Tùy chọn hệ thống ",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(16, 505),
            Size = new Size(482, 64)
        };

        chkStartup = new CheckBox
        {
            Text = "Khởi động cùng Windows",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(16, 24),
            Size = new Size(200, 24),
            Checked = ConfigManager.Current.StartWithWindows
        };
        chkStartup.CheckedChanged += (s, e) =>
        {
            ConfigManager.SetStartupWithWindows(chkStartup.Checked);
        };

        chkShowOnStart = new CheckBox
        {
            Text = "Bật hộp thoại khi khởi động",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(240, 24),
            Size = new Size(220, 24),
            Checked = ConfigManager.Current.ShowWindowOnStartup
        };
        chkShowOnStart.CheckedChanged += (s, e) =>
        {
            ConfigManager.Current.ShowWindowOnStartup = chkShowOnStart.Checked;
            ConfigManager.Save();
        };

        gbSystem.Controls.Add(chkStartup);
        gbSystem.Controls.Add(chkShowOnStart);
        this.Controls.Add(gbSystem);

        // ==========================================
        // 5. BOTTOM BUTTONS (UniKey style)
        // ==========================================
        btnOpenDict = new Button
        {
            Text = "📂 Từ Điển...",
            Location = new Point(16, 576),
            Size = new Size(105, 36),
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btnOpenDict.Click += (s, e) => OpenDictionaryFile();
        this.Controls.Add(btnOpenDict);

        btnResetDefault = new Button
        {
            Text = "⚙️ Mặc định",
            Location = new Point(130, 576),
            Size = new Size(105, 36),
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btnResetDefault.Click += (s, e) => ResetToDefault();
        this.Controls.Add(btnResetDefault);

        btnMinimizeToTray = new Button
        {
            Text = "🔽 Đóng (Về Tray)",
            Location = new Point(245, 576),
            Size = new Size(130, 36),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            BackColor = Color.FromArgb(226, 232, 240),
            Cursor = Cursors.Hand
        };
        btnMinimizeToTray.Click += (s, e) => HideToTray(true);
        this.Controls.Add(btnMinimizeToTray);

        btnExit = new Button
        {
            Text = "❌ Kết thúc",
            Location = new Point(386, 576),
            Size = new Size(112, 36),
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(220, 38, 38),
            Cursor = Cursors.Hand
        };
        btnExit.Click += (s, e) => ExitApplication();
        this.Controls.Add(btnExit);

        // UniKey Source Attribution & Credit
        Label lblUniKeyCredit = new Label
        {
            Text = "⭐ Lõi bộ gõ tiếng Việt kế thừa từ mã nguồn mở UniKey (Tác giả: Phạm Kim Long - GPL v2)",
            Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(16, 622),
            Size = new Size(482, 18),
            TextAlign = ContentAlignment.MiddleCenter
        };
        this.Controls.Add(lblUniKeyCredit);

        // Setup System Tray
        SetupTrayIcon();

        // Form events
        this.FormClosing += (s, e) =>
        {
            if (!reallyExit)
            {
                e.Cancel = true;
                HideToTray(false);
            }
        };
    }

    private void SetupTrayIcon()
    {
        trayMenu = new ContextMenuStrip();
        trayMenu.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

        var miOpen = new ToolStripMenuItem("Bảng điều khiển...", null, (s, e) => ShowFromTray())
        {
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        trayMenu.Items.Add(miOpen);
        trayMenu.Items.Add(new ToolStripSeparator());

        // Toggle Vietnamese
        miToggleViet = new ToolStripMenuItem("Tiếng Việt [V] (Ctrl+Shift)", null, (s, e) =>
        {
            KeyboardEngine.ToggleVietnamese(playBeep: true);
        });
        trayMenu.Items.Add(miToggleViet);

        // Input Methods Submenu
        var miMethods = new ToolStripMenuItem("Kiểu gõ");
        miMethodTelex = new ToolStripMenuItem("Telex", null, (s, e) => SetMethod(InputMethod.Telex));
        miMethodVni = new ToolStripMenuItem("VNI", null, (s, e) => SetMethod(InputMethod.VNI));
        miMethodSimple = new ToolStripMenuItem("Simple Telex", null, (s, e) => SetMethod(InputMethod.SimpleTelex));
        miMethodViqr = new ToolStripMenuItem("VIQR", null, (s, e) => SetMethod(InputMethod.VIQR));
        miMethods.DropDownItems.AddRange(new ToolStripItem[] { miMethodTelex, miMethodVni, miMethodSimple, miMethodViqr });
        trayMenu.Items.Add(miMethods);

        trayMenu.Items.Add(new ToolStripSeparator());

        // Anti-Toxic Toggle
        miToggleToxic = new ToolStripMenuItem("Bảo vệ chống Toxic (F11)", null, (s, e) =>
        {
            ConfigManager.Current.IsEnabled = !ConfigManager.Current.IsEnabled;
            ConfigManager.Save();
            UpdateUIState();
        });
        trayMenu.Items.Add(miToggleToxic);

        miSound = new ToolStripMenuItem("Âm thanh nhắc nhở", null, (s, e) =>
        {
            ConfigManager.Current.SoundEnabled = !ConfigManager.Current.SoundEnabled;
            ConfigManager.Save();
            UpdateUIState();
        });
        trayMenu.Items.Add(miSound);

        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(new ToolStripMenuItem("Mở file từ điển...", null, (s, e) => OpenDictionaryFile()));
        trayMenu.Items.Add(new ToolStripMenuItem("Về Anti Ăn Toxic (AUT)...", null, (s, e) => ShowAboutDialog()));
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(new ToolStripMenuItem("Thoát", null, (s, e) => ExitApplication()));

        trayIcon = new NotifyIcon
        {
            ContextMenuStrip = trayMenu,
            Visible = true,
            Text = "Anti Ăn Toxic (AUT) - Bộ gõ Tiếng Việt & Chống Toxic"
        };

        // Click on tray icon toggles language like UniKey (Left Click: [V] <-> [E])
        trayIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                KeyboardEngine.ToggleVietnamese(playBeep: true);
            }
        };

        // Double-click opens control panel window like UniKey
        trayIcon.MouseDoubleClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                // Revert the toggle that happened on the 1st click so state is preserved
                KeyboardEngine.ToggleVietnamese(playBeep: false);
                ShowFromTray();
            }
        };

        UpdateTrayIconGraphics();
    }

    private void SetMethod(InputMethod m)
    {
        ConfigManager.Current.Method = m;
        ConfigManager.Save();
        cbMethod.SelectedIndex = (int)m;
        UpdateUIState();
    }

    private void ResetToDefault()
    {
        ConfigManager.Current.EnableVietnamese = true;
        ConfigManager.Current.Method = InputMethod.Telex;
        ConfigManager.Current.EncodingCharset = Charset.Unicode;
        ConfigManager.Current.SwitchKey = SwitchKeyMode.CtrlShift;
        ConfigManager.Current.SpellCheck = true;
        ConfigManager.Current.ModernTonePlacement = true;
        ConfigManager.Current.FreeMarking = true;
        ConfigManager.Current.IsEnabled = true;
        ConfigManager.Current.SoundEnabled = true;
        ConfigManager.Save();

        cbCharset.SelectedIndex = 0;
        cbMethod.SelectedIndex = 0;
        rbCtrlShift.Checked = true;
        chkSpellCheck.Checked = true;
        chkModernTone.Checked = true;
        chkFreeMarking.Checked = true;
        UpdateUIState();

        MessageBox.Show("Đã khôi phục cài đặt mặc định chuẩn UniKey!", "Anti Ăn Toxic (AUT)", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateUIState()
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(UpdateUIState);
            return;
        }

        bool vietEnabled = ConfigManager.Current.EnableVietnamese;
        bool toxicEnabled = ConfigManager.Current.IsEnabled;
        string methodName = ConfigManager.Current.Method.ToString();

        // Header Language Icon
        if (btnHeaderLangIcon != null)
        {
            btnHeaderLangIcon.Text = vietEnabled ? "V" : "E";
            btnHeaderLangIcon.BackColor = vietEnabled ? Color.FromArgb(220, 38, 38) : Color.FromArgb(37, 99, 235);
        }

        // Vietnamese Button
        if (vietEnabled)
        {
            btnToggleViet.Text = $"🇻🇳  ĐANG BẬT TIẾNG VIỆT [V: {methodName.ToUpper()}] (CLICK ĐỔI SANG [E])";
            btnToggleViet.BackColor = Color.FromArgb(220, 38, 38); // UniKey Red!
            btnToggleViet.ForeColor = Color.White;
        }
        else
        {
            btnToggleViet.Text = "🔤  ĐANG GÕ TIẾNG ANH [E] (CLICK ĐỂ BẬT TIẾNG VIỆT [V])";
            btnToggleViet.BackColor = Color.FromArgb(71, 85, 105);
            btnToggleViet.ForeColor = Color.White;
        }

        // Toxic Button
        if (toxicEnabled)
        {
            btnToggleToxic.Text = "● BẢO VỆ CHỐNG TOXIC: ĐANG BẬT";
            btnToggleToxic.BackColor = Color.FromArgb(16, 185, 129); // Green
            btnToggleToxic.ForeColor = Color.White;
        }
        else
        {
            btnToggleToxic.Text = "○ BẢO VỆ CHỐNG TOXIC: ĐÃ TẮT";
            btnToggleToxic.BackColor = Color.FromArgb(148, 163, 184); // Gray
            btnToggleToxic.ForeColor = Color.White;
        }

        if (cbMethod.SelectedIndex != (int)ConfigManager.Current.Method)
        {
            cbMethod.SelectedIndex = (int)ConfigManager.Current.Method;
        }

        chkSound.Checked = ConfigManager.Current.SoundEnabled;

        // Tray menu checkmarks
        miToggleViet.Text = vietEnabled ? "✓ Tiếng Việt [V] (Ctrl+Shift)" : "  Tiếng Anh [E] (Ctrl+Shift)";
        miToggleViet.Checked = vietEnabled;
        miMethodTelex.Checked = (ConfigManager.Current.Method == InputMethod.Telex);
        miMethodVni.Checked = (ConfigManager.Current.Method == InputMethod.VNI);
        miMethodSimple.Checked = (ConfigManager.Current.Method == InputMethod.SimpleTelex);
        miMethodViqr.Checked = (ConfigManager.Current.Method == InputMethod.VIQR);

        miToggleToxic.Checked = toxicEnabled;
        miSound.Checked = ConfigManager.Current.SoundEnabled;

        string trayText = $"AUT - [{(vietEnabled ? "V" : "E")}: {methodName}] (Toxic: {(toxicEnabled ? "Bật" : "Tắt")})";
        if (trayText.Length > 63) trayText = trayText.Substring(0, 63);
        trayIcon.Text = trayText;
        UpdateTrayIconGraphics();
    }

    private void UpdateTrayIconGraphics()
    {
        bool vietEnabled = ConfigManager.Current.EnableVietnamese;
        using Bitmap bmp = new Bitmap(32, 32);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background circle: Red for [V], Blue for [E]
            Color bgColor = vietEnabled ? Color.FromArgb(220, 38, 38) : Color.FromArgb(37, 99, 235);
            using (SolidBrush brush = new SolidBrush(bgColor))
            {
                g.FillEllipse(brush, 1, 1, 30, 30);
            }

            // Draw "V" or "E"
            string letter = vietEnabled ? "V" : "E";
            using (Font f = new Font("Segoe UI", 16f, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(letter, f, textBrush, new RectangleF(0, 1, 32, 32), sf);
            }
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using Icon tempIcon = Icon.FromHandle(hIcon);
            Icon newIcon = (Icon)tempIcon.Clone();
            trayIcon.Icon?.Dispose();
            trayIcon.Icon = newIcon;
            this.Icon = newIcon;
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private void HandleWordRescued(string toxic, string replacement, int totalCount)
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(() => HandleWordRescued(toxic, replacement, totalCount));
            return;
        }

        lblRescuedCount.Text = $"🛡️ Đã cứu bạn khỏi bị report/ban: {totalCount} lần";
        string time = DateTime.Now.ToString("HH:mm:ss");
        lblLastRescue.Text = $"[{time}] Vừa đổi: '{toxic}' ➔ '{replacement}'";
        lblLastRescue.ForeColor = Color.FromArgb(16, 185, 129);
    }

    private static bool _trayNoticeShown = false;

    private void HandleStatusChanged()
    {
        UpdateUIState();
    }

    private void HideToTray(bool showBalloon)
    {
        this.Hide();
        if (showBalloon && !_trayNoticeShown)
        {
            _trayNoticeShown = true;
            trayIcon.ShowBalloonTip(2000, "Anti Ăn Toxic (AUT)", "Nhấp vào icon để đổi [V] ⮂ [E] như UniKey.\nNhấp đúp hoặc chuột phải để mở bảng điều khiển.", ToolTipIcon.Info);
        }
    }

    private void ShowFromTray()
    {
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.BringToFront();
        this.Activate();
    }

    private void OpenDictionaryFile()
    {
        string dictPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "toxic_dict.json");
        if (!File.Exists(dictPath))
        {
            dictPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "toxic_dict.json");
        }

        if (File.Exists(dictPath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = dictPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở file từ điển: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show("Không tìm thấy file toxic_dict.json!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ShowAboutDialog()
    {
        MessageBox.Show(
            "Anti Ăn Toxic (AUT) - Bộ gõ Tiếng Việt & Chống Toxic v1.0 Pro\n\n" +
            "• Thuật toán gõ tiếng Việt được kế thừa trực tiếp từ mã nguồn mở UniKey 3.6.2.\n" +
            "• Tác giả UniKey: Phạm Kim Long\n" +
            "• Giấy phép: GNU General Public License (GPL) v2\n\n" +
            "Chân thành cảm ơn tác giả Phạm Kim Long vì di sản mã nguồn mở quý giá cho cộng đồng người dùng tiếng Việt!",
            "Về Anti Ăn Toxic (AUT) & Bản quyền UniKey",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!reallyExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray(false);
            return;
        }
        base.OnFormClosing(e);
    }

    private void ExitApplication()
    {
        reallyExit = true;
        trayIcon.Visible = false;
        trayIcon.Dispose();
        KeyboardEngine.Stop();
        Application.Exit();
    }
}
