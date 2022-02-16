using System;

namespace Multitool.Interop.Power
{
    /// <summary>
    /// CPU power states (as defined in ACPI)
    /// </summary>
    [Flags]
    public enum CpuPowerStates
    {
        /// <summary>
        /// S1 state supported
        /// </summary>
        S1Supported,
        /// <summary>
        /// S2 state supported
        /// </summary>
        S2Supported,
        /// <summary>
        /// S3 state supported
        /// </summary>
        S3Supported,
        /// <summary>
        /// Hibernation
        /// </summary>
        S4Supported,
        /// <summary>
        /// S5 state supported
        /// </summary>
        S5Supported,
        /// <summary>
        /// Default
        /// </summary>
        Default
    }
}
