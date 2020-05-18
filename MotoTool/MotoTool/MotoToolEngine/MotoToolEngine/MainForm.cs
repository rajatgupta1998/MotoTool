﻿
using AutoUpdaterDotNET;
using MaterialSkin;
using MaterialSkin.Controls;
using AndroidCtrl.ADB;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using AndroidCtrl;
using AndroidCtrl.Fastboot;
using System.Collections.Generic;
using AndroidCtrl.Tools;
using System.Threading.Tasks;

namespace Franco28Tool.Engine
{
    public partial class MainForm : MaterialForm
    {
        [System.ComponentModel.Browsable(false)]
        private readonly MaterialSkinManager materialSkinManager;
        private PerformanceCounter ramCounter;
        private PerformanceCounter cpuCounter;
        private SettingsMng oConfigMng = new SettingsMng();
        string ToolVer = Assembly.GetEntryAssembly().GetName().Version.ToString();
        public Form activeForm = null;
        ArrayList devicecheck = new ArrayList();
        IDDeviceState state = General.CheckDeviceState(ADB.Instance().DeviceID);

        public MainForm()
        {
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.EnforceBackcolorOnAllComponents = true;
            materialSkinManager.AddFormToManage(this);
            InitializeComponent();
            cAppend("Checking MotoTool updates.");
            cehck4updates();
            cAppend("Loading tool... Please wait...");
            ADB.PATH_DIRECTORY = @"C:\adb\";
            Fastboot.PATH_DIRECTORY = @"C:\adb\";
            cAppend("Settings adb & fastboot path... Please wait...");
            oConfigMng.LoadConfig();
            cAppend("Checking MotoDrivers...");
            if (File.Exists(@"C:\Program Files (x86)\Motorola Mobility\Motorola Device Manager\uninstall.exe"))
            {
                if (File.Exists(@"C:\Program Files\MotoTool\MotorolaDeviceManager_2.5.4.exe"))
                {
                    string _batDir = string.Format(@"C:\Program Files\MotoTool\");
                    using (Process proc = new Process())
                    {
                        proc.StartInfo.WorkingDirectory = _batDir;
                        proc.StartInfo.FileName = "remove.bat";
                        proc.StartInfo.CreateNoWindow = false;
                        proc.Start();
                        proc.WaitForExit();
                        int ExitCode = proc.ExitCode;
                    }
                }
                cAppend("Checking MotoDrivers... OK");
            }
            else
            {
                cAppend("Checking MotoDrivers... ERROR");
                CheckMotoDrivers.MotoDrivers();
            }
            updateColorLoad();
            AvoidFlick();
            InitializeRAMCounter();
            InitialiseCPUCounter();
            updateTimer_Tick();
        }

        public void CheckandDeploy()
        {
            if (ADB.IntegrityCheck() == false)
            {
                Deploy.ADB();
            }
            if (Fastboot.IntegrityCheck() == false)
            {
                Deploy.Fastboot();
            }
            if (ADB.IsStarted)
            {
                ADB.Stop();
                ADB.Stop(true);
            }
            else
            {
                ADB.Start();
            }
        }

        public void cAppend(string message)
        {
            this.Invoke((Action)delegate
            {
                console.AppendText(string.Format("\n{0} : {1}", DateTime.Now, message));
                console.ScrollToCaret();
            });
        }

        private async void MainForm_LoadAsync(object sender, EventArgs e)
        {
            oConfigMng.LoadConfig();
            cAppend("Deploying adb & fastboot...");
            await Task.Run(() => CheckandDeploy());
            await Task.Run(() => DeviceDetectionService());
            oConfigMng.Config.ToolVersion = ToolVer;
            oConfigMng.Config.ToolCompiled = Utils.GetLinkerDateTime(Assembly.GetEntryAssembly(), null).ToString();
            cAppend("Checking MotoTool ver: " + ToolVer);
            cAppend("Setting MotoTool Theme...");
            if (oConfigMng.Config.ToolTheme == null || oConfigMng.Config.ToolTheme == "")
            {
                cAppend("Setting MotoTool Theme... LIGHT ADDED");
                materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
                materialSkinManager.ColorScheme = new ColorScheme(Primary.Indigo500, Primary.Indigo700, Primary.Indigo100, Accent.Pink200, TextShade.WHITE);
                oConfigMng.Config.ToolTheme = "light";
            }
            cAppend("Checking saved device...");
            if (oConfigMng.Config.DeviceCodenmae == null || oConfigMng.Config.DeviceCodenmae == "")
            {
                cAppend("Checking saved device... NOT FOUND");
                oConfigMng.Config.DeviceCodenmae = "";
            }
            else
            {
                cAppend("Checking saved device... " + oConfigMng.Config.DeviceCodenmae);
            }
            cAppend("Checking saved device carrier...");
            if (oConfigMng.Config.DeviceFirmware == null || oConfigMng.Config.DeviceFirmware == "")
            {
                cAppend("Checking saved device carrier... NOT FOUND");
                oConfigMng.Config.DeviceFirmware = "";
            }
            else
            {
                cAppend("Checking saved device carrier... " + oConfigMng.Config.DeviceFirmware);
            }
            cAppend("Checking internet connection...");
            if (InternetCheck.ConnectToInternet() == true)
            {
                cAppend("Checking internet connection... OK");
                oConfigMng.Config.ToolInternet = "Online";
            }
            else
            {
                cAppend("Checking internet connection... ERROR");
                oConfigMng.Config.ToolInternet = "Offline";
            }
            cAppend("Applying tool settings...");
            if (oConfigMng.Config.DrawerUseColors == null || oConfigMng.Config.DrawerUseColors == "")
            {
                oConfigMng.Config.DrawerUseColors = "false";
            }
            if (oConfigMng.Config.DrawerHighlightWithAccent == null || oConfigMng.Config.DrawerHighlightWithAccent == "")
            {
                oConfigMng.Config.DrawerHighlightWithAccent = "true";
            }
            if (oConfigMng.Config.DrawerBackgroundWithAccent == null || oConfigMng.Config.DrawerBackgroundWithAccent == "")
            {
                oConfigMng.Config.DrawerBackgroundWithAccent = "false";
            }
            if (oConfigMng.Config.DrawerShowIconsWhenHidden == null || oConfigMng.Config.DrawerShowIconsWhenHidden == "")
            {
                oConfigMng.Config.DrawerShowIconsWhenHidden = "true";
            }
            if (oConfigMng.Config.Autosavelogs == null || oConfigMng.Config.Autosavelogs == "")
            {
                oConfigMng.Config.Autosavelogs = "true";
            }
            cAppend("Applying tool settings... OK");
            oConfigMng.SaveConfig();
            cAppend("MotoTool was loaded!");
        }

        private void InitialiseCPUCounter()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
        }

