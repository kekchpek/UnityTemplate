using System.Collections.Generic;
using UnityEngine.Pool;

public static class ListPoolInterlocked<T>
{

    private static readonly object _lock = new();
    
    static public List<T> Get() 
    {
        lock(_lock) {
            return ListPool<T>.Get();
        }
    }

    static public void Release(List<T> obj) 
    {
        lock(_lock) {
            ListPool<T>.Release(obj);
        }
    }

}