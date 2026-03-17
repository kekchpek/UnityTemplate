using System.Collections.Generic;
using kekchpek.Auxiliary.Collections;

namespace kekchpek.Auxiliary.Pools
{
    public class AdvancedHyperListPool<T>
    {

        private readonly Stack<HyperList<T>> _pool;

        public AdvancedHyperListPool(int listsCount = 1, int listCapacity = 1)
        {
            _pool = new Stack<HyperList<T>>(listsCount);
            for (int i = 0; i < listsCount; i++)
            {
                _pool.Push(new HyperList<T>(listCapacity));
            }
        }

        public HyperList<T> Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            return new HyperList<T>();
        }

        public void Release(HyperList<T> list)
        {
            list.Clear();
            _pool.Push(list);
        }

    }
}