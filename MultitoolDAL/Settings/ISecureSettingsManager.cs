using System;
using System.Security.Cryptography;

namespace Multitool.Data.Settings
{
    public interface ISecureSettingsManager : ISettingsManager, IDisposable
    {
        bool HashKey { get; init; }
        HashAlgorithm HashAlgorithm { get; init; }
    }
}