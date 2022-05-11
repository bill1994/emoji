using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;


namespace Kyub.Async.Image
{
    public static partial class AsyncImageLoader
    {
        public static bool IsSupported()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            return true;
#else
            return false;
#endif
        }

        /// <summary> Load image synchronously. This variant uses the default <c>LoaderSettings</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static bool LoadImage(Texture2D texture, byte[] data)
        {
            return LoadImage(texture, data, LoaderSettings.Default);
        }

        /// <summary> Load image synchronously. This variant is similar to <c>ImageConversion.LoadImage</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static bool LoadImage(Texture2D texture, byte[] data, bool markNonReadable)
        {
            var loaderSettings = LoaderSettings.Default;
            loaderSettings.markNonReadable = markNonReadable;
            return LoadImage(texture, data, loaderSettings);
        }

        /// <summary> Load image synchronously. This variant accepts a <c>LoaderSettings</c>. Useful for debugging and profiling.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static bool LoadImage(Texture2D texture, byte[] data, LoaderSettings loaderSettings)
        {
            try
            {
                if (data == null || data.Length == 0) throw new System.Exception("Input data is null or empty.");

                using (var importer = new ImageImporter(data, loaderSettings))
                {
                    importer.LoadIntoTexture(texture);
                }

                return true;
            }
            catch (System.Exception e)
            {
                if (loaderSettings.logException) Debug.LogException(e);

                return false;
            }
        }

        /// <summary> Load image synchronously. This variant uses the default <c>LoaderSettings</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static bool LoadImage(Texture2D texture, NativeArray<byte> dataNative)
        {
            return LoadImage(texture, dataNative, LoaderSettings.Default);
        }

        /// <summary> Load image synchronously. This variant is similar to <c>ImageConversion.LoadImage</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static bool LoadImage(Texture2D texture, NativeArray<byte> dataNative, bool markNonReadable)
        {
            var loaderSettings = LoaderSettings.Default;
            loaderSettings.markNonReadable = markNonReadable;
            return LoadImage(texture, dataNative, loaderSettings);
        }

        public static unsafe bool LoadImage(Texture2D texture, NativeArray<byte> dataNative, LoaderSettings loaderSettings)
        {
            if (dataNative == null || dataNative.Length == 0) throw new System.Exception("Input data is null or empty.");

            var dataPtr = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr<byte>(dataNative);
            return LoadImage(texture, dataPtr, dataNative.Length, loaderSettings);
        }

        /// <summary> Load image synchronously. This variant uses the default <c>LoaderSettings</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static bool LoadImage(Texture2D texture, IntPtr dataPtr, int dataLen)
        {
            return LoadImage(texture, dataPtr, dataLen, LoaderSettings.Default);
        }

        /// <summary> Load image synchronously. This variant is similar to <c>ImageConversion.LoadImage</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static bool LoadImage(Texture2D texture, IntPtr dataPtr, int dataLen, bool markNonReadable)
        {
            var loaderSettings = LoaderSettings.Default;
            loaderSettings.markNonReadable = markNonReadable;
            return LoadImage(texture, dataPtr, dataLen, loaderSettings);
        }

        public static bool LoadImage(Texture2D texture, IntPtr dataPtr, int dataLen, LoaderSettings loaderSettings)
        {
            try
            {
                if (dataPtr == IntPtr.Zero || dataLen == 0) throw new System.Exception("Input data is null or empty.");

                using (var importer = new ImageImporter(dataPtr, dataLen, loaderSettings))
                {
                    importer.LoadIntoTexture(texture);
                }

                return true;
            }
            catch (System.Exception e)
            {
                if (loaderSettings.logException) Debug.LogException(e);

                return false;
            }
        }

        /// <summary> Load image asynchronously. This variant uses the default <c>LoaderSettings</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static Task<bool> LoadImageAsync(Texture2D texture, byte[] data)
        {
            return LoadImageAsync(texture, data, LoaderSettings.Default);
        }

        /// <summary> Load image asynchronously. This variant is similar to <c>ImageConversion.LoadImage</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static Task<bool> LoadImageAsync(Texture2D texture, byte[] data, bool markNonReadable)
        {
            var loaderSettings = LoaderSettings.Default;
            loaderSettings.markNonReadable = markNonReadable;
            return LoadImageAsync(texture, data, loaderSettings);
        }

        /// <summary>Load image asynchronously. This variant accepts a <c>LoaderSettings</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static async Task<bool> LoadImageAsync(Texture2D texture, byte[] data, LoaderSettings loaderSettings)
        {
            try
            {
                if (data == null || data.Length == 0) throw new System.Exception("Input data is null or empty.");

                using (var importer = await Task.Run(() => new ImageImporter(data, loaderSettings)))
                {
                    await importer.LoadIntoTextureAsync(texture);
                }

                return true;
            }
            catch (System.Exception e)
            {
                if (loaderSettings.logException) Debug.LogException(e);

                return false;
            }
        }

