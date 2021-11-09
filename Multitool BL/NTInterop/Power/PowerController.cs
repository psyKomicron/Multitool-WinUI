using Multitool.NTInterop.Codes;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Multitool.NTInterop.Power
{
    public class PowerController
    {
        public const uint ShutdownFlag = 0x00040000;

        public bool ForceApplicationShutdown { get; set; }

        /// <summary>
        /// Locks the local computer.
        /// </summary>
        public void Lock()
        {
            if (!LockWorkStation())
            {
                throw new OperationFailedException("LockWorkStation failed", InteropHelper.GetLastError("LockWorkStation returned zero code"));
            }
            else
            {
                Trace.TraceInformation("Successfully locked the local computer");
            }
        }

        /// <summary>
        /// Suspends (puts in sleep mode) the local computer.
        /// </summary>
        public void Suspend()
        {
            if (!SetSuspendState(false, false, false))
            {
                throw new OperationFailedException("SetSuspendState failed", InteropHelper.GetLastError("SetSuspendState returned zero code"));
            }
            else
            {
                Trace.TraceInformation("Successfully put the local computer to sleep");
            }
        }

        /// <summary>
        /// Put the local computer in hibernation state (S4).
        /// </summary>
        public void Hibernate()
        {
            if (PowerCapabilities.IsHibernationAllowed())
            {
                if (!SetSuspendState(false, true, false))
                {
                    throw new OperationFailedException("SetSuspendState failed", InteropHelper.GetLastError("SetSuspendState returned zero code"));
                }
                else
                {
                    Trace.TraceInformation("Successfully put the local computer into hibernation");
                }
            }
            else
            {
                throw new NotSupportedException("Hibernation is not possible on this computer");
            }
        }

        /// <summary>
        /// Shuts down the local computer.
        /// </summary>
        public void Shutdown()
        {
            if (!InitiateSystemShutdown(null, null, 0, ForceApplicationShutdown, false, ShutdownFlag))
            {
                throw new OperationFailedException("InitiateSystemShutdown returned non zero code", InteropHelper.GetLastError("InitiateSystemShutdown returned zero code"));
            }
        }

        /// <summary>
        /// Restarts the local computer.
        /// </summary>
        public void Restart()
        {
            uint res = InitiateShutdown(null, null, 0, 0x80, ShutdownFlag);
            if (res != (uint)SystemCodes.ERROR_SUCCESS)
            {
                throw new OperationFailedException("InitiateShutdown failed", InteropHelper.GetLastError("InitiateSystemShutdown returned non-zero code", res));
            }
        }

        #region dll imports
        [DllImport("User32.dll", SetLastError = true)]
        static extern bool LockWorkStation();

        [DllImport("PowrProf.dll", SetLastError = true)]
        static extern bool SetSuspendState(
            bool hibernate,
            bool forceCritical,
            bool disabledWakeEvent
        );

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool InitiateSystemShutdown(
            string lpMachineName,
            string lpMessage,
            uint dwTimeout,
            bool bForceAppsClosed,
            bool bRebootAfterShutdown,
            uint dwReason
        );

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern uint InitiateShutdown(
            string lpMachineName,
            string lpMessage,
            uint dwGracePeriod,
            uint dwShutdownFlags,
            uint dwReason
            );
        #endregion
    }
}
