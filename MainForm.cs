using System.Diagnostics;
using System.Linq;
using Eto.Forms;
using Eto.Drawing;

namespace tekken6ultrawidefix
{
    public class MainForm : Form{
        private struct patchData{
            public byte[] AspectRatio;
            public byte[] Camera;
        }

        //presets
        private Dictionary<string, patchData> presets = new Dictionary<string, patchData>(){
            {
                "21:9",
                new patchData{
                    AspectRatio = new byte[] { 0x40, 0x15, 0x55, 0x55 }, //2.333
                    Camera = new byte[] { 0x43, 0x90, 0x00, 0x00 } //288
                }
            },{
                "32:9 (Normal Battle)",
                new patchData{
                    AspectRatio = new byte[] { 0x40, 0x63, 0x8E, 0x39 }, //3.555
                    Camera = new byte[] { 0x43, 0x83, 0x00, 0x00 } //262
                }
            },{
                "32:9 (Scenario Campaign)",
                new patchData{
                    AspectRatio = new byte[] { 0x40, 0x63, 0x8E, 0x39 }, //3.555
                    Camera = new byte[] { 0x43, 0x6B, 0x00, 0x00 } //235
                }
            }
        };

        private readonly byte[] PATTERN_SEARCH = new byte[]{
            0x43, 0xB4, 0x00, 0x00,
            0x43, 0x48, 0x00, 0x00
        };

        private IMemoryScanner scanner;

        //camera depth offset removes 0x70 (makes vertical bounds similar to default 16:9 aspect)
        private const int OFFSET_AR_FROM_CAM = -0x70;
        //all serials ID variations for t6 (this is what i found, but i only tested on BLUS30359)
        private readonly string[] gameIDs = { "BLUS30359", "BLES00635", "BLJS10067" };

        //original bytes
        private readonly byte[] PATCH_AR_OLD = new byte[] { 0x3F, 0xE3, 0x8E, 0x39 };
        private readonly byte[] PATCH_CAM_OLD = new byte[] { 0x43, 0xB4, 0x00, 0x00 };

        //game hook n sh
        private Process gameProcess;
        private IntPtr addressFound = IntPtr.Zero;
        private bool isHooked = false;
        private bool isScanning = false;
        private UITimer tickTimer;

        //settings data
        private bool minimizeOnTray = true;
        private bool restoreOnExit = true;
        private bool automaticScan = true;
        private bool enabledOnStart = false;
        private int selectedPresetIndex = 0;

        //tool elements and such
        private Label toolStatus;
        private CheckBox checkboxUltrawideFix;
        private DropDown comboPresets;
        private Button btnManualScan;
        private TrayIndicator trayIcon;

        public MainForm(){
            scanner = crossPlataformScanner.Create(); //get scanner for each oOS
            
            //window form
            Title = "Tekken6UltrawideFix";
            Icon = Icon.FromResource("tekken6ultrawidefix.assets.6fixicon.ico");
            MinimumSize = new Size(300, 160); Padding = 10;
            Maximizable = false; Resizable = false;

            toolStatus = new Label { TextAlignment = TextAlignment.Left };
            checkboxUltrawideFix = new CheckBox { Text = "Enable Ultrawide Fix" };
            comboPresets = new DropDown();

            btnManualScan = new Button(); UpdateManualScanButton();
            btnManualScan.Click += async (s, e) => {
                if (isHooked || isScanning) return;
                
                btnManualScan.Text = "Scanning..."; 
                btnManualScan.Enabled = false; 

                if (gameProcess == null || gameProcess.HasExited) {
                    var list = Process.GetProcessesByName("rpcs3");
                    foreach(var p in list){
                        string t = p.MainWindowTitle.ToUpper();
                        if(gameIDs.Any(ser => t.Contains(ser))){
                            gameProcess = p;
                            break;
                        }
                    }
                }

                if (gameProcess != null && !gameProcess.HasExited) {
                    await PerformScanAsync(); 
                }

                UpdateManualScanButton();
            };

            var layout = new DynamicLayout();
            layout.Spacing = new Size(5, 3);
            layout.AddRow(toolStatus);
            layout.AddRow(new Panel{Height = 4});
            layout.AddRow(checkboxUltrawideFix);
            layout.AddRow(comboPresets);

            layout.Add(null);
            layout.AddRow(btnManualScan);

            Content = layout;
            
            SetupLogic();
            SetupTray();
            
            tickTimer = new UITimer();
            tickTimer.Interval = 1; //check interval (1sec by def)
            tickTimer.Elapsed += UpdateTick;
            tickTimer.Start();
        }

