using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    // 소규모 전투의 실행 상태를 관리하며 공유 정의 에셋은 변경하지 않는다.
    public class CombatController : MonoBehaviour
    {
        [SerializeField] private RunController run;
        [SerializeField] private RecruitmentController recruitment;
        [SerializeField] private EnemyDefinition enemy;
        [SerializeField, Min(0.1f)] private float spawnInterval;
        [SerializeField, Min(0)] private float spawnDistance;
        [SerializeField, Min(1)] private int maximumEnemies;
        [SerializeField, Min(0)] private float boundaryMargin;

        private class EnemyState
        {
            public Transform root;
            public Animator animator;
            public float health;
            public float deathTime;
            public EnemyDefinition definition;
            public bool isBoss;
        }
        private class Shot
        {
            public Transform visual;
            public Vector3 direction;
            public AttackDefinition definition;
            public float remaining;
            public float damage;
            public EnemyState target;
        }
        private readonly List<EnemyState> enemies = new List<EnemyState>();
        private readonly List<Shot> shots = new List<Shot>();
        private readonly Dictionary<Transform, float> cooldowns = new Dictionary<Transform, float>();
        private readonly Dictionary<Transform, Animator[]> attackers = new Dictionary<Transform, Animator[]>();
        private class PendingAttack
        {
            public float remaining;
            public Vector3 target;
            public EnemyState enemyTarget;
            public AttackDefinition definition;
        }
        private readonly Dictionary<Transform, PendingAttack> pending = new Dictionary<Transform, PendingAttack>();
        private Transform combatRoot;
        private float spawnTimer;
        public int ShotsFired { get; private set; }
        public int Kills { get; private set; }
        public int Hits { get; private set; }
        public int EnemyCount => enemies.Count;
        public int ProjectileCount => shots.Count;
        public bool BossDefeated { get; private set; }

        public void Configure(EnemyDefinition definition, float interval, int limit)
        {
            enemy = definition;
            spawnInterval = interval;
            maximumEnemies = limit;
        }

        public bool SpawnBoss(EnemyDefinition definition)
        {
            if (combatRoot == null || definition == null || definition.prefab == null) return false;
            Spawn(definition, true);
            return true;
        }

        private void OnEnable()
        {
            if (run == null) return;
            run.RunStarted += Begin;
            run.RunEnded += Clear;
        }
        private void OnDisable()
        {
            if (run != null) { run.RunStarted -= Begin; run.RunEnded -= Clear; }
            Clear();
        }
        private void Begin()
        {
            Clear();
            ShotsFired = Kills = Hits = 0;
            if (enemy == null || enemy.prefab == null || spawnInterval <= 0) return;
            combatRoot = new GameObject("Run Combat").transform;
            combatRoot.SetParent(transform, false);
            foreach (var unit in recruitment.Units)
            {
                cooldowns[unit.Key] = 0;
                attackers[unit.Key] = unit.Key.GetComponentsInChildren<Animator>();
            }
            spawnTimer = 0;
        }
        private void LateUpdate()
        {
            if (run != null && run.IsRunning) Tick(Time.deltaTime);
        }
        public void Tick(float dt)
        {
            if (!run.IsRunning || combatRoot == null || dt <= 0) return;
            spawnTimer -= dt;
            if (spawnTimer <= 0)
            {
                if (enemies.Count < maximumEnemies) Spawn();
                spawnTimer = spawnInterval;
            }
            MoveEnemies(dt);
            foreach (var unit in recruitment.Units)
            {
                AttackDefinition attack = unit.Value.attack;
                if (unit.Key == null || attack == null || attack.range <= 0 || attack.interval <= 0) continue;
                cooldowns[unit.Key] -= dt;
                PendingAttack preparation;
                if (pending.TryGetValue(unit.Key, out preparation))
                {
                    Vector3 aim = AimPoint(unit.Key, preparation);
                    run.FaceTarget(unit.Key, aim);
                    preparation.remaining -= dt;
                    if (preparation.remaining <= 0)
                    {
                        ExecuteAttack(unit.Key, aim, preparation.definition, preparation.enemyTarget);
                        pending.Remove(unit.Key);
                    }
                    continue;
                }
                if (cooldowns[unit.Key] > 0) continue;
                EnemyState target = NearestEnemy(unit.Key.position, attack.range);
                if (target == null) continue;
                if (attack.style == AttackDefinition.AttackStyle.MeleeLine &&
                    !HasLineTarget(unit.Key, attack)) continue;
                run.FaceTarget(unit.Key, attack.style == AttackDefinition.AttackStyle.MeleeLine
                    ? unit.Key.position + run.MoveDirection(unit.Key) : target.root.position);
                if (!string.IsNullOrEmpty(attack.animationTrigger))
                    foreach (Animator animator in attackers[unit.Key]) animator.SetTrigger(attack.animationTrigger);
                pending[unit.Key] = new PendingAttack { remaining = attack.windup,
                    target = target.root.position, enemyTarget = target, definition = attack };
                cooldowns[unit.Key] = Mathf.Max(attack.interval, attack.windup);
            }
            MoveShots(dt);
        }
        private void Spawn()
        {
            Spawn(enemy, false);
        }

        private void Spawn(EnemyDefinition definition, bool isBoss)
        {
            float angle = Random.value * Mathf.PI * 2;
            Vector3 position = run.Commander.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * spawnDistance;
            Bounds bounds = run.Ground.bounds;
            position.x = Mathf.Clamp(position.x, bounds.min.x + boundaryMargin, bounds.max.x - boundaryMargin);
            position.z = Mathf.Clamp(position.z, bounds.min.z + boundaryMargin, bounds.max.z - boundaryMargin);
            position.y = run.Commander.position.y;
            GameObject model = Instantiate(definition.prefab, position, Quaternion.identity, combatRoot);
            enemies.Add(new EnemyState { root = model.transform, animator = model.GetComponentInChildren<Animator>(), health = definition.health, definition = definition, isBoss = isBoss });
        }
        private void MoveEnemies(float dt)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyState state = enemies[i];
                EnemyDefinition enemy = state.definition;
                if (state.health <= 0)
                {
                    state.deathTime -= dt;
                    if (state.deathTime <= 0) { Destroy(state.root.gameObject); enemies.RemoveAt(i); }
                    continue;
                }
                Transform target = run.Commander;
                float distance = FlatDistance(state.root.position, target.position);
                foreach (var unit in recruitment.Units)
                {
                    if (unit.Key == null) continue;
                    float candidate = FlatDistance(state.root.position, unit.Key.position);
                    if (candidate < distance) { distance = candidate; target = unit.Key; }
                }
                Vector3 direction = target.position - state.root.position;
                direction.y = 0;
                float step = Mathf.Min(enemy.speed * dt, Mathf.Max(0, distance - enemy.stoppingDistance));
                if (step > 0)
                {
                    state.root.position += direction.normalized * step;
                    state.root.rotation = Quaternion.RotateTowards(state.root.rotation, Quaternion.LookRotation(direction), enemy.turnSpeed * dt);
                }
                if (state.animator != null && !string.IsNullOrEmpty(enemy.movingParameter))
                    state.animator.SetBool(enemy.movingParameter, step > 0);
            }
        }
        private EnemyState NearestEnemy(Vector3 position, float range)
        {
            EnemyState nearest = null;
            foreach (EnemyState state in enemies)
            {
                if (state.health <= 0) continue;
                float distance = FlatDistance(position, state.root.position);
                if (distance <= range) { range = distance; nearest = state; }
            }
            return nearest;
        }
        private Vector3 AimPoint(Transform source, PendingAttack attack)
        {
            if (attack.definition.style == AttackDefinition.AttackStyle.MeleeLine)
                return source.position + run.MoveDirection(source);
            return attack.enemyTarget != null && attack.enemyTarget.root != null && attack.enemyTarget.health > 0
                ? attack.enemyTarget.root.position : attack.target;
        }

        private bool InMelee(Vector3 origin, Vector3 direction, Vector3 target, AttackDefinition attack)
        {
            Vector3 offset = target - origin;
            offset.y = 0;
            if (attack.style == AttackDefinition.AttackStyle.MeleeLine)
            {
                float along = Vector3.Dot(offset, direction);
                return along >= 0 && along <= attack.range &&
                    (offset - direction * along).sqrMagnitude <= attack.hitRadius * attack.hitRadius;
            }
            return offset.sqrMagnitude <= attack.range * attack.range &&
                Vector3.Angle(direction, offset) <= attack.arcAngle * 0.5f;
        }

        private bool HasLineTarget(Transform source, AttackDefinition attack)
        {
            foreach (EnemyState state in enemies)
                if (state.health > 0 && InMelee(source.position, run.MoveDirection(source), state.root.position, attack)) return true;
            return false;
        }

        private void ExecuteAttack(Transform source, Vector3 target, AttackDefinition attack, EnemyState enemyTarget)
        {
            Vector3 direction = target - source.position;
            direction.y = 0;
            if (direction.sqrMagnitude <= Mathf.Epsilon) direction = source.forward;
            direction.Normalize();
            run.FaceTarget(source, target);
            float damage = attack.damage * run.CurrentTactic.damageMultiplier;
            if (attack.style == AttackDefinition.AttackStyle.MeleeArc || attack.style == AttackDefinition.AttackStyle.MeleeLine)
            {
                foreach (EnemyState state in enemies)
                    if (state.health > 0 && InMelee(source.position, direction, state.root.position, attack))
                        ApplyDamage(state, damage, direction, attack.knockback);
                // 근접 판정은 한 번만 적용하고 Cube는 짧게 표시한다.
                if (attack.projectilePrefab != null)
                {
                    var effect = Instantiate(attack.projectilePrefab,
                        source.position + direction * (attack.range * 0.5f) + Vector3.up * attack.height,
                        Quaternion.LookRotation(direction), combatRoot);
                    effect.transform.localScale = new Vector3(attack.hitRadius * 2, 0.2f, attack.range);
                    Destroy(effect, 0.15f);
                }
                return;
            }
            if (attack.projectilePrefab == null || attack.speed <= 0) return;
            Transform visual = Instantiate(attack.projectilePrefab, source.position + Vector3.up * attack.height,
                Quaternion.LookRotation(direction), combatRoot).transform;
            shots.Add(new Shot { visual = visual, direction = direction, definition = attack,
                remaining = attack.range, damage = damage, target = enemyTarget });
            ShotsFired++;
        }
        private void MoveShots(float dt)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Shot shot = shots[i];
                Vector3 start = shot.visual.position;
                if (shot.definition.style == AttackDefinition.AttackStyle.HomingProjectile)
                {
                    if (shot.target == null || shot.target.root == null || shot.target.health <= 0)
                        shot.target = NearestEnemy(start, shot.remaining);
                    if (shot.target != null)
                    {
                        Vector3 aim = shot.target.root.position - start;
                        aim.y = 0;
                        if (aim.sqrMagnitude > 0.0001f) shot.direction = aim.normalized;
                        shot.visual.rotation = Quaternion.LookRotation(shot.direction);
                    }
                }
                float distance = Mathf.Min(shot.definition.speed * dt, shot.remaining);
                Vector3 end = start + shot.direction * distance;
                EnemyState hit = null;
                float earliest = float.MaxValue;
                // 이동 선분으로 판정해 빠른 검기가 적을 통과하는 현상을 방지한다.
                foreach (EnemyState state in enemies)
                {
                    if (state.health <= 0) continue;
                    Vector3 offset = state.root.position - start;
                    offset.y = 0;
                    float along = Mathf.Clamp(Vector3.Dot(offset, shot.direction), 0, distance);
                    if ((offset - shot.direction * along).sqrMagnitude <= shot.definition.hitRadius * shot.definition.hitRadius && along < earliest)
                    { hit = state; earliest = along; }
                }
                shot.visual.position = end;
                shot.remaining -= distance;
                if (hit != null)
                {
                    ApplyDamage(hit, shot.damage, shot.direction, shot.definition.knockback);
                }
                if (hit != null || shot.remaining <= 0) { Destroy(shot.visual.gameObject); shots.RemoveAt(i); }
            }
        }
        private void ApplyDamage(EnemyState target, float damage, Vector3 direction, float knockback)
        {
            if (target.health <= 0) return;
            Hits++;
            target.health -= damage;
            if (target.health <= 0)
            {
                Kills++;
                if (target.isBoss) BossDefeated = true;
                target.deathTime = target.definition.deathDuration;
                if (target.animator != null && !string.IsNullOrEmpty(target.definition.deathTrigger)) target.animator.SetTrigger(target.definition.deathTrigger);
            }
            else if (knockback > 0)
            {
                Vector3 position = target.root.position + direction * knockback;
                Bounds bounds = run.Ground.bounds;
                position.x = Mathf.Clamp(position.x, bounds.min.x + boundaryMargin, bounds.max.x - boundaryMargin);
                position.z = Mathf.Clamp(position.z, bounds.min.z + boundaryMargin, bounds.max.z - boundaryMargin);
                target.root.position = position;
            }
        }
        private static float FlatDistance(Vector3 a, Vector3 b)
        { a.y = b.y = 0; return Vector3.Distance(a, b); }
        private void Clear()
        {
            if (combatRoot != null) { combatRoot.gameObject.SetActive(false); Destroy(combatRoot.gameObject); }
            combatRoot = null;
            BossDefeated = false;
            enemies.Clear(); shots.Clear(); cooldowns.Clear(); attackers.Clear(); pending.Clear();
        }
    }
}

