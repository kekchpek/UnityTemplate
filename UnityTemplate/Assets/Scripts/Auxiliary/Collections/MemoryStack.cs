using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Auxiliary.Collections
{
    public unsafe class MemoryStack
    {
        private byte[] _buffer;
        private int _fillIndex;

        public MemoryStack(int initialCapacity = 0)
        {
            _buffer = new byte[initialCapacity];
        }

        public unsafe int Push<T>(T item) where T : unmanaged
        {
            var itemSize = sizeof(T);
            if (_buffer.Length < _fillIndex + itemSize)
            {
                _buffer = new byte[_buffer.Length * 2];
            }
            var itemSpan = new Span<byte>(&item, itemSize);
            itemSpan.CopyTo(_buffer.AsSpan(_fillIndex, itemSize));
            var fill = _fillIndex;
            _fillIndex += itemSize;
            return fill;
        }

        public unsafe int Push(Span<byte> itemMemory)
        {
            var itemSize = itemMemory.Length;
            if (_buffer.Length < _fillIndex + itemSize)
            {
                _buffer = new byte[_buffer.Length * 2];
            }
            itemMemory.CopyTo(_buffer.AsSpan(_fillIndex, itemSize));
            var fill = _fillIndex;
            _fillIndex += itemSize;
            return fill;
        }

        public unsafe T Pop<T>() where T : unmanaged
        {
            var itemSize = sizeof(T);
            if (_buffer.Length < itemSize)
            {
                Debug.LogError($"Not enough space in buffer to pop item of size {itemSize}");
                return default;
            }
            return MemoryMarshal.Read<T>(_buffer.AsSpan(_fillIndex - itemSize, itemSize));
        }

        public ReadOnlySpan<byte> GetMemory(int startIndex, int length)
        {
            return _buffer.AsSpan(startIndex, length);
        }

        public void Clear()
        {
            _fillIndex = 0;
        }

    }
}