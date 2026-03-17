using System.Linq;
using GameResources.Domain;
using kekchpek.MVVM.Models.GameResources.Prices;
using AssetsSystem;
using UnityEngine;
using Zenject;

namespace kekchpek.MVVM.Models.GameResources.Components
{
    public class FixedPriceLabel : MonoBehaviour
    {

        [SerializeField]
        private ResourceComponent _resource;

        [SerializeField]
        private string _format = "0.##";
        
        [SerializeField]
        private Color _affordableColor = Color.white;
        [SerializeField]
        private Color _unaffordableColor = Color.red;

        private IPrice _currentPrice;

        private IAssetsModel _assetsModel;

        [Inject]
        public void Construct(IAssetsModel assetsModel)
        {
            _assetsModel = assetsModel;
        }
            
        public void SetPrice(IPrice price)
        {
            if (_currentPrice != null)
            {
                _currentPrice.Affordable.Unbind(UpdateAffordable);
            }
            var first = price.FirstOrDefault();
            if (first == default)
            {
                Debug.LogError("No price!");
                return;
            }
            _currentPrice = price;
            _resource.SetAmount(first.amount.ToString(_format));
            _resource.SetIcon(_assetsModel.LoadAsset<Sprite>(GameResourcesStrings.GetIconPath(first.resourceId)));
            _currentPrice.Affordable.Bind(UpdateAffordable);
        }

        private void UpdateAffordable(bool isAffordable)
        {
            _resource.SetFontColor(isAffordable ? _affordableColor : _unaffordableColor);
        }
    }
}