using UnityEngine;
using UnityEngine.UI;

namespace kekchpek.AuxiliaryComponents
{
    public class RenderTargetSceenScaler : MonoBehaviour
    {

        [SerializeField] private Camera _camera;
        [SerializeField] private RenderTexture _renderTexture;

        private int _width;
        private int _height;

        private void Update() 
        {
            if (_width != Screen.width || _height != Screen.height)
            {
                _width = Screen.width;
                _height = Screen.height;
                _renderTexture.Release();
                _renderTexture.width = _width;
                _renderTexture.height = _height;
                _renderTexture.Create();
                _camera.aspect = (float)_width / _height;
                _camera.targetTexture = _renderTexture;
            }
        }

    }
}