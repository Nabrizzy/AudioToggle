using Microsoft.Win32;
using System.Windows.Media;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AudioToggler
{
    public partial class MainWindow : Window
    {
        public Dictionary<int, string> Devices;

        private readonly string _registryPath = @"SOFTWARE\Nabrizzy";
        private readonly string _registryKey = "AudioToggleIndex";
        private readonly string _helperExePath = "EndPointController.exe";
        private int _lastIndex
        {
            get
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(_registryPath);
                try
                {
                    if (key == null)
                        Registry.CurrentUser.CreateSubKey(_registryPath);

                    key = Registry.CurrentUser.OpenSubKey(_registryPath, true);
                    if (key.GetValue(_registryKey) == null)
                        key.SetValue(_registryKey, "1");

                    return int.Parse(key.GetValue(_registryKey).ToString());
                }
                finally
                {
                    key.Close();
                }
            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_registryPath, true))
                {
                    key.SetValue(_registryKey, value.ToString());
                }
            }
        }
        private int _highestIndex
        {
            get
            {
                if (Devices != null)
                {
                    return Devices.Aggregate((x, y) => x = x.Key > y.Key ? x : y).Key;
                }

                return 1;
            }
        }



        public MainWindow()
        {
            this.ShowActivated = false;
            this.ShowInTaskbar = false;
            this.Topmost = true;
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            BackgroundBorder.Background = new SolidColorBrush(Colors.Black) { Opacity = .4d };
            DeviceLabel.Opacity = .9d;

            Devices = FetchAudioDevices();
            _lastIndex = (_lastIndex + 1) > _highestIndex ? 1 : (_lastIndex + 1);
            ChangeDevice(_lastIndex);
            string newDeviceName = Devices[_lastIndex];
            newDeviceName = newDeviceName.Substring(0, newDeviceName.LastIndexOf("(") - 1);

            while (newDeviceName.Count(x => x == '(') != newDeviceName.Count(x => x == ')'))
            {
                newDeviceName = newDeviceName.Substring(0, newDeviceName.LastIndexOf('('));
            }
            
            DeviceLabel.Content = newDeviceName;

            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            Task.Run(() =>
            {
                System.Threading.Thread.Sleep(1500);
            })
            .ContinueWith(_ => 
            {
                this.Close();
            }, scheduler);
        }


        public void ChangeDevice(int index)
        {
            using (Process process = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = _helperExePath,
                    Arguments = index.ToString()
                }
            })
            {
                process.Start();
            }
        }

        public Dictionary<int, string> FetchAudioDevices()
        {
            Dictionary<int, string> devices = new Dictionary<int, string>();

            Process process = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    FileName = _helperExePath,
                    Arguments = @"-f ""%d;%ws"""
                }
            };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string[] lines = output.Split('\n').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Replace("\r", "")).ToArray<string>();

            foreach (string line in lines)
            {
                if (line.Contains(';'))
                {
                    string[] values = line.Split(';');
                    devices.Add(int.Parse(values[0]), values[1]);
                }
            }

            return devices;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Closing -= Window_Closing;
            e.Cancel = true;
            var animation = new DoubleAnimation(0, (Duration)TimeSpan.FromMilliseconds(300));
            animation.Completed += (s, _) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, animation);
        }
    }
}
