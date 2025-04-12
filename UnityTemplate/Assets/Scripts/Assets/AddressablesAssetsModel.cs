using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Diagnostics.Time;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AssetsSystem
{
    public class AddressablesAssetsModel : IAssetsModel
    {

        private const int CacheSize = 1000;
        
        private readonly Dictionary<string, object> _cache = new(CacheSize);

        private readonly Task _initTask;


        public AddressablesAssetsModel()
        {
            _initTask = Application.isPlaying 
                ? Initialize() 
                : Task.CompletedTask;
        }

        private async Task Initialize()
        {
            using (TimeDebug.StartMeasure("Addressables initialization"))
            {
                await Addressables.InitializeAsync().Task;
            }
        }

        public async UniTask FetchRemoteAssetsData()
        {
            using (TimeDebug.StartMeasure("Addressables fetch await init task"))
            {
                await _initTask;
            }

            using (TimeDebug.StartMeasure("Addressables refreshing catalogs"))
            {
                await RefreshCatalogs();
            }
        }

        public async Task DownloadAssets(IEnumerable<string> paths, IProgress<(int current, int max)> progress = null)
        {
            await _initTask;
            var pathsArr = paths.ToArray();
            progress?.Report((0, pathsArr.Length));
            Debug.Log("Downloading assets.");
            for (var i = 0; i < pathsArr.Length; i++)
            {
                var path = pathsArr[i];
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError($"{GetType().Name}: GroupKey is null or empty.");
                    return;
                }

                Debug.Log($"Downloading group {path}");

                long bundleSize;
                using (TimeDebug.StartMeasure($"Get download size {path}"))
                {
                    bundleSize = await Addressables.GetDownloadSizeAsync(path).Task;
                }

                if (bundleSize >= 0)
                {
                    Debug.Log($"Download bundle size = {bundleSize} bytes");
                    var op = Addressables.DownloadDependenciesAsync(path);
                    await op.Task;
                    // Download task always success even when downloading failed.
                    // Probably it is addressable(1.21.19) bug.
                    // Anyway, check exception explicitly for now.
                    if (op.OperationException != null)
                        throw op.OperationException;
                    Addressables.Release(op);
                    Debug.Log($"Group {path} downloaded!");
                }
                else
                {
                    Debug.Log($"Group {path} already downloaded. No need to download.");
                }
                progress?.Report((i + 1, pathsArr.Length));
            }
        }

        private async Task RefreshCatalogs()
        {
            Debug.Log("Check catalogs for update");
            var catalogUpdates = await Addressables.CheckForCatalogUpdates().Task;
            
            if (catalogUpdates is not {Count: > 0})
            {
                Debug.Log("No catalogs to update");
                return;
            }
            foreach (var catalogUpdate in catalogUpdates)
            {
                Debug.Log($"Catalog to update: {catalogUpdate}");
            }
            await Addressables.UpdateCatalogs(true).Task
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        Debug.LogWarning($"Failed to update catalogs. Error = {t.Exception.Message}");
                    }

                    if (t.Result is { Count: > 0 })
                    {
                        Debug.Log("Catalogs updated successfully");
                        foreach (var locator in t.Result)
                        {
                            Debug.Log($"Updated locator id: {locator.LocatorId}");
                        }
                    }
                    else
                    {
                        Debug.Log("No updated locators");
                    }
                });
        }

        public async Task<T> LoadAsset<T>(string path, bool cache = true)
        {
            object asset;
            lock (_cache)
            {
                if (_cache.TryGetValue(path, out asset))
                {
                    if (asset is T castAsset)
                    {
                        return castAsset;
                    }

                    throw new InvalidCastException(
                        $"Can not cast loaded asset to specified type. Path = {path}. Expected type = {typeof(T).Name}. Actual type = {asset.GetType().Name}");

                }
            }
            var loadOp = Addressables.LoadAssetAsync<T>(path);
            asset = await loadOp.Task;
            if (loadOp.OperationException != null)
            {
                throw loadOp.OperationException;
            }
            if (asset == null)
            {
                throw new InvalidOperationException($"Asset at path {path} is not loaded.");
            }
            if (cache)
            {
                lock (_cache)
                {
                    if (!_cache.ContainsKey(path))
                        _cache.Add(path, asset);
                }
            }

            return (T)asset;
        }

        public Task CacheAsset<T>(string path)
        {
            return LoadAsset<T>(path);
        }

        public T GetCachedAsset<T>(string path)
        {
            if (_cache.TryGetValue(path, out var asset))
            {
                if (asset is T castAsset)
                {
                    return castAsset;
                }

                throw new InvalidCastException("Can not cast loaded asset to specified type.");
                
            }

            throw new InvalidOperationException($"No cached asset at path {path}");
        }

        public bool TryGetCachedAsset<T>(string path, out T asset)
        {
            if (_cache.TryGetValue(path, out var a))
            {
                if (a is T castAsset)
                {
                    asset = castAsset;
                    return true;
                }
            }

            asset = default;
            return false;
        }

        public void ReleaseAllLoadedAssets()
        {
            var keys = _cache.Keys.ToArray();
            foreach (var key in keys)
            {
                Addressables.Release(_cache[key]);
            }
            _cache.Clear();
        }

        public void ReleaseLoadedAssets(string pathPattern)
        {
            if (_cache.ContainsKey(pathPattern))
            {
                Addressables.Release(_cache[pathPattern]);
                _cache.Remove(pathPattern);
            }

            var keys = _cache.Keys.ToArray();
            foreach (var key in keys)
            {
                if (Regex.IsMatch(key, pathPattern))
                {
                    Addressables.Release(_cache[key]);
                    _cache.Remove(key);
                }
            }
        }
        
        public async Task ClearReleasedAssets()
        {
            await Resources.UnloadUnusedAssets();
        }
    }
}
