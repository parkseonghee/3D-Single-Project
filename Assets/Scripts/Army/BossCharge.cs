using UnityEngine;

namespace ArmySurvivor.Army
{
    // Driven by CombatController so pauses, card selections and death stop the attack.
    public sealed class BossCharge : MonoBehaviour
    {
        [Min(0.1f)] public float roarInterval = 5f;
        [Min(0.1f)] public float roarDuration = 2.966667f;
        [Min(0.1f)] public float chargeSpeed = 18f;
        [Min(0.1f)] public float maxChargeDuration = 1.5f;
        [Min(0.1f)] public float hitRadius = 1.4f;
        [Min(0)] public float damageMultiplier = 2f;
        public string walkState = "Base Layer.Walk";
        public string roarState = "Base Layer.roar";
        public string runState = "Base Layer.Run";
        private Animator animator;
        private float untilRoar, phaseTime, chargeDistance;
        private Vector3 direction;
        private int phase; // walking, roaring, charging
        private bool hit;

        public void ResetAttack()
        {
            animator = GetComponentInChildren<Animator>();
            phase = 0; untilRoar = roarInterval; phaseTime = 0; hit = false;
            Play(walkState);
        }

        public void Tick(float dt, Transform commander, UnitHealth health, EnemyDefinition definition,
            Bounds ground, float margin, float receivedDamageMultiplier, float slowMultiplier)
        {
            if (commander == null || health == null || health.IsDead) return;
            untilRoar -= dt;
            Vector3 offset = commander.position - transform.position; offset.y = 0;
            if (phase == 0)
            {
                if (untilRoar <= 0)
                {
                    phase = 1; phaseTime = 0; untilRoar = roarInterval;
                    Play(roarState);
                }
                else
                {
                    float step = Mathf.Min(definition.speed * slowMultiplier * dt,
                        Mathf.Max(0, offset.magnitude - definition.stoppingDistance));
                    transform.position = Clamp(transform.position + offset.normalized * step, ground, margin);
                }
                Face(offset, definition.turnSpeed * dt);
                return;
            }
            phaseTime += dt;
            if (phase == 1)
            {
                Face(offset, definition.turnSpeed * dt);
                if (phaseTime < roarDuration) return;
                direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : transform.forward;
                chargeDistance = offset.magnitude + hitRadius * 2;
                phase = 2; phaseTime = 0; hit = false;
                transform.rotation = Quaternion.LookRotation(direction);
                Play(runState);
                return;
            }
            Vector3 start = transform.position;
            float distance = Mathf.Min(chargeSpeed * dt, chargeDistance);
            Vector3 end = Clamp(start + direction * distance, ground, margin);
            // Swept hit detection prevents fast charges from tunnelling through the player.
            Vector3 segment = end - start;
            Vector3 toPlayer = commander.position - start; toPlayer.y = 0;
            float t = segment.sqrMagnitude > 0 ? Mathf.Clamp01(Vector3.Dot(toPlayer, segment) / segment.sqrMagnitude) : 0;
            if (!hit && (toPlayer - segment * t).sqrMagnitude <= hitRadius * hitRadius)
            {
                hit = true;
                health.TakeDamage(definition.damage * damageMultiplier * receivedDamageMultiplier);
            }
            transform.position = end;
            chargeDistance -= distance;
            if (chargeDistance <= 0.001f || phaseTime >= maxChargeDuration || segment.sqrMagnitude < 0.000001f)
            {
                phase = 0; Play(walkState);
            }
        }

        private void Play(string state) { if (animator != null) animator.Play(state, 0, 0); }
        private void Face(Vector3 offset, float degrees)
        {
            if (offset.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), degrees);
        }
        private static Vector3 Clamp(Vector3 point, Bounds bounds, float margin)
        {
            point.x = Mathf.Clamp(point.x, bounds.min.x + margin, bounds.max.x - margin);
            point.z = Mathf.Clamp(point.z, bounds.min.z + margin, bounds.max.z - margin);
            return point;
        }
    }
}
