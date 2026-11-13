using TMPro;
using UnityEngine;

namespace ArmySurvivor.Tutorial
{
    [CreateAssetMenu(menuName = "Army Survivor/Tutorial/Style")]
    public sealed class TutorialStyle : ScriptableObject
    {
        [Header("강조")]
        public Shader spotlightShader;
        public Color dimColor = new Color(0, 0, 0, 0.72f);
        public Vector2 referenceResolution = new Vector2(1920, 1080);
        public int sortingOrder = 1000;
        public Color outlineColor = new Color(1f, 0.82f, 0.36f, 1f);
        [Min(0.5f)] public float outlineThickness = 4f;
        [Min(0)] public float blinkSpeed = 2f;
        [Min(0)] public float edgeFeather = 1.5f;
        [Header("설명")]
        public Vector2 bubbleSize = new Vector2(520, 230);
        public Vector2 topBannerSize = new Vector2(700, 68);
        [Min(12)] public float topBannerTitleSize = 22;
        [Min(12)] public float topBannerBodySize = 17;
        [Min(12)] public float titleSize = 32;
        [Min(12)] public float bodySize = 24;
        [Min(12)] public float progressSize = 19;
        [Min(0)] public float bubbleGap = 30;
        [Min(0)] public float screenMargin = 20;
        [Min(0)] public float appearSeconds = 0.22f;
        public TMP_FontAsset font;
        public Color bubbleColor = new Color(0.08f, 0.13f, 0.2f, 0.97f);
        public Color textColor = new Color(1f, 0.94f, 0.78f);
        [Header("UI 배치 (xMin, yMin, xMax, yMax)")]
        public Vector4 titleOffsets = new Vector4(20, -64, -20, -18);
        public Vector4 bodyOffsets = new Vector4(20, -165, -20, -75);
        public Vector4 progressOffsets = new Vector4(20, 8, 100, 42);
        public Vector2 arrowSize = new Vector2(36, 48);
        public float arrowFontSize = 42;
        public string arrowGlyph = "▼";
        [Header("화살표")]
        public bool showArrow = true;
        [Min(0)] public float arrowBob = 8;
        [Min(0)] public float arrowSpeed = 3;
    }
}
