using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace Kyub.Collections.Unsafe
{

    /// <summary>
    /// This class is merely a wrapper around the UnmanagedMemoryStream .NET Framework class,
    /// simply to allow clients to use the UnmanagedMemoryStream with our ArrayNoLOH(byte) class
    /// without having to mark the calling code in the client as unsafe. For our purposes, this
    /// class essentially substitutes as the equivalent of the fixed buffer mode portion of the
    /// MemoryStream .NET Framework class. The UnmanagedMemoryStream .NET Framework class does
    /// NOT make a copy of the memory and store it in the stream, it simply allows you to do
    /// stream operations on the existing memory.
    /// </summary>
    public class UnsafeMemoryStream : UnmanagedMemoryStream
    {
        //Right now defaults to automatically dispose UnmanagedArray<byte> array passed into
        //constructor when this class is disposed, to make sure don't leak memory if
        //forget to dispose array.
        private UnmanagedArray<byte> _array = null;
        private bool _automaticallyDisposeArray = true;

        private UnsafeMemoryStream()
        {
        }

        unsafe public UnsafeMemoryStream(UnmanagedArray<byte> array)
            : this(array, 0, array.Length)
        {
        }

        unsafe public UnsafeMemoryStream(UnmanagedArray<byte> array, int offset, int length)
            : this(array, 0, array.Length, false)
        {
            _array = array;
        }


        unsafe public UnsafeMemoryStream(UnmanagedArray<byte> array, bool automaticallyDisposeArray)
            : this(array, 0, array.Length, automaticallyDisposeArray)
        {
            _array = array;
            _automaticallyDisposeArray = automaticallyDisposeArray;
        }

        unsafe public UnsafeMemoryStream(UnmanagedArray<byte> array, int offset, int length, bool automaticallyDisposeArray)
           : base((byte*)IntPtr.Add(array.NativePtr, offset), length, length, FileAccess.ReadWrite)
        {
            _array = array;
            _automaticallyDisposeArray = automaticallyDisposeArray;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            //automatically dispose array when this class is disposed
            if (_automaticallyDisposeArray && _array != null)
            {
                _array.Dispose();
                _array = null;
            }
        }

        public virtual UnmanagedArray<byte> GetNativeBuffer()
        {
            return _array;
        }
    }
}
