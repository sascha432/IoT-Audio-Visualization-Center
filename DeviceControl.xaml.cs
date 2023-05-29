using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Analyzer
{
    /// <summary>
    /// Interaktionslogik für DeviceControl.xaml
    /// </summary>
    public partial class DeviceControl : UserControl
    {
        public DeviceControl()
        {
            InitializeComponent();
            RefAll();
        }

        public object MyDevice
        {
            get { return (object)GetValue(MyDeviceProperty); }
            set { SetValue(MyDeviceProperty, value); }
        }

        public static readonly DependencyProperty MyDeviceProperty =
            DependencyProperty.Register("MyDevice", typeof(object), typeof(DeviceControl), new FrameworkPropertyMetadata(new UdpDevice("Meta", "192.168.0.210", 21324, 32, 2),
                 FrameworkPropertyMetadataOptions.AffectsRender,
                   new PropertyChangedCallback(OnObjectChanged)));

        public int Smoothing
        {
            get
            {
                return DeviceItem.Smoothing;
            }
        }

        public UdpDevice DeviceItem { get { return (UdpDevice)MyDevice; } }

        private static void OnObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DeviceControl devC = d as DeviceControl;
            devC.RefAll();
        }

        public void RefAll()
        {
            if (DeviceItem.DeviceName.ToLower() == "meta") return;
            lblDetails.Content = Details;
            grpDevice.Header = DeviceName;
            spcDev.Smoothing = Smoothing;
            ckbSmoothing.IsChecked = DeviceItem.Smooth;
            if (!(bool)ckbSmoothing.IsChecked) spcDev.Smoothing = 0;
        }

        public class ddElement
        {
            public PackIconFontAwesome p = new PackIconFontAwesome();
            public string name;
            public ddElement(string n, bool isStar)
            {
                name = n;
                
                if (isStar) p.Kind = PackIconFontAwesomeKind.StarRegular;
                p.UpdateLayout();
            }
        }

        public Grid getDdGrid(UdpDevice.Pattern p)
        {
            Grid d = new Grid();
            PackIconFontAwesome s = new PackIconFontAwesome(); s.Kind = PackIconFontAwesomeKind.StarRegular;
            s.VerticalAlignment = VerticalAlignment.Center;

            if (p.name.Contains("⋆"))
            {
                d.Children.Add(s);
                Label l = new Label(); l.Content = "\t" + p.name.Replace("⋆", "");
                d.Children.Add(l);
            }
            else
            {
                Label l = new Label(); l.Content = string.Format("{0}\t{1}", p.id, p.name);
                d.Children.Add(l);
            }
            return d;
        }

        public string DeviceName
        {
            get { return DeviceItem.DeviceName; }
        }

        public string Details
        {
            get
            {
                string host = DeviceItem.Hostname;
                if (!String.IsNullOrEmpty(host)) return "Hostname: " + host + ", IP: " + DeviceItem.Ip;
                else return "IP: " + DeviceItem.Ip;
            }
        }

        private void CkbEnable_Changed(object sender, RoutedEventArgs e)
        {
            if (ckbEnable.IsChecked == true)
            {
                DeviceItem.Start();
                spcDev.enable();
            }
            else
            {
                DeviceItem.Stop();
                spcDev.disable();
            }
        }

        private void CkbSmoothing_Changed(object sender, RoutedEventArgs e)
        {
            if (ckbSmoothing.IsChecked == true)
            {
                DeviceItem.Smooth = true;
                spcDev.Smoothing = DeviceItem.Smoothing;
            }
            else
            {
                DeviceItem.Smooth = false;
                spcDev.Smoothing = 0;
            }
        }

        private void BtnWeb_Click(object sender, RoutedEventArgs e)
        {
            
            System.Diagnostics.Process.Start("http://" + DeviceItem.Ip);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            //foreach (UdpDevice u in MyUtils.UdpDevices) u.Stop();
            MainWindow mwInstance = Window.GetWindow(this) as MainWindow;
            //DeviceItem.Stop();
            foreach (UdpDevice u in MyUtils.UdpDevices) u.Stop();
            ckbEnable.IsChecked = false;
            Window w = new EditDevice(DeviceItem.DeepCopy(), mwInstance);

            w.Show();
        }

        private void dropDrop_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem m = ((MenuItem)e.Source);
                string value = m.DataContext as string;
                int num = Convert.ToInt32(value.ToString().Substring(0, value.ToString().IndexOf('\t')));
                DeviceItem.setPatternAsync(num);
                if (m.Name == "MenuVisualizers")
                {
                    ckbEnable.IsChecked = true;
                }
                else
                {
                    ckbEnable.IsChecked = false;
                }
            }
            catch(Exception)
            {
                ;
            }
        }
    }
}