        private void InitializeRAMCounter()
        {
            ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
        }

        private void updateTimer_Tick()
        {
            Timer timer = new Timer();
            timer.Interval = (1 * 1000);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        private delegate void SetRefreshCallback();

        private void SetRefresh()
        {
            if (this.InvokeRequired)
            {
                SetRefreshCallback d = new SetRefreshCallback(SetRefresh);
                base.Invoke(d);
            }
            else
                base.Refresh();
        }

        public new void Refresh()
        {
            SetRefresh();
        }

        public void AvoidFlick()
        {
            this.DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            oConfigMng.LoadConfig();
            labelFreeRam.Text = "      Free RAM: " + Convert.ToInt64(ramCounter.NextValue()).ToString() + " MB";
            labelCPUusage.Text = "      CPU: " + Convert.ToInt64(cpuCounter.NextValue()).ToString() + "%";
            labelFreeSpace.Text = @"      Folder Size: C:\adb: " + Folders.GetDirectorySize(@"C:\adb") + " MB";
            labelUserName.Text = "      User: " + Environment.UserName;
            TextBoxDebug.Text = "Remember to always Backup your efs and persist folders!";
            if (oConfigMng.Config.DeviceFirmware == "" || oConfigMng.Config.DeviceFirmware == null)
            {
                TextBoxDebugInfo.Text = "Device Channel: ---";
            }
            else
            {
                TextBoxDebugInfo.Text = "Device Channel: " + oConfigMng.Config.DeviceFirmware;
            }
            if (oConfigMng.Config.DeviceCodenmae == "" || oConfigMng.Config.DeviceFirmware == null)
            {
                materialButtonBlankFlash.Enabled = false;
                materialButtonFlashLogo.Enabled = false;
                materialButtonFlashTWRP.Enabled = false;
                materialButtonBootTWRP.Enabled = false;
                this.Text = "MotoTool v" + oConfigMng.Config.ToolVersion;
            }
            else
            {
                this.Text = "MotoTool v" + oConfigMng.Config.ToolVersion + " - " + oConfigMng.Config.DeviceCodenmae;
                materialButtonBlankFlash.Enabled = true;
                materialButtonFlashLogo.Enabled = true;
                materialButtonFlashTWRP.Enabled = true;
                materialButtonBootTWRP.Enabled = true;
                if (oConfigMng.Config.DeviceCodenmae == oConfigMng.Config.DeviceCodenmae)
                {
                    LoadDeviceServer.CheckDevice(oConfigMng.Config.DeviceCodenmae + ".xml", oConfigMng.Config.DeviceCodenmae);
                }
            }
            if (oConfigMng.Config.ToolInternet == "Online")
            {
                DisplayWarning("Network lost... Please check your internet connection!");
            }
            Firmwares.CreateFirmwareFolder();
        }

        public void openChildFormWarningNoti(Form childForm)
        {
            if (activeForm != null) activeForm.Close();
            activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Top;
            panelDownload.Controls.Add(childForm);
            panelDownload.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();
        }

        private void DisplayWarning(string errortext)
        {
            var t = new Timer();
            var noti = new NotiPanel();
            t.Interval = 3000;
            if (InternetCheck.ConnectToInternet() == false)
            {
                noti.label2.Text = errortext;
                openChildFormWarningNoti(noti);
            }
            t.Tick += (s, e) =>
            {
                t.Stop();
                noti.Hide();
            };
            t.Start();
        }

        public void canceltoolload()
        {
            cAppend("MotoTool: DEVICE NOT COMPATIBLE: " + oConfigMng.Config.DeviceCodenmae + "Device " + oConfigMng.Config.DeviceCodenmae + " is not compatible. This MotoTool works with: " + " \nBeckham, Doha, Lake, Evert, Potter, Sanders, River, Ocean, Lima, Sofiar, Sofia");
            Dialogs.WarningDialog("MotoTool: DEVICE NOT COMPATIBLE: " + oConfigMng.Config.DeviceCodenmae, "Device " + oConfigMng.Config.DeviceCodenmae + " is not compatible. This MotoTool works with: " + " \nBeckham, Doha, Lake, Evert, Potter, Sanders, River, Ocean, Lima, Sofiar, Sofia");
            KillWhenExit();
        }

        public void DeviceCompatible()
        {
            oConfigMng.LoadConfig();
            if (oConfigMng.Config.DeviceCodenmae == "")
            {
                TextBoxDebug.Text = "Please connect your device, so MotoTool can check your device!";
            }
            if (oConfigMng.Config.DeviceCodenmae != "beckham" ||
                oConfigMng.Config.DeviceCodenmae != "doha" ||
                oConfigMng.Config.DeviceCodenmae != "evert" ||
                oConfigMng.Config.DeviceCodenmae != "lake" ||
                oConfigMng.Config.DeviceCodenmae != "lima" ||
                oConfigMng.Config.DeviceCodenmae != "river" ||
                oConfigMng.Config.DeviceCodenmae != "potter" ||
                oConfigMng.Config.DeviceCodenmae != "ocean" ||
                oConfigMng.Config.DeviceCodenmae != "sanders" ||
                oConfigMng.Config.DeviceCodenmae != "sofiar" ||
                oConfigMng.Config.DeviceCodenmae == "sofia" ||
                oConfigMng.Config.DeviceCodenmae == "ginkgo")
            {
                canceltoolload();
            }
        }

        private void SetDeviceList()
        {
            string active = String.Empty;

            this.Invoke((Action)delegate
            {
                // Here we refresh our combobox
                devicecheck.Clear();
            });

            // This will get the currently connected ADB devices
            List<DataModelDevicesItem> adbDevices = ADB.Devices();

            // This will get the currently connected Fastboot devices
            List<DataModelDevicesItem> fastbootDevices = Fastboot.Devices();

            foreach (DataModelDevicesItem device in adbDevices)
            {
                this.Invoke((Action)delegate
                {
                    cAppend("Device adb connected!");
                    devicecheck.Add(" Device: Online! - ADB");
                    devicecheck.Add(" Device Codename: " + LoadDeviceServer.devicecodename);
                    devicecheck.Add(" Mode: " + state);
                    listBoxDeviceStatus.DataSource = devicecheck;
                    GetProp();
                });
            }
            foreach (DataModelDevicesItem device in fastbootDevices)
            {
                this.Invoke((Action)delegate
                {
                    cAppend("Device fastboot connected!");
                    devicecheck.Add(" Device: Online! - FASTBOOT");
                    devicecheck.Add(" Device Codename: " + LoadDeviceServer.devicecodename);
                    devicecheck.Add(" Mode: " + state);
                    listBoxDeviceStatus.DataSource = devicecheck;
                    GetProp();
                });
            }
            // This calls will select the BASE class if we have no connected devices
            ADB.SelectDevice();
            Fastboot.SelectDevice();
        }

        private void DeviceDetectionService()
        {
            ADB.Start();

            // Here we initiate the BASE Fastboot instance
            Fastboot.Instance();

            //This will starte a thread which checks every 10 sec for connected devices and call the given callback
            if (Fastboot.ConnectionMonitor.Start())
            {
                //Here we define our callback function which will be raised if a device connects or disconnects
                Fastboot.ConnectionMonitor.Callback += ConnectionMonitorCallback;

                // Here we check if ADB is running and initiate the BASE ADB instance (IsStarted() will everytime check if the BASE ADB class exists, if not it will create it)
                if (ADB.IsStarted)
                {
                    //Here we check for connected devices
                    SetDeviceList();

                    //This will starte a thread which checks every 10 sec for connected devices and call the given callback
                    if (ADB.ConnectionMonitor.Start())
                    {
                        //Here we define our callback function which will be raised if a device connects or disconnects
                        ADB.ConnectionMonitor.Callback += ConnectionMonitorCallback;
                    }
                }
            }
        }

        public void ConnectionMonitorCallback(object sender, ConnectionMonitorArgs e)
        {
            this.Invoke((Action)delegate
            {
                // Do what u want with the "List<DataModelDevicesItem> e.Devices"
                // The "sender" is a "string" and returns "adb" or "fastboot"
                SetDeviceList();
            });
        }

        public async void GetProp()
        {
            oConfigMng.LoadConfig();
            if (oConfigMng.Config.DeviceFirmware == "" || oConfigMng.Config.DeviceCodenmae == "")
            {
                cAppend("Device Info: Getting device codename and carrier...");
                if (IDDeviceState.UNKNOWN == state)
                {
                    cAppend("Device Info: Waiting for device...");
                    await Task.Run(() => ADB.WaitForDevice());
                    cAppend("Device Info: Getting device codename and carrier...");
                    List<String> getdevicecodename = await Task.Run(() => ADB.Instance().Execute("shell /system/bin/getprop ro.product.device"));
                    List<String> getcarrier = await Task.Run(() => ADB.Instance().Execute("shell /system/bin/getprop ro.carrier"));
                    getdevicecodename.ToString();
                    getcarrier.ToString();
                    var result = String.Join("", getdevicecodename.ToArray());
                    var result2 = String.Join("", getcarrier.ToArray());
                    oConfigMng.Config.DeviceCodenmae = result;
                    oConfigMng.Config.DeviceFirmware = result2;
                }
                else
                {
                    cAppend("Device Info: Your device is in the wrong state. Please connect your device.\n");
                }
                oConfigMng.SaveConfig();
                DeviceCompatible();
            }
            else
            {
                cAppend("Device Info: Device already added. Device: " + oConfigMng.Config.DeviceCodenmae + " Device carrier: " + oConfigMng.Config.DeviceFirmware);
            }
        }

        private void AutoUpdater_ApplicationExitEvent()
        {
            Text = @"Closing application...";
            Thread.Sleep(2000);
            Application.Exit();
        }

        private void cehck4updates()
        {
            System.Timers.Timer timer = new System.Timers.Timer
            {
                Interval = 1 * 30 * 100,
                SynchronizingObject = this
            };
            timer.Elapsed += delegate
            {
                AutoUpdater.Start("https://mototoolengine.000webhostapp.com/MotoTool/Update.xml");
            };
            timer.Start();
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;
            if (args != null)
            {
                if (args.IsUpdateAvailable)
                {
                    cAppend("Cheking tool updates... NEW UPDATE! " + args.CurrentVersion);
                    this.Text = "New MotoTool Update " + args.CurrentVersion + ", please update now!";
                    AutoUpdater.ShowUpdateForm(args);
                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                        {
                            Application.Exit();
                        }
                    }
                    catch (Exception exception)
                    {
                        Logs.DebugErrorLogs(exception);
                        Dialogs.ErrorDialog(exception.Message, exception.GetType().ToString());
                        Application.Restart();
                    }
                }
                else
                {
                    Dialogs.InfoDialog("No update available", "There is no update available please try again later, or Tool will let you know!");
                    Application.Restart();
                }
            }
            else
            {
                Dialogs.ErrorDialog("Update check failed", "There is a problem reaching update server please check your internet connection and try again later.");
                Application.Restart();
            }
        }

