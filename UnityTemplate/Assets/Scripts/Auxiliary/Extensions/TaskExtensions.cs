using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace kekchpek.Auxiliary.Extensions
{
    public static class TaskExtensions
    {
        public static void LogOnError(this Task task)
        {
            task.ContinueWith(t => {
                if (t.Exception != null) {
                    Debug.LogError(t.Exception.Message);
                }
            }, 
            cancellationToken: CancellationToken.None,
            continuationOptions: TaskContinuationOptions.ExecuteSynchronously, 
            scheduler: TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static async UniTask ToUniTask(this Task task)
        {
            await task;
        }
    }
}