using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Kyub.Serialization;

namespace Kyub.Credentials
{
    [System.Serializable]
    public sealed class CredentialsKeyStore
    {
        #region Consts

        private const int KEY_SIZE = 256;
        private const int BLOCK_SIZE = 256;

        private const string STORAGE_ALIAS = "_STORAGE";
        private const string IALIAS = "_IV";

        #endregion

        #region Private Variables

        [System.NonSerialized]
        private static CredentialsKeyStore s_instance = null;

        //This seed is encrypted
        [SerializeField, SerializeProperty("k")]
        string m_seed;
        [SerializeField, SerializeProperty("y")]
        List<string> m_keys = new List<string>();
        [SerializeField, SerializeProperty("b")]
        List<string> m_values = new List<string>();

        HashSet<string> _originalKeys = new HashSet<string>();
        Dictionary<string, string> _storageKV = new Dictionary<string, string>();

        #endregion

        #region Statis Constructors

        static CredentialsKeyStore()
        {
            s_instance = null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            s_instance = null;
        }
#endif

        #endregion

        #region Internal Helper Functions (Instance)

        string GenerateInstanceSeed()
        {
            string iv = GenerateIV(GenerateKeyChain(null));
            string decryptedSeed = null;

            //Try decrypt the serialized seed in instance
            if(!string.IsNullOrEmpty(m_seed))
                decryptedSeed = Decrypt(iv, null, m_seed);

            //we must generate a new seed if we failed to retrieve the old seed generated (the instance can be invalidated if failed to retrieve GetString using the new decrypted seed)
            if (string.IsNullOrEmpty(decryptedSeed))
            {
                //Generate new Decrypted seed and save the encrypted value in instance
                decryptedSeed = (System.DateTime.UtcNow - DateTime.MinValue).TotalMilliseconds.ToString() + UnityEngine.Random.Range(int.MinValue, int.MaxValue).ToString();
                m_seed = Encrypt(iv, null, decryptedSeed);
            }
            return decryptedSeed;
        }

        string GetInstanceString(string key)
        {
            var internalStorageKey = GenerateInternalStorateKey(key);
            if (!string.IsNullOrEmpty(internalStorageKey))
            {
                string encryptedValue = null;
                if (_storageKV.TryGetValue(internalStorageKey, out encryptedValue))
                {
                    var decryptedSeed = GenerateInstanceSeed();
                    var iv = GenerateIV(decryptedSeed + internalStorageKey);
                    var value = Decrypt(iv, decryptedSeed, encryptedValue);
                    return value;
                }
            }
            return null;
        }

        bool SetInstanceString(string key, string value)
        {
            var internalStorageKey = GenerateInternalStorateKey(key);
            if (!string.IsNullOrEmpty(internalStorageKey))
            {
                if (value != null)
                {
                    var decryptedSeed = GenerateInstanceSeed();
                    var iv = GenerateIV(decryptedSeed + internalStorageKey);
                    var encryptedData = Encrypt(iv, decryptedSeed, value);

                    _originalKeys.Add(key);
                    _storageKV[internalStorageKey] = encryptedData;
                    return true;
                }
                else
                {
                    DeleteInstanceKey(key);
                }
            }
            return false;
        }

        bool DeleteInstanceKey(string key)
        {
            var instanceStorageKey = GenerateInternalStorateKey(key);
            if (!string.IsNullOrEmpty(instanceStorageKey))
            {
                _originalKeys.Remove(key);
                return _storageKV.Remove(instanceStorageKey);
            }

            return false;
        }

        bool ContainsInstanceKey(string key)
        {
            return _originalKeys.Contains(key);
        }

        #endregion

        #region Public Functions (Static)

        public static string[] GetAllKeys()
        {
            if (s_instance == null)
                Load_Internal();

            return new List<string>(s_instance._originalKeys).ToArray();
        }

        public static bool SetString(string key, string value)
        {
            if (s_instance == null)
                Load_Internal();

            if (string.IsNullOrEmpty(key))
                return false;

            return s_instance.SetInstanceString(key.ToLower(), value);
        }

