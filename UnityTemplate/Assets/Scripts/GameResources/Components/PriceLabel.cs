using System.Collections.Generic;
using kekchpek.MVVM.Models.GameResources.Prices;
using AssetsSystem;
using GameResources.Domain;
using UnityEngine;
using Zenject;

namespace kekchpek.MVVM.Models.GameResources.Components
{
    public class PriceLabel : MonoBehaviour
    {

        [SerializeField]
        private Color _availableText;

        [SerializeField]
        private Color _unavailableText;

        private IPrice _price;

        [SerializeField]
        private ResourceComponent _resourceLabelPrefab;

        private readonly List<ResourceComponent> _resourcesLabels = UnityEngine.Pool.ListPool<ResourceComponent>.Get();

        private Transform _transform;

        private IAssetsModel _assetsModel;

        [Inject]
        public void Construct(IAssetsModel assetsModel)
        {
            _assetsModel = assetsModel;
        }

        private void Awake()
        {
            InitializeTransform();
        }

        public void SetupPrice(IPrice price)
        {
            InitializeTransform();
            ReleasePrice();
            if (price == null)
                return;

            _price = price;
            var i = 0;
            foreach ((ResourceId resourceId, string amount) in _price.GetTextRepresentation())
            {
                ResourceComponent label;
                if (i < _resourcesLabels.Count)
                {
                    label = _resourcesLabels[i];
                }
                else
                {
                    label = Instantiate(_resourceLabelPrefab, _transform);
                    _resourcesLabels.Add(label);
                }
                label.gameObject.SetActive(true);
                label.SetAmount(amount);
                label.SetIcon(_assetsModel.GetCachedAsset<Sprite>(GameResourcesStrings.GetIconPath(resourceId)));
                i++;
            }

            for (; i < _resourcesLabels.Count; i++)
            {
                _resourcesLabels[i].gameObject.SetActive(false);
            }
            _price.Affordable.Bind(UpdateAffordability);
        }

        private void InitializeTransform() 
        {
            if (_transform)
                return;
            _transform = transform;
        }

        private void ReleasePrice()
        {
            if (_price == null)
                return;
            _price.Affordable.Unbind(UpdateAffordability);
            _price = null;
        }

        private void UpdateAffordability(bool isAffordable)
        {
            var color = isAffordable ? _availableText : _unavailableText;
            foreach (var res in _resourcesLabels)
            {
                res.SetFontColor(color);
            }
        }

        private void OnDestroy()
        {
            UnityEngine.Pool.ListPool<ResourceComponent>.Release(_resourcesLabels);
            ReleasePrice();
        }
    }
}
