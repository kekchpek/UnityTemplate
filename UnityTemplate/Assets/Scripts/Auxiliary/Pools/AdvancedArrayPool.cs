using System;
using kekchpek.Auxiliary.Collections;
using UnityEngine;

namespace kekchpek.Auxiliary.Pools
{
    public class AdvancedArrayPool<T>
    {
        private readonly SortedMultiMap<int, T[]> _pool = new();
        
        public int MaxArraysCount { get; private set; } = int.MaxValue;

        public AdvancedArrayPool((int arraysCount, int arraysCapacity)[] prewarmData)
        {
            for (int i = 0; i < prewarmData.Length; i++)
            {
                var (arraysCount, arraysCapacity) = prewarmData[i];
                for (int j = 0; j < arraysCount; j++)
                {
                    _pool.Add(i, new T[arraysCapacity]);
                }
            }
        }

        public void UpdateMaxArraysCount(int maxArraysCount)
        {
            MaxArraysCount = maxArraysCount;
            RemoveExtraArrays();
        }

        public T[] Get(int capacity)
        {
            if (_pool.TryGetClosestLargerKey(capacity, out var outcomeCapacity))
            {
                if (_pool.Remove(outcomeCapacity, out var existingArray))
                {
                    return existingArray;
                }
                else 
                {
                    Debug.LogError($"Failed to remove array from pool for capacity {outcomeCapacity}");
                }
            }
            var newCapacity = GetNearestPowerOfTwo(capacity);
            var array = new T[newCapacity];
            return array;
        }

        public void Release(T[] array)
        {
            if (_pool.Count >= MaxArraysCount && array.Length <= _pool.First.Key)
            {
                return;
            }
            _pool.Add(array.Length, array);
            RemoveExtraArrays();
        }

        private void RemoveExtraArrays() {
            while (_pool.Count > MaxArraysCount)
            {
                _pool.Remove(_pool.First.Key, out var _);
            }
        }

        private int GetNearestPowerOfTwo(int capacity)
        {
            return (int)Mathf.Pow(2, Mathf.CeilToInt(Mathf.Log(capacity, 2)));
        }
    }
}