using Microsoft.UI.Dispatching;

using System.Diagnostics;

using Windows.Devices.Enumeration;

namespace MultitoolWinUI.Models
{
    public class DeviceInformationViewModel : Model
    {
        public DeviceInformationViewModel(DeviceInformation info, DispatcherQueue dispatcherQueue) : base(dispatcherQueue)
        {
            DeviceInformation = info;
#if false
            foreach (KeyValuePair<string, object> item in info.Properties)
            {
                Properties += item.Key + ": " + item.Value?.ToString() + "\n";
            }
#endif
            Properties = string.Empty;
            Kind += "Device kind: ";
            Kind += info.Kind switch
            {
                DeviceInformationKind.Unknown => "Unknown",
                DeviceInformationKind.DeviceInterface => "DeviceInterface",
                DeviceInformationKind.DeviceContainer => "DeviceContainer",
                DeviceInformationKind.Device => "Device",
                DeviceInformationKind.DeviceInterfaceClass => "DeviceInterfaceClass",
                DeviceInformationKind.AssociationEndpoint => "AssociationEndpoint",
                DeviceInformationKind.AssociationEndpointContainer => "AssociationEndpointContainer",
                DeviceInformationKind.AssociationEndpointService => "AssociationEndpointService",
                DeviceInformationKind.DevicePanel => "DevicePanel",
                _ => "Unknown",
            };
            IsEnabled = info.IsEnabled ? "Device enabled" : "Device disabled";
            IsDefault = info.IsDefault ? "Default" : "Not default";

            // enclosure location
            if (info.EnclosureLocation != null)
            {
                EnclosureLocation = "Device location: ";
                EnclosureLocation += info.EnclosureLocation.InDock ? "In dock\n" : string.Empty;
                EnclosureLocation += info.EnclosureLocation.InLid ? "In lid\n" : string.Empty;
                EnclosureLocation += info.EnclosureLocation.RotationAngleInDegreesClockwise;
                PanelLocation += info.EnclosureLocation.Panel switch
                {
                    Panel.Front => "Front",
                    Panel.Back => "Back",
                    Panel.Top => "Top",
                    Panel.Bottom => "Bottom",
                    Panel.Left => "Left",
                    Panel.Right => "Right",
                    Panel.Unknown => "Unknown",
                    _ => "Unknown",
                };
            }
            else
            {
                EnclosureLocation += "No information";
                PanelLocation += "No information";
            }

            if (info.Pairing != null)
            {
                Pairing += "Pairing protection: ";
                Pairing += info.Pairing.ProtectionLevel switch
                {
                    DevicePairingProtectionLevel.None => "None",
                    DevicePairingProtectionLevel.Encryption => "Encryption",
                    DevicePairingProtectionLevel.EncryptionAndAuthentication => "Encryption and authentication",
                    _ => "Default",
                };
                Pairing += info.Pairing.IsPaired ? "\nIs paired\n" : string.Empty;
                Pairing += info.Pairing.CanPair ? "Can pair" : string.Empty;
            }
            else
            {
                Pairing = "No information";
            }
        }

        public DeviceInformation DeviceInformation { get; }

        public string Name => DeviceInformation.Name;

        public string Properties { get; }

        public string Kind { get; }

        public string IsEnabled { get; }

        public string IsDefault { get; }

        public string Id => DeviceInformation.Id;

        public string EnclosureLocation { get; }

        public string PanelLocation { get; }

        public string Pairing { get; }
    }
}
