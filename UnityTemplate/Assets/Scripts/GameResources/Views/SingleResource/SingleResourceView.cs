using kekchpek.MVVM.Models.GameResources.Static;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityMVVM;

namespace kekchpek.MVVM.Models.GameResources.Views.SingleResource
{
    public class SingleResourceView : ViewBehaviour<ISingleResourceViewModel>
    {

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _text;
        
        [SerializeField]
        private string _resourceId;

        [SerializeField]
        private string _format = "0.##";

        protected override void OnViewModelSet()
        {
            base.OnViewModelSet();
            if (_icon)
                ViewModel.EnableIconLoading();
            if (!string.IsNullOrEmpty(_resourceId))
                ViewModel!.SetResource(_resourceId);
            SmartBind(ViewModel!.Icon, x =>
            {
                if (_icon)
                    _icon.sprite = x;
            });
            SmartBind(ViewModel!.Value, x =>
            {
                if (_text)
                    _text.text = ResourceFormatting.FormatNumber(x);
            });
        }
    }
}