        public void openChildForm(Form childForm)
        {
            if (activeForm != null) activeForm.Close();
            activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            panelChild.Controls.Add(childForm);
            panelChild.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();
        }

        public void openChildFormFirmware(Form childForm)
        {
            if (activeForm != null) activeForm.Close();
            activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            panelFimrware.Controls.Add(childForm);
            panelFimrware.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();
        }

        public void openChildFormFlashTool(Form childForm)
        {
            if (activeForm != null) activeForm.Close();
            activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            panelFlashTool.Controls.Add(childForm);
            panelFlashTool.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();
        }

        public void downloadcall(string xmlname, string xmlpath)
        {
            var call = new DownloadUI();
            DownloadsMng.TOOLDOWNLOAD(oConfigMng.Config.DeviceCodenmae, xmlname, xmlpath);
            openChildFormWarningNoti(call);
        }

        public void qBootExecuteCommand()
        {
            string _batDir = string.Format(@"C:\adb\Others\" + LoadDeviceServer.unbrickname);
            using (Process proc = new Process())
            {
                proc.StartInfo.WorkingDirectory = _batDir;
                proc.StartInfo.FileName = "blank-flash.bat";
                proc.StartInfo.CreateNoWindow = false;
                proc.Start();
                proc.WaitForExit();
                int ExitCode = proc.ExitCode;
            }
        }

        #region ToolTheme
        private void materialButtonChangeTheme_Click(object sender, EventArgs e)
        {
            oConfigMng.LoadConfig();

            materialSkinManager.Theme = materialSkinManager.Theme == MaterialSkinManager.Themes.DARK ? MaterialSkinManager.Themes.LIGHT : MaterialSkinManager.Themes.DARK;

            if (materialSkinManager.Theme == MaterialSkinManager.Themes.DARK)
            {
                cAppend("Theme changed to: DARK");
                oConfigMng.Config.ToolTheme = "dark";
            }

            if (materialSkinManager.Theme == MaterialSkinManager.Themes.LIGHT)
            {
                cAppend("Theme changed to: LIGHT");
                oConfigMng.Config.ToolTheme = "light";
            }
            oConfigMng.SaveConfig();
            updateColor();
        }

        private int colorSchemeIndex;

        public bool AutoSaveLogs { get; private set; }

        public void updateColorLoad()
        {
            oConfigMng.LoadConfig();

            if (oConfigMng.Config.ToolTheme == "light")
            {
                cAppend("Loading MotoTool Theme... LIGHT ADDED");
                materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            }
            if (oConfigMng.Config.ToolTheme == "dark")
            {
                cAppend("Loading MotoTool Theme... DARK ADDED");
                materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            }
            if (oConfigMng.Config.ToolThemeBlueColor == "true")
            {
                colorSchemeIndex = 0;
            }
            if (oConfigMng.Config.ToolThemeGreenColor == "true")
            {
                colorSchemeIndex = 1;
            }
            if (oConfigMng.Config.ToolThemeIndigoColor == "true")
            {
                colorSchemeIndex = 2;
            }
            if (oConfigMng.Config.DrawerUseColors == "true")
            {
                DrawerUseColors = materialSwitchDrawerUseColors.Checked;
                DrawerUseColors = materialSwitchDrawerUseColors.Checked = true;
            }
            if (oConfigMng.Config.DrawerHighlightWithAccent == "false")
            {
                DrawerHighlightWithAccent = materialSwitchDrawerHighlightWithAccent.Checked;
                DrawerHighlightWithAccent = materialSwitchDrawerHighlightWithAccent.Checked = false;
            }
            if (oConfigMng.Config.DrawerBackgroundWithAccent == "true")
            {
                DrawerBackgroundWithAccent = materialSwitchDrawerBackgroundWithAccent.Checked;
                DrawerBackgroundWithAccent = materialSwitchDrawerBackgroundWithAccent.Checked = true;
            }
            if (oConfigMng.Config.DrawerShowIconsWhenHidden == "false")
            {
                DrawerShowIconsWhenHidden = materialSwitchDrawerShowIconsWhenHidden.Checked;
                DrawerShowIconsWhenHidden = materialSwitchDrawerShowIconsWhenHidden.Checked = false;
            }
            updateColor();
        }

        private void MaterialButtonChangeColor_Click(object sender, EventArgs e)
        {
            colorSchemeIndex++;
            if (colorSchemeIndex > 2)
                colorSchemeIndex = 0;
            updateColor();
        }

        private void updateColor()
        {
            switch (colorSchemeIndex)
            {
                case 0:
                    materialSkinManager.ColorScheme = new ColorScheme(
                        materialSkinManager.Theme == MaterialSkinManager.Themes.DARK ? Primary.Teal500 : Primary.Indigo500,
                        materialSkinManager.Theme == MaterialSkinManager.Themes.DARK ? Primary.Teal700 : Primary.Indigo700,
                        materialSkinManager.Theme == MaterialSkinManager.Themes.DARK ? Primary.Teal200 : Primary.Indigo100,
                        Accent.Pink200,
                        TextShade.WHITE);
                    oConfigMng.Config.ToolThemeBlueColor = "true";
                    oConfigMng.Config.ToolThemeIndigoColor = "false";
                    oConfigMng.Config.ToolThemeGreenColor = "false";
                    oConfigMng.SaveConfig();
                    break;
                case 1:
                    materialSkinManager.ColorScheme = new ColorScheme(
                        Primary.Green600,
                        Primary.Green700,
                        Primary.Green200,
                        Accent.Red100,
                        TextShade.WHITE);
                    oConfigMng.Config.ToolThemeBlueColor = "false";
                    oConfigMng.Config.ToolThemeIndigoColor = "false";
                    oConfigMng.Config.ToolThemeGreenColor = "true";
                    oConfigMng.SaveConfig();
                    break;
                case 2:
                    materialSkinManager.ColorScheme = new ColorScheme(
                        Primary.BlueGrey800,
                        Primary.BlueGrey900,
                        Primary.BlueGrey500,
                        Accent.LightBlue200,
                        TextShade.WHITE);
                    oConfigMng.Config.ToolThemeBlueColor = "false";
                    oConfigMng.Config.ToolThemeIndigoColor = "true";
                    oConfigMng.Config.ToolThemeGreenColor = "false";
                    oConfigMng.SaveConfig();
                    break;
            }
            Invalidate();
        }

        private void materialSwitchDrawerUseColors_CheckedChanged(object sender, EventArgs e)
        {
            DrawerUseColors = materialSwitchDrawerUseColors.Checked;
            oConfigMng.LoadConfig();
            if (DrawerUseColors = materialSwitchDrawerUseColors.Checked == true)
            {
                oConfigMng.Config.DrawerUseColors = "true";
            }
            else
            {
                oConfigMng.Config.DrawerUseColors = "false";
            }
            oConfigMng.SaveConfig();
            Invalidate();
        }

        private void MaterialSwitchDrawerHighlightWithAccent_CheckedChanged(object sender, EventArgs e)
        {
            DrawerHighlightWithAccent = materialSwitchDrawerHighlightWithAccent.Checked;
            oConfigMng.LoadConfig();
            if (DrawerHighlightWithAccent = materialSwitchDrawerHighlightWithAccent.Checked == true)
            {
                oConfigMng.Config.DrawerHighlightWithAccent = "true";
            }
            else
            {
                oConfigMng.Config.DrawerHighlightWithAccent = "false";
            }
            oConfigMng.SaveConfig();
            Invalidate();
        }

        private void MaterialSwitchDrawerBackgroundWithAccent_CheckedChanged(object sender, EventArgs e)
        {
            DrawerBackgroundWithAccent = materialSwitchDrawerBackgroundWithAccent.Checked;
            oConfigMng.LoadConfig();
            if (DrawerBackgroundWithAccent = materialSwitchDrawerBackgroundWithAccent.Checked == true)
            {
                oConfigMng.Config.DrawerBackgroundWithAccent = "true";
            }
            else
            {
                oConfigMng.Config.DrawerBackgroundWithAccent = "false";
            }
            oConfigMng.SaveConfig();
            Invalidate();
        }

        private void materialSwitchDrawerShowIconsWhenHidden_CheckedChanged(object sender, EventArgs e)
        {
            DrawerShowIconsWhenHidden = materialSwitchDrawerShowIconsWhenHidden.Checked;
            oConfigMng.LoadConfig();
            if (DrawerShowIconsWhenHidden = materialSwitchDrawerShowIconsWhenHidden.Checked == true)
            {
                oConfigMng.Config.DrawerShowIconsWhenHidden = "true";
            }
            else
            {
                oConfigMng.Config.DrawerShowIconsWhenHidden = "false";
            }
            oConfigMng.SaveConfig();
            Invalidate();
        }

        #endregion ToolTheme

        private void materialButtonUnlock_Click(object sender, EventArgs e)
        {
            Dialogs.WarningDialog("Bootloader: README PLEASE!", "To Unlock Moto Bootloader: Sign in on the following page. Follow the guide on the page. The Tool will open a CMD Console where drivers are allocated. Here you must enter the commands lines!");

            if (File.Exists(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe") & File.Exists(@"C:\Program Files\Google\Chrome\Application\chrome.exe") == true)
            {
                BrowserCheck.StartBrowser("MicrosoftEdge.exe", "https://motorola-global-portal.custhelp.com/app/standalone/bootloader/unlock-your-device-b");
            }
            else
            {
                BrowserCheck.StartBrowser("Chrome.exe", "https://motorola-global-portal.custhelp.com/app/standalone/bootloader/unlock-your-device-b");
            }
            Directory.SetCurrentDirectory(@"C:\\adb\\");
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.FileName = "cmd.exe";
            process.StartInfo = startInfo;
            process.Start();
        }

        private async void materialButtonLock_ClickAsync(object sender, EventArgs e)
        {
            var result = MessageBox.Show("1°: Do you want to Lock the bootloader? This will erase all your data!", "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                Dialogs.WarningDialog("Bootloader: Warning!", "Try to not UNPLUG THE DEVICE during the process!");
                TextBoxDebug.Text = "Checking device connection...";
                if (Fastboot.Instance().DeviceID.Equals(true))
                {
                    TextBoxDebug.Text = "Checking device connection... OK";
                    Thread.Sleep(1000);
                    TextBoxDebug.Text = "Locking bootloader...";
                    Thread.Sleep(1000);
                    Fastboot.Instance().Execute("oem lock");
                    Thread.Sleep(500);
                    var result2 = MessageBox.Show("2°: Are you sure that you want to Lock the bootloader? This will erase all your data!", "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result2 == DialogResult.Yes)
                    {
                        if (Fastboot.Instance().DeviceID.Equals(true))
                        {
                            Thread.Sleep(1000);
                            TextBoxDebug.Text = "Locking bootloader...";
                            Thread.Sleep(1000);
                            Fastboot.Instance().Execute("oem lock");
                            TextBoxDebug.Text = "Locking bootloader... OK";
                            Thread.Sleep(500);
                            await Task.Run(() => Fastboot.Instance().Reboot(IDBoot.REBOOT));
                            TextBoxDebug.Text = "Rebooting...";
                        }
                        else
                        {
                            TextBoxDebug.Text = "Checking device connection...";
                            Thread.Sleep(1000);
                            Strings.MSGBOXBootloaderWarning();
                        }
                    }
                    else
                    {
                        Dialogs.WarningDialog("Bootloader", "Lock Bootloader canceled...");
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    TextBoxDebug.Text = "Please connect your device...";
                    Thread.Sleep(1000);
                    Strings.MSGBOXBootloaderWarning();
                }
            }
        }

        private async void materialButtonFlashTWRP_Click(object sender, EventArgs e)
        {
            string fileName = @"C:\adb\TWRP\" + LoadDeviceServer.twrpname;
            if (oConfigMng.Config.DeviceCodenmae == "doha" ||
                oConfigMng.Config.DeviceCodenmae == "river" ||
                oConfigMng.Config.DeviceCodenmae == "beckham" ||
                oConfigMng.Config.DeviceCodenmae == "ocean")
            {
                Dialogs.TWRPPermanent("FLASH: TWRP", "Hey! This option is not for A/B devices!, but you can download TWRP installer and flash it! first you need to boot TWRP!");
                return;
            }
            if (oConfigMng.Config.DeviceCodenmae == "evert" ||
                oConfigMng.Config.DeviceCodenmae == "lake" ||
                oConfigMng.Config.DeviceCodenmae == "lima" ||
                oConfigMng.Config.DeviceCodenmae == "sofiar" ||
                oConfigMng.Config.DeviceCodenmae == "sofia")
            {
                Dialogs.InfoDialog("FLASH: TWRP", "Hey! This option is not for A/B devices!, check the Boot option!");
                return;
            }
            if (oConfigMng.Config.DeviceCodenmae == "")
            {
                Dialogs.WarningDialog("FLASH: TWRP", "Please connect your device, so MotoTool can check your device!");
                return;
            }
            if (oConfigMng.Config.DeviceCodenmae == oConfigMng.Config.DeviceCodenmae)
            {
                LoadDeviceServer.CheckDevice(oConfigMng.Config.DeviceCodenmae + ".xml", oConfigMng.Config.DeviceCodenmae);
            }
            if (!File.Exists(fileName))
            {
                downloadcall("/TWRP.xml", "TWRP");
                return;
            }
            else
            {
                if (state == IDDeviceState.UNKNOWN)
                {
                    cAppend("FLASH TWRP: Waiting for device...");
                    await Task.Run(() => ADB.WaitForDevice());
                    cAppend("FLASH TWRP: Rebooting into bootloader mode.");
                    await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
                }
                if (state == IDDeviceState.FASTBOOT)
                {
                    cAppend("FLASH TWRP: Flashing TWRP...");
                    await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.RECOVERY, LoadDeviceServer.twrpname));
                    cAppend("FLASH TWRP: Done flashing TWRP.\n");
                }
                else
                {
                    Thread.Sleep(1000);
                    Strings.MSGBOXBootloaderWarning();
                    cAppend("FLASH TWRP: Your device is in the wrong state. Please put your device in the bootloader.\n");
                }
            }
        }

        private async void materialButtonBootTWRP_Click(object sender, EventArgs e)
        {
            if (oConfigMng.Config.DeviceCodenmae == "lima" || oConfigMng.Config.DeviceCodenmae == "sofiar" || oConfigMng.Config.DeviceCodenmae == "sofia")
            {
                Dialogs.InfoDialog("BOOT: TWRP", "Hey this device doesn't have twrp yet! :(");
                return;
            }
            if (oConfigMng.Config.DeviceCodenmae == "")
            {
                Dialogs.WarningDialog("FLASH: TWRP", "Please connect your device, so MotoTool can check your device!");
                return;
            }
            if (oConfigMng.Config.DeviceCodenmae == oConfigMng.Config.DeviceCodenmae)
            {
                LoadDeviceServer.CheckDevice(oConfigMng.Config.DeviceCodenmae + ".xml", oConfigMng.Config.DeviceCodenmae);
            }
            string fileName = @"C:\adb\TWRP\" + LoadDeviceServer.twrpname;
            if (!File.Exists(fileName))
            {
                downloadcall("/TWRP.xml", "TWRP");
                return;
            }
            else
            {
                if (state == IDDeviceState.DEVICE || state == IDDeviceState.RECOVERY || state == IDDeviceState.FASTBOOT)
                {
                    cAppend("BOOT TWRP: Waiting for device...");
                    await Task.Run(() => ADB.WaitForDevice());
                    cAppend("BOOT TWRP: Rebooting into bootloader mode.");
                    await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
                    cAppend("BOOT TWRP: Botting TWRP...");
                    await Task.Run(() => Fastboot.Instance().Flash(IDDevicePartition.BOOT, LoadDeviceServer.twrpname));
                    cAppend("BOOT TWRP: Done booted TWRP.\n");
                }
                else
                {
                    Thread.Sleep(1000);
                    Strings.MSGBOXBootloaderWarning();
                    cAppend("BOOT TWRP: Your device is in the wrong state. Please put your device in the bootloader.\n");
                }
            }
        }

        private async void materialButtonRebootBootloader_Click(object sender, EventArgs e)
        {
            if (IDDeviceState.UNKNOWN == state)
            {
                cAppend("REBOOT BOOTLOADER: Waiting for device...");
                await Task.Run(() => ADB.WaitForDevice());
                cAppend("REBOOT BOOTLOADER: Rebooting into bootloader mode.");
                await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
            }
            else
            {
                Thread.Sleep(1000);
                Strings.MSGBOXBootloaderWarning();
                cAppend("REBOOT BOOTLOADER: Your device is in the wrong state. Please put your device in the bootloader.\n");
            }
        }

        private async void materialButtonRebootRecovery_Click(object sender, EventArgs e)
        {
            if (IDDeviceState.UNKNOWN == state)
            {
                cAppend("REBOOT RECOVERY: Waiting for device...");
                await Task.Run(() => ADB.WaitForDevice());
                cAppend("REBOOT RECOVERY: Rebooting into recovery mode.");
                await Task.Run(() => ADB.Instance().Reboot(IDBoot.RECOVERY));
            }
            else
            {
                Thread.Sleep(1000);
                Strings.MSGBOXBootloaderWarning();
                cAppend("REBOOT RECOVERY: Your device is in the wrong state.\n");
            }
        }

        private async void materialButtonBlankFlash_Click(object sender, EventArgs e)
        {
            if (oConfigMng.Config.DeviceCodenmae == "lima" ||
                oConfigMng.Config.DeviceCodenmae == "potter" ||
                oConfigMng.Config.DeviceCodenmae == "sanders" ||
                oConfigMng.Config.DeviceCodenmae == "sofiar" ||
                oConfigMng.Config.DeviceCodenmae == "sofia")
            {
                Dialogs.InfoDialog("BLANKFLASH: " + oConfigMng.Config.DeviceCodenmae, "Hey this device doesn't have blankflash yet! :(. If you know about an existing blankflash you can contact me and i'll add it!");
                return;
            }
            if (oConfigMng.Config.DeviceCodenmae == "")
            {
                Dialogs.WarningDialog("BLANKFLASH: " + oConfigMng.Config.DeviceCodenmae, "Please connect your device, so MotoTool can check your device!");
                return;
            }
            if (oConfigMng.Config.DeviceCodenmae == oConfigMng.Config.DeviceCodenmae)
            {
                LoadDeviceServer.CheckDevice(oConfigMng.Config.DeviceCodenmae + ".xml", oConfigMng.Config.DeviceCodenmae);
            }
            string blankflashfilepath = @"C:\adb\Others\" + LoadDeviceServer.unbrickname;
            if (!File.Exists(blankflashfilepath) &&
                !Directory.Exists(@"C:\adb\Others\" + oConfigMng.Config.DeviceBlankFlash))
            {
                downloadcall("/BLANKFLASH.xml", "BLANKFLASH");
                oConfigMng.Config.DeviceBlankFlash = LoadDeviceServer.unbrickname;
                oConfigMng.SaveConfig();
                return;
            }
            else
            {
                Thread.Sleep(1000);
                cAppend("Blankflash info: " + oConfigMng.Config.DeviceBlankFlash);
                cAppend("Botting into Bootloader mode...");
                Directory.SetCurrentDirectory(@"C:\adb\Others\" + oConfigMng.Config.DeviceBlankFlash);
                await Task.Run(() => qBootExecuteCommand());
                cAppend("Botting into Bootloader mode... OK");
                Thread.Sleep(1000);
                Dialogs.InfoDialog("BlankFlash", "Please, check if your device has booted up into Bootloader mode!");
            }
        }

        private async void materialButtonFlashLogo_Click(object sender, EventArgs e)
        {
            string logopath = @"C:\adb\" + oConfigMng.Config.DeviceFirmware + oConfigMng.Config.DeviceFirmwareInfo;
            if (oConfigMng.Config.DeviceCodenmae == "")
            {
                Dialogs.WarningDialog("FLASH: LOGO", "Please connect your device, so MotoTool can check your device!");
                return;
            }
            if (!File.Exists(logopath))
            {
                Dialogs.ErrorDialog("LOGO: Missing", "Can't find logo image... " + "\nDevice: " + oConfigMng.Config.DeviceCodenmae + "\nFirmware: " + oConfigMng.Config.DeviceFirmware);
            }
            else
            {
                if (state == IDDeviceState.DEVICE || state == IDDeviceState.RECOVERY || state == IDDeviceState.FASTBOOT)
                {
                    cAppend("FLASH LOGO: Waiting for device...");
                    await Task.Run(() => ADB.WaitForDevice());
                    cAppend("FLASH LOGO: Rebooting into bootloader mode.");
                    await Task.Run(() => ADB.Instance().Reboot(IDBoot.BOOTLOADER));
                    cAppend("FLASH LOGO: Flashing LOGO...");
                    await Task.Run(() => Fastboot.Instance().Execute("flash logo" + logopath + "logo.bin"));
                    cAppend("FLASH LOGO: Done flashing LOGO.\n");
                }
                else
                {
                    Thread.Sleep(1000);
                    Strings.MSGBOXBootloaderWarning();
                    cAppend("FLASH LOGO: Your device is in the wrong state. Please put your device in the bootloader.\n");
                }
            }
        }

        private void materialButtonCheckUpdates_Click(object sender, EventArgs e)
        {
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
        }

        private void materialButtonCMD_Click(object sender, EventArgs e)
        {
            Directory.SetCurrentDirectory(@"C:\\adb\\");
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.FileName = "cmd.exe";
            process.StartInfo = startInfo;
            process.Start();
        }

        private void materialButtonUninstall_Click(object sender, EventArgs e)
        {
            var unins = new Uninstall();
            unins.ShowDialog();
        }

        private void materialButtonHelp_Click(object sender, EventArgs e)
        {
            openChildForm(new Franco28Tool.Engine.Help());
        }

        private void materialButtonOpenFirmwareFolder_Click(object sender, EventArgs e)
        {
            if (oConfigMng.Config.DeviceFirmware == "---")
            {
                return;
            }
            if (oConfigMng.Config.DeviceFirmware == oConfigMng.Config.DeviceFirmware)
            {
                MotoFirmware.OpenFirmwareFolder(oConfigMng.Config.DeviceFirmware);
            }
        }

        private void materialButtonFirmwareCard_Click(object sender, EventArgs e)
        {
            openChildFormFirmware(new Firmwares());
        }

        private void materialButtonFlashTool_Click(object sender, EventArgs e)
        {
            openChildFormFlashTool(new MotoFlashVisual());
        }

        private void materialButtonOpenADBFolder_Click(object sender, EventArgs e)
        {
            try
            {
                Folders.OpenFolder(@"adb");
            }
            catch (Exception er)
            {
                Logs.DebugErrorLogs(er);
                Dialogs.ErrorDialog("ERROR: Open Folder", "Error: " + er);
            }
        }

        private void materialButtonclearallfolders_Click(object sender, EventArgs e)
        {
            var clear = new ClearAllFolders();
            clear.ShowDialog();
        }

        private void materialButtonAddNewDevice_Click(object sender, EventArgs e)
        {
            oConfigMng.LoadConfig();
            oConfigMng.Config.DeviceCodenmae = "";
            GetProp();
            oConfigMng.SaveConfig();
        }

        private void materialButtonRemoveToolDeviceData_Click(object sender, EventArgs e)
        {
            oConfigMng.LoadConfig();
            oConfigMng.Config.DeviceCodenmae = "";
            oConfigMng.Config.DeviceFirmwareInfo = "";
            oConfigMng.Config.DeviceTWPRInfo = "";
            oConfigMng.Config.DeviceBlankFlash = "";
            oConfigMng.Config.FirmwareExtracted = "";
            oConfigMng.Config.DeviceFirmware = "";
            oConfigMng.SaveConfig();
            Dialogs.InfoDialog("MotoTool: Device data", "All device data removed!");
        }

        private void materialSwitchAutoSaveLogs_CheckedChanged(object sender, EventArgs e)
        {
            AutoSaveLogs = materialSwitchAutoSaveLogs.Checked;
            oConfigMng.LoadConfig();
            if (DrawerUseColors = materialSwitchAutoSaveLogs.Checked == true)
            {
                oConfigMng.Config.Autosavelogs = "true";
            }
            else
            {
                oConfigMng.Config.Autosavelogs = "false";
            }
            oConfigMng.SaveConfig();
            Invalidate();
        }

        public void KillWhenExit()
        {
            ADB.ConnectionMonitor.Stop();
            ADB.ConnectionMonitor.Callback -= ConnectionMonitorCallback;
            ADB.Stop();
            Fastboot.Dispose();
            ADB.Dispose();
            oConfigMng.LoadConfig();
            if (oConfigMng.Config.Autosavelogs == "true")
            {
                try
                {
                    string filePath = @"C:\adb\.settings\Logs\ToolLogs.txt";
                    console.SaveFile(filePath, RichTextBoxStreamType.PlainText);
                }
                catch (Exception ex)
                {
                    Logs.DebugErrorLogs(ex);
                    Dialogs.ErrorDialog("An error has occured while attempting to save the output...", ex.ToString());
                }
            }
            oConfigMng.SaveConfig();
            Application.ExitThread();
            Application.Exit();
            base.Dispose(Disposing);
        }

        private void exit(object sender, FormClosedEventArgs e)
        {
            KillWhenExit();
        }
    }
}