using AssetsSystem;
using AsyncReactAwait.Bindable;
using GameResources.Domain;
using UnityEngine;
using UnityMVVM.ViewModelCore;

namespace kekchpek.MVVM.Models.GameResources.Views.SingleResource
{
    public class SingleResourceViewModel : ViewModel, ISingleResourceViewModel, ISingleResourceViewHandle
    {
        private readonly IResourcesModel _resourcesModel;
        private readonly IAssetsModel _assetsModel;

        private readonly Mutable<Sprite> _icon = new();
        private readonly Mutable<float> _value = new();

        private ResourceId? _setResourceId;

        public IBindable<Sprite> Icon => _icon;
        public IBindable<float> Value => _value;

        private bool _iconLoadingEnabled = false;

        public SingleResourceViewModel(
            IResourcesModel resourcesModel,
            IAssetsModel assetsModel)
        {
            _resourcesModel = resourcesModel;
            _assetsModel = assetsModel;
        }

        public void EnableIconLoading()
        {
            _iconLoadingEnabled = true;
        }

        public void SetResource(string resourceId)
        {
            SetResource(ResourceId.FromString(resourceId));
        }
        
        public async void SetResource(ResourceId resourceId)
        {
            _setResourceId = resourceId;
            _value.Proxy(_resourcesModel.GetResource(resourceId));
            if (_iconLoadingEnabled)
            {
                var icon = await _assetsModel.LoadAsset<Sprite>(GameResourcesStrings.GetIconPath(resourceId));
                if (_setResourceId == resourceId)
                    _icon.Value = icon;
            }
        }
    }
}