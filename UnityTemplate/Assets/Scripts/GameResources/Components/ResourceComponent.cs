using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Image = UnityEngine.UI.Image;

namespace kekchpek.MVVM.Models.GameResources.Components
{
    public class ResourceComponent : MonoBehaviour
    {

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _amountText;

        public void SetAmount(string amount)
        {
            if (_amountText)
                _amountText.text = amount;
        }

        public void SetIcon(Sprite icon)
        {
            if (!_icon)
                return;
            _icon.sprite = icon;
        }

        public void SetFontColor(Color color)
        {
            if (_amountText) 
                _amountText.color = color;
        }

        
    }
}