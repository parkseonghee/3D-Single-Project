using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Equipment
{
    [Serializable] public sealed class OwnedEquipment { public string id; public int count; }
    [Serializable] public sealed class EquipmentSave
    {
        public int tickets, sinceA, sinceS;
        public string equippedId;
        public List<OwnedEquipment> owned = new List<OwnedEquipment>();
    }

    // Shares the game's existing save slots; no separate account/PlayerPrefs persistence.
    public static class EquipmentInventory
    {
        static EquipmentSave state = new EquipmentSave();
        static EquipmentCatalog cachedCatalog;
        public static EquipmentItem EquippedItem
        {
            get
            {
                if (string.IsNullOrEmpty(state.equippedId) || Count(state.equippedId) < 1) return null;
                if (cachedCatalog == null) cachedCatalog = Resources.Load<EquipmentCatalog>("EquipmentCatalog");
                return cachedCatalog != null ? cachedCatalog.Find(state.equippedId) : null;
            }
        }
        public static float SurvivalWeaponDamageMultiplier
        {
            get
            {
                EquipmentItem item = EquippedItem;
                if (item == null || !item.isWeapon) return 1f;
                // 강화하면 등급 보정 중 1을 넘는 부분도 같은 비율로 커진다.
                return 1f + (GradeDamage(item) - 1f) * EffectScale;
            }
        }

        // ── 서바이벌 인장 보급 강화 (그 판에서만 유지, 저장하지 않음) ──
        static int survivalUpgrade;
        public static int EquippedUpgrade => survivalUpgrade;
        // 장착 장비 효과 배율. 강화 1단계마다 catalog.survivalUpgradeBonus만큼 커진다.
        public static float EffectScale => ScaleAt(survivalUpgrade);
        public static float ScaleAt(int upgrade)
        {
            var catalog = Catalog;
            return 1f + Math.Max(0, upgrade) * (catalog != null ? catalog.survivalUpgradeBonus : 0f);
        }
        static EquipmentCatalog Catalog
        {
            get
            {
                if (cachedCatalog == null) cachedCatalog = Resources.Load<EquipmentCatalog>("EquipmentCatalog");
                return cachedCatalog;
            }
        }
        public static float GradeDamage(EquipmentItem item)
        {
            var table = Catalog != null ? Catalog.survivalGradeDamage : null;
            int index = (int)item.grade;
            return table != null && index < table.Length ? table[index] : 1f;
        }

        // 전투 코드가 읽는 장착 효과. 모두 강화 배율을 반영한다.
        public static float SquadDamageBonus => (EquippedItem?.squadDamageBonus ?? 0f) * EffectScale;
        public static float SquadAttackSpeedBonus => (EquippedItem?.squadAttackSpeedBonus ?? 0f) * EffectScale;
        public static float CommanderMoveBonus => (EquippedItem?.commanderMoveBonus ?? 0f) * EffectScale;
        public static float CommanderDamageReduction => Mathf.Min(0.8f, (EquippedItem?.commanderDamageReduction ?? 0f) * EffectScale);
        public static float CommanderHealOnKillRatio => (EquippedItem?.commanderHealOnKillRatio ?? 0f) * EffectScale;
        public static float SlowOnHitChance => Mathf.Min(1f, (EquippedItem?.slowOnHitChance ?? 0f) * EffectScale);
        public static float SlowOnHitDuration => EquippedItem?.slowOnHitDuration ?? 0f;

        // 서바이벌 인장: 새 장비로 교체한다. 강화 단계는 0으로 돌아간다.
        public static void EquipSurvivalItem(EquipmentItem item)
        {
            if (item == null) return;
            var owned = state.owned.Find(x => x.id == item.id);
            if (owned == null) state.owned.Add(new OwnedEquipment { id = item.id, count = 1 });
            state.equippedId = item.id;
            survivalUpgrade = 0;
            Changed?.Invoke();
        }

        // 서바이벌 인장: 장착 중인 장비를 한 단계 강화한다.
        public static bool UpgradeEquipped()
        {
            var catalog = Catalog;
            if (EquippedItem == null || catalog == null || survivalUpgrade >= catalog.survivalMaxUpgrade) return false;
            survivalUpgrade++;
            Changed?.Invoke();
            return true;
        }
        public static bool CanUpgradeEquipped =>
            EquippedItem != null && Catalog != null && survivalUpgrade < Catalog.survivalMaxUpgrade;

        // 서바이벌 인장 카드 후보. 등급 확률은 보급 카탈로그 값을 그대로 쓰고, 같은 장비는 한 번만 제시한다.
        // 장착 장비가 후보로 뽑히면 강화 카드가 되고, 일정 확률로 강화 카드를 따로 보장한다.
        public static List<EquipmentItem> RollSurvivalChoices(EquipmentCatalog catalog)
        {
            var result = new List<EquipmentItem>();
            if (catalog?.items == null) return result;
            var pool = new List<EquipmentItem>(Array.FindAll(catalog.items,
                x => x != null && (catalog.survivalIncludeNonWeapons || x.isWeapon)));
            var equipped = EquippedItem;
            // 최대 강화에 도달한 장비는 다시 제시하지 않는다.
            if (equipped != null && !CanUpgradeEquipped) pool.Remove(equipped);
            int count = Math.Min(catalog.survivalChoiceCount, pool.Count);
            if (equipped != null && pool.Contains(equipped) && UnityEngine.Random.value < catalog.survivalUpgradeOfferChance)
            {
                result.Add(equipped);
                pool.Remove(equipped);
            }
            while (result.Count < count && pool.Count > 0)
            {
                float roll = UnityEngine.Random.value;
                ItemGrade grade = roll < catalog.sRate ? ItemGrade.S :
                    roll < catalog.sRate + catalog.aRate ? ItemGrade.A :
                    roll < catalog.sRate + catalog.aRate + catalog.bRate ? ItemGrade.B : ItemGrade.C;
                // 그 등급이 이미 다 뽑혔으면 가까운 등급(아래 먼저)에서 고른다.
                List<EquipmentItem> candidates = null;
                for (int step = 0; step <= 3 && (candidates == null || candidates.Count == 0); step++)
                    foreach (int g in new[] { (int)grade - step, (int)grade + step })
                    {
                        if (g < 0 || g > 3) continue;
                        candidates = pool.FindAll(x => (int)x.grade == g);
                        if (candidates.Count > 0) break;
                    }
                var pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                result.Add(pick);
                pool.Remove(pick);
            }
            // 강화 카드가 항상 첫 칸에 오지 않도록 섞는다.
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }
            return result;
        }

        // 장비 효과 문구. scale은 강화 배율(ScaleAt), 서바이벌이면 무기 등급 피해 배율도 보여 준다.
        public static string EffectText(EquipmentItem item, float scale, bool survival, string separator = "\n")
        {
            var catalog = Catalog;
            if (item == null || catalog == null) return "";
            var lines = new List<string>();
            if (survival && item.isWeapon)
                lines.Add(string.Format(catalog.survivalGradeDamageFormat, (1f + (GradeDamage(item) - 1f) * scale).ToString("0.##")));
            void Add(float value, string format) { if (value > 0) lines.Add(string.Format(format, Percent(value * scale))); }
            Add(item.squadDamageBonus, catalog.squadDamageFormat);
            Add(item.squadAttackSpeedBonus, catalog.squadAttackSpeedFormat);
            Add(item.commanderMoveBonus, catalog.commanderMoveFormat);
            if (item.commanderDamageReduction > 0)
                lines.Add(string.Format(catalog.commanderDamageReductionFormat, Percent(Mathf.Min(0.8f, item.commanderDamageReduction * scale))));
            if (item.commanderHealOnKillRatio > 0)
                lines.Add(string.Format(catalog.commanderHealOnKillFormat, (item.commanderHealOnKillRatio * scale * 100f).ToString("0.#")));
            if (item.slowOnHitChance > 0)
                lines.Add(string.Format(catalog.slowOnHitFormat, Percent(Mathf.Min(1f, item.slowOnHitChance * scale)), item.slowOnHitDuration.ToString("0.#")));
            return string.Join(separator, lines);
        }
        static string Percent(float value) => Mathf.RoundToInt(value * 100f).ToString();
#if UNITY_EDITOR
        static EquipmentSave testOriginal;
        public static bool IsTesting => testOriginal != null;
        public static bool UnlimitedTestTickets { get; set; }
        public static void BeginTest()
        {
            if (IsTesting) return;
            testOriginal = Copy(state);
            UnlimitedTestTickets = true;
            state = new EquipmentSave { tickets = 1000 };
            Changed?.Invoke();
        }
        public static void EndTest()
        {
            if (!IsTesting) return;
            state = testOriginal; testOriginal = null; UnlimitedTestTickets = false;
            Changed?.Invoke();
        }
        public static void ConfigureTest(int tickets, int sinceA, int sinceS)
        {
            if (!IsTesting) return;
            state.tickets = Math.Max(0, tickets);
            state.sinceA = Math.Max(0, sinceA); state.sinceS = Math.Max(0, sinceS);
            Changed?.Invoke();
        }
#endif
        static EquipmentSave Copy(EquipmentSave value) => JsonUtility.FromJson<EquipmentSave>(JsonUtility.ToJson(value));
        public static event Action Changed;
        public static int Tickets => state.tickets;
        public static int SinceA => state.sinceA;
        public static int SinceS => state.sinceS;
        public static string EquippedId => state.equippedId;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize() {
            Changed = null; state = new EquipmentSave(); cachedCatalog = null; survivalUpgrade = 0;
#if UNITY_EDITOR
            testOriginal = null; UnlimitedTestTickets = false;
#endif
        }
        public static void Reset() { Restore(null); }
        public static int Count(string id) => state.owned.Find(x => x.id == id)?.count ?? 0;
        public static void AddTickets(int count)
        {
            if (count <= 0) return;
            state.tickets = (int)Math.Min(int.MaxValue, (long)state.tickets + count);
            Changed?.Invoke();
        }
        public static bool Equip(string id)
        {
            if (!string.IsNullOrEmpty(id) && Count(id) < 1) return false;
            state.equippedId = id;
            Changed?.Invoke();
            return true;
        }
        public static EquipmentSave Capture()
        {
#if UNITY_EDITOR
            // Never write test rewards/tickets into real save slots.
            if (IsTesting) return Copy(testOriginal);
#endif
            return Copy(state);
        }
        public static void Restore(EquipmentSave saved)
        {
#if UNITY_EDITOR
            testOriginal = null; UnlimitedTestTickets = false;
#endif
            state = saved == null ? new EquipmentSave() : JsonUtility.FromJson<EquipmentSave>(JsonUtility.ToJson(saved));
            survivalUpgrade = 0;
            state.tickets = Math.Max(0, state.tickets);
            state.sinceA = Math.Max(0, state.sinceA);
            state.sinceS = Math.Max(0, state.sinceS);
            state.owned ??= new List<OwnedEquipment>();
            state.owned.RemoveAll(x => x == null || string.IsNullOrEmpty(x.id) || x.count < 1);
            if (Count(state.equippedId) < 1) state.equippedId = null;
            Changed?.Invoke();
        }
        public static bool TryDraw(EquipmentCatalog catalog, int count, out List<EquipmentItem> result)
        {
            result = new List<EquipmentItem>();
#if UNITY_EDITOR
            if (IsTesting && UnlimitedTestTickets && (count == 1 || count == 10)) state.tickets = Math.Max(state.tickets, count);
#endif
            if (catalog == null || !catalog.IsValid || (count != 1 && count != 10) || state.tickets < count) return false;
            state.tickets -= count;
            for (int i = 0; i < count; i++)
            {
                result.Add(DrawOne(catalog, ItemGrade.C));
            }
            Changed?.Invoke();
            return true;
        }

        // 보스 보상(기획서 9.1): 인장 없이 1회 뽑는다. 등급은 minimum 이상으로 올리고,
        // 보장 카운터는 일반 뽑기와 같은 규칙으로 갱신한다.
        public static EquipmentItem GrantGuaranteedDraw(EquipmentCatalog catalog, ItemGrade minimum)
        {
            if (catalog == null || !catalog.IsValid) return null;
            var item = DrawOne(catalog, minimum);
            Changed?.Invoke();
            return item;
        }

        private static EquipmentItem DrawOne(EquipmentCatalog catalog, ItemGrade minimum)
        {
            float roll = UnityEngine.Random.value;
            ItemGrade grade = roll < catalog.sRate ? ItemGrade.S : roll < catalog.sRate + catalog.aRate ? ItemGrade.A
                : roll < catalog.sRate + catalog.aRate + catalog.bRate ? ItemGrade.B : ItemGrade.C;
            if (catalog.HasSRewards && state.sinceS >= catalog.sGuarantee - 1) grade = ItemGrade.S;
            else if (state.sinceA >= catalog.aGuarantee - 1 && grade < ItemGrade.A) grade = ItemGrade.A;
            if (grade < minimum) grade = minimum;
            // 데이터에 해당 등급 장비가 없으면 한 단계씩 내려 존재하는 등급에서 뽑는다.
            while (grade > ItemGrade.C && !catalog.HasGrade(grade)) grade--;
            var pool = Array.FindAll(catalog.items, x => x.grade == grade);
            var item = pool[UnityEngine.Random.Range(0, pool.Length)];
            if (catalog.HasSRewards) state.sinceS = grade == ItemGrade.S ? 0 : state.sinceS + 1;
            state.sinceA = grade >= ItemGrade.A ? 0 : state.sinceA + 1;
            var owned = state.owned.Find(x => x.id == item.id);
            if (owned == null) state.owned.Add(new OwnedEquipment { id = item.id, count = 1 });
            else if (owned.count < int.MaxValue) owned.count++;
            return item;
        }
    }
}
