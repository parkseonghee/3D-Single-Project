using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        public readonly struct ManualHit
        {
            public readonly Transform Source, Target;
            public readonly Vector3 Position;
            public readonly float Damage; // Before enemy mitigation; extra hits apply it once through ApplyDamage.
            public readonly bool Killed, WasSuppressed, ChargeImpact;
            public ManualHit(Transform source, Transform target, Vector3 position, float damage,
                bool killed, bool wasSuppressed, bool chargeImpact)
            { Source = source; Target = target; Position = position; Damage = damage;
              Killed = killed; WasSuppressed = wasSuppressed; ChargeImpact = chargeImpact; }
        }
        public event System.Action<ManualHit> OnManualEligibleHit;
        private struct ManualBuff { public Vector2 bonus; public Transform target; public bool single; }
        private readonly Dictionary<object, ManualBuff> manualBuffs = new Dictionary<object, ManualBuff>();
        public void SetManualBuff(object source, float damage, float speed, Transform target = null)
        { manualBuffs[source] = new ManualBuff { bonus = new Vector2(Mathf.Max(0, damage), Mathf.Max(0, speed)), target = target, single = target != null }; }
        public void RemoveManualBuff(object source) => manualBuffs.Remove(source);
        private Vector2 ManualBonus(Transform unit)
        {
            if (unit == null || unit == run.Commander || experience == null || experience.GetProgress(unit) == null)
                return Vector2.zero;
            Vector2 sum = Vector2.zero;
            foreach (var value in manualBuffs.Values)
                if (!value.single || value.target == unit) sum += value.bonus;
            return sum;
        }

        public bool ManualEnemyAlive(Transform target) => FindManualEnemy(target) != null;
        private EnemyState FindManualEnemy(Transform target)
        {
            if (target == null) return null;
            foreach (var enemy in enemies) if (enemy.root == target && enemy.health > 0) return enemy;
            return null;
        }
        public void ManualSuppress(Transform target, float duration)
        {
            var enemy = FindManualEnemy(target);
            if (enemy == null) return;
            float previous = enemy.suppressionRemaining;
            ApplySuppression(enemy);
            enemy.suppressionRemaining = Mathf.Max(previous, duration);
        }
        public List<Transform> ManualEnemiesInRadius(Vector3 center, float radius)
        {
            var result = new List<Transform>();
            foreach (var enemy in enemies)
                if (enemy.root != null && enemy.health > 0 && FlatDistance(center, enemy.root.position) <= radius)
                    result.Add(enemy.root);
            return result;
        }
        public void ManualDamage(Transform target, float damage, Transform source)
        {
            var enemy = FindManualEnemy(target);
            if (enemy != null) ApplyDamage(enemy, damage, Vector3.zero, 0, false, source, true);
        }
        public void ManualAreaDamage(Vector3 center, float radius, float damage, Transform source)
        {
            // Snapshot because damage/death callbacks may mutate combat state.
            foreach (var target in ManualEnemiesInRadius(center, radius)) ManualDamage(target, damage, source);
        }
        private struct ManualCharge { public object source; public float bonus, until; }
        private readonly Dictionary<Transform, ManualCharge> manualCharges = new Dictionary<Transform, ManualCharge>();
        public void SetManualCharge(object source, Transform unit, float bonus, float duration)
        { manualCharges[unit] = new ManualCharge { source = source, bonus = bonus, until = Time.time + duration }; }
        public void RemoveManualCharges(object source)
        {
            var remove = new List<Transform>();
            foreach (var pair in manualCharges) if (pair.Value.source == source) remove.Add(pair.Key);
            foreach (var unit in remove) manualCharges.Remove(unit);
        }
        private float ConsumeManualCharge(Transform unit)
        {
            if (!manualCharges.TryGetValue(unit, out var value)) return 1;
            manualCharges.Remove(unit);
            return Time.time < value.until ? 1 + value.bonus : 1;
        }
    }
}
