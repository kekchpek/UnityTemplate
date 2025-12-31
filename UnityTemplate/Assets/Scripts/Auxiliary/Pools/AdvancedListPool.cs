using System.Collections.Generic;

namespace kekchpek.Auxiliary.Pools
{
    public class AdvancedListPool<T>
    {

        private readonly Stack<List<T>> _pool = new(5);

        public AdvancedListPool(int listsCount = 1, int listCapacity = 1)
        {
            for (int i = 0; i < listsCount; i++)
            {
                _pool.Push(new List<T>(listCapacity));
            }
        }

        public List<T> Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            return new List<T>();
        }

        public void Release(List<T> list)
        {
            list.Clear();
            _pool.Push(list);
        }

    }
}