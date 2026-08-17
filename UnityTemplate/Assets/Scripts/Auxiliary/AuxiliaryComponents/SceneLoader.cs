using UnityEngine;
using UnityEngine.SceneManagement;

namespace kekchpek.AuxiliaryComponents
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private string _sceneName;
        [SerializeField] private bool _isAdditive;

        private void Start() {
            SceneManager.LoadScene(_sceneName, _isAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single);
        }
    }
}