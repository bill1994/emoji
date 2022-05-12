// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Kyub.Async.Extensions
{
    [StructLayout(LayoutKind.Sequential)]
    public static class DownloadHandlerExtension
    {
        public static NativeArray<byte>? GetNativeDataArray(this DownloadHandler handler)
        {
#if UNITY_2021_1_OR_NEWER
            if (handler != null)
            {
                var nativeData = handler.GetType().GetMethod("GetNativeData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (nativeData != null)
                {
                    return (NativeArray<byte>)nativeData.Invoke(handler, null);
                }
            }
#endif
            return null;
        }

        public static void GetTextureContentAsync(this DownloadHandler handler, Action<Texture2D> callback)
        {
            bool useAsync = false;

#if SUPPORT_IMAGE_ASYNC
            useAsync = handler != null && Image.AsyncImageLoader.IsSupported();
            if (useAsync)
            {
                var nativeArrayNullable = handler.GetNativeDataArray();
                // Unity 2021 is the first unity to expose GetNativeDataArray,
                // so its the only unity to support alloc-free async ImageCreation
                if (nativeArrayNullable == null)
                {
                    useAsync = false;
                }
                else if (nativeArrayNullable.Value.Length > 0)
                {
                    Image.AsyncImageLoader.CreateFromImageAsync(nativeArrayNullable.Value).ContinueWith((result) =>
                    {
                        var texture = result.Result;
                        if (callback != null)
                            callback.Invoke(texture);
                    });
                    return;
                }
                else
                {
                    if (callback != null)
                        callback.Invoke(null);
                    return;
                }
            }
#endif
            if (!useAsync)
            {
                var texture = handler is DownloadHandlerTexture ? ((DownloadHandlerTexture)handler).texture : null;
                if (callback != null)
                    callback.Invoke(texture);
            }
        }

    }
}