using System.Collections;
using ArmySurvivor.Building;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public class SceneTravel : MonoBehaviour
    {
        [SerializeField] private Button readyButton;
        [SerializeField] private BuildingPlacementController village;
        [SerializeField] private GameObject[] villageRoots;
        [SerializeField] private string playScene = "PlayScene";

        private GameObject[] playRoots;
        private bool loading;
        public BuildingPlacementController Village => village;

        private void Start()
        {
            readyButton.onClick.AddListener(Prepare);
        }

        public void Prepare()
        {
            if (!loading) StartCoroutine(EnterPlayScene());
        }

        private IEnumerator EnterPlayScene()
        {
            loading = true;
            village.Cancel();
            // 마을을 남겨 두므로 건물, 자원, 고용 병력이 왕복해도 초기화되지 않는다.
            SetRootsActive(villageRoots, false);
            if (playRoots == null)
            {
                yield return SceneManager.LoadSceneAsync(playScene, LoadSceneMode.Additive);
                playRoots = SceneManager.GetSceneByName(playScene).GetRootGameObjects();
            }
            else
            {
                SetRootsActive(playRoots, true);
            }
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(playScene));
            loading = false;
        }

        public void ReturnToVillage()
        {
            if (loading) return;
            SetRootsActive(playRoots, false);
            SetRootsActive(villageRoots, true);
            SceneManager.SetActiveScene(gameObject.scene);
        }

        private static void SetRootsActive(GameObject[] roots, bool active)
        {
            if (roots == null) return;
            foreach (GameObject root in roots) root.SetActive(active);
        }
    }
}
