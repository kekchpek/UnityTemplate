using System;
using System.Collections.Generic;

namespace kekchpek.SaveSystem.Utils
{
    public static class StaticBufferPool
    {
        // Pool is organized by the raw byte capacity of the buffer. Each entry keeps NativeList instances whose
        // element size is always 1 (so each element is a single byte). This allows us to reuse unmanaged buffers
        // of the required size while exposing them through NativeList which provides convenient unsafe access.
        private static readonly Dictionary<(int size, int elementSize), Stack<NativeList>> _pools = new Dictionary<(int size, int elementSize), Stack<NativeList>>();
        private static readonly object _lock = new object();

        public static IEnumerable<(int size, int elementSize, int count)> GetPoolStats() {
            foreach (var (key, pools) in _pools) {
                yield return (key.size, key.elementSize, pools.Count);
            }
        }

        public static void Prewarm(IEnumerable<(int size, int elementSize, int count)> sizeCountPairs)
        {
            if (sizeCountPairs == null)
                throw new ArgumentNullException(nameof(sizeCountPairs));

            foreach (var (size, elementSize, count) in sizeCountPairs)
            {
                if (size <= 0 || count <= 0)
                    continue;
                if (!_pools.TryGetValue((size, elementSize), out var pool))
                {
                    pool = new Stack<NativeList>();
                    _pools[(size, elementSize)] = pool;
                }

                for (int i = 0; i < count; i++)
                {
                    // Element size is 1 byte, capacity equals requested size.
                    pool.Push(new NativeList(elementSize, size));
                }
            }
        }

        internal static NativeList Get(int size, int elementSize)
        {
            if (size <= 0)
                throw new ArgumentException("Size must be positive", nameof(size));

            if (_pools.TryGetValue((size, elementSize), out var pool) && pool.Count > 0)
            {
                var buffer = pool.Pop();
                buffer.SetCount(0);
                return buffer;
            }


            // Element size is 1 byte, capacity equals requested size.
            return new NativeList(elementSize, size);
        }

        internal static void Release(NativeList buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var size = buffer.Capacity;
            if (!_pools.TryGetValue((size, buffer.ElementSize), out var pool))
            {
                pool = new Stack<NativeList>();
                _pools[(size, buffer.ElementSize)] = pool;
            }

            // Currently NativeList doesn't provide a direct API for clearing the content or resetting its count.
            // As the count tracking is an internal detail, we simply push the buffer back to the pool and rely on
            // the consumer to overwrite the previous data on the next use.
            pool.Push(buffer);

        }
    }
}