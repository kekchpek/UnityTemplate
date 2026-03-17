using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssetsSystem;
using Cysharp.Threading.Tasks;

namespace kekchpek.Assets
{
    public class NoAssetsModel : IAssetsModel
    {
        public UniTask FetchRemoteAssetsData() => UniTask.CompletedTask;

        public Task DownloadAssets(IEnumerable<string> paths, IProgress<(int current, int max)> progress = null) => Task.CompletedTask;

        public Task<T> LoadAsset<T>(string path, bool cache = true) => Task.FromResult(default(T));

        public Task CacheAsset<T>(string path) => Task.CompletedTask;

        public Task<bool> AssetExists(string path) => Task.FromResult(false);

        public T GetCachedAsset<T>(string path) => default;

        public bool TryGetCachedAsset<T>(string path, out T asset)
        {
            asset = default;
            return false;
        }

        public void ReleaseAllLoadedAssets() { }

        public void ReleaseLoadedAssets(string pathPattern) { }

        public Task ClearReleasedAssets() => Task.CompletedTask;
    }
}
