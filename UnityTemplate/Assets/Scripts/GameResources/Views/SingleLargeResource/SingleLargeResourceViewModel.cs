using BigInteger = System.Numerics.BigInteger;
using AssetsSystem;
using AsyncReactAwait.Bindable;
using GameResources.Domain;
using UnityEngine;
using UnityMVVM.ViewModelCore;

namespace kekchpek.MVVM.Models.GameResources.Views.SingleLargeResource
{
    public class SingleLargeResourceViewModel : ViewModel, ISingleLargeResourceViewModel, ISingleLargeResourceViewHandle
    {
        private readonly ILargeResourcesModel _resourcesModel;
        private readonly IAssetsModel _assetsModel;

        private readonly Mutable<Sprite> _icon = new();
        private readonly Mutable<BigInteger> _value = new();

        private ResourceId? _setResourceId;

        public IBindable<Sprite> Icon => _icon;
        public IBindable<BigInteger> Value => _value;

        private bool _iconLoadingEnabled;

        public SingleLargeResourceViewModel(
            ILargeResourcesModel resourcesModel,
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
