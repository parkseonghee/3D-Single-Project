using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        private float rewardWarCryUntil;
        private float rewardWarCryBonus;
        public void ApplyRewardHealing()
        {
            foreach (var ally in allies.Values)
                if (ally != null && !ally.IsDead) ally.Heal(ally.Maximum);
        }
        public void ApplyRewardWarCry(float duration, float bonus)
        { rewardWarCryUntil = Time.time + duration; rewardWarCryBonus = bonus; }

        public bool CanPlaceRewardMines => magicTrapPrefab != null && magicTrapSmokePrefab != null &&
            trapFireballPrefab != null && trapPortalPrefab != null;
        public void ApplyRewardMines()
        {
            if (!CanPlaceRewardMines || combatRoot == null) return;
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI * 2 / 10;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 4f;
                PlaceMagicTrapAt(run.Commander, run.Commander.position + offset);
            }
        }
        public bool SpawnMiniBoss(EnemyDefinition definition)
        {
            if (!run.IsRunning || combatRoot == null || definition == null || definition.prefab == null) return false;
            Spawn(definition, false, true);
            return true;
        }

        public void ApplyRewardExplosion(float damage)
        {
            // Use the normal damage path so deaths, drops and animations stay consistent.
            var camera = Camera.main;
            foreach (var target in enemies)
            {
                if (target.health <= 0 || camera == null) continue;
                Vector3 point = camera.WorldToViewportPoint(target.root.position);
                if (point.z > 0 && point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1)
                    ApplyDamage(target, damage, Vector3.up, 0);
            }
        }

        public void ApplyRewardInvulnerability(float seconds)
        {
            foreach (var ally in allies.Values)
                if (ally != null && !ally.IsDead) ally.GrantTemporaryInvulnerability(seconds);
        }
    }
}