        public static bool SetCredential<T>(string key, T value)
        {
            return SetString(key, value != null ? SerializationUtils.ToJson(value) : null);
        }

        public static string GetString(string key)
        {
            if (s_instance == null)
                Load_Internal();

            string value = s_instance.GetInstanceString(key.ToLower());

            //Failed to decrypt, we must invalidate instance
            if (value == null)
                DeleteAll();

            return value;
        }

        public static T GetCredential<T>(string key)
        {
            var json = GetString(key);
            if (!string.IsNullOrEmpty(json))
                return SerializationUtils.FromJson<T>(json);

            return default(T);
        }

        public static bool DeleteKey(string key)
        {
            if (s_instance == null)
                Load_Internal();

            return s_instance.DeleteInstanceKey(key.ToLower());
        }

        public static void DeleteAll()
        {
            if (s_instance == null)
                Load_Internal();

            s_instance._originalKeys.Clear();
            s_instance._storageKV.Clear();
            Save();
        }

        public static bool HasKey(string key)
        {
            if (s_instance == null)
                Load_Internal();

            return s_instance.ContainsInstanceKey(key.ToLower());
        }

        public static void Save()
        {
            Save_Internal();
        }

        #endregion

        #region Internal Save/Load Functions (Static)

        static void Save_Internal()
        {
            var storageKey = GenerateStorageKey();
            if (s_instance != null && s_instance._storageKV.Count > 0)
            {
                s_instance.m_values.Clear();
                s_instance.m_keys.Clear();

                //Serialize Original Keys with Encrypted Values
                foreach(var key in s_instance._originalKeys)
                {
                    var internalStorageKey = GenerateInternalStorateKey(key);
                    if (s_instance._storageKV.ContainsKey(internalStorageKey))
                    {
                        s_instance.m_keys.Add(key);
                        var value = s_instance._storageKV[internalStorageKey];
                        s_instance.m_values.Add(value);
                    }
                }

                var storageIv = GenerateIV(storageKey);
                var encryptedStorage = Encrypt(storageIv, null, SerializationUtils.ToJson(s_instance));
                PlayerPrefs.SetString(storageKey, encryptedStorage);

                //Terminate the instance apply to player prefs
                s_instance = null;
            }
            else if (PlayerPrefs.HasKey(storageKey))
                PlayerPrefs.DeleteKey(storageKey);

            PlayerPrefs.Save();
        }