        private void SetupLogic(){
            ResetUI();
            LoadSettings();
            
            //dropdwn
            foreach(var key in presets.Keys)comboPresets.Items.Add(key);
            comboPresets.SelectedIndexChanged += ComboPresets_SelectedIndexChanged;
            comboPresets.Enabled = enabledOnStart;
            if(comboPresets.Items.Count > 0)
                comboPresets.SelectedIndex = (selectedPresetIndex >= 0 && selectedPresetIndex < comboPresets.Items.Count) ? selectedPresetIndex : 0;

            checkboxUltrawideFix.Checked = enabledOnStart;
            checkboxUltrawideFix.CheckedChanged += CheckboxUltrawideFix_CheckedChanged;

            UpdateManualScanButton();

            //MENUBAR
            //restore on exit checkbox
            var restoreOnExitItem = new CheckMenuItem {Text = "Restore patch on Exit", Checked = restoreOnExit};
            restoreOnExitItem.Click += (s, e) => { restoreOnExit = restoreOnExitItem.Checked; SaveSettings(); };
            //minimize to tray so it runs in background
            var minimizeTrayitem = new CheckMenuItem {Text = "Minimize to Tray", Checked = minimizeOnTray};
            minimizeTrayitem.Click += (s, e) => { minimizeOnTray = minimizeTrayitem.Checked; SaveSettings(); };
            //automatic scan toggle
            var automaticScanItem = new CheckMenuItem {Text = "Automatic Scan", Checked = automaticScan};
            automaticScanItem.Click += (s, e) => { 
                automaticScan = automaticScanItem.Checked; 
                UpdateManualScanButton(); SaveSettings();
            };

            //about wind
            var aboutMenu = new ButtonMenuItem {Text = "&About"};
            aboutMenu.Click += (s,e) => {
                var dialog = new Dialog { Title = "About", ClientSize = new Size(100, 16), Padding = 8, Resizable = false };
                var label = new Label { Text = "Tekken6UltrawideFix v0.1\nby renzk", TextAlignment = TextAlignment.Center };
                
                var linkGit = new LinkButton { Text = "My Github" }; 
                linkGit.Click += (sender, ev) => Application.Instance.Open("https://github.com/r3nzk");
                var linkRepo = new LinkButton { Text = "Repo" }; 
                linkRepo.Click += (sender, ev) => Application.Instance.Open("https://github.com/r3nzk/tekken6-ultrawidefix");
                
                var linkRow = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5 };
                linkRow.Items.Add(linkGit); linkRow.Items.Add(new Label { Text = "|" }); linkRow.Items.Add(linkRepo);

                var okBtn = new Button { Text = "Close" };
                okBtn.Click += (sender, ev) => dialog.Close();
                
                var dlgLayout = new DynamicLayout();
                dlgLayout.AddCentered(label);
                dlgLayout.AddRow(new Panel { Height = 5 });
                dlgLayout.AddCentered(linkRow);
                dlgLayout.AddCentered(okBtn);
                
                dialog.Content = dlgLayout;
                dialog.ShowModal(this);
            };;

            Menu = new MenuBar{
                Items ={
                    new ButtonMenuItem { Text = "&Settings", Items = {restoreOnExitItem, minimizeTrayitem, automaticScanItem }}
                }, AboutItem = aboutMenu
            };
        }

        #region tray behaviour

        private void SetupTray(){
            trayIcon = new TrayIndicator();
            trayIcon.Title = "Tekken6UltrawideFix";
            trayIcon.Icon = Icon.FromResource("tekken6ultrawidefix.assets.6fixicon.ico");
            trayIcon.Visible = false;
            
            var exitItem = new ButtonMenuItem {Text = "Exit"};
            exitItem.Click += (s, e) => this.Close();
            trayIcon.Menu = new ContextMenu(exitItem);

            trayIcon.Activated += (s, e) =>RestoreFromTray();
        }
        private void RestoreFromTray(){
            this.Visible = true;
            this.WindowState = WindowState.Normal;
            trayIcon.Visible = false;
        }
        protected override void OnWindowStateChanged(EventArgs e){
            base.OnWindowStateChanged(e);
            if (WindowState == WindowState.Minimized && minimizeOnTray){
                this.Visible = false;
                trayIcon.Visible = true;
            }
        }

        #endregion


        protected override void OnClosing(System.ComponentModel.CancelEventArgs e){
            if(restoreOnExit && isHooked && addressFound != IntPtr.Zero)
                ApplyPatch(false);

            if(trayIcon != null)trayIcon.Dispose();
                base.OnClosing(e);
        }

