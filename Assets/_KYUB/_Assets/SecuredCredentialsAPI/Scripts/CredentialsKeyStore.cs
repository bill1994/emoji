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
        private const string IV_ALIAS = "_IV";

        #endregion

        #region Private Variables

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

        #region Internal Helper Functions (Instance)

        string GenerateInstanceSeed()
        {
            string v_iv = GenerateIV(GenerateKeyChain(null));
            string v_decryptedSeed = null;

            //Try decrypt the serialized seed in instance
            if(!string.IsNullOrEmpty(m_seed))
                v_decryptedSeed = Decrypt(v_iv, null, m_seed);

            //we must generate a new seed if we failed to retrieve the old seed generated (the instance can be invalidated if failed to retrieve GetString using the new decrypted seed)
            if (string.IsNullOrEmpty(v_decryptedSeed))
            {
                //Generate new Decrypted seed and save the encrypted value in instance
                v_decryptedSeed = (System.DateTime.UtcNow - DateTime.MinValue).TotalMilliseconds.ToString() + UnityEngine.Random.Range(int.MinValue, int.MaxValue).ToString();
                m_seed = Encrypt(v_iv, null, v_decryptedSeed);
            }
            return v_decryptedSeed;
        }

        string GetInstanceString(string p_key)
        {
            var v_internalStorageKey = GenerateInternalStorateKey(p_key);
            if (!string.IsNullOrEmpty(v_internalStorageKey))
            {
                string v_encryptedValue = null;
                if (_storageKV.TryGetValue(v_internalStorageKey, out v_encryptedValue))
                {
                    var v_decryptedSeed = GenerateInstanceSeed();
                    var v_iv = GenerateIV(v_decryptedSeed + v_internalStorageKey);
                    var v_value = Decrypt(v_iv, v_decryptedSeed, v_encryptedValue);
                    return v_value;
                }
            }
            return null;
        }

        bool SetInstanceString(string p_key, string p_value)
        {
            var v_internalStorageKey = GenerateInternalStorateKey(p_key);
            if (!string.IsNullOrEmpty(v_internalStorageKey))
            {
                if (p_value != null)
                {
                    var v_decryptedSeed = GenerateInstanceSeed();
                    var v_iv = GenerateIV(v_decryptedSeed + v_internalStorageKey);
                    var v_encryptedData = Encrypt(v_iv, v_decryptedSeed, p_value);

                    _originalKeys.Add(p_key);
                    _storageKV[v_internalStorageKey] = v_encryptedData;
                    return true;
                }
                else
                {
                    DeleteInstanceKey(p_key);
                }
            }
            return false;
        }

        bool DeleteInstanceKey(string p_key)
        {
            var v_instanceStorageKey = GenerateInternalStorateKey(p_key);
            if (!string.IsNullOrEmpty(v_instanceStorageKey))
            {
                _originalKeys.Remove(p_key);
                return _storageKV.Remove(v_instanceStorageKey);
            }

            return false;
        }

        bool ContainsInstanceKey(string p_key)
        {
            return _originalKeys.Contains(p_key);
        }

        #endregion

        #region Public Functions (Static)

        public static string[] GetAllKeys()
        {
            if (s_instance == null)
                Load_Internal();

            return new List<string>(s_instance._originalKeys).ToArray();
        }

        public static bool SetString(string p_key, string p_value)
        {
            if (s_instance == null)
                Load_Internal();

            return s_instance.SetInstanceString(p_key.ToLower(), p_value);
        }

        public static bool SetCredential<T>(string p_key, T p_value)
        {
            return SetString(p_key, p_value != null ? SerializationUtils.ToJson(p_value) : null);
        }

        public static string GetString(string p_key)
        {
            if (s_instance == null)
                Load_Internal();

            string v_value = s_instance.GetInstanceString(p_key.ToLower());

            //Failed to decrypt, we must invalidate instance
            if (v_value == null)
                DeleteAll();

            return v_value;
        }

        public static T GetCredential<T>(string p_key)
        {
            var v_json = GetString(p_key);
            if (!string.IsNullOrEmpty(v_json))
                return SerializationUtils.FromJson<T>(v_json);

            return default(T);
        }

        public static bool DeleteKey(string p_key)
        {
            if (s_instance == null)
                Load_Internal();

            return s_instance.DeleteInstanceKey(p_key.ToLower());
        }

        public static void DeleteAll()
        {
            if (s_instance == null)
                Load_Internal();

            s_instance._originalKeys.Clear();
            s_instance._storageKV.Clear();
            Save();
        }

        public static bool HasKey(string p_key)
        {
            if (s_instance == null)
                Load_Internal();

            return s_instance.ContainsInstanceKey(p_key.ToLower());
        }

        public static void Save()
        {
            Save_Internal();
        }

        #endregion

        #region Internal Save/Load Functions (Static)

        static void Save_Internal()
        {
            var v_storageKey = GenerateStorageKey();
            if (s_instance != null && s_instance._storageKV.Count > 0)
            {
                s_instance.m_values.Clear();
                s_instance.m_keys.Clear();

                //Serialize Original Keys with Encrypted Values
                foreach(var v_key in s_instance._originalKeys)
                {
                    var v_internalStorageKey = GenerateInternalStorateKey(v_key);
                    if (s_instance._storageKV.ContainsKey(v_internalStorageKey))
                    {
                        s_instance.m_keys.Add(v_key);
                        var v_value = s_instance._storageKV[v_internalStorageKey];
                        s_instance.m_values.Add(v_value);
                    }
                }

                var v_storageIv = GenerateIV(v_storageKey);
                var v_encryptedStorage = Encrypt(v_storageIv, null, SerializationUtils.ToJson(s_instance));
                PlayerPrefs.SetString(v_storageKey, v_encryptedStorage);

                //Terminate the instance apply to player prefs
                s_instance = null;
            }
            else if (PlayerPrefs.HasKey(v_storageKey))
                PlayerPrefs.DeleteKey(v_storageKey);

            PlayerPrefs.Save();
        }

        static void Load_Internal()
        {
            var v_storageKey = GenerateStorageKey();

            if (PlayerPrefs.HasKey(v_storageKey))
            {
                var v_encryptedValue = PlayerPrefs.GetString(v_storageKey);
                var v_storageIv = GenerateIV(v_storageKey);

                if (!string.IsNullOrEmpty(v_encryptedValue))
                {
                    var v_json = Decrypt(v_storageIv, null, v_encryptedValue);
                    if (!string.IsNullOrEmpty(v_json))
                    {
                        try
                        {
                            s_instance = SerializationUtils.FromJson<CredentialsKeyStore>(v_json);

                            //Fill values in Dict
                            if (s_instance.m_keys != null)
                            {
                                for (int i = 0; i < s_instance.m_keys.Count; i++)
                                {
                                    var v_key = s_instance.m_keys[i];
                                    if (!string.IsNullOrEmpty(v_key))
                                    {
                                        //Save as internal storage key in dictionary
                                        var v_internalStorageKey = GenerateInternalStorateKey(v_key);
                                        if (!string.IsNullOrEmpty(v_internalStorageKey))
                                        {
                                            s_instance._originalKeys.Add(v_key);
                                            var v_value = s_instance.m_values != null && s_instance.m_values.Count > i ? s_instance.m_values[i] : null;
                                            s_instance._storageKV[v_internalStorageKey] = v_value;
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
        /// Encrypt the specified p_data using specified IV
        /// </summary>
        static string Encrypt(string p_iv, string p_seed, string p_value)
        {
            try
            {
                if (p_iv == null)
                    p_iv = "";
                using (RijndaelManaged v_aes = new RijndaelManaged())
                {
                    v_aes.KeySize = KEY_SIZE;
                    v_aes.BlockSize = BLOCK_SIZE;
                    v_aes.Padding = PaddingMode.PKCS7;
                    v_aes.GenerateKey();
                    v_aes.GenerateIV();
                    v_aes.Key = Encoding.UTF8.GetBytes(GenerateKeyChain(p_seed));
                    v_aes.IV = Encoding.UTF8.GetBytes(p_iv);
                    ICryptoTransform v_encrypt = v_aes.CreateEncryptor(v_aes.Key, v_aes.IV);
                    byte[] xBuff = null;
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, v_encrypt, CryptoStreamMode.Write))
                        {
                            byte[] xXml = Encoding.UTF8.GetBytes(p_value);
                            cs.Write(xXml, 0, xXml.Length);
                        }
                        xBuff = ms.ToArray();
                    }
                    string v_output = Convert.ToBase64String(xBuff);
                    return v_output;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Decrypt the specified p_data using specified IV
        /// </summary>
        static string Decrypt(string p_iv, string p_seed, string p_encryptedValue)
        {
            try
            {
                if (p_iv == null)
                    p_iv = "";
                if (p_encryptedValue == null)
                    p_encryptedValue = "";

                if (!string.IsNullOrEmpty(p_encryptedValue.Trim()))
                {
                    using (RijndaelManaged v_aes = new RijndaelManaged())
                    {
                        v_aes.KeySize = KEY_SIZE;
                        v_aes.BlockSize = BLOCK_SIZE;
                        v_aes.Mode = CipherMode.CBC;
                        v_aes.Padding = PaddingMode.PKCS7;
                        v_aes.Key = Encoding.UTF8.GetBytes(GenerateKeyChain(p_seed));
                        v_aes.IV = Encoding.UTF8.GetBytes(p_iv);
                        var v_encrypt = v_aes.CreateDecryptor();
                        byte[] xBuff = null;
                        using (var ms = new MemoryStream())
                        {
                            using (var cs = new CryptoStream(ms, v_encrypt, CryptoStreamMode.Write))
                            {
                                byte[] xXml = Convert.FromBase64String(p_encryptedValue);
                                cs.Write(xXml, 0, xXml.Length);
                            }
                            xBuff = ms.ToArray();
                        }
                        string v_output = Encoding.UTF8.GetString(xBuff);
                        return v_output;
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
        static string GenerateKeyChain(string p_seed)
        {
            if (p_seed == null)
                p_seed = "";
            return HashMD5(SystemInfo.deviceUniqueIdentifier + Application.identifier + p_seed);
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
        static string GenerateIV(string p_key)
        {
            return HashMD5(p_key + Application.identifier + IV_ALIAS + SystemInfo.deviceUniqueIdentifier);
        }

        /// <summary>
        /// Used to generate Key inside StorageData (in Dictionary)
        /// </summary>
        static string GenerateInternalStorateKey(string p_key)
        {
            return HashSHA256(Application.identifier + p_key + SystemInfo.deviceUniqueIdentifier);
        }

        #endregion

        #region Internal Hash Functions (Static)

        static string HashSHA512(string p_string)
        {
            if (!string.IsNullOrEmpty(p_string))
            {
                using (var hash = System.Security.Cryptography.SHA512.Create())
                {
                    return Hash_Internal(p_string, hash);
                }
            }
            return "";
        }

        static string HashSHA256(string p_string)
        {
            if (!string.IsNullOrEmpty(p_string))
            {
                using (var hash = System.Security.Cryptography.SHA256.Create())
                {
                    return Hash_Internal(p_string, hash);
                }
            }
            return "";
        }

        static string HashMD5(string p_string)
        {
            if (!string.IsNullOrEmpty(p_string))
            {
                using (var hash = System.Security.Cryptography.MD5.Create())
                {
                    return Hash_Internal(p_string, hash);
                }
            }
            return "";
        }

        static string Hash_Internal(string p_string, System.Security.Cryptography.HashAlgorithm p_hashAlgorithm)
        {
            if (p_hashAlgorithm != null && !string.IsNullOrEmpty(p_string))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(p_string);
                var hashedInputBytes = p_hashAlgorithm.ComputeHash(bytes);

                // Convert to text
                // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte 
                var hashedInputStringBuilder = new System.Text.StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                var v_hash = hashedInputStringBuilder.ToString();

                return v_hash;
            }
            return "";
        }

        #endregion
    }
}