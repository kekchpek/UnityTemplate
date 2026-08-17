using System.Collections.Generic;
using UnityEngine;

namespace AuxiliaryComponents.Tabs
{
    public class TabsView : MonoBehaviour
    {

        [SerializeField]
        private List<Tab> _tabs;

        private void Awake()
        {
            foreach (var tab in _tabs)
            {
                tab.OnClick += HandleTabClick;
            }
            _tabs[0].SetActive(true);
            for (int i = 1; i < _tabs.Count; i++)
            {
                _tabs[i].SetActive(false);
            }
        }

        private void HandleTabClick(Tab tab)
        {
            foreach (var t in _tabs)
            {
                t.SetActive(t == tab);
            }
        }

    }
}