        private void UpdateTick(object sender, EventArgs e){
            if(gameProcess != null && !gameProcess.HasExited){
                gameProcess.Refresh();
                string t = gameProcess.MainWindowTitle.ToUpper();
                if(!gameIDs.Any(s => t.Contains(s))){
                    gameProcess = null;
                }
            }

            if(gameProcess == null || gameProcess.HasExited){
                if(toolStatus.Text != "Waiting for Tekken 6...") ResetUI();
                if(automaticScan){
                    var list = Process.GetProcessesByName("rpcs3");
                    foreach(var p in list){
                        string t = p.MainWindowTitle.ToUpper();
                        if(gameIDs.Any(s => t.Contains(s))){
                            gameProcess = p;
                            UpdateStatus("Tekken 6 Found! Searching address..", Colors.DarkOliveGreen);
                            return;
                        }
                    }
                }
                return;
            }

            if(!isHooked && !isScanning){
                if(automaticScan){
                    _ = PerformScanAsync();
                }
            }else if(isHooked){
                if(toolStatus.TextColor != Colors.Green){
                    UpdateStatus("Ready!", Colors.Green);

                    if(checkboxUltrawideFix.Checked == true)   
                        ApplyPatch(true);
                }
            }
        }

        private async Task PerformScanAsync(){
            isScanning = true;
            await Task.Run(() =>{
                IntPtr camAddr = scanner.FindPattern(gameProcess, PATTERN_SEARCH);
                if(camAddr != IntPtr.Zero){
                    addressFound = camAddr;
                    isHooked = true;
                }
            });
            isScanning = false;
        }

                private void ComboPresets_SelectedIndexChanged(object sender, EventArgs e){
            if(isHooked && checkboxUltrawideFix.Checked == true)ApplyPatch(true);
            SaveSettings();
        }

                private void CheckboxUltrawideFix_CheckedChanged(object sender, EventArgs e){
            ApplyPatch(checkboxUltrawideFix.Checked == true);
            comboPresets.Enabled = checkboxUltrawideFix.Checked == true;
            SaveSettings();
        }

        private void ApplyPatch(bool enable){
            if(!isHooked || addressFound == IntPtr.Zero || gameProcess.HasExited)return;

            IntPtr arAddress = IntPtr.Add(addressFound, OFFSET_AR_FROM_CAM);

            try{
                if(enable){
                    string selectedKey = comboPresets.SelectedKey;
                    if(presets.ContainsKey(selectedKey)){
                        var data = presets[selectedKey];
                        scanner.WriteBytes(gameProcess, arAddress, data.AspectRatio);
                        scanner.WriteBytes(gameProcess, addressFound, data.Camera);
                    }
                }else{
                    scanner.WriteBytes(gameProcess, arAddress, PATCH_AR_OLD);
                    scanner.WriteBytes(gameProcess, addressFound, PATCH_CAM_OLD);
                }
            }catch{
                ResetUI();
            }
        }

        #region settings ini
        private string GetIniPath() {
            string exePath = Environment.ProcessPath ?? AppDomain.CurrentDomain.BaseDirectory;
            string dir = System.IO.Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;
            return System.IO.Path.Combine(dir, "settings.ini");
        }

        private void LoadSettings() {
            string path = GetIniPath();
            if(System.IO.File.Exists(path)){
                var lines = System.IO.File.ReadAllLines(path);
                foreach(var line in lines) {
                    var parts = line.Split('=');
                    if(parts.Length == 2) {
                        var key = parts[0].Trim();
                        var val = parts[1].Trim().ToLower();
                        bool boolVal = val == "true" || val == "1";
                        if(key == "MinimizeOnTray") minimizeOnTray = boolVal;
                        else if(key == "RestoreOnExit")restoreOnExit = boolVal;
                        else if(key == "AutomaticScan")automaticScan = boolVal;
                        else if(key == "SelectedPreset")int.TryParse(val, out selectedPresetIndex);
                        else if (key == "Enabled") enabledOnStart = boolVal;
                    }
                }
            }else{
                SaveSettings();
            }
        }

        private void SaveSettings(){
            try{
                string path = GetIniPath();
                string content = "MinimizeOnTray=" + minimizeOnTray +
                                "\nRestoreOnExit=" + restoreOnExit +
                                "\nAutomaticScan=" + automaticScan +
                                "\nSelectedPreset=" + comboPresets.SelectedIndex +
                                "\nEnabled=" + (checkboxUltrawideFix.Checked == true);
                System.IO.File.WriteAllText(path, content);
            }catch{}
        }

        #endregion

        private void UpdateManualScanButton(){
            if(btnManualScan == null || isScanning) return;
            btnManualScan.Enabled = !automaticScan && !isHooked;
            btnManualScan.Text = automaticScan ? "Automatic Scan is Enabled" : "Manual Scan";
        }

        private void UpdateStatus(string text, Color color){
            toolStatus.Text = text;
            toolStatus.TextColor = color;
        }

        private void ResetUI(){
            UpdateManualScanButton();
            UpdateStatus("Waiting for Tekken 6...", Colors.Red);

            gameProcess = null;
            isHooked = false;
            addressFound = IntPtr.Zero;
        }
    }
}









