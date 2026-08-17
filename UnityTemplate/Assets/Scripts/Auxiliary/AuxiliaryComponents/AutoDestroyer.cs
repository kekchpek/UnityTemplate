using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AuxiliaryComponents
{
    public class AutoDestroyer : MonoBehaviour
    {
        [SerializeField] private float _delay;

        [SerializeField] private bool _ignoreTimeScale = false;

        private async void Start()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_delay), ignoreTimeScale: _ignoreTimeScale);
            if (this)
                Destroy(gameObject);
        }
    }
}