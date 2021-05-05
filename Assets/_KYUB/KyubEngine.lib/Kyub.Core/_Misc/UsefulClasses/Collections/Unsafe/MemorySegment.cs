// ==++==
// 
//   Copyright (c) Kyub Interactive.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  MemorySegment<T>
**
**
** Purpose: Convenient wrapper for an array or unmanaged array, an offset, and
**          a count.  Ideally used in streams & collections.
**          Net Classes will consume an array of these.
**
**
===========================================================*/

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Runtime.CompilerServices;

namespace Kyub.Collections.Unsafe
{
    public unsafe struct MemorySegment<T> : IList<T>, IReadOnlyList<T> where T : unmanaged
    {
        T* _offsetArrayPtr;
        UnsafeArray<T> _nativeArray;
        T[] _array;
        int _offset;
        int _count;

        public MemorySegment(MemorySegment<T> segment, int offset, int count)
        {
            if (segment == null)
                throw new ArgumentNullException("array");

            _nativeArray = segment._nativeArray;
            _array = segment._array;
            _offset = offset;
            _count = count;
            _offsetArrayPtr = _nativeArray != null ? (T*)_nativeArray.NativePtr + offset : null;

        }

        public MemorySegment(T[] array) : this(array, 0, array.Length)
        {
        }

        public MemorySegment(T[] array, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NeedNonNegNum");
            if (array.Length - offset < count)
                throw new ArgumentException("Argument_InvalidOffLen");

            _nativeArray = null;
            _array = array;
            _offset = offset;
            _count = count;
            _offsetArrayPtr = null;
        }

        public MemorySegment(UnsafeArray nativeArray) : this(nativeArray, 0, nativeArray.Length)
        {
        }

        public MemorySegment(UnsafeArray nativeArray, int offset, int count)
        {
            if (nativeArray == null)
                throw new ArgumentNullException("array");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NeedNonNegNum");

            var offsetInBytes = offset * nativeArray.ElementSize;
            var countInBytes = count * nativeArray.ElementSize;
            if (nativeArray.AllocatedBytes - offsetInBytes < countInBytes)
                throw new ArgumentException("Argument_InvalidOffLen");

            _nativeArray = nativeArray.UnsafeCast<T>();
            _array = null;
            _offset = offset;
            _count = count;
            _offsetArrayPtr = _nativeArray != null ? (T*)_nativeArray.NativePtr + offset : null;
        }

        public T[] Array
        {
            get
            {
                return _array;
            }
        }

        public UnsafeArray<T> NativeArray
        {
            get
            {
                return _nativeArray;
            }
        }


