using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kyub.Collections.Unsafe
{
    public abstract unsafe class UnsafeArray : IEnumerable, IList, IDisposable
    {
        protected static object s_refCountLock = new object();
        protected static Dictionary<IntPtr, int> s_refCount = new Dictionary<IntPtr, int>();

        protected IntPtr _nativePtr = IntPtr.Zero;
        protected int _length = 0;
        protected int _elementSize = 1;

        /// <summary>
        /// Change to true if want to add pressure to GC also...this doesn't cause array to 
        /// use managed memory from GC to back the array, but does add the count of bytes of
        /// unmanaged memory used in array to the GC's calculations used to determine when to
        /// do a GC collection. It just gives the GC a hint (byte count) to use in its
        /// collection calculations, it doesn't cause array to use any GC memory to back
        /// the array.
        /// </summary>
        protected bool _gcPressure = true;

        /// <summary>
        /// Releases all unmanaged memory backing this array.
        /// </summary>
        protected virtual bool Free()
        {
            var sucess = false;
            if (_nativePtr != IntPtr.Zero)
            {
                lock (s_refCountLock)
                {
                    int refCount;

                    //Decrement RefCount
                    if (!s_refCount.TryGetValue(_nativePtr, out refCount) || refCount <= 1)
                    {
                        s_refCount.Remove(_nativePtr);

                        Marshal.FreeHGlobal(_nativePtr);
                        _nativePtr = IntPtr.Zero;

                        if (_gcPressure)
                            GC.RemoveMemoryPressure(_length * _elementSize);

                        //UnityEngine.Debug.Log($"[UnmanagedArray] Free Len: {(_length * _elementSize)}");
                    }
                    else
                    {
                        s_refCount[_nativePtr] -= 1;

                        //UnityEngine.Debug.Log($"[UnmanagedArray] Decrement To: {(s_refCount[_nativePtr])}");
                    }
                }

                sucess = true;
            }

            _length = 0;

            return sucess;
        }


        /// <summary>
        /// Gets pointer for unmanaged memory backing this array.
        /// </summary>
        public IntPtr NativePtr { get { return _nativePtr; } }

        public int Length { get { return _length; } }

        public int LengthInBytes { get { return _length * _elementSize; } }

        public int ElementSize { get { return _elementSize; } }

        public UnmanagedArray<Y> UnsafeCast<Y>() where Y : unmanaged
        {
            if (this is UnmanagedArray<Y>)
                return this as UnmanagedArray<Y>;

            return new UnmanagedArray<Y>(this, true);
        }

        #region IList

        /// <summary>
        /// Count of items in the array.
        /// </summary>
        public int Count
        {
            get { return _length; }
        }

        #region ICollection

        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        public abstract bool IsFixedSize { get; }
        public abstract bool IsSynchronized { get; }
        public abstract object SyncRoot { get; }

        object IList.this[int index] { get { return GetAt(index); }  set { SetAt(index, value); }  }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        int IList.Add(object value)
        {
            throw new NotImplementedException();
        }

        void IList.Clear()
        {
            throw new NotImplementedException();
        }

        void IList.Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        void IList.Remove(object value)
        {
            throw new NotImplementedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        bool IList.Contains(object value)
        {
            throw new NotImplementedException();
        }

        int IList.IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        protected abstract object GetAt(int index);

        protected abstract void SetAt(int index, object value);

        #endregion

        #endregion

        #region IDisposable

        public bool IsDisposed()
        {
            return _disposed;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UnsafeArray()
        {
            Dispose(false);
        }

        protected bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    //managed here...
                }

                //unmanaged here...

                Free();
            }
            this._disposed = true;
        }

        #endregion

        #region Static Utils

        public static int IndexOf(UnsafeArray array, object item, int startIndex, int count)
        {
            if (array != null)
            {
                var finalLength = Math.Min(array.Length, count);
                for (int i = startIndex; i < finalLength; i++)
                {
                    if (array.GetAt(i).Equals(item))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static unsafe void Copy(UnsafeArray sourceArray, int sourceIndex, UnsafeArray destinationArray, int destinationIndex, int length)
        {
            if (sourceArray.Count <= 0)
                throw new Exception("Nothing in source array to copy to destination array.");
            if (destinationArray.Count <= 0)
                throw new Exception("Destination array empty. Destination array must be large enough to hold source array.");
            if (destinationIndex < 0 || destinationIndex >= destinationArray.Count - 1)
                throw new IndexOutOfRangeException();
            if (length > destinationArray.Count)
                throw new ArgumentException("Destination array being copied to is not large enough to hold source array, when starting copy at specified arrayIndex.", "array");

            var blockLen = length * sourceArray._elementSize;

            IntPtr destinationAddress = destinationIndex == 0 ? destinationArray.NativePtr : IntPtr.Add(destinationArray.NativePtr, destinationIndex * destinationArray._elementSize);
            IntPtr sourceAddress = sourceIndex == 0 ? sourceArray.NativePtr : IntPtr.Add(sourceArray.NativePtr, sourceIndex * sourceArray._elementSize);

            Buffer.MemoryCopy(sourceAddress.ToPointer(), destinationAddress.ToPointer(), blockLen, blockLen);
        }

        public static unsafe void Copy<T>(UnsafeArray sourceArray, int sourceIndex, T[] destinationArray, int destinationIndex, int length) where T : unmanaged
        {
            if (sourceArray.Count <= 0)
                throw new Exception("Nothing in source array to copy to destination array.");
            if (destinationArray.Length <= 0)
                throw new Exception("Destination array empty. Destination array must be large enough to hold source array.");
            if (destinationIndex < 0 || destinationIndex >= destinationArray.Length - 1)
                throw new IndexOutOfRangeException();
            if (length > destinationArray.Length)
                throw new ArgumentException("Destination array being copied to is not large enough to hold source array, when starting copy at specified arrayIndex.", "array");

            var blockLen = length * Marshal.SizeOf(typeof(T));

            fixed (T* pDestinationArray = destinationArray)
            {
                byte* pDestinationOffsetArray = (byte*)pDestinationArray;
                pDestinationOffsetArray += destinationIndex;

                IntPtr sourceAddress = sourceIndex == 0 ? sourceArray.NativePtr : IntPtr.Add(sourceArray.NativePtr, sourceIndex * sourceArray._elementSize);
                Buffer.MemoryCopy(sourceAddress.ToPointer(), pDestinationOffsetArray, blockLen, blockLen);
            }
        }

        public static unsafe void Copy<T>(T[] sourceArray, int sourceIndex, UnsafeArray destinationArray, int destinationIndex, int length) where T : unmanaged
        {
            if (sourceArray.Length <= 0)
                throw new Exception("Nothing in source array to copy to destination array.");
            if (destinationArray.Count <= 0)
                throw new Exception("Destination array empty. Destination array must be large enough to hold source array.");
            if (destinationIndex < 0 || destinationIndex >= destinationArray.Count - 1)
                throw new IndexOutOfRangeException();
            if (length > destinationArray.Count)
                throw new ArgumentException("Destination array being copied to is not large enough to hold source array, when starting copy at specified arrayIndex.", "array");

            var blockLen = length * Marshal.SizeOf(typeof(T));
            fixed (T* pSourceArray = sourceArray)
            {
                byte* pSourceOffsetArray = (byte*)pSourceArray;
                pSourceOffsetArray += sourceIndex;

                IntPtr destinationAddress = destinationIndex == 0 ? destinationArray.NativePtr : IntPtr.Add(destinationArray.NativePtr, destinationIndex * destinationArray._elementSize);
                Buffer.MemoryCopy(pSourceOffsetArray, destinationAddress.ToPointer(), blockLen, blockLen);
            }
        }

        public static unsafe void BlockCopy(UnsafeArray source, int sourceOffsetInBytes, Array destination, int destinationOffsetInBytes, int lengthInBytes)
        {
            if (destination is byte[])
            {
                fixed (byte* destinationP = (byte[])destination)
                {
                    var destPIndex = destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (destination is sbyte[])
            {
                fixed (sbyte* destinationP = (sbyte[])destination)
                {
                    byte* destPIndex = (byte*)destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (destination is short[])
            {
                fixed (short* destinationP = (short[])destination)
                {
                    byte* destPIndex = (byte*)destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (destination is ushort[])
            {
                fixed (ushort* destinationP = (ushort[])destination)
                {
                    byte* destPIndex = (byte*)destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (destination is int[])
            {
                fixed (int* destinationP = (int[])destination)
                {
                    byte* destPIndex = (byte*)destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (destination is uint[])
            {
                fixed (uint* destinationP = (uint[])destination)
                {
                    byte* destPIndex = (byte*)destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (destination is long[])
            {
                fixed (long* destinationP = (long[])destination)
                {
                    byte* destPIndex = (byte*)destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (destination is ulong[])
            {
                fixed (ulong* destinationP = (ulong[])destination)
                {
                    byte* destPIndex = (byte*)destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (destination is bool[])
            {
                fixed (bool* destinationP = (bool[])destination)
                {
                    byte* destPIndex = (byte*)destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (destination is char[])
            {
                fixed (char* destinationP = (char[])destination)
                {
                    byte* destPIndex = (byte*)destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (destination is double[])
            {
                fixed (double* destinationP = (double[])destination)
                {
                    byte* destPIndex = (byte*)destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (destination is float[])
            {
                fixed (float* destinationP = (float[])destination)
                {
                    byte* destPIndex = (byte*)destinationP + destinationOffsetInBytes;
                    var sourceAddress = IntPtr.Add(source.NativePtr, sourceOffsetInBytes).ToPointer();
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
        }

        public static unsafe void BlockCopy(Array source, int sourceOffsetInBytes, UnsafeArray destination, int destinationOffsetInBytes, int lengthInBytes)
        {
            if (source is byte[])
            {
                fixed (byte* sourceP = (byte[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (source is sbyte[])
            {
                fixed (sbyte* sourceP = (sbyte[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (source is short[])
            {
                fixed (short* sourceP = (short[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (source is ushort[])
            {
                fixed (ushort* sourceP = (ushort[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (source is int[])
            {
                fixed (int* sourceP = (int[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (source is uint[])
            {
                fixed (uint* sourceP = (uint[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (source is long[])
            {
                fixed (long* sourceP = (long[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (source is ulong[])
            {
                fixed (ulong* sourceP = (ulong[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (source is bool[])
            {
                fixed (bool* sourceP = (bool[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (source is char[])
            {
                fixed (char* sourceP = (char[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (source is double[])
            {
                fixed (double* sourceP = (double[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }
            else if (source is float[])
            {
                fixed (float* sourceP = (float[])source)
                {
                    var destPIndex = IntPtr.Add(destination.NativePtr, destinationOffsetInBytes).ToPointer();
                    var sourceAddress = (byte*)sourceP + sourceOffsetInBytes;
                    Buffer.MemoryCopy(sourceAddress, destPIndex, lengthInBytes, lengthInBytes);
                }
            }

           
        }

        #endregion
    }

    public unsafe class UnmanagedArray<T> : UnsafeArray, IList<T>, ICollection, IList
            where T : unmanaged //this constrains T to only allow user to use value-type (i.e. byte, int, long, etc...)
    {
        protected T* _unsafeNativePtr = null;

        public unsafe UnmanagedArray(UnsafeArray nativeArray, bool shareBuffer)
        {
            _elementSize = Math.Max(1, Marshal.SizeOf(typeof(T)));
            _length = nativeArray == null? 0 : nativeArray.LengthInBytes / _elementSize;

            _nativePtr = nativeArray == null || !shareBuffer? IntPtr.Zero : nativeArray.NativePtr;

            if (_nativePtr == IntPtr.Zero)
            {
                _nativePtr = Marshal.AllocHGlobal(_length * _elementSize);
                if (_nativePtr == IntPtr.Zero)
                    throw new OutOfMemoryException("Allocation request failed.");

                if (_gcPressure) GC.AddMemoryPressure(_length * _elementSize);
            }

            _unsafeNativePtr = (T*)_nativePtr;

            //Increment RefCount
            lock (s_refCountLock)
            {
                if (s_refCount.ContainsKey(_nativePtr))
                    s_refCount[_nativePtr] += 1;
                else
                    s_refCount[_nativePtr] = 1;
            }

            if(!shareBuffer)
                UnsafeArray.Copy(nativeArray, 0, this, 0, _length);
        }

        public unsafe UnmanagedArray(T[] array)
        {
            _elementSize = Math.Max(1, Marshal.SizeOf(typeof(T)));
            _length = array == null ? 0 : array.Length;

            _nativePtr = Marshal.AllocHGlobal(_length * _elementSize);
            if (_nativePtr == IntPtr.Zero)
                throw new OutOfMemoryException("Allocation request failed.");

            if (_gcPressure) GC.AddMemoryPressure(_length * _elementSize);

            _unsafeNativePtr = (T*)_nativePtr;

            //Increment RefCount
            lock (s_refCountLock)
            {
                if (s_refCount.ContainsKey(_nativePtr))
                    s_refCount[_nativePtr] += 1;
                else
                    s_refCount[_nativePtr] = 1;
            }

            //Copy Array
            if(array != null && _length > 0)
                UnsafeArray.Copy(array, 0, this, 0, _length);
        }

        public unsafe UnmanagedArray(int length)
        {
            _elementSize = Math.Max(1, Marshal.SizeOf(typeof(T)));
            _length = length;

            _nativePtr = Marshal.AllocHGlobal(_length * _elementSize);
            if (_nativePtr == IntPtr.Zero)
                throw new OutOfMemoryException("Allocation request failed.");

            _unsafeNativePtr = (T*)_nativePtr;

            //Increment RefCount
            lock (s_refCountLock)
            {
                if (s_refCount.ContainsKey(_nativePtr))
                    s_refCount[_nativePtr] += 1;
                else
                    s_refCount[_nativePtr] = 1;
            }

            if (_gcPressure) GC.AddMemoryPressure(_length * _elementSize);
        }

        /// <summary>
        /// Releases all unmanaged memory backing this array.
        /// </summary>
        protected override bool Free()
        {
            _unsafeNativePtr = null;
            return base.Free();
            
        }

        public unsafe T* UnsafeNativePtr
        {
            get
            {
                return _unsafeNativePtr;
            }
        }

        #region IList

        /////////////////////////
        // IIndexer<T> is basically to get around the problem of not being able
        // to cast to type T. For each new value-type you want to support, you
        // simply add another IIndexer<type> interface to this UnmanagedArray<T> class
        // declaration, and specify how that specific type is going to be parsed
        // in the actual implemention of that IIndexer<type> interface.
        // We lose some overall throughput speed and efficiency going through these
        // extra interfaces. So, if speed is the highest concern and much more
        // important than style of the code, you could re-write UnmanagedArray<T> to be
        // a separate class for each value-type, putting the type parsing logic
        // directly in the main indexer. (e.g. re-write UnmanagedArray<T> to ByteUnmanagedArray,
        // IntUnmanagedArray, LongUnmanagedArray, etc.) When you benchmark to compare, make
        // sure you compile in Release mode for an accurate comparison.
        /////////////////////////
        public unsafe virtual T this[int index]
        {
            get
            {
                //if (index < 0 || index >= this.Count)
                //    throw new IndexOutOfRangeException();

                return _unsafeNativePtr[index]; //Unsafe.Read<T>(_unsafeArrayPtr + (index * _size));
            }
            set
            {
                //if (index < 0 || index >= this.Count)
                //    throw new IndexOutOfRangeException();

                _unsafeNativePtr[index] = value;
                //Unsafe.Write<T>(_unsafeArrayPtr + (index * _size), value);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            try
            {
                for (int i = 0; i < _length; i++)
                {
                    yield return this[i];
                }
            }
            finally
            {
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region ICollection

        /// <summary>
        /// Inserts item into array at the specified index.
        /// </summary>
        public void Insert(int index, T item)
        {
            this[index] = item;
        }

        /// <summary>
        /// Copies this source UnmanagedArray(T) to a destination UnmanagedArray(T) without using
        /// Large Object Heap.
        /// 
        /// Caution: This can be dangerous. Be extremely careful not to copy outside the bounds
        /// of the destination array.
        /// </summary>
        /// <param name="array">Destination UnmanagedArray(T).</param>
        /// <param name="arrayIndex">Starting index inside destination UnmanagedArray(T) to begin copying this entire source UnmanagedArray(T).</param>
        public unsafe void CopyTo(UnmanagedArray<T> array, int arrayIndex)
        {
            if (this.Count <= 0)
                throw new Exception("Nothing in source array to copy to destination array.");
            if (array.Count <= 0)
                throw new Exception("Destination array empty. Destination array must be large enough to hold source array.");
            if (arrayIndex < 0 || arrayIndex >= array.Count - 1)
                throw new IndexOutOfRangeException();
            if ((arrayIndex + this.Count) > array.Count)
                throw new ArgumentException("Destination array being copied to is not large enough to hold source array, when starting copy at specified arrayIndex.", "array");

            var blockLen = Math.Min((_length * _elementSize), (array._length - arrayIndex) * array._elementSize);

            Copy(this, 0, array, arrayIndex, blockLen);
        }

        /// <summary>
        /// CopyTo(T[] array, int arrayIndex).
        /// Must use CopyTo(UnmanagedArray(T) array, int arrayIndex) method to stay off Large Object Heap.
        /// </summary>
        public unsafe void CopyTo(T[] array, int arrayIndex)
        {
            if (this.Count <= 0)
                throw new Exception("Nothing in source array to copy to destination array.");
            if (array.Length <= 0)
                throw new Exception("Destination array empty. Destination array must be large enough to hold source array.");
            if (arrayIndex < 0 || arrayIndex >= array.Length - 1)
                throw new IndexOutOfRangeException();
            if ((arrayIndex + this.Count) > array.Length)
                throw new ArgumentException("Destination array being copied to is not large enough to hold source array, when starting copy at specified arrayIndex.", "array");

            var blockLen = Math.Min((_length * _elementSize), (array.Length - arrayIndex) * Marshal.SizeOf(typeof(T)));

            Copy(this, 0, array, arrayIndex, blockLen);
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public int IndexOf(T item)
        {
            return IndexOf(this, item, 0, _length);
        }

        #region NotImplemented

        public void Clear()
        {
        }

        /// <summary>
        /// Add(T item) method not supported.
        /// Must use array indexer (example: array[0]=0x1) to add item at the specified index.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Add(T item)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// RemoveAt(int index) method not supported.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Remove(T item) method not supported.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        int IList.Add(object value)
        {
            throw new NotImplementedException();
        }

        void IList.Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        void IList.Remove(object value)
        {
            throw new NotImplementedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            UnsafeArray.BlockCopy(this, 0, array, index * Buffer.ByteLength(array), Math.Min(LengthInBytes, array.Length - index));
        }

        bool IList.Contains(object value)
        {
            if (value is T)
                return Contains((T)value);

            return false;
        }

        int IList.IndexOf(object value)
        {
            if(value is T)
                return  IndexOf((T)value);

            return -1;
        }

        protected override object GetAt(int index)
        {
            return this[index];
        }

        protected override void SetAt(int index, object value)
        {
            if (value is T)
                this[index] = (T)value;
        }

        #endregion

        #endregion

        #endregion

        #region Casting Operators

        //////////////////////////
        // Instructions:
        //
        //   For each new IIndexer<type> you add to UnmanagedArray<T> class declaration on top,
        //   add the new pair of explicit casting operators here if you want to support
        //   casting to and from the .NET array type (i.e. UnmanagedArray<byte> to byte[],
        //   and byte[] to UnmanagedArray<byte>, etc...)
        //
        //////////////////////////
        ///////////////////////////////////////////////////////////////////////
        // Important: These are here purely for convience. They are simply to
        // make it easy to cast back and forth between this UnmanagedArray(T)
        // class and the built-in .NET Framework array types (i.e. byte[],
        // int[], long[], etc.) for integrating with existing methods with
        // method parameters that only accept built-in .NET Framework array
        // types. These casting operators are not good. They basically defeat
        // the entire purpose of this class, because they must either create
        // a new .NET Framework array type to return for the cast (which goes
        // on Large Object Heap for large arrays), or make a duplicate second
        // copy in unmanaged memory for casting the other way.
        // 
        // Note: Need to add a new pair of these casting operators when you add
        // a new IIndexer(T) that supports a new value-type if you want to have
        // the ability to cast to and from that .NET Framework value-type array.
        // 
        // Caution: Only use these casting operators if there's no other way
        // to do what you're trying accomplish. Using these is either very
        // inefficient with memory, or can cause stuff to go on the Large
        // Object Heap. May NOT want to support these casting operation at all
        // because of thier impact on the Large Object Heap. It may actually
        // make it to easy for an end-user to simply cast this array to a .NET
        // array which will go on the Large Object Heap, without even realizing it.
        //
        // For a fun kind of out there future enhancement that just might be
        // possible but would require some work would be to possibly modify the
        // coreclr and corefx open-source code so there's a bit that can be set
        // in the object header for the built-in .NET Framework value-type array
        // object, which tells the GC to ignore this object when it does a GC
        // collection. That would allow use to do what we call 'Type-Facing'.
        // 'Type-Facing' is essentially creating a null .Net value-type array
        // (i.e. byte[] b = null), adding the object header, method table
        // and length section of bytes from the .Net value-type byte[] to the
        // beginning of the unmanaged memory backing our UnmanagedArray(T) array,
        // and changing the pointer of that .Net value-type byte[] to the address
        // of our unmanaged memory backing our UnmanagedArray(T) array. This worked
        // in our tests, our UnmanagedArray(T) array now looked and functioned like
        // the .Net value-type byte[] (e.g. our UnmanagedArray(T) array now
        // essentially had a 'Face' of a .NET byte[]), however the GC would still
        // collect the .NET byte[] when the GC decided to do a GC collection at
        // various times because the .NET byte[] was pointing at unmanaged memory
        // whose memory address was outside the begin and ending address range of
        // the managed memory the GC manages and the GC would think it's ok to
        // clean up and destroy the object. At that point, even though our
        // unmanaged memory was still in place, the .NET byte[] object was now
        // unusuable.
        ///////////////////////////////////////////////////////////////////////
        public static explicit operator byte[](UnmanagedArray<T> array)
        {
            byte[] a = new byte[array.Count];
            Marshal.Copy(array.NativePtr, a, 0, array.Count);
            return a;
        }
        public static explicit operator UnmanagedArray<T>(byte[] array)
        {
            UnmanagedArray<T> a = new UnmanagedArray<T>(array.GetLength(0));
            Marshal.Copy(array, 0, a.NativePtr, array.GetLength(0));
            return a;
        }

        public static explicit operator int[](UnmanagedArray<T> array)
        {
            int[] a = new int[array.Count];
            Marshal.Copy(array.NativePtr, a, 0, array.Count);
            return a;
        }
        public static explicit operator UnmanagedArray<T>(int[] array)
        {
            UnmanagedArray<T> a = new UnmanagedArray<T>(array.GetLength(0));
            Marshal.Copy(array, 0, a.NativePtr, array.GetLength(0));
            return a;
        }

        public static explicit operator long[](UnmanagedArray<T> array)
        {
            long[] a = new long[array.Count];
            Marshal.Copy(array.NativePtr, a, 0, array.Count);
            return a;
        }
        public static explicit operator UnmanagedArray<T>(long[] array)
        {
            UnmanagedArray<T> a = new UnmanagedArray<T>(array.GetLength(0));
            Marshal.Copy(array, 0, a.NativePtr, array.GetLength(0));
            return a;
        }

        public static explicit operator double[](UnmanagedArray<T> array)
        {
            double[] a = new double[array.Count];
            Marshal.Copy(array.NativePtr, a, 0, array.Count);
            return a;
        }
        public static explicit operator UnmanagedArray<T>(double[] array)
        {
            UnmanagedArray<T> a = new UnmanagedArray<T>(array.GetLength(0));
            Marshal.Copy(array, 0, a.NativePtr, array.GetLength(0));
            return a;
        }

        #endregion

        #region IDisposable

        public override bool IsFixedSize 
        { 
            get {return true;} 
        }

        public override bool IsSynchronized
        {
            get { return false; }
        }

        public override object SyncRoot
        {
            get { return null; }
        }


        #endregion
    }
}