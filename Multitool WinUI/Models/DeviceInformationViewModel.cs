using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Devices.Enumeration;

namespace MultitoolWinUI.Models
{
    public class DeviceInformationViewModel
    {
        public DeviceInformationViewModel(DeviceInformation info)
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
            switch (info.Kind)
            {
                case DeviceInformationKind.DeviceInterface:
                    Kind += "DeviceInterface";
                    break;
                case DeviceInformationKind.DeviceContainer:
                    Kind += "DeviceContainer";
                    break;
                case DeviceInformationKind.Device:
                    Kind += "Device";
                    break;
                case DeviceInformationKind.DeviceInterfaceClass:
                    Kind += "DeviceInterfaceClass";
                    break;
                case DeviceInformationKind.AssociationEndpoint:
                    Kind += "AssociationEndpoint";
                    break;
                case DeviceInformationKind.AssociationEndpointContainer:
                    Kind += "AssociationEndpointContainer";
                    break;
                case DeviceInformationKind.AssociationEndpointService:
                    Kind += "AssociationEndpointService";
                    break;
                case DeviceInformationKind.DevicePanel:
                    Kind += "DevicePanel";
                    break;
                default:
                    Kind += "Unknown";
                    break;
            }
            IsEnabled = info.IsEnabled ? "Device enabled" : "Device disabled";
            IsDefault = info.IsDefault ? "Default" : "Not default";

            // enclosure location
            if (info.EnclosureLocation != null)
            {
                EnclosureLocation = "Device location: ";
                EnclosureLocation += info.EnclosureLocation.InDock ? "In dock\n" : string.Empty;
                EnclosureLocation += info.EnclosureLocation.InLid ? "In lid\n" : string.Empty;
                EnclosureLocation += info.EnclosureLocation.RotationAngleInDegreesClockwise;
                switch (info.EnclosureLocation.Panel)
                {
                    case Panel.Front:
                        PanelLocation += "Front";
                        break;
                    case Panel.Back:
                        PanelLocation += "Back";
                        break;
                    case Panel.Top:
                        PanelLocation += "Top";
                        break;
                    case Panel.Bottom:
                        PanelLocation += "Bottom";
                        break;
                    case Panel.Left:
                        PanelLocation += "Left";
                        break;
                    case Panel.Right:
                        PanelLocation += "Right";
                        break;
                    default:
                        PanelLocation += "Unknown";
                        break;
                }
            }
            else
            {
                Debug.WriteLine("info.EnclosureLocation == null");
                EnclosureLocation += "No information";
                PanelLocation += "No information";
            }

            if (info.Pairing != null)
            {
                Pairing += "Pairing protection: ";
                switch (info.Pairing.ProtectionLevel)
                {
                    case DevicePairingProtectionLevel.None:
                        Pairing += "None";
                        break;
                    case DevicePairingProtectionLevel.Encryption:
                        Pairing += "Encryption";
                        break;
                    case DevicePairingProtectionLevel.EncryptionAndAuthentication:
                        Pairing += "Encryption and authentication";
                        break;
                    default:
                        Pairing += "Default";
                        break;
                }
                Pairing += info.Pairing.IsPaired ? "\nIs paired\n" : string.Empty;
                Pairing += info.Pairing.CanPair ? "Can pair" : string.Empty;
            }
            else
            {
                Debug.WriteLine("info.Pairing == null");
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
