using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multitool.NTInterop.Power
{
    [Flags]
    public enum CpuPowerStates
    {
        S1Supported,
        S2Supported,
        S3Supported,
        /// <summary>
        /// Hibernation
        /// </summary>
        S4Supported,
        S5Supported,
        Default
    }
}
