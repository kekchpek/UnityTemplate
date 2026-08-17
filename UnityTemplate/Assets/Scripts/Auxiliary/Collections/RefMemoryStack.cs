using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Auxiliary.Collections
{
    public ref struct RefMemoryStack
    {
        private readonly Span<byte> _buffer;
        private int _fillIndex;

        public RefMemoryStack(Span<byte> buffer)
        {
            _buffer = buffer;
            _fillIndex = 0;
        }

        public unsafe bool Push<T>(T item) where T : unmanaged
        {
            var itemSize = sizeof(T);
            if (_buffer.Length < _fillIndex + itemSize)
            {
                return false;
            }
            var itemSpan = new Span<byte>(&item, itemSize);
            itemSpan.CopyTo(_buffer.Slice(_fillIndex, itemSize));
            _fillIndex += itemSize;
            return true;
        }

        public bool Push(ReadOnlySpan<byte> itemMemory)
        {
            var itemSize = itemMemory.Length;
            if (_buffer.Length < _fillIndex + itemSize)
            {
                return false;
            }
            itemMemory.CopyTo(_buffer.Slice(_fillIndex, itemSize));
            _fillIndex += itemSize;
            return true;
        }

        public unsafe bool Pop<T>(out T item) where T : unmanaged
        {
            var itemSize = sizeof(T);
            if (_buffer.Length < itemSize)
            {
                item = default;
                return false;
            }
            item = MemoryMarshal.Read<T>(_buffer.Slice(_fillIndex - itemSize, itemSize));
            _fillIndex -= itemSize;
            return true;
        }

        public Span<byte> AddAndGetMemory(int size) {
            if (_buffer.Length < _fillIndex + size)
            {
                return default;
            }
            var memory = _buffer.Slice(_fillIndex, size);
            _fillIndex += size;
            return memory;
        }

        public void Clear()
        {
            _fillIndex = 0;
        }
        
    }
}