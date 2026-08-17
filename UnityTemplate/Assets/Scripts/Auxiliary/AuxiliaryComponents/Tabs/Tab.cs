using System;
using kekchpek.AuxiliaryComponents.SimpleButton;
using UnityEngine;
using UnityEngine.UI;

namespace AuxiliaryComponents.Tabs
{
    public class Tab : MonoBehaviour
    {

        public event Action<Tab> OnClick;
        
        [SerializeField]
        private SimpleButton _button;

        [SerializeField]
        private GameObject _content;
        
        [SerializeField]
        private GameObject _activeLayout;

        [SerializeField]
        private GameObject _inactiveLayout;

        private void Awake() {
            _button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            OnClick?.Invoke(this);
        }

        public void SetActive(bool isActive)
        {
            _content.SetActive(isActive);
            _activeLayout?.SetActive(isActive);
            _inactiveLayout?.SetActive(!isActive);
            _button.Interactable = !isActive;
        }



    }
}