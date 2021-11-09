using System;
using System.Runtime.CompilerServices;

namespace Multitool.NTInterop
{
    /// <summary>
    /// Indicates that a given operation has failed.
    /// </summary>
    public class OperationFailedException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public OperationFailedException() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="operationName">the name of the failed operation</param>
        /// <param name="reason">message for why the operation failed</param>
        /// <param name="internalEx">internal exception</param>
        public OperationFailedException(string reason, Exception internalEx, [CallerMemberName] string operationName = null) : base("Operation failed: '" + operationName + "'. Reason: " + reason, internalEx) { }
    }
}
