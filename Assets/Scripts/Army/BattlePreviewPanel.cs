using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace ArmySurvivor.Army
{
    // 편성 화면의 출전 미리보기(기획서 7.1). UI는 PlayScene에 배치하고 여기서는 내용만 채운다.
    public class BattlePreviewPanel : MonoBehaviour
    {
        [Header("씬 배치 참조")]
        [SerializeField] private DayController day;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text enemyList;
        [SerializeField] private TMP_Text bossLine;
        [SerializeField] private TMP_Text modifierName;
        [SerializeField] private TMP_Text modifierDescription;

        [Header("표시 문구")]
        [SerializeField] private string titleFormat = "Day {0} 출전 정보";
        [SerializeField] private string bossFormat = "보스 · {0} ({1}초 버틴 뒤 등장)";
        [SerializeField] private string modifierFormat = "전장 변수 · {0}";
        [SerializeField] private string noModifierName = "전장 변수 없음";
        [SerializeField] private string noModifierDescription = "기본 전장입니다.";
        [SerializeField] private Color boostedColor = new Color(1f, .62f, .35f);

        private readonly List<DaySettings.EnemyShare> shares = new List<DaySettings.EnemyShare>();
        private readonly StringBuilder builder = new StringBuilder();
        private bool subscribed;

        private void OnEnable()
        {
            // 부모 활성화 중 SetActive를 바꾸면 오류가 나므로 서바이벌에서는 CanvasGroup으로 숨긴다.
            bool adventure = GameModeManager.Current == GameMode.Adventure;
            if (group != null) { group.alpha = adventure ? 1 : 0; group.blocksRaycasts = adventure; }
            if (!adventure) return;
            if (day != null && !subscribed) { day.DayChanged += Refresh; subscribed = true; }
            Refresh();
        }

        private void OnDisable()
        {
            if (day != null && subscribed) { day.DayChanged -= Refresh; subscribed = false; }
        }

        public void Refresh()
        {
            if (day == null || day.Settings == null || title == null || enemyList == null) return;
            DaySettings settings = day.Settings;
            int current = day.Day;
            BattlefieldModifierDefinition modifier = settings.GetBattlefieldModifier(current);
            title.text = string.Format(titleFormat, current);

            settings.GetEnemyShares(current, modifier, shares);
            builder.Clear();
            string boosted = ColorUtility.ToHtmlStringRGB(boostedColor);
            foreach (DaySettings.EnemyShare item in shares)
            {
                string name = EnemyName(item.enemy);
                int percent = Mathf.RoundToInt(item.share * 100f);
                if (builder.Length > 0) builder.Append('\n');
                if (item.boosted) builder.Append("<color=#").Append(boosted).Append('>');
                builder.Append(name).Append("  ").Append(percent).Append('%');
                if (item.boosted) builder.Append(" ▲</color>");
            }
            enemyList.text = builder.ToString();

            if (bossLine != null)
            {
                DaySettings.Stage stage = settings.GetStage(current);
                bool boss = day.IsBossDay && stage.boss != null;
                bossLine.text = boss ? string.Format(bossFormat, EnemyName(stage.boss), Mathf.RoundToInt(stage.survivalSeconds)) : "";
            }

            if (modifierName != null)
                modifierName.text = modifier != null ? string.Format(modifierFormat, modifier.displayName) : noModifierName;
            if (modifierDescription != null)
                modifierDescription.text = modifier != null ? modifier.description : noModifierDescription;
        }

        private static string EnemyName(EnemyDefinition enemy) =>
            enemy == null ? "-" : string.IsNullOrEmpty(enemy.displayName) ? enemy.name : enemy.displayName;
    }
}
