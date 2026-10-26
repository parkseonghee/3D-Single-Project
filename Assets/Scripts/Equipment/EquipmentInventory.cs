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
        public static event Action Changed;
        public static int Tickets => state.tickets;
        public static int SinceA => state.sinceA;
        public static int SinceS => state.sinceS;
        public static string EquippedId => state.equippedId;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize() { Changed = null; state = new EquipmentSave(); }
        public static void Reset() { state = new EquipmentSave(); Changed?.Invoke(); }
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
        public static EquipmentSave Capture() => JsonUtility.FromJson<EquipmentSave>(JsonUtility.ToJson(state));
        public static void Restore(EquipmentSave saved)
        {
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
