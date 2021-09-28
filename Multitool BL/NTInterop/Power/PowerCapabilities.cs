using Multitool.NTInterop.Structs;

using System.Runtime.InteropServices;

namespace Multitool.NTInterop.Power
{
    /// <summary>
    /// Represents the power capabilities of this system
    /// </summary>
    public class PowerCapabilities
    {
        #region DllImports
        [DllImport("powrprof.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        static extern bool GetPwrCapabilities(out SYSTEM_POWER_CAPABILITIES sysCaps);
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public PowerCapabilities()
        {
            GetPowerCapabilities();
            CpuStates = GetCpuPowerStates();
        }

        #region properties

        /// <summary>
        ///  True if the system has a power button (power on/power off) 
        /// </summary>
        public bool PowerButtonPresent { get; private set; }

        /// <summary>
        /// True if the system has a sleep button
        /// </summary>
        public bool SleepButtonPresent { get; private set; }

        /// <summary>
        /// True if the system has a lid
        /// </summary>
        public bool LidPresent { get; private set; }

        /// <summary>
        /// Supported states by the CPU (S states)
        /// </summary>
        public CpuPowerStates CpuStates { get; private set; }

        /// <summary>
        /// ACPI S1 supported
        /// </summary>
        public bool S1 { get; private set; }

        /// <summary>
        /// ACPI S2 supported
        /// </summary>
        public bool S2 { get; private set; }

        /// <summary>
        /// ACPI S3 allowed
        /// </summary>
        public bool S3 { get; private set; }

        /// <summary>
        /// ACPI S4 supported (hibernation)
        /// </summary>
        public bool S4 { get; private set; }

        /// <summary>
        /// ACPI S5 supported
        /// </summary>
        public bool S5 { get; private set; }

        /// <summary>
        /// True if throttling is available
        /// </summary>
        public bool ProcessorThrottle { get; private set; }

        /// <summary>
        /// Current power mode maximal throttle
        /// </summary>
        public byte ProcessorMaxThrottle { get; private set; }

        /// <summary>
        /// Current power mode minimal throttle
        /// </summary>
        public byte ProcessorMinThrottle { get; private set; }

        /// <summary>
        /// True if the system has batteries (i.e. laptop)
        /// </summary>
        public bool SystemBatteriesPresent { get; private set; }

        /// <summary>
        /// True if the batteries are considered short term
        /// </summary>
        public bool BatteriesAreShortTerm { get; private set; }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-battery_reporting_scale
        /// </summary>
        public BatteryReportingScale BatterieScale1 { get; private set; }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-battery_reporting_scale
        /// </summary>
        public BatteryReportingScale BatterieScale2 { get; private set; }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-battery_reporting_scale
        /// </summary>
        public BatteryReportingScale BatterieScale3 { get; private set; }

        /// <summary>
        /// True if the hibernation file (c:\hiber.sys)
        /// </summary>
        public bool HibernationFilePresent { get; private set; }

        /// <summary>
        /// ?
        /// </summary>
        public bool FullWake { get; private set; }

        /// <summary>
        /// True if the system supports video dimming
        /// </summary>
        public bool VideoDimPresent { get; private set; }

        /// <summary>
        /// ?
        /// </summary>
        public bool ApmPresent { get; private set; }

        /// <summary>
        /// True if an uninterruptible power supply is present
        /// </summary>
        public bool UpsPresent { get; private set; }

        /// <summary>
        /// Thermal control
        /// </summary>
        public bool ThermalControl { get; private set; }

        /// <summary>
        /// Disk spin down time (minutes)
        /// </summary>
        public bool DiskSpinDown { get; private set; }

        /// <summary>
        /// ?
        /// </summary>
        public SystemPowerState AcOnLineWake { get; private set; }

        /// <summary>
        /// ?
        /// </summary>
        public SystemPowerState SoftLidWake { get; private set; }

        /// <summary>
        /// ?
        /// </summary>
        public SystemPowerState RtcWake { get; private set; }

        /// <summary>
        /// ?
        /// </summary>
        public SystemPowerState MinDeviceWakeState { get; private set; }

        /// <summary>
        /// ?
        /// </summary>
        public SystemPowerState DefaultLowLatencyWake { get; private set; }

        #endregion

        #region public methods

        /// <summary>
        /// Checks if the system allows CPU S4 state, named Hibernation
        /// </summary>
        /// <returns>True if hibernation is allowed</returns>
        public static bool IsHibernationAllowed()
        {
            if (GetPwrCapabilities(out SYSTEM_POWER_CAPABILITIES sys))
            {
                return sys.SystemS4;
            }
            else
            {
                throw InteropHelper.GetLastError("GetPwrCapabilities returned zero code");
            }
        }

        #endregion

        #region private methods

        private void GetPowerCapabilities()
        {
            if (GetPwrCapabilities(out SYSTEM_POWER_CAPABILITIES sys))
            {
                PowerButtonPresent = sys.PowerButtonPresent;
                SleepButtonPresent = sys.SleepButtonPresent;
                LidPresent = sys.LidPresent;
                S1 = sys.SystemS1;
                S2 = sys.SystemS2;
                S3 = sys.SystemS3;
                S4 = sys.SystemS4;
                S5 = sys.SystemS5;
                ProcessorMaxThrottle = sys.ProcessorMaxThrottle;
                ProcessorMinThrottle = sys.ProcessorMinThrottle;
                ProcessorThrottle = sys.ProcessorThrottle;
                SystemBatteriesPresent = sys.SystemBatteriesPresent;
                BatteriesAreShortTerm = sys.BatteriesAreShortTerm;
                BatterieScale1 = new BatteryReportingScale(sys.BatteryScale[0].Granularity, sys.BatteryScale[0].Granularity);
                BatterieScale2 = new BatteryReportingScale(sys.BatteryScale[1].Granularity, sys.BatteryScale[1].Granularity);
                BatterieScale3 = new BatteryReportingScale(sys.BatteryScale[2].Granularity, sys.BatteryScale[2].Granularity);
                HibernationFilePresent = sys.HiberFilePresent;
                FullWake = sys.FullWake;
                VideoDimPresent = sys.VideoDimPresent;
                ApmPresent = sys.UpsPresent;
                UpsPresent = sys.UpsPresent;
                ThermalControl = sys.ThermalControl;
                DiskSpinDown = sys.DiskSpinDown;
                AcOnLineWake = new SystemPowerState(sys.AcOnLineWake);
                SoftLidWake = new SystemPowerState(sys.SoftLidWake);
                RtcWake = new SystemPowerState(sys.RtcWake);
                MinDeviceWakeState = new SystemPowerState(sys.MinDeviceWakeState);
                DefaultLowLatencyWake = new SystemPowerState(sys.DefaultLowLatencyWake);
            }
            else
            {
                throw InteropHelper.GetLastError(nameof(GetPwrCapabilities) + " failed", 0);
            }
        }

        private CpuPowerStates GetCpuPowerStates()
        {
            CpuPowerStates states = CpuPowerStates.Default;
            if (S1)
            {
                states |= CpuPowerStates.S1Supported;
            }
            if (S2)
            {
                states |= CpuPowerStates.S2Supported;
            }
            if (S3)
            {
                states |= CpuPowerStates.S3Supported;
            }
            if (S4)
            {
                states |= CpuPowerStates.S4Supported;
            }
            if (S5)
            {
                states |= CpuPowerStates.S5Supported;
            }
            return states;
        }

        #endregion
    }
}
