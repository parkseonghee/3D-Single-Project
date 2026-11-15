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
                return item.grade == ItemGrade.S ? 1.6f : item.grade == ItemGrade.A ? 1.35f :
                    item.grade == ItemGrade.B ? 1.2f : 1.1f;
            }
        }
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
            Changed = null; state = new EquipmentSave(); cachedCatalog = null;
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
            state.tickets = Math.Max(0, state.tickets);
            state.sinceA = Math.Max(0, state.sinceA);
            state.sinceS = Math.Max(0, state.sinceS);
            state.owned ??= new List<OwnedEquipment>();
            state.owned.RemoveAll(x => x == null || string.IsNullOrEmpty(x.id) || x.count < 1);
            if (Count(state.equippedId) < 1) state.equippedId = null;
            Changed?.Invoke();
        }
        public static EquipmentItem GrantRandomSurvivalWeapon(EquipmentCatalog catalog)
        {
            if (catalog?.items == null) return null;
            var weapons = Array.FindAll(catalog.items, x => x != null && x.isWeapon);
            if (weapons.Length == 0) return null;
            // The existing supply catalog also defines rarity odds. A seal draws only
            // weapons, but keeps those odds instead of making S-grade commonplace.
            float roll = UnityEngine.Random.value;
            ItemGrade grade = roll < catalog.sRate ? ItemGrade.S :
                roll < catalog.sRate + catalog.aRate ? ItemGrade.A :
                roll < catalog.sRate + catalog.aRate + catalog.bRate ? ItemGrade.B : ItemGrade.C;
            var pool = Array.FindAll(weapons, x => x.grade == grade);
            if (pool.Length == 0) pool = weapons;
            var item = pool[UnityEngine.Random.Range(0, pool.Length)];
            var owned = state.owned.Find(x => x.id == item.id);
            if (owned == null) state.owned.Add(new OwnedEquipment { id = item.id, count = 1 });
            else if (owned.count < int.MaxValue) owned.count++;
            state.equippedId = item.id;
            Changed?.Invoke();
            return item;
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
