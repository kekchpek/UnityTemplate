using System;
using AsyncReactAwait.Bindable;
using UnityEngine;

namespace kekchpek.Auxiliary.Application
{
    public class ApplicationService : MonoBehaviour, IApplicationService
    {

        private readonly Mutable<(int width, int height)> _screenSize = new Mutable<(int width, int height)>((Screen.width, Screen.height));

        public IBindable<(int width, int height)> ScreenSize => _screenSize;
        
        public event Action ApplicationQuit;

        private void Update()
        {
            _screenSize.Value = (Screen.width, Screen.height);
        }

        private void OnApplicationQuit()
        {
            ApplicationQuit?.Invoke();
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}