using ArmySurvivor.Army;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Equipment
{
    // 전투 화면에 지금 장착한 장비(이름·등급·강화 단계·효과)를 보여 준다. UI는 PlayScene Run UI에 배치한다.
    public sealed class EquippedWeaponHud : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image gradeBand;
        [SerializeField] private TMP_Text grade;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text effects;
        [SerializeField] private string gradeFormat = "{0}";
        [SerializeField] private string upgradeSuffix = " +{0}";
        [Tooltip("모험 모드 돌파 표시. {0} = 돌파 단계")]
        [SerializeField] private string breakthroughSuffix = " · {0}돌파";
        [SerializeField] private string emptyName = "장비 없음";
        [SerializeField] private string emptyEffects = "인장을 주워 장비를 얻으세요";
        [SerializeField] private string adventureEmptyEffects = "마을 왕국 보급소에서 장착";
        [Tooltip("효과 문구를 한 줄로 이을 때 쓰는 구분자")]
        [SerializeField] private string effectSeparator = " · ";
        [Header("등급 색 (C, B, A, S)")]
        [SerializeField] private Color[] gradeColors =
        {
            new Color(0.62f, 0.66f, 0.72f), new Color(0.35f, 0.62f, 0.95f),
            new Color(0.72f, 0.42f, 0.95f), new Color(1f, 0.74f, 0.25f)
        };
        [SerializeField] private Color emptyColor = new Color(0.4f, 0.44f, 0.5f);

        private void OnEnable()
        {
            EquipmentInventory.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => EquipmentInventory.Changed -= Refresh;

        private void Refresh()
        {
            var item = EquipmentInventory.EquippedItem;
            bool survival = GameModeManager.Current == GameMode.Survival;
            if (root != null) root.SetActive(true);
            if (item == null)
            {
                itemName.text = emptyName;
                grade.text = "-";
                effects.text = survival ? emptyEffects : adventureEmptyEffects;
                grade.color = emptyColor;
                if (gradeBand != null) gradeBand.color = emptyColor;
                return;
            }
            int level = EquipmentInventory.EquippedUpgrade;
            Color color = (int)item.grade < gradeColors.Length ? gradeColors[(int)item.grade] : Color.white;
            grade.text = string.Format(gradeFormat, item.grade);
            grade.color = color;
            if (gradeBand != null) gradeBand.color = color;
            itemName.text = item.displayName + (level > 0 ? string.Format(survival ? upgradeSuffix : breakthroughSuffix, level) : "");
            effects.text = EquipmentInventory.EffectText(item, EquipmentInventory.EffectScale, survival, effectSeparator);
        }
    }
}
