using System;
using System.Security.Cryptography;

namespace Multitool.Data.Settings
{
    /// <summary>
    /// Defines behavior for classes managing secure settings (password, sensitive data...)
    /// </summary>
    public interface ISecureSettingsManager : ISettingsManager, IDisposable
    {
        /// <summary>
        /// <see langword="true"/> to hash the key with <see cref="HashAlgorithm"/>.
        /// </summary>
        bool HashKey { get; init; }
        /// <summary>
        /// Algorithm to hash the key with.
        /// </summary>
        HashAlgorithm HashAlgorithm { get; init; }
    }
}