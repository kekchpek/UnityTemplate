using System;
using Cysharp.Threading.Tasks;
using UnityMVVM.ViewModelCore;

namespace kekchpek.Auxiliary.Extensions
{
    public static class ViewModelExtensions
    {
        public static async UniTask AwaitDestruction(this IViewModel viewModel)
        {
            UniTaskCompletionSource completionSource = new();
            Action<IViewModel> destroyAction = null;
            destroyAction = new Action<IViewModel>(
                vm => {
                    completionSource.TrySetResult();
                    vm.Destroyed -= destroyAction;
                });
            viewModel.Destroyed += destroyAction;
            await completionSource.Task;
        }
    }
}