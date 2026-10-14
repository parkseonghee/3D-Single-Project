using TMPro;
using UnityEngine;

namespace ArmySurvivor.Army
{
    internal static class UiFontLocator
    {
        public static TMP_FontAsset ExistingKoreanFont()
        {
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (text.font != null && text.font != fallback) return text.font;
            return fallback;
        }
    }
}
