using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using ManagedBass.Wasapi;

namespace Analyzer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        [DllImport("User32.dll", SetLastError = true)]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        private NotifyIcon _notifyIcon;
        private bool _isExit;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow = null;

            // check if another process is running
            Process[] instances = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location));
            Process current = Process.GetCurrentProcess();
            int numInstances = instances.Count();
            if (numInstances > 1)
            {
                for (int i = 0; i < numInstances; i++)
                {
                    if (instances[i].Id != current.Id)
                    {
                        // bring the first process that has a different id to the foreground
                        IntPtr hMainWnd = new IntPtr(instances[i].MainWindowHandle.ToInt32());
                        SwitchToThisWindow(hMainWnd, true);
                        break;
                    }
                }
                // and terminate the current instance to avoid having multiple instances running
                System.Windows.Application.Current.Shutdown();
                return;
            }

            MainWindow = new MainWindow();
            MainWindow.Closing += MainWindow_Closing;

            _notifyIcon = new NotifyIcon();
            _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
            _notifyIcon.Icon = Analyzer.Properties.Resources.AppIcon;
            _notifyIcon.Visible = true;

            CreateContextMenu2();
            ShowMainWindow();
        }

        /*
        private void CreateContextMenu()
        {
            _notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add("Dashboard").Click += (s, e) => ShowMainWindow();
            _notifyIcon.ContextMenuStrip.Items.Add("Enable All").Click += (s, e) => MyUtils.EnableAll();
            _notifyIcon.ContextMenuStrip.Items.Add("Disable All").Click += (s, e) => MyUtils.DisableAll();

            ToolStrip ts = new ToolStrip();
            ToolStripDropDownButton tsddb = new ToolStripDropDownButton("device x");
            ts.Items.Add(tsddb);
            //tsddb.DropDown = _notifyIcon.ContextMenuStrip;
            

            _notifyIcon.ContextMenuStrip.Items.Add("Exit").Click += (s, e) => ExitApplication();
        }
        */

        private void CreateContextMenu2()
        {
            System.Windows.Forms.ContextMenu m = new System.Windows.Forms.ContextMenu();
            m.MenuItems.Add("Dashboard").Click += (s, e) => ShowMainWindow();
            m.MenuItems.Add("Enable All").Click += (s, e) => MyUtils.EnableAll();
            m.MenuItems.Add("Disable All").Click += (s, e) => MyUtils.DisableAll();
            List<MenuItem> mItems = new List<MenuItem>();
            for (int i = 0; i < BassWasapi.DeviceCount; i++)
            {
                var device = BassWasapi.GetDeviceInfo(i);
                if (device.IsEnabled && device.IsLoopback)
                {
                    var x = new MenuItem(string.Format("{0} - {1}", i, device.Name), AudioSwitching);
                    mItems.Add(x);
                }
            }
            MenuItem mi = new MenuItem("Audio Device",mItems.ToArray());
            
            m.MenuItems.Add(mi);
            //tsddb.DropDown = _notifyIcon.ContextMenuStrip;


            m.MenuItems.Add("Exit").Click += (s, e) => ExitApplication();
            _notifyIcon.ContextMenu = m;
        }

        public void ExitApplication()
        {
            _isExit = true;
            if (MainWindow != null)
            {
                MainWindow.Close();
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }

        private void ShowMainWindow()
        {
            if (MainWindow != null)
            {
                if (MainWindow.IsVisible)
                {
                    if (MainWindow.WindowState == WindowState.Minimized)
                    {
                        MainWindow.WindowState = WindowState.Normal;
                    }
                    MainWindow.Activate();
                }
                else
                {
                    MainWindow.Show();
                }
            }
        }

        private void AudioSwitching(object sender, EventArgs e)
        {
            var element = sender as MenuItem;
            MyUtils.SwitchDeviceFromString(element.Text);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_isExit)
            {
                e.Cancel = true;
                if (MainWindow != null)
                {
                    MainWindow.Hide(); // A hidden window can be shown again, a closed one not
                }
            }
        }
    }
}