        /// <summary> Load image asynchronously. This variant uses the default <c>LoaderSettings</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static Task<bool> LoadImageAsync(Texture2D texture, NativeArray<byte> dataNative)
        {
            return LoadImageAsync(texture, dataNative, LoaderSettings.Default);
        }

        /// <summary> Load image asynchronously. This variant is similar to <c>ImageConversion.LoadImage</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static Task<bool> LoadImageAsync(Texture2D texture, NativeArray<byte> dataNative, bool markNonReadable)
        {
            var loaderSettings = LoaderSettings.Default;
            loaderSettings.markNonReadable = markNonReadable;
            return LoadImageAsync(texture, dataNative, loaderSettings);
        }

        /// <summary>Load image asynchronously. This variant accepts a <c>LoaderSettings</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static unsafe Task<bool> LoadImageAsync(Texture2D texture, NativeArray<byte> dataNative, LoaderSettings loaderSettings)
        {
            if (dataNative == null || dataNative.Length == 0) throw new System.Exception("Input data is null or empty.");

            var dataPtr = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr<byte>(dataNative);
            return LoadImageAsync(texture, dataPtr, dataNative.Length, loaderSettings);
        }

        /// <summary> Load image asynchronously. This variant uses the default <c>LoaderSettings</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static Task<bool> LoadImageAsync(Texture2D texture, IntPtr dataPtr, int dataLen)
        {
            return LoadImageAsync(texture, dataPtr, dataLen, LoaderSettings.Default);
        }

        /// <summary> Load image asynchronously. This variant is similar to <c>ImageConversion.LoadImage</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static Task<bool> LoadImageAsync(Texture2D texture, IntPtr dataPtr, int dataLen, bool markNonReadable)
        {
            var loaderSettings = LoaderSettings.Default;
            loaderSettings.markNonReadable = markNonReadable;
            return LoadImageAsync(texture, dataPtr, dataLen, loaderSettings);
        }

        /// <summary>Load image asynchronously. This variant accepts a <c>LoaderSettings</c>.</summary>
        /// <returns>True if the data can be loaded, false otherwise.</returns>
        public static async Task<bool> LoadImageAsync(Texture2D texture,IntPtr dataPtr, int dataLen, LoaderSettings loaderSettings)
        {
            try
            {
                if (dataPtr == IntPtr.Zero || dataLen == 0) throw new System.Exception("Input data is null or empty.");

                using (var importer = await Task.Run(() => new ImageImporter(dataPtr, dataLen, loaderSettings)))
                {
                    await importer.LoadIntoTextureAsync(texture);
                }

                return true;
            }
            catch (System.Exception e)
            {
                if (loaderSettings.logException) Debug.LogException(e);

                return false;
            }
        }

        /// <summary>Create <c>Texture2D</c> from image data synchronously. This variant uses a default <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static Texture2D CreateFromImage(byte[] data)
        {
            return CreateFromImage(data, LoaderSettings.Default);
        }

        /// <summary>Create <c>Texture2D</c> from image data synchronously. This variant accepts a <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static Texture2D CreateFromImage(byte[] data, LoaderSettings loaderSettings)
        {
            try
            {
                if (data == null || data.Length == 0) throw new System.Exception("Input data is null or empty.");

                using (var importer = new ImageImporter(data, loaderSettings))
                {
                    return importer.CreateNewTexture();
                }
            }
            catch (System.Exception e)
            {
                if (loaderSettings.logException) Debug.LogException(e);

                return null;
            }
        }

        /// <summary>Create <c>Texture2D</c> from image data synchronously. This variant uses a default <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static Texture2D CreateFromImage(NativeArray<byte> dataNative)
        {
            return CreateFromImage(dataNative, LoaderSettings.Default);
        }

        /// <summary>Create <c>Texture2D</c> from image data synchronously. This variant accepts a <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static unsafe Texture2D CreateFromImage(NativeArray<byte> dataNative, LoaderSettings loaderSettings)
        {
            if (dataNative == null || dataNative.Length == 0) throw new System.Exception("Input data is null or empty.");

            var dataPtr = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr<byte>(dataNative);

            return CreateFromImage(dataPtr, dataNative.Length, loaderSettings);
        }

        /// <summary>Create <c>Texture2D</c> from image data synchronously. This variant uses a default <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static Texture2D CreateFromImage(IntPtr dataPtr, int dataLen)
        {
            return CreateFromImage(dataPtr, dataLen, LoaderSettings.Default);
        }

