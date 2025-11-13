using UnityEngine;
using UnityEngine.UI;

namespace AuxiliaryComponents
{
    [RequireComponent(typeof(Image))]
    [ExecuteInEditMode]
    public class NativeSizeFitter : MonoBehaviour
    {

        private Image _image;
        private Sprite _prevSprite;

        private void OnEnable()
        {
            Start();
        }

        private void Start()
        {
            _image = GetComponent<Image>();
            Update();
        }

        private void Update()
        {
            if (System.Object.ReferenceEquals(_image.sprite, _prevSprite) == false)
            {
                _image.SetNativeSize();
            }

            _prevSprite = _image.sprite;
        }
    }
}
