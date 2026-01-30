using AssetsSystem;
using UnityEngine;
using UnityMVVM.ViewModelCore.PrefabsProvider;

namespace DI
{
    public class AssetsViewsPrefabsProvider : IViewsPrefabsProvider
    {
        private readonly IAssetsModel _assetsModel;


        public AssetsViewsPrefabsProvider(IAssetsModel assetsModel)
        {
            _assetsModel = assetsModel;
        }
        
        public GameObject GetViewPrefab(string viewName)
        {
            if (_assetsModel.TryGetCachedAsset<GameObject>(viewName, out var prefab))
            {
                return prefab;
            }
            Debug.LogError($"No asset cached in memory for {viewName}");
            return null!;
        }
        
    }
}