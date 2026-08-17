using UnityEngine;

namespace kekchpek.Auxiliary.Components
{
    public class ClickObjectSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _clickObjectPrefab;
        [SerializeField] private Transform _clickObjectParent;


        private void Update() {
            if (Input.GetMouseButtonDown(0)) {
                var trs = Instantiate(_clickObjectPrefab, _clickObjectParent).transform;
                trs.localPosition = Vector3.zero;
            }
        }
    }
}