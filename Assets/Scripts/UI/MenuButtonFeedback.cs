using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArmySurvivor.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Text label;
        [SerializeField] private CanvasGroup marker;
        [SerializeField] private Color normalColor = new Color(0.88f,0.90f,0.92f);
        [SerializeField] private Color highlightColor = new Color(1f,0.84f,0.50f);
        [SerializeField] private float slideDistance = 10f;
        [SerializeField] private float transitionSpeed = 12f;
        private Button button;
        private bool hovered, selected;
        private float blend;
        private Vector2 rest;

        public void Configure(Text text, CanvasGroup indicator) { label=text;marker=indicator; }
        private void Awake() { button=GetComponent<Button>(); rest=label.rectTransform.anchoredPosition; }
        private void OnDisable() { hovered=selected=false;blend=0; if(label!=null){label.color=normalColor;label.rectTransform.anchoredPosition=rest;} if(marker!=null)marker.alpha=0; }
        private void Update()
        {
            bool active=button.IsInteractable()&&(hovered||selected);
            blend=Mathf.MoveTowards(blend,active?1f:0f,Time.unscaledDeltaTime*transitionSpeed);
            label.color=button.IsInteractable()?Color.Lerp(normalColor,highlightColor,blend):new Color(0.45f,0.47f,0.5f);
            label.rectTransform.anchoredPosition=rest+Vector2.right*(slideDistance*blend);
            marker.alpha=blend;
        }
        public void OnPointerEnter(PointerEventData e) { hovered=true; }
        public void OnPointerExit(PointerEventData e) { hovered=false; }
        public void OnSelect(BaseEventData e) { selected=true; }
        public void OnDeselect(BaseEventData e) { selected=false; }
    }
}