        static void Load_Internal()
        {
            var storageKey = GenerateStorageKey();

            if (PlayerPrefs.HasKey(storageKey))
            {
                var encryptedValue = PlayerPrefs.GetString(storageKey);
                var storageIv = GenerateIV(storageKey);

                if (!string.IsNullOrEmpty(encryptedValue))
                {
                    var json = Decrypt(storageIv, null, encryptedValue);
                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            s_instance = SerializationUtils.FromJson<CredentialsKeyStore>(json);

                            //Fill values in Dict
                            if (s_instance.m_keys != null)
                            {
                                for (int i = 0; i < s_instance.m_keys.Count; i++)
                                {
                                    var key = s_instance.m_keys[i];
                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        //Save as internal storage key in dictionary
                                        var internalStorageKey = GenerateInternalStorateKey(key);
                                        if (!string.IsNullOrEmpty(internalStorageKey))
                                        {
                                            s_instance._originalKeys.Add(key);
                                            var value = s_instance.m_values != null && s_instance.m_values.Count > i ? s_instance.m_values[i] : null;
                                            s_instance._storageKV[internalStorageKey] = value;
                                        }
                                    }
                                }
                                s_instance.m_keys.Clear();
                                s_instance.m_values.Clear();
                            }
                        }
                        catch (Exception e){ Debug.Log(e.StackTrace); }
                    }
                }
            }
            if (s_instance == null)
                s_instance = new CredentialsKeyStore();
        }

        #endregion

        #region Internal Encryption/Decryption Functions (Static)

        /// <summary>
        /// Encrypt the specified data using specified IV
        /// </summary>
        static string Encrypt(string iv, string seed, string value)
        {
            try
            {
                if (iv == null)
                    iv = "";
                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    aes.KeySize = KEY_SIZE;
                    aes.BlockSize = BLOCK_SIZE;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.GenerateKey();
                    aes.GenerateIV();
                    aes.Key = Encoding.UTF8.GetBytes(GenerateKeyChain(seed));
                    aes.IV = Encoding.UTF8.GetBytes(iv);
                    ICryptoTransform encrypt = aes.CreateEncryptor(aes.Key, aes.IV);
                    byte[] xBuff = null;
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write))
                        {
                            byte[] xXml = Encoding.UTF8.GetBytes(value);
                            cs.Write(xXml, 0, xXml.Length);
                        }
                        xBuff = ms.ToArray();
                    }
                    string output = Convert.ToBase64String(xBuff);
                    return output;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Decrypt the specified data using specified IV
        /// </summary>
        static string Decrypt(string iv, string seed, string encryptedValue)
        {
            try
            {
                if (iv == null)
                    iv = "";
                if (encryptedValue == null)
                    encryptedValue = "";

                if (!string.IsNullOrEmpty(encryptedValue.Trim()))
                {
                    using (RijndaelManaged aes = new RijndaelManaged())
                    {
                        aes.KeySize = KEY_SIZE;
                        aes.BlockSize = BLOCK_SIZE;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.Key = Encoding.UTF8.GetBytes(GenerateKeyChain(seed));
                        aes.IV = Encoding.UTF8.GetBytes(iv);
                        var encrypt = aes.CreateDecryptor();
                        byte[] xBuff = null;
                        using (var ms = new MemoryStream())
                        {
                            using (var cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write))
                            {
                                byte[] xXml = Convert.FromBase64String(encryptedValue);
                                cs.Write(xXml, 0, xXml.Length);
                            }
                            xBuff = ms.ToArray();
                        }
                        string output = Encoding.UTF8.GetString(xBuff);
                        return output;
                    }
                }
            }
            catch { }
            return null;
        }

        #endregion

        #region Internal Key/IV Functions (Static)

        /// <summary>
        /// Generate the keychain used inside Aes Encryption (this is for internal use only
        /// </summary>
        static string GenerateKeyChain(string seed)
        {
            if (seed == null)
                seed = "";
            return HashMD5(SystemInfo.deviceUniqueIdentifier + Application.identifier + seed);
        }

        /// <summary>
        /// The key to acess the CredentialKeyStore instance in PlayerPrefs (this is public in playerprefs)
        /// </summary>
        static string GenerateStorageKey()
        {
            return HashSHA256(GenerateKeyChain(null) + STORAGE_ALIAS);
        }

        /// <summary>
        /// Generate iv to use as encryption parameter
        /// </summary>
        static string GenerateIV(string key)
        {
            return HashMD5(key + Application.identifier + IALIAS + SystemInfo.deviceUniqueIdentifier);
        }

        /// <summary>
        /// Used to generate Key inside StorageData (in Dictionary)
        /// </summary>
        static string GenerateInternalStorateKey(string key)
        {
            return HashSHA256(Application.identifier + key + SystemInfo.deviceUniqueIdentifier);
        }

        #endregion

        #region Internal Hash Functions (Static)

        static string HashSHA512(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                using (var hash = System.Security.Cryptography.SHA512.Create())
                {
                    return Hash_Internal(value, hash);
                }
            }
            return "";
        }

        static string HashSHA256(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                using (var hash = System.Security.Cryptography.SHA256.Create())
                {
                    return Hash_Internal(value, hash);
                }
            }
            return "";
        }

        static string HashMD5(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                using (var hash = System.Security.Cryptography.MD5.Create())
                {
                    return Hash_Internal(value, hash);
                }
            }
            return "";
        }

        static string Hash_Internal(string value, System.Security.Cryptography.HashAlgorithm hashAlgorithm)
        {
            if (hashAlgorithm != null && !string.IsNullOrEmpty(value))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(value);
                var hashedInputBytes = hashAlgorithm.ComputeHash(bytes);

                // Convert to text
                // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte 
                var hashedInputStringBuilder = new System.Text.StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                var hash = hashedInputStringBuilder.ToString();

                return hash;
            }
            return "";
        }

        #endregion
    }
}