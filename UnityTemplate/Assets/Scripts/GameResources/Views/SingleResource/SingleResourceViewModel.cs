using AssetsSystem;
using AsyncReactAwait.Bindable;
using AsyncReactAwait.Bindable.BindableExtensions;
using GameResources.Domain;
using kekchpek.MVVM.Models.GameResources.Static;
using UnityEngine;
using UnityMVVM.ViewModelCore;

namespace kekchpek.MVVM.Models.GameResources.Views.SingleResource
{
    public class SingleResourceViewModel : ViewModel, ISingleResourceViewModel, ISingleResourceViewHandle
    {
        private readonly IResourcesModel _resourcesModel;
        private readonly ILargeResourcesModel _largeResourcesModel;
        private readonly IAssetsModel _assetsModel;

        private readonly Mutable<Sprite> _icon = new();
        private readonly Mutable<string> _value = new();
        private readonly Mutable<float> _rawValue = new();

        private ResourceId? _setResourceId;
        private bool _hasRawValue;

        public IBindable<Sprite> Icon => _icon;
        public IBindable<string> Value => _value;
        public IBindable<float> RawValue => _rawValue;
        public bool HasRawValue => _hasRawValue;

        private bool _iconLoadingEnabled = false;

        public SingleResourceViewModel(
            ILargeResourcesModel largeResourcesModel,
            IResourcesModel resourcesModel,
            IAssetsModel assetsModel)
        {
            _largeResourcesModel = largeResourcesModel;
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
            if (_resourcesModel.HasResource(resourceId))
            {
                var resource = _resourcesModel.GetResource(resourceId);
                _value.Proxy(resource.ConvertTo(ResourceFormatting.FormatNumber));
                _rawValue.Proxy(resource);
                _hasRawValue = true;
            }
            else if (_largeResourcesModel.HasResource(resourceId))
            {
                _value.Proxy(_largeResourcesModel.GetResource(resourceId).ConvertTo(ResourceFormatting.FormatNumber));
                _hasRawValue = false;
            }
            if (_iconLoadingEnabled)
            {
                var icon = await _assetsModel.LoadAsset<Sprite>(GameResourcesStrings.GetIconPath(resourceId));
                if (_setResourceId == resourceId)
                    _icon.Value = icon;
            }
        }
    }
}
