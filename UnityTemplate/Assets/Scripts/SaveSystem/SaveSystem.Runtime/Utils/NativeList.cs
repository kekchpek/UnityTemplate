using System;
using System.Runtime.InteropServices;

namespace kekchpek.SaveSystem.Utils
{
    internal unsafe class NativeList
    {

        public void* Data => _data;
        public int Count => _count;
        public int ElementSize => _elementSize;
        public int Capacity => _capacity;
        public int AmountOfBytes => _count * _elementSize;

        private void* _data;
        private int _count;
        private int _capacity;
        private int _elementSize;


        public NativeList(int elementSize, int capacity = 0) {
            _data = (void*)Marshal.AllocHGlobal(capacity * elementSize);
            _elementSize = elementSize;
            _count = 0;
            _capacity = capacity;
        }
        
        public NativeList(void* data, int count, int elementSize) {
            _data = (void*)Marshal.AllocHGlobal(count * elementSize);
            var span = new Span<byte>(data, count * elementSize);
            span.CopyTo(new Span<byte>(_data, count * elementSize));
            _elementSize = elementSize;
            _count = count;
            _capacity = count;
        }

        public void Add(void* item) {
            if (_count >= _capacity) {
                if (_capacity == 0) {
                    _capacity = 1;
                } else {
                    _capacity *= 2;
                }
                var newData = (void*)Marshal.AllocHGlobal(_capacity * _elementSize);
                var oldDataSpan = new Span<byte>(_data, _count * _elementSize);
                oldDataSpan.CopyTo(new Span<byte>(newData, _count * _elementSize));
                Marshal.FreeHGlobal((IntPtr)_data);
                _data = newData;
            }
            var itemSpan = new Span<byte>(item, _elementSize);
            itemSpan.CopyTo(new Span<byte>((byte*)((long)_data + _count * _elementSize), _elementSize));
            _count++;
        }

        public void Set(int index, void* item) {
            if (index < 0 || index >= _count) {
                throw new Exception("Index out of range");
            }
            var itemSpan = new Span<byte>(item, _elementSize);
            itemSpan.CopyTo(new Span<byte>((byte*)((long)_data + index * _elementSize), _elementSize));
        }

        public void SetCount(int count) {
            if (count < 0 || count > _capacity) {
                throw new Exception("Count out of range");
            }
            _count = count;
        }

        public void* Get(int index) {
            if (index < 0 || index >= _count) {
                throw new Exception("Index out of range");
            }
            return (void*)((long)_data + index * _elementSize);
        }

        ~NativeList() {
            Marshal.FreeHGlobal((IntPtr)_data);
        }

    }
}