using System;

namespace Multitool.NTInterop
{
    /// <summary>
    /// Indicates that a given operation has failed.
    /// </summary>
    public class OperationFailedException : Exception
    {
        public OperationFailedException() : base() { }

        public OperationFailedException(string operationName, string reason, Exception internalEx) : base("Operation failed: '" + operationName + "'. Reason: " + reason, internalEx) { }
    }
}
