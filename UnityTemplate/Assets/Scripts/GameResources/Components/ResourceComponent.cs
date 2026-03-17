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

        private Task<Sprite> _iconTask;

        public void SetAmount(string amount)
        {
            if (_amountText)
                _amountText.text = amount;
        }

        public async void SetIcon(Task<Sprite> t)
        {
            if (!_icon)
                return;
            if (t == _iconTask)
                return;
            _iconTask = t;
            await t;
            if (t == _iconTask)
                _icon.sprite = t.Result;

        }

        public void SetFontColor(Color color)
        {
            if (_amountText) 
                _amountText.color = color;
        }

        
    }
}