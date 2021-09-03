using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MultitoolWinUI.Models;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

using Windows.Devices.Enumeration;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MultitoolWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ComputerDevicesPage : Page
    {
        public ComputerDevicesPage()
        {
            InitializeComponent();
        }

        public ObservableCollection<DeviceInformationViewModel> Devices { get; set; } = new ObservableCollection<DeviceInformationViewModel>();

        private void AddDevices(DeviceInformationCollection collection)
        {
            Debug.WriteLine("Adding devices...");
            if (collection.Count > 0)
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    Devices.Add(new DeviceInformationViewModel(collection[i], DispatcherQueue));
                }
            }
        }

        private async void AllDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            Devices.Clear();
            AddDevices(await DeviceInformation.FindAllAsync());
        }

        private async void StorageDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            Devices.Clear();
            AddDevices(await DeviceInformation.FindAllAsync(DeviceClass.PortableStorageDevice));
        }

        private async void AudioDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            Devices.Clear();
            AddDevices(await DeviceInformation.FindAllAsync(DeviceClass.AudioRender));
            AddDevices(await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture));
        }
    }
}