        public int Offset
        {
            get
            {
                return _offset;
            }
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public int Length
        {
            get
            {
                return _count;
            }
        }

        public bool IsValid()
        {
            if (_nativeArray == null && _array == null)
                return false;
            if (_offset < 0)
                return false;
            if (_count < 0)
                return false;

            if (_nativeArray != null)
            {
                int elementSize = _nativeArray.ElementSize;
                if (_nativeArray.AllocatedBytes - (_offset * elementSize) < _count * elementSize)
                    return false;
            }
            else if (_array != null)
            {
                if (_array.Length - _offset < _count)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Cast Memory Segment with UnsafeArray to another type without allocate
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <returns></returns>
        public MemorySegment<Y> UnsafeCast<Y>() where Y : unmanaged
        {
            if (_nativeArray == null && _array == null)
                return new MemorySegment<Y>();

            UnsafeArray castNativeArray = _nativeArray;
            if (castNativeArray == null)
                castNativeArray = new UnsafeArray<T>(this._array);

            castNativeArray = castNativeArray.UnsafeCast<Y>();

            var selfElementSize = Math.Max(1, Marshal.SizeOf<T>());
            var castElementSize = Math.Max(1, Marshal.SizeOf<Y>());

            var offsetInBytes = _offset * selfElementSize;
            var countInBytes = _count * selfElementSize;

            var newOffset = offsetInBytes / castElementSize;
            var newCount = countInBytes / castElementSize;

            return new MemorySegment<Y>(castNativeArray, newOffset, newCount);
        }

        public override int GetHashCode()
        {
            return null == _array && _nativeArray == null
                        ? 0
                        : (_array != null?
                        _array.GetHashCode() ^ _offset ^ _count :
                        _nativeArray.GetHashCode() ^ _offset ^ _count);
        }

        public override bool Equals(Object obj)
        {
            if (obj is MemorySegment<T>)
                return Equals((MemorySegment<T>)obj);
            else
                return false;
        }

        public bool Equals(MemorySegment<T> obj)
        {
            if (Object.ReferenceEquals(obj, this))
                return true;

            if (Object.ReferenceEquals(this, null) || Object.ReferenceEquals(obj, null))
                return false;

            return ((obj._array == null && obj._nativeArray == null && _array == null && _nativeArray == null) || 
                (obj._array != null && obj._array == _array)  || 
                (obj._nativeArray != null && obj._nativeArray == _nativeArray)) && 
                obj._offset == _offset && obj._count == _count;
        }

        public static bool operator ==(MemorySegment<T> a, MemorySegment<T> b)
        {
            if (Object.ReferenceEquals(a, b))
                return true;

            if (Object.ReferenceEquals(a, null) || Object.ReferenceEquals(b, null))
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(MemorySegment<T> a, MemorySegment<T> b)
        {
            return !(a == b);
        }

        #region IList<T>

        public T this[int index]
        {
            get
            {
                //if (_array == null && _nativeArray == null)
                //    throw new InvalidOperationException("InvalidOperation_NullArray");
                //if (index < 0 || index >= _count)
                //    throw new ArgumentOutOfRangeException("index");

                return _array != null? _array[_offset + index] : _offsetArrayPtr[index];
            }
            set
            {
                //if (_array == null && _nativeArray == null)
                //    throw new InvalidOperationException("InvalidOperation_NullArray");
                //if (index < 0 || index >= _count)
                //    throw new ArgumentOutOfRangeException("index");

                if (_array != null)
                    _array[_offset + index] = value;
                else
                    _offsetArrayPtr[index] = value;
            }
        }

        int IList<T>.IndexOf(T item)
        {
            if (_array == null && _nativeArray == null)
                throw new InvalidOperationException("InvalidOperation_NullArray");

            int index = _array != null ?
                System.Array.IndexOf<T>(_array, item, _offset, _count) :
                UnsafeArray<T>.IndexOf(_nativeArray, item, _offset, _count);

            return index >= 0 ? index - _offset : -1;
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region IReadOnlyList<T>
        T IReadOnlyList<T>.this[int index]
        {
            get
            {
                if (_array == null && _nativeArray == null)
                    throw new InvalidOperationException("InvalidOperation_NullArray");
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException("index");

                return _array[_offset + index];
            }
        }
        #endregion IReadOnlyList<T>

        #region ICollection<T>
        bool ICollection<T>.IsReadOnly
        {
            get
            {
                // the indexer setter does not throw an exception although IsReadOnly is true.
                // This is to match the behavior of arrays.
                return true;
            }
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            if (_array == null && _nativeArray == null)
                throw new InvalidOperationException("InvalidOperation_NullArray");

            int index = _array != null ?
                System.Array.IndexOf<T>(_array, item, _offset, _count) :
                UnsafeArray.IndexOf(_nativeArray, item, _offset, _count);

            return index >= 0;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (_array == null && _nativeArray == null)
                throw new InvalidOperationException("InvalidOperation_NullArray");

            if (_array != null)
                System.Array.Copy(_array, _offset, array, arrayIndex, _count);
            else
            {
                UnsafeArray.Copy(_nativeArray, _offset, array, arrayIndex, _count);
            }
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable<T>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            if (_array == null)
                throw new InvalidOperationException("InvalidOperation_NullArray");

            return new MemorySegmentEnumerator(this);
        }
        #endregion

        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_array == null)
                throw new InvalidOperationException("InvalidOperation_NullArray");

            return new MemorySegmentEnumerator(this);
        }
        #endregion

        #region Static Utils

        public static unsafe void Copy(MemorySegment<T> sourceArray, int sourceIndex, MemorySegment<T> destinationArray, int destinationIndex, int length)
        {
            if (sourceArray.Array != null && destinationArray.Array != null)
            {
                System.Array.Copy(sourceArray.Array, sourceArray.Offset + sourceIndex, destinationArray.Array, destinationArray.Offset + destinationIndex, length);
            }
            else if (sourceArray.Array != null && destinationArray.NativeArray != null)
            {
                UnsafeArray.Copy(sourceArray.Array, sourceArray.Offset + sourceIndex, destinationArray.NativeArray, destinationArray.Offset + destinationIndex, length);
            }
            else if (sourceArray.NativeArray != null && destinationArray.Array != null)
            {
                UnsafeArray.Copy(sourceArray.NativeArray, sourceArray.Offset + sourceIndex, destinationArray.Array, destinationArray.Offset + destinationIndex, length);
            }
            else if (sourceArray.NativeArray != null && destinationArray.NativeArray != null)
            {
                UnsafeArray.Copy(sourceArray.NativeArray, sourceArray.Offset + sourceIndex, destinationArray.NativeArray, destinationArray.Offset + destinationIndex, length);
            }
        }

        public static unsafe void Copy(MemorySegment<T> sourceArray, int sourceIndex, T[] destinationArray, int destinationIndex, int length)
        {
            if (sourceArray.Array != null)
            {
                System.Array.Copy(sourceArray.Array, sourceArray.Offset + sourceIndex, destinationArray, destinationIndex, length);
            }
            else if (sourceArray.NativeArray != null)
            {
                UnsafeArray.Copy(sourceArray.NativeArray, sourceArray.Offset + sourceIndex, destinationArray, destinationIndex, length);
            }
        }

        public static unsafe void Copy(MemorySegment<T> sourceArray, int sourceIndex, UnsafeArray<T> destinationArray, int destinationIndex, int length)
        {
            if (sourceArray.Array != null)
            {
                UnsafeArray.Copy(sourceArray.Array, sourceArray.Offset + sourceIndex, destinationArray, destinationIndex, length);
            }
            else if (sourceArray.NativeArray != null)
            {
                UnsafeArray.Copy(sourceArray.NativeArray, sourceArray.Offset + sourceIndex, destinationArray, destinationIndex, length);
            }
        }

        public static unsafe void Copy(T[] sourceArray, int sourceIndex, MemorySegment<T> destinationArray, int destinationIndex, int length)
        {
            if (destinationArray.Array != null)
            {
                System.Array.Copy(sourceArray, sourceIndex, destinationArray.Array, destinationArray.Offset + destinationIndex, length);
            }
            else if (destinationArray.NativeArray != null)
            {
                UnsafeArray.Copy(sourceArray, sourceIndex, destinationArray.NativeArray, destinationArray.Offset + destinationIndex, length);
            }
        }

        public static unsafe void Copy(UnsafeArray<T> sourceArray, int sourceIndex, MemorySegment<T> destinationArray, int destinationIndex, int length)
        {
            if (destinationArray.Array != null)
            {
                UnsafeArray.Copy(sourceArray, sourceIndex, destinationArray.Array, destinationArray.Offset + destinationIndex, length);
            }
            else if (destinationArray.NativeArray != null)
            {
                UnsafeArray.Copy(sourceArray, sourceIndex, destinationArray.NativeArray, destinationArray.Offset + destinationIndex, length);
            }
        }

        public static unsafe void BlockCopy(MemorySegment<T> source, int sourceOffsetInBytes, Array destination, int destinationOffsetInBytes, int lengthInBytes)
        {
            var baseSourceOffsetInBytes = (source.Offset * Marshal.SizeOf<T>());
            if (source.Array != null)
            {
                Buffer.BlockCopy(source.Array, baseSourceOffsetInBytes + sourceOffsetInBytes, destination, destinationOffsetInBytes, lengthInBytes);
            }
            else if (source.NativeArray != null)
            {
                UnsafeArray.BlockCopy(source.NativeArray, baseSourceOffsetInBytes + sourceOffsetInBytes, destination, destinationOffsetInBytes, lengthInBytes);
            }
        }

        public static unsafe void BlockCopy(Array source, int sourceOffsetInBytes, MemorySegment<T> destination, int destinationOffsetInBytes, int lengthInBytes)
        {
            var baseDestinationOffsetInBytes = (destination.Offset * Marshal.SizeOf<T>());
            if (destination.Array != null)
            {
                Buffer.BlockCopy(source, sourceOffsetInBytes, destination.Array, baseDestinationOffsetInBytes + destinationOffsetInBytes, lengthInBytes);
            }
            else if (destination.NativeArray != null)
            {
                UnsafeArray.BlockCopy(source, sourceOffsetInBytes, destination.NativeArray, baseDestinationOffsetInBytes + destinationOffsetInBytes, lengthInBytes);
            }
        }

        #endregion

        #region Internal Enumerator

        [Serializable]
        private sealed class MemorySegmentEnumerator : IEnumerator<T>
        {
            private UnsafeArray _nativeArray;
            private T[] _array;
            private int _start;
            private int _end;
            private int _current;

            internal MemorySegmentEnumerator(MemorySegment<T> MemorySegment)
            {
                _nativeArray = MemorySegment._nativeArray;
                _array = MemorySegment._array;
                _start = MemorySegment._offset;
                _end = _start + MemorySegment._count;
                _current = _start - 1;
            }

            public bool MoveNext()
            {
                if (_current < _end)
                {
                    _current++;
                    return (_current < _end);
                }
                return false;
            }

            public T Current
            {
                get
                {
                    if (_current < _start) throw new InvalidOperationException("InvalidOperation_EnumNotStarted");
                    if (_current >= _end) throw new InvalidOperationException("InvalidOperation_EnumEnded");
                    return _array[_current];
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                _current = _start - 1;
            }

            public void Dispose()
            {
            }
        }

        #endregion
    }

}