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
            Changed = null; state = new EquipmentSave();
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
                float roll = UnityEngine.Random.value;
                ItemGrade grade = roll < catalog.sRate ? ItemGrade.S : roll < catalog.sRate + catalog.aRate ? ItemGrade.A
                    : roll < catalog.sRate + catalog.aRate + catalog.bRate ? ItemGrade.B : ItemGrade.C;
                if (catalog.HasSRewards && state.sinceS >= catalog.sGuarantee - 1) grade = ItemGrade.S;
                else if (state.sinceA >= catalog.aGuarantee - 1 && grade < ItemGrade.A) grade = ItemGrade.A;
                var pool = Array.FindAll(catalog.items, x => x.grade == grade);
                var item = pool[UnityEngine.Random.Range(0, pool.Length)];
                if (catalog.HasSRewards) state.sinceS = grade == ItemGrade.S ? 0 : state.sinceS + 1;
                state.sinceA = grade >= ItemGrade.A ? 0 : state.sinceA + 1;
                var owned = state.owned.Find(x => x.id == item.id);
                if (owned == null) state.owned.Add(new OwnedEquipment { id = item.id, count = 1 });
                else if (owned.count < int.MaxValue) owned.count++;
                result.Add(item);
            }
            Changed?.Invoke();
            return true;
        }
    }
}
