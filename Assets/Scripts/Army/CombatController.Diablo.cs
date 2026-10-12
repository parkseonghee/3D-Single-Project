using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        public void SuspendArmyForDiablo()
        {
            ClearFocusCommand();
            CancelCurrentAttacks();
            foreach (var enemy in enemies)
                if (enemy.attackTarget != run.Commander) enemy.attackTarget = null;
            foreach (var unit in recruitment.Units.Keys)
            {
                run.SetAttacking(unit, false);
                if (allies.TryGetValue(unit, out var health)) health.IsInvulnerable = false;
            }
            // Already emitted attacks are cancelled before their owners disappear.
            for (int i = shots.Count - 1; i >= 0; i--)
                if (shots[i].source != run.Commander)
                { Destroy(shots[i].visual.gameObject); shots.RemoveAt(i); }
        }

        public bool DiabloPointerBlocked(Vector2 position) => PointerOverInteractiveUI(position);

        public void FireDiabloProjectile(AttackDefinition attack, Vector3 direction, float damage, float scale, bool piercing = false)
        {
            if (combatRoot == null || attack == null || attack.projectilePrefab == null) return;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.001f) return;
            direction.Normalize();
            float boundaryDistance = DistanceToBoundary(run.Commander.position, direction, run.Ground.bounds);
            float travelDistance = piercing ? boundaryDistance : Mathf.Min(attack.range, boundaryDistance);
            if (travelDistance <= 0) return;
            var visual = Instantiate(attack.projectilePrefab, run.Commander.position + Vector3.up * attack.height,
                Quaternion.LookRotation(direction), combatRoot).transform;
            visual.localScale *= scale;
            shots.Add(new Shot { source = run.Commander, visual = visual, direction = direction,
                definition = attack, damage = damage, piercing = piercing, remaining = travelDistance });
            ShotsFired++;
        }

        public bool CastDiabloMeteor(float radius, float damageMultiplier)
        {
            if (!CanPlaceRewardMines || combatRoot == null) return false;
            // Reservoir sampling avoids allocating a candidate list each second.
            EnemyState selected = null;
            int count = 0;
            foreach (var enemy in enemies)
                if (enemy.root != null && enemy.health > 0 && FlatDistance(enemy.root.position, run.Commander.position) <= radius)
                    if (Random.Range(0, ++count) == 0) selected = enemy;
            if (selected == null) return false;
            var trap = new MagicTrapState { owner = run.Commander,
                position = new Vector3(selected.root.position.x, run.Commander.position.y + 0.05f, selected.root.position.z),
                damageMultiplier = damageMultiplier };
            BeginMeteorFall(trap);
            magicTraps.Add(trap);
            return true;
        }
    }
}
