using DigitalRuby.LightningBolt;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("마법사 연쇄 번개")]
        [SerializeField] private GameObject chainLightningPrefab;
        [SerializeField, Min(0.1f)] private float chainLightningJumpRange = 8f;
        [SerializeField, Min(0.05f)] private float chainLightningLifetime = 0.25f;

        private void AttackWithChainLightning(Transform source, EnemyState first, AttackDefinition attack, float damage)
        {
            if (first == null || first.root == null || first.health <= 0) return;
            Vector3 firstPosition = first.root.position;
            EnemyState second = null;
            float nearestDistance = chainLightningJumpRange;
            foreach (EnemyState candidate in enemies)
            {
                if (candidate == first || candidate.root == null || candidate.health <= 0) continue;
                float distance = FlatDistance(firstPosition, candidate.root.position);
                if (distance > nearestDistance) continue;
                nearestDistance = distance;
                second = candidate;
            }

            Vector3 height = Vector3.up * attack.height;
            CreateChainBolt(source.position + height, firstPosition + height);
            ApplyDamage(first, damage, (firstPosition - source.position).normalized, 0, false, source);
            if (second != null)
            {
                CreateChainBolt(firstPosition + height, second.root.position + height);
                ApplyDamage(second, damage, (second.root.position - firstPosition).normalized, 0, false, source);
            }
            ShotsFired++;
        }

        private void CreateChainBolt(Vector3 start, Vector3 end)
        {
            GameObject effect = Instantiate(chainLightningPrefab, Vector3.zero, Quaternion.identity, combatRoot);
            LightningBoltScript bolt = effect.GetComponent<LightningBoltScript>();
            bolt.StartObject = null;
            bolt.EndObject = null;
            bolt.StartPosition = start;
            bolt.EndPosition = end;
            bolt.Force3D = true;
            bolt.ManualMode = false;
            Destroy(effect, chainLightningLifetime);
        }
    }
}
