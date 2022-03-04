using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;

namespace Multitool.Data.Settings
{
    public class SecureSettingsManager : ISecureSettingsManager
    {
        private static readonly string defaultResource = "MultitoolWinUI";
        private readonly PasswordVault vault = new();

        /// <summary>
        /// Default contructor.
        /// </summary>
        public SecureSettingsManager()
        {
        }

        /// <inheritdoc/>
        public HashAlgorithm HashAlgorithm { get; init; }

        /// <inheritdoc/>
        public bool HashKey { get; init; }

        /// <inheritdoc/>
        public event TypedEventHandler<IUserSettingsManager, string> SettingsChanged;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (HashAlgorithm != null)
            {
                HashAlgorithm.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        /// <remarks>
        /// If <paramref name="resource"/> is not valued (null or empty), the <see cref="SecureSettingsManager"/> will use
        /// <see cref="defaultResource"/> as the default resource.
        /// </remarks>
        /// <inheritdoc/>
        public void Edit(string globalKey, string settingKey, object value)
        {
            throw new NotImplementedException();
        }

        /// <remarks>
        /// If <paramref name="resource"/> is not valued (null or empty), the <see cref="SecureSettingsManager"/> will use
        /// <see cref="defaultResource"/> as the default resource.
        /// </remarks>
        /// <inheritdoc/>
        public T Get<T>(string globalKey, string settingKey)
        {
            if (string.IsNullOrWhiteSpace(globalKey))
            {
                globalKey = defaultResource;
            }
            var credential = vault.Retrieve(globalKey, settingKey);
            credential.RetrievePassword();
            return (T)Convert.ChangeType(credential.Password, typeof(T));
        }

        #region Not supported
        /// <inheritdoc/>
        public List<string> ListKeys() => ListKeys(defaultResource);

        /// <remarks>
        /// If <paramref name="resource"/> is not valued (null or empty), the <see cref="SecureSettingsManager"/> will use
        /// <see cref="defaultResource"/> as the default resource.
        /// </remarks>
        /// <inheritdoc/>
        public List<string> ListKeys(string globalKey)
        {
            try
            {
                IReadOnlyList<PasswordCredential> credentials = vault.FindAllByResource(globalKey);
                if (credentials.Count > 0)
                {
                    List<string> keys = new();
                    foreach (var credential in credentials)
                    {
                        keys.Add(credential.UserName);
                    }
                    return keys;
                }
            }
            catch (COMException ex)
            {
                Trace.TraceError(ex.ToString());
            }
            return null;
        } 
        #endregion

        /// <remarks>
        /// If <paramref name="resource"/> is not valued (null or empty), the <see cref="SecureSettingsManager"/> will use
        /// <see cref="defaultResource"/> as the default resource.
        /// </remarks>
        /// <inheritdoc/>
        public void Remove(string globalKey, string settingKey)
        {
            if (string.IsNullOrWhiteSpace(globalKey))
            {
                globalKey = defaultResource;
            }
            if (string.IsNullOrWhiteSpace(settingKey))
            {
                throw new ArgumentException($"{nameof(settingKey)} is empty.");
            }

            try
            {
                IReadOnlyList<PasswordCredential> credentials = vault.FindAllByUserName(settingKey);
                if (credentials.Count > 0)
                {
                    if (credentials.Count == 1)
                    {
                        vault.Remove(credentials[0]);
                        return;
                    }
                    else
                    {
                        Trace.TraceInformation($"More than 1 credential associated with \"{settingKey}\", checking for match with resource key.");
                        foreach (var credential in credentials)
                        {
                            if (credential.Resource == globalKey)
                            {
                                vault.Remove(credential);
                                return;
                            }
                        }
                        Trace.TraceWarning($"Not credentials associated with {globalKey}/{settingKey}");
                    }
                }

                // thrown only if we don't find the key or if the 'credentials' have more than 1 entry but none match
                // has the right resource key.
                throw new SettingNotFoundException($"setting key (\"{settingKey}\") was not found in the vault.");
            }
            catch (COMException ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        /// <inheritdoc/>
        public void Reset()
        {
            IReadOnlyList<PasswordCredential> credentials;
            try
            {
#if DEBUG
                credentials = vault.RetrieveAll();
#else
                credentials = vault.FindAllByResource(DefaultResource); 
#endif
            }
            catch (COMException ex)
            {
                Trace.TraceError($"Failed to get credentials from vault ({ex.Message})");
                //Trace.TraceError(ex.ToString());
                return;
            }

            if (credentials.Count > 0)
            {
                foreach (var credential in credentials)
                {
                    vault.Remove(credential);
                }
                Trace.TraceInformation("Removed all saved credentials.");
            }
            else
            {
                Trace.TraceWarning("Nothing to reset, vault is empty");
            }
        }

        /// <remarks>
        /// If <paramref name="resource"/> is not valued (null or empty), the <see cref="SecureSettingsManager"/> will use
        /// <see cref="defaultResource"/> as the default resource.
        /// </remarks>
        /// <inheritdoc/>
        public void Save(string globalKey, string key, object value)
        {
            string password = value.ToString();
            string resource = string.IsNullOrWhiteSpace(globalKey) ? defaultResource : globalKey;
            CheckArguments(key, password);

            string credentialKey = HashKey ? Hash(key) : key;
            PasswordCredential passwordCredential = null;

            try
            {
                IReadOnlyList<PasswordCredential> credentials = vault.FindAllByResource(defaultResource);
                foreach (var credential in credentials)
                {
                    if (credential.UserName == credentialKey)
                    {
                        passwordCredential = credential;
                        break;
                    }
                }
            }
            catch (COMException ex)
            {
                Trace.TraceError($"Failed to get credentials from vault ({ex.Message})");
                //Trace.TraceError(ex.ToString());
            }

            if (passwordCredential != null)
            {
                passwordCredential.RetrievePassword();
                string savedPassword = passwordCredential.Password;
                if (savedPassword == password)
                {
                    Trace.TraceWarning($"Not saving {key}, password is identical.");
                }
                else
                {
                    passwordCredential.Password = password;
                    if (passwordCredential.Properties.TryGetValue("creation-timestamp", out object o))
                    {
                        passwordCredential.Properties["creation-timestamp"] = DateTime.Now;
                    }
                    else
                    {
                        passwordCredential.Properties.Add(new("creation-timestamp", DateTime.Now));
                    }
                    Trace.TraceInformation($"Edited {key} in password manager.");
                }
            }
            else
            {
                passwordCredential = new(resource, credentialKey, password);
                vault.Add(passwordCredential);
                Trace.TraceInformation($"Saved {key} in password manager.");
            }
        }

        public bool TryGet<T>(string globalKey, string name, out T value)
        {
            throw new NotImplementedException();
        }

        public object TryGet(string globalKey, string settingKey)
        {
            throw new NotImplementedException();
        }

        private static void CheckArguments(string key, string password)
        {
            byte flag = 0;
            if (string.IsNullOrWhiteSpace(key))
            {
                flag = 0b1;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                flag |= 0b10;
            }

            if (flag != 0)
            {
                if (flag == 0b11)
                {
                    ArgumentException keyException = new("Password key is empty");
                    ArgumentException passwordException = new("Password value is empty");
                    throw new AggregateException(keyException, passwordException);
                }
                else if (flag == 0b1)
                {
                    throw new ArgumentException("Password key is empty");
                }
                else
                {
                    throw new ArgumentException("Password value is empty");
                }
            }
        }

        private string Hash(string toHash)
        {
            if (HashAlgorithm == null)
            {
                throw new InvalidOperationException($"Cannot hash with this.{nameof(HashAlgorithm)} null.");
            }
            Windows.Storage.Streams.IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(toHash, BinaryStringEncoding.Utf8);
            //byte[] array = new byte[buffer.Length];
            CryptographicBuffer.CopyToByteArray(buffer, out byte[] array);
            byte[] hashed = HashAlgorithm.ComputeHash(array);
            return Encoding.UTF8.GetString(hashed);
        }
    }
}
