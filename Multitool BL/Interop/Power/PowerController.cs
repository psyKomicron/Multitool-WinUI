using Microsoft.Win32.SafeHandles;

using System;
using System.Runtime.InteropServices;

using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Shutdown;

using static Multitool.Interop.Codes.SystemCodes;
using static Windows.Win32.PInvoke;

namespace Multitool.Interop.Power
{
    public class PowerController
    {
        public const uint ShutdownFlag = 0x00040000;

        public bool Force { get; set; }

        /// <summary>
        /// Locks the local computer.
        /// </summary>
        public void Lock()
        {
            if (!LockWorkStation())
            {
                throw new OperationFailedException("LockWorkStation failed", InteropHelper.GetLastError("LockWorkStation returned zero code"));
            }
        }

        /// <summary>
        /// Locks the local computer.
        /// </summary>
        public void Lock(double delay)
        {
        }

        /// <summary>
        /// Suspends (puts in sleep mode) the local computer.
        /// </summary>
        public void Suspend()
        {
            if (SetSuspendState(new(), Force ? new(1) : new(), new()).Value == 0)
            {
                throw new OperationFailedException("SetSuspendState failed", InteropHelper.GetLastError("SetSuspendState returned zero code"));
            }
        }

        /// <summary>
        /// Suspends (puts in sleep mode) the local computer.
        /// </summary>
        public void Suspend(double delay)
        {
        }

        /// <summary>
        /// Put the local computer in hibernation state (S4).
        /// </summary>
        public void Hibernate()
        {
            if (PowerCapabilities.IsHibernationAllowed())
            {
                BOOLEAN bHibernate = new(1);
                BOOLEAN bForce = Force ? new(1) : new();
                BOOLEAN bWakeUpEventsDisabled = new();

                if (SetSuspendState(bHibernate, bForce, bWakeUpEventsDisabled).Value == 0)
                {
                    throw new OperationFailedException("SetSuspendState failed.", InteropHelper.GetLastError("SetSuspendState returned zero code."));
                }
            }
            else
            {
                throw new NotSupportedException("Hibernation is not possible on this computer.");
            }
        }

        /// <summary>
        /// Put the local computer in hibernation state (S4).
        /// </summary>
        public void Hibernate(double delay)
        {
        }

        /// <summary>
        /// Shuts down the local computer.
        /// </summary>
        public void Shutdown()
        {
            SHUTDOWN_FLAGS dwShutdownFlags = SHUTDOWN_FLAGS.SHUTDOWN_POWEROFF;
            if (Force)
            {
                dwShutdownFlags |= SHUTDOWN_FLAGS.SHUTDOWN_FORCE_SELF;
            }

#if true
            uint res = InitiateShutdown(null,
                    null,
                    60,
                    dwShutdownFlags,
                    SHUTDOWN_REASON.SHTDN_REASON_FLAG_USER_DEFINED);

            if (res != ERROR_SUCCESS)
            {
                if (res == ERROR_SHUTDOWN_USERS_LOGGED_ON)
                {
                    throw new OperationFailedException("Cannot shutdown local computer while other users are logged in.", InteropHelper.GetLastError("InitiateSystemShutdown returned ERROR_SHUTDOWN_USERS_LOGGED_ON", res));
                }
                else if (res == ERROR_ACCESS_DENIED)
                {
                    throw new OperationFailedException("Failed to shutdown local computer, the application does not have shutdown privileges.", InteropHelper.GetLastError("InitiateSystemShutdown returned ERROR_ACCESS_DENIED", res));
                }
                else
                {
                    throw new OperationFailedException("Failed to shutdown local computer.", InteropHelper.GetLastError("InitiateSystemShutdown returned non-zero code", res));
                }
            } 
#else
            // Get token
            //HANDLE tokenHandle = new();
            SafeFileHandle tokenHandle = new();
            //HANDLE processHandle = (HANDLE)GetCurrentProcess().Value;
            SafeHandleZeroOrMinusOneIsInvalid processHandle = new();
            OpenProcessToken(processHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES, &tokenHandle);

            TOKEN_PRIVILEGES privileges = new();
            AdjustTokenPrivileges(tokenHandle, new BOOL(), privileges, 0);
            
            if (ExitWindowsEx(EXIT_WINDOWS_FLAGS.EWX_SHUTDOWN | EXIT_WINDOWS_FLAGS.EWX_POWEROFF, 1).Value == 0)
            {
                throw new OperationFailedException("Failed to shutdown local computer.", InteropHelper.GetLastError("Failed to shutdown local computer."));
            }
#endif
        }

        /// <summary>
        /// Shuts down the local computer.
        /// </summary>
        public void Shutdown(double delay)
        {
        }

        /// <summary>
        /// Restarts the local computer.
        /// </summary>
        public void Restart()
        {
            SHUTDOWN_FLAGS dwShutdownFlags = SHUTDOWN_FLAGS.SHUTDOWN_RESTART;
            if (Force)
            {
                dwShutdownFlags |= SHUTDOWN_FLAGS.SHUTDOWN_FORCE_SELF;
            }

            uint res = InitiateShutdown(null,
                null,
                0,
                dwShutdownFlags,
                SHUTDOWN_REASON.SHTDN_REASON_FLAG_USER_DEFINED);

            if (res != ERROR_SUCCESS)
            {
                if (res == ERROR_SHUTDOWN_USERS_LOGGED_ON)
                {
                    throw new OperationFailedException("Restart failed, other users are logged in.", InteropHelper.GetLastError("Restart failed.", res));
                }
                else
                {
                    throw new OperationFailedException("Restart failed", InteropHelper.GetLastError("InitiateSystemShutdown returned non-zero code", res));
                }
            }
        }

        /// <summary>
        /// Restarts the local computer.
        /// </summary>
        public void Restart(double delay)
        {
        }
    }
}
