using System.Threading.Tasks;
using UnityEngine;

namespace AssetsSystem
{
    public static class IAssetsModelExtensions
    {
        /// <summary>
        /// Tries to load asset of specified type.
        /// </summary>
        /// <typeparam name="T">The type of the asset.</typeparam>
        /// <param name="assets">The assets model.</param>
        /// <param name="path">The path to the asset.</param>
        /// <returns>Returns tuple with success flag and loaded asset.</returns>
        public static async Task<(bool success, T asset)> TryLoadAsset<T>(this IAssetsModel assets, string path)
        {
            var loadOp = assets.LoadAsset<T>(path);
            await loadOp.ContinueWith(_ => { });
            if (loadOp.Exception != null)
            {
                Debug.LogException(loadOp.Exception);
                return (false, default);
            }

            if (loadOp.Result == null)
            {
                Debug.LogError($"Asset at path {path} is null.");
                return (false, default);
            }

            return (true, loadOp.Result);
        }

        /// <summary>
        /// Tries to cache asset of specified type.
        /// </summary>
        /// <typeparam name="T">The type of the asset.</typeparam>
        /// <param name="assets">The assets model.</param>
        /// <param name="path">The path to the asset.</param>
        /// <returns>Returns true if asset is cached, false otherwise.</returns>
        public static async Task<bool> TryCacheAsset<T>(this IAssetsModel assets, string path)
        {
            var loadOp = assets.CacheAsset<T>(path);
            await loadOp.ContinueWith(_ => { });
            if (loadOp.Exception != null)
            {
                Debug.LogException(loadOp.Exception);
                return false;
            }

            return true;
        }
    }
}
