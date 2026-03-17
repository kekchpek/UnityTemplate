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

        [SerializeField]
        private string _format = "0.##";

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
            _transform = transform;
        }

        public void SetupPrice(IPrice price)
        {
            ReleasePrice();
            if (price != null)
            {
                _price = price;
                var i = 0;
                foreach (var (id, amount) in _price)
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
                    label.SetAmount(amount.ToString(_format));
                    label.SetIcon(_assetsModel.LoadAsset<Sprite>(GameResourcesStrings.GetIconPath(id)));
                    i++;
                }

                for (; i < _resourcesLabels.Count; i++)
                {
                    _resourcesLabels[i].gameObject.SetActive(false);
                }
                _price.Affordable.Bind(UpdateAffordability);
            }
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