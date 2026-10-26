#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.UI.Editor
{
    public static class StartSceneMenuLayout
    {
        [MenuItem("Army Survivor/Start Scene/Apply Minimal Menu Layout")]
        public static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(Application.isPlaying || scene.path!="Assets/Scenes/StartScene.unity") throw new System.InvalidOperationException("Open StartScene in Edit Mode.");
            var canvas=GameObject.Find("Canvas").transform;
            var old=canvas.Find("Menu Dark Gradient");
            if(old!=null) Object.DestroyImmediate(old.gameObject);
            var shade=new GameObject("Menu Dark Gradient",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            shade.transform.SetParent(canvas,false);shade.transform.SetAsFirstSibling();
            var image=shade.GetComponent<Image>();
            image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/menu-dark-gradient.png");
            image.raycastTarget=false;
            var rect=image.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(0.52f,1);rect.offsetMin=rect.offsetMax=Vector2.zero;
            string[] names={"NewGameButton","AdventureModeButton","ContinueButton","SettingsButton","QuitButton"};
            for(int i=0;i<names.Length;i++)
            {
                var t=canvas.Find(names[i]);var button=t.GetComponent<Button>();var bg=t.GetComponent<Image>();
                bg.sprite=null;bg.color=Color.clear;bg.raycastTarget=true;button.transition=Selectable.Transition.None;
                var r=(RectTransform)t;r.anchorMin=r.anchorMax=new Vector2(0,0);r.pivot=new Vector2(0,0.5f);r.anchoredPosition=new Vector2(112,350-i*56);r.sizeDelta=new Vector2(340,48);
                var text=t.GetComponentInChildren<Text>();text.fontSize=25;text.alignment=TextAnchor.MiddleLeft;text.color=new Color(0.88f,0.9f,0.92f);text.raycastTarget=false;
                var tr=text.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(24,0);tr.offsetMax=Vector2.zero;
                text.resizeTextForBestFit=false;
                var prior=t.Find("Hover Marker");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
                var mark=new GameObject("Hover Marker",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(CanvasGroup));mark.transform.SetParent(t,false);
                var mr=(RectTransform)mark.transform;mr.anchorMin=mr.anchorMax=new Vector2(0,0.5f);mr.sizeDelta=new Vector2(4,22);mr.anchoredPosition=new Vector2(3,0);
                mark.GetComponent<Image>().color=new Color(1,0.84f,0.5f);mark.GetComponent<Image>().raycastTarget=false;
                var group=mark.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
                var fx=t.GetComponent<MenuButtonFeedback>();if(fx==null)fx=t.gameObject.AddComponent<MenuButtonFeedback>();fx.Configure(text,group);
                var nav=button.navigation;nav.mode=Navigation.Mode.Vertical;button.navigation=nav;
                EditorUtility.SetDirty(button);
            }
            var best=canvas.Find("SurvivalBestTimeText").GetComponent<TMPro.TMP_Text>();
            var br=best.rectTransform;br.anchorMin=br.anchorMax=Vector2.zero;br.pivot=new Vector2(0,0.5f);br.anchoredPosition=new Vector2(136,400);br.sizeDelta=new Vector2(340,25);best.fontSize=17;best.alignment=TMPro.TextAlignmentOptions.Left;best.color=new Color(0.65f,0.7f,0.74f);best.raycastTarget=false;
            var camera=Camera.main;
            camera.transform.position=new Vector3(24,30,-48);
            camera.transform.LookAt(new Vector3(0,3,5));
            camera.transform.position-=camera.transform.right*8f;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
