using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Tutorial
{
    // Put this on the existing options panel. The control is created only once at runtime.
    public sealed class TutorialOptions : MonoBehaviour
    {
        [SerializeField] private string resetLabel = "튜토리얼 초기화";
        [SerializeField] private Vector2 position = new Vector2(0, -355);
        [SerializeField] private Vector2 size = new Vector2(290, 55);
        [SerializeField] private TMP_FontAsset font;
        [SerializeField, Min(12)] private float fontSize = 24;
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.13f, 0.2f, 0.97f);
        [SerializeField] private Color textColor = new Color(1f, 0.94f, 0.78f);
        private void Awake()
        {
            var rect = new GameObject("Tutorial Reset", typeof(RectTransform), typeof(Image), typeof(Button))
                .GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.GetComponent<Image>().color = backgroundColor;
            rect.GetComponent<Button>().onClick.AddListener(ResetProgress);
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(rect, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            label.text = resetLabel;
            label.font = font;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = textColor;
            label.raycastTarget = false;
        }

        public void ResetProgress() => TutorialProgressStore.Clear();
    }
}