        /// <summary>Create <c>Texture2D</c> from image data synchronously. This variant accepts a <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static Texture2D CreateFromImage(IntPtr dataPtr, int dataLen, LoaderSettings loaderSettings)
        {
            try
            {
                if (dataPtr == IntPtr.Zero || dataLen == 0) throw new System.Exception("Input data is null or empty.");

                using (var importer = new ImageImporter(dataPtr, dataLen, loaderSettings))
                {
                    return importer.CreateNewTexture();
                }
            }
            catch (System.Exception e)
            {
                if (loaderSettings.logException) Debug.LogException(e);

                return null;
            }
        }

        /// <summary>Create <c>Texture2D</c> from image data asynchronously. This variant uses a default <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static Task<Texture2D> CreateFromImageAsync(byte[] data)
        {
            return CreateFromImageAsync(data, LoaderSettings.Default);
        }

        /// <summary>Create <c>Texture2D</c> from image data asynchronously. This variant accepts a <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static async Task<Texture2D> CreateFromImageAsync(byte[] data, LoaderSettings loaderSettings)
        {
            try
            {
                if (data == null || data.Length == 0) throw new System.Exception("Input data is null or empty.");

                using (var importer = await Task.Run(() => new ImageImporter(data, loaderSettings)))
                {
                    return await importer.CreateNewTextureAsync();
                }
            }
            catch (System.Exception e)
            {
                if (loaderSettings.logException) Debug.LogException(e);

                return null;
            }
        }

        /// <summary>Create <c>Texture2D</c> from image data asynchronously. This variant uses a default <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static Task<Texture2D> CreateFromImageAsync(NativeArray<byte> dataNative)
        {
            return CreateFromImageAsync(dataNative, LoaderSettings.Default);
        }

        /// <summary>Create <c>Texture2D</c> from image data asynchronously. This variant accepts a <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static unsafe Task<Texture2D> CreateFromImageAsync(NativeArray<byte> dataNative, LoaderSettings loaderSettings)
        {
            if (dataNative == null || dataNative.Length == 0) throw new System.Exception("Input data is null or empty.");

            var dataPtr = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr<byte>(dataNative);

            return CreateFromImageAsync(dataPtr, dataNative.Length, loaderSettings);
        }

        /// <summary>Create <c>Texture2D</c> from image data asynchronously. This variant uses a default <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static Task<Texture2D> CreateFromImageAsync(IntPtr dataPtr, int dataLen)
        {
            return CreateFromImageAsync(dataPtr, dataLen, LoaderSettings.Default);
        }

        /// <summary>Create <c>Texture2D</c> from image data asynchronously. This variant accepts a <c>LoaderSettings</c>. Linear and mipmap count can be specified.</summary>
        /// <returns><c>Texture2D</c> object if the data can be loaded, null otherwise.</returns>
        public static async Task<Texture2D> CreateFromImageAsync(IntPtr dataPtr, int dataLen, LoaderSettings loaderSettings)
        {
            try
            {
                if (dataPtr == IntPtr.Zero || dataLen == 0) throw new System.Exception("Input data is null or empty.");

                using (var importer = await Task.Run(() => new ImageImporter(dataPtr, dataLen, loaderSettings)))
                {
                    return await importer.CreateNewTextureAsync();
                }
            }
            catch (System.Exception e)
            {
                if (loaderSettings.logException) Debug.LogException(e);

                return null;
            }
        }

        /// <summary>Settings used by the image loader.</summary>
        public struct LoaderSettings
        {
            /// <summary>Create linear texture. Only applicable to methods that create new <c>Texture2D</c>. Defaults to false.</summary>
            public bool linear;
            /// <summary>Texture data won't be readable on the CPU after loading. Defaults to false.</summary>
            public bool markNonReadable;
            /// <summary>Whether or not to generate mipmaps. Defaults to true.</summary>
            public bool generateMipmap;
            /// <summary>Automatically calculate the number of mipmap levels. Defaults to true. Only applicable to methods that create new <c>Texture2D</c>.</summary>
            public bool autoMipmapCount;
            /// <summary>Mipmap count, including the base level. Must be greater than 1. Only applicable to methods that create new <c>Texture2D</c>.</summary>
            public int mipmapCount;
            /// <summary>Used to explicitly specify the image format. Defaults to FIF_UNKNOWN, which the image format will be automatically determined.</summary>
            public FreeImage.Format format;
            /// <summary>Whether or not to log exception caught by this method. Defaults to true.</summary>
            public bool logException;

            public static LoaderSettings Default => new LoaderSettings
            {
                linear = false,
                markNonReadable = false,
                generateMipmap = true,
                autoMipmapCount = true,
                format = FreeImage.Format.FIF_UNKNOWN,
                logException = true,
            };
        }
    }
}
