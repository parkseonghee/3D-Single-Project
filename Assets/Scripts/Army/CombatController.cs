using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    // 소규모 전투의 실행 상태를 관리하며 공유 정의 에셋은 변경하지 않는다.
    public partial class CombatController : MonoBehaviour
    {
        [SerializeField] private RunController run;
        [SerializeField] private RecruitmentController recruitment;
        [SerializeField] private EnemyDefinition enemy;
        [SerializeField, Min(0.1f)] private float spawnInterval;
        [SerializeField, Min(0)] private float spawnDistance;
        [SerializeField, Min(1)] private int maximumEnemies;
        [SerializeField, Min(0)] private float boundaryMargin;
        [SerializeField, Min(1)] private float commanderHealth = 300;
        [Header("몬스터 피격 이펙트")]
        [SerializeField] private GameObject bloodEffect;
        [SerializeField, Min(0)] private float bloodHeight = 1;
        [SerializeField, Min(0.01f)] private float bloodScale = 1;
        [SerializeField, Min(0.1f)] private float bloodLifetime = 1.5f;
        [Header("병사 피의 갈증")]
        [SerializeField] private GameObject healingEffectPrefab;
        [SerializeField, Min(0.01f)] private float healingEffectScale = 1f;
        [SerializeField, Min(0.1f)] private float healingEffectLifetime = 5f;
        [Header("병사 회전검")]
        [SerializeField] private GameObject spinningSwordPrefab;
        [SerializeField, Min(0.1f)] private float spinningSwordRadius = 1.5f;
        [SerializeField, Min(1f)] private float spinningSwordSpeed = 180f;
        [SerializeField, Min(0.1f)] private float spinningSwordDamageMultiplier = 0.5f;
        [SerializeField, Min(0.1f)] private float spinningSwordHitRadius = 1.05f;
        [SerializeField, Min(0.1f)] private float spinningSwordHitInterval = 0.5f;
        [SerializeField, Min(0.01f)] private float spinningSwordScale = 1f;
        [Header("병사 회전 베기")]
        [SerializeField] private GameObject spinSlashEffectPrefab;
        [SerializeField, Min(0.1f)] private float spinSlashBaseRadius = 3f;
        [SerializeField, Min(0)] private float spinSlashRadiusPerLevel = 0.5f;
        [SerializeField, Min(1f)] private float spinSlashDamageMultiplier = 1.75f;
        [SerializeField, Min(0)] private float spinSlashAttackSpeedPerLevel = 0.12f;
        [SerializeField, Min(0.1f)] private float spinSlashEffectLifetime = 2f;
        [Header("병사 전투 깃발")]
        [SerializeField] private GameObject bannerPrefab;
        [SerializeField, Min(0.1f)] private float bannerInterval = 10f;
        [SerializeField, Min(0.01f)] private float bannerScale = 0.45f;
        [SerializeField, Min(0)] private float bannerAttackSpeedPerLevel = 0.1f;
        [Header("궁수 제압 사격")]
        [SerializeField] private GameObject suppressionDebuffPrefab;
        [SerializeField] private Vector3 suppressionDebuffLocalPosition = new Vector3(0, 0.1f, 0);
        [SerializeField] private Vector3 suppressionDebuffScale = Vector3.one * 1.5f;
        [SerializeField, Range(0, 1)] private float suppressionSlowPercent = 0.4f;
        [SerializeField, Min(0.1f)] private float suppressionDuration = 2.5f;
        public float SuppressionSlowPercent => suppressionSlowPercent * 100f;
        public float SuppressionDuration => suppressionDuration;
        [Header("궁수 폭발 사격")]
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField, Min(0.1f)] private float explosionRadius = 2.5f;
        [SerializeField, Min(0)] private float explosionDamageMultiplier = 0.5f;
        [SerializeField] private float explosionEffectHeight = 0.65f;
        [SerializeField, Min(0.01f)] private float explosionEffectScale = 1f;
        [SerializeField, Min(0.1f)] private float explosionEffectLifetime = 2f;
        public float ExplosionRadius => explosionRadius;
        public float ExplosionDamagePercent => explosionDamageMultiplier * 100f;
        [Header("병사 낙하 창")]
        [SerializeField] private GameObject weaponStrikeEffectPrefab;
        [SerializeField, Min(0.1f)] private float weaponStrikeInterval = 15f;
        [SerializeField] private ParticleSystem weaponStrikeImpactParticle;
        [SerializeField, Min(0)] private float weaponStrikeImpactDelay = 0.3f;
        [SerializeField, Min(0.1f)] private float weaponStrikeRadius = 2.5f;
        [SerializeField, Min(0)] private float weaponStrikeDamageMultiplier = 2f;
        [SerializeField, Min(0.01f)] private float weaponStrikeEffectScale = 0.2f;
        [SerializeField] private float weaponStrikeGroundOffset;
        [SerializeField, Min(0.1f)] private float weaponStrikeEffectLifetime = 2.5f;
        public float WeaponStrikeInterval => weaponStrikeInterval;
        public float WeaponStrikeImpactDelay => weaponStrikeImpactParticle != null
            ? weaponStrikeImpactParticle.main.startDelay.constant : weaponStrikeImpactDelay;
        public float WeaponStrikeRadius => weaponStrikeRadius;
        private readonly Dictionary<Transform, UnitHealth> allies = new Dictionary<Transform, UnitHealth>();
        private BattleExperience experience;
        private void Awake() => experience = GetComponent<BattleExperience>();
        private float AttackInterval(Transform unit, AttackDefinition attack)
        {
            int level = experience != null ? experience.SpinSlashLevel(unit) : 0;
            float speedBonus = Mathf.Max(0, level - 1) * spinSlashAttackSpeedPerLevel + bannerAttackSpeedBonus;
            if (teamAttackSpeedRemaining > 0) speedBonus += teamAttackSpeedBonus;
            return attack.interval / (1f + speedBonus);
        }
        private float AttackDamage(Transform unit, AttackDefinition attack)
        {
            float damage = attack.damage * run.CurrentTactic.damageMultiplier;
            if (teamAttackRemaining > 0) damage *= 1f + teamAttackBonus;
            if (experience != null && Random.value < experience.FocusCriticalChance(unit))
                damage *= experience.FocusCriticalMultiplier(unit);
            return damage;
        }
        public bool Defeated { get; private set; }
        public event System.Action<Vector3> EnemyKilled;

        private class EnemyState
        {
            public Transform root;
            public Animator animator;
            public UnitHealth vitality;
            public float health => vitality.Current;
            public float attackCooldown;
            public Transform attackTarget;
            public float attackTime;
            public bool attackHit;
            public float deathTime;
            public EnemyDefinition definition;
            public bool isBoss;
            public float suppressionRemaining;
            public Transform suppressionEffect;
        }
        private class Shot
        {
            public Transform source;
            public Transform visual;
            public Vector3 direction;
            public AttackDefinition definition;
            public float remaining;
            public float damage;
            public EnemyState target;
            public bool piercing;
            public bool suppression;
            public bool explosion;
            public readonly HashSet<EnemyState> hitEnemies = new HashSet<EnemyState>();
            public readonly HashSet<EnemyState> splashedEnemies = new HashSet<EnemyState>();
        }
        private readonly List<EnemyState> enemies = new List<EnemyState>();
        private readonly List<Shot> shots = new List<Shot>();
        private readonly Dictionary<Transform, float> cooldowns = new Dictionary<Transform, float>();
        private readonly Dictionary<Transform, float> weaponStrikeCooldowns = new Dictionary<Transform, float>();
        private readonly Dictionary<Transform, int> basicAttackCounts = new Dictionary<Transform, int>();
        private class BannerState
        {
            public GameObject visual;
            public float remaining;
            public int level;
        }
        private readonly Dictionary<Transform, BannerState> banners = new Dictionary<Transform, BannerState>();
        private float bannerAttackSpeedBonus;
        private class SpinningSword
        {
            public Transform visual;
            public Vector3 previousPosition;
            public bool positioned;
            public readonly Dictionary<EnemyState, float> nextHit = new Dictionary<EnemyState, float>();
        }
        private readonly Dictionary<Transform, List<SpinningSword>> spinningSwords = new Dictionary<Transform, List<SpinningSword>>();
        private float swordAngle;
        private class PendingWeaponStrike
        {
            public Transform source;
            public Vector3 impactPoint;
            public float remaining;
            public float damage;
        }
        private readonly List<PendingWeaponStrike> pendingWeaponStrikes = new List<PendingWeaponStrike>();
        private readonly Dictionary<Transform, Animator[]> attackers = new Dictionary<Transform, Animator[]>();
        private class PendingAttack
        {
            public float remaining;
            public Vector3 target;
            public EnemyState enemyTarget;
            public AttackDefinition definition;
        }
        private readonly Dictionary<Transform, PendingAttack> pending = new Dictionary<Transform, PendingAttack>();
        private class MeleeAttack
        {
            public int chainHits;
            public Vector3 flameTrailPosition;
            public EnemyState target;
            public bool returning;
            public float windup = -1;
        }
        private readonly Dictionary<Transform, MeleeAttack> melee = new Dictionary<Transform, MeleeAttack>();
        private Transform combatRoot;
        private readonly List<GameObject> healingEffects = new List<GameObject>();
        private float spawnTimer;
        private DaySettings.EnemySpawn[] enemyPool;
        private DaySettings.Wave[] waves;
        private float waveDuration = 90;
        private int enemySafetyLimit = 120;
        private int maximumWave = 4;
        public int WaveNumber { get; private set; }
        public float CurrentSpawnInterval { get; private set; }
        public int CurrentBatchSize { get; private set; }
        public int CurrentEnemyLimit { get; private set; }
        public int ShotsFired { get; private set; }
        public int Kills { get; private set; }
        public int Hits { get; private set; }
        public int EnemyCount => enemies.Count;
        public int ProjectileCount => shots.Count;
        public bool BossDefeated { get; private set; }

        public void Configure(EnemyDefinition definition, float interval, int limit, DaySettings.EnemySpawn[] pool = null,
            float duration = 90, DaySettings.Wave[] waveSettings = null, int safetyLimit = 120, int waveLimit = 4)
        {
            enemy = definition;
            spawnInterval = interval;
            maximumEnemies = limit;
            enemyPool = pool;
            waves = waveSettings;
            waveDuration = Mathf.Max(1, duration);
            enemySafetyLimit = Mathf.Max(1, safetyLimit);
            maximumWave = Mathf.Max(1, waveLimit);
            spawnTimer = 0;
            UpdateWave(0);
        }

        private void UpdateWave(float elapsed)
        {
            float progress = Mathf.Clamp01(elapsed / waveDuration);
            DaySettings.Wave selected = null;
            WaveNumber = 1;
            if (waves != null)
                for (int i = 0; i < Mathf.Min(waves.Length, maximumWave); i++)
                {
                    var wave = waves[i];
                    if (wave == null || wave.startProgress > progress) continue;
                    if (selected != null && wave.startProgress < selected.startProgress) continue;
                    selected = wave;
                    WaveNumber = i + 1;
                }
            CurrentSpawnInterval = Mathf.Max(0.1f, spawnInterval * (selected == null ? 1 : selected.intervalMultiplier));
            CurrentBatchSize = Mathf.Max(1, selected == null ? 1 : selected.batchSize);
            CurrentEnemyLimit = Mathf.Clamp(Mathf.CeilToInt(maximumEnemies *
                (selected == null ? 1 : selected.capacityMultiplier)), 1, enemySafetyLimit);
            // 더 빠른 웨이브로 넘어갈 때 이전 단계의 긴 대기 시간을 남기지 않는다.
            spawnTimer = Mathf.Min(spawnTimer, CurrentSpawnInterval);
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
            Defeated = false;
            if (enemy == null || enemy.prefab == null || spawnInterval <= 0) return;
            combatRoot = new GameObject("Run Combat").transform;
            combatRoot.SetParent(transform, false);
            RegisterHealth(run.Commander, commanderHealth, new Color(1, 0.8f, 0.2f));
            foreach (var unit in recruitment.Units)
            {
                cooldowns[unit.Key] = 0;
                attackers[unit.Key] = unit.Key.GetComponentsInChildren<Animator>();
                RegisterHealth(unit.Key, unit.Value.health, new Color(0.2f, 0.8f, 0.3f));
            }
            spawnTimer = 0;
        }
        private void LateUpdate()
        {
            if (run != null && run.IsRunning) Tick(Time.deltaTime);
        }
        public void Tick(float dt)
        {
            if (!run.IsRunning || run.IsPaused || run.IsChoosingUpgrade || combatRoot == null || dt <= 0) return;
            if (run.CommanderDead)
            {
                Defeated = true;
                if (allies[run.Commander].DeathFinished) run.ReturnToPreparation();
                return;
            }
            UpdateWave(run.Elapsed);
            spawnTimer -= dt;
            if (spawnTimer <= 0)
            {
                int count = Mathf.Min(CurrentBatchSize, CurrentEnemyLimit - enemies.Count);
                for (int i = 0; i < count; i++) Spawn();
                // 일시 정지나 프레임 지연 뒤 밀린 몬스터가 한꺼번에 생성되지 않게 한다.
                spawnTimer = CurrentSpawnInterval;
            }
            MoveEnemies(dt);
            UpdateMagicTraps(dt);
            UpdateLasers(dt);
            UpdateTeamShields(dt);
            UpdateTeamAttack(dt);
            UpdateTeamAttackSpeed(dt);
            UpdateShouts(dt);
            UpdateFlameTrails(dt);
            UpdateMarchingHorn(dt);
            UpdateBanners(dt);
            AdvanceWeaponStrikes(dt);
            UpdateSpinningSwords(dt);
            if (allies[run.Commander].IsDead)
            {
                Defeated = true;
                return;
            }
            foreach (var unit in recruitment.Units)
            {
                AttackDefinition attack = unit.Value.attack;
                if (unit.Key == null || !unit.Key.gameObject.activeSelf || attack == null || attack.range <= 0 || attack.interval <= 0) continue;
                if (allies[unit.Key].IsDead) continue;
                UpdateWeaponStrike(unit.Key, attack, dt);
                cooldowns[unit.Key] -= dt;
                if (attack.style == AttackDefinition.AttackStyle.Charge ||
                    attack.style == AttackDefinition.AttackStyle.ApproachArc)
                {
                    UpdateMelee(unit.Key, attack, dt);
                    continue;
                }
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
                cooldowns[unit.Key] = Mathf.Max(AttackInterval(unit.Key, attack), attack.windup);
            }
            MoveShots(dt);
        }
        private void UpdateBanners(float dt)
        {
            bannerAttackSpeedBonus = 0;
            if (experience == null || bannerPrefab == null) return;
            foreach (var entry in recruitment.Units)
            {
                Transform unit = entry.Key;
                if (unit == null || !allies.TryGetValue(unit, out UnitHealth health)) continue;
                int level = unit.gameObject.activeSelf && !health.IsDead ? experience.BannerLevel(unit) : 0;
                if (!banners.TryGetValue(unit, out BannerState state))
                {
                    if (level == 0) continue;
                    state = new BannerState { remaining = bannerInterval };
                    banners.Add(unit, state);
                }
                state.remaining -= dt;
                if (state.remaining <= 0)
                {
                    if (state.visual != null)
                    {
                        state.visual.SetActive(false);
                        Destroy(state.visual);
                    }
                    state.visual = null;
                    state.remaining = bannerInterval;
                    if (level > 0)
                    {
                        Vector3 position = unit.position + unit.right * 1.25f;
                        Bounds ground = run.Ground.bounds;
                        position.x = Mathf.Clamp(position.x, ground.min.x, ground.max.x);
                        position.z = Mathf.Clamp(position.z, ground.min.z, ground.max.z);
                        state.visual = Instantiate(bannerPrefab, position, bannerPrefab.transform.rotation, combatRoot);
                        state.visual.transform.localScale *= bannerScale;
                    }
                }
                if (state.visual != null)
                {
                    state.level = Mathf.Max(state.level, level);
                    bannerAttackSpeedBonus = Mathf.Max(bannerAttackSpeedBonus,
                        state.level * bannerAttackSpeedPerLevel);
                }
            }
        }
        private void UpdateSpinningSwords(float dt)
        {
            if (experience == null || spinningSwordPrefab == null) return;
            swordAngle = Mathf.Repeat(swordAngle + spinningSwordSpeed * dt, 360f);
            foreach (var entry in recruitment.Units)
            {
                Transform unit = entry.Key;
                int count = unit != null && allies.TryGetValue(unit, out UnitHealth health) &&
                    unit.gameObject.activeSelf && !health.IsDead ? experience.SpinningSwordCount(unit) : 0;
                if (!spinningSwords.TryGetValue(unit, out List<SpinningSword> swords))
                {
                    swords = new List<SpinningSword>();
                    spinningSwords[unit] = swords;
                }
                while (swords.Count > count)
                {
                    Destroy(swords[swords.Count - 1].visual.gameObject);
                    swords.RemoveAt(swords.Count - 1);
                }
                while (swords.Count < count)
                {
                    Transform visual = Instantiate(spinningSwordPrefab, combatRoot).transform;
                    visual.localScale *= spinningSwordScale;
                    swords.Add(new SpinningSword { visual = visual });
                }
                for (int i = 0; i < swords.Count; i++)
                {
                    float angle = swordAngle + 360f * i / swords.Count;
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                    Transform visual = swords[i].visual;
                    visual.position = unit.position + direction * spinningSwordRadius + Vector3.up * 0.85f;
                    visual.rotation = Quaternion.Euler(0, angle, -55f);
                    Vector3 previous = swords[i].positioned ? swords[i].previousPosition : visual.position;
                    foreach (EnemyState target in enemies)
                    {
                        if (target.root == null || target.health <= 0 ||
                            FlatDistanceToSegment(target.root.position, previous, visual.position) > spinningSwordHitRadius ||
                            (swords[i].nextHit.TryGetValue(target, out float next) && Time.time < next)) continue;
                        swords[i].nextHit[target] = Time.time + spinningSwordHitInterval;
                        ApplyDamage(target, AttackDamage(unit, entry.Value.attack) * spinningSwordDamageMultiplier,
                            direction, 0, false, unit);
                    }
                    swords[i].previousPosition = visual.position;
                    swords[i].positioned = true;
                }
            }
        }
        private void UpdateWeaponStrike(Transform unit, AttackDefinition attack, float dt)
        {
            if (experience == null || !experience.HasWeaponStrike(unit)) return;
            if (!weaponStrikeCooldowns.TryGetValue(unit, out float remaining))
                remaining = weaponStrikeInterval;
            remaining -= dt;
            if (remaining > 0)
            {
                weaponStrikeCooldowns[unit] = remaining;
                return;
            }
            // 살아 있는 적을 각각 같은 확률로 선택한다.
            EnemyState target = null;
            int seen = 0;
            foreach (EnemyState candidate in enemies)
            {
                if (candidate.root == null || candidate.health <= 0) continue;
                seen++;
                if (Random.Range(0, seen) == 0) target = candidate;
            }
            if (target == null)
            {
                weaponStrikeCooldowns[unit] = 0;
                return;
            }
            weaponStrikeCooldowns[unit] = weaponStrikeInterval;
            Vector3 impactPoint = target.root.position;
            float impactDelay = WeaponStrikeImpactDelay;
            pendingWeaponStrikes.Add(new PendingWeaponStrike
            {
                source = unit,
                impactPoint = impactPoint,
                remaining = impactDelay,
                damage = AttackDamage(unit, attack) * weaponStrikeDamageMultiplier
            });
            if (weaponStrikeEffectPrefab == null) return;
            GameObject effect = Instantiate(weaponStrikeEffectPrefab,
                impactPoint + Vector3.up * weaponStrikeGroundOffset,
                weaponStrikeEffectPrefab.transform.rotation, combatRoot);
            effect.transform.localScale *= weaponStrikeEffectScale;
            Animator effectAnimator = effect.GetComponent<Animator>();
            if (effectAnimator != null)
            {
                effectAnimator.Rebind();
                effectAnimator.Update(0);
            }
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.loop = false;
                particle.Clear(true);
                particle.Play(true);
            }
            Destroy(effect, Mathf.Max(weaponStrikeEffectLifetime, impactDelay + 0.1f));
        }

        private void AdvanceWeaponStrikes(float dt)
        {
            // 새로 생성한 이펙트는 다음 프레임부터 시간을 줄여 파티클 지연과 피해가 일치한다.
            for (int i = pendingWeaponStrikes.Count - 1; i >= 0; i--)
            {
                PendingWeaponStrike strike = pendingWeaponStrikes[i];
                strike.remaining -= dt;
                if (strike.remaining > 0) continue;
                foreach (EnemyState target in enemies)
                {
                    if (target.root == null || target.health <= 0 ||
                        FlatDistance(strike.impactPoint, target.root.position) > weaponStrikeRadius) continue;
                    Vector3 direction = target.root.position - strike.impactPoint;
                    direction.y = 0;
                    ApplyDamage(target, strike.damage, direction.normalized, 0, false, strike.source);
                }
                pendingWeaponStrikes.RemoveAt(i);
            }
        }
        private void Spawn()
        {
            Spawn(ChooseEnemy(), false);
        }

        private EnemyDefinition ChooseEnemy()
        {
            float total = 0;
            if (enemyPool == null) return enemy;
            foreach (var entry in enemyPool)
                if (entry != null && entry.enemy != null && entry.enemy.prefab != null)
                    total += Mathf.Max(0, entry.weight);
            if (total <= 0) return enemy;
            float roll = Random.value * total;
            foreach (var entry in enemyPool)
            {
                if (entry == null || entry.enemy == null || entry.enemy.prefab == null || entry.weight <= 0) continue;
                roll -= entry.weight;
                if (roll <= 0) return entry.enemy;
            }
            return enemy;
        }

        private void RegisterHealth(Transform unit, float maximum, Color color)
        {
            var health = unit.GetComponent<UnitHealth>();
            if (health == null) health = unit.gameObject.AddComponent<UnitHealth>();
            health.Initialize(maximum, color);
            allies[unit] = health;
        }

        private void UpdateMelee(Transform unit, AttackDefinition attack, float dt)
        {
            UnitHealth unitHealth = allies[unit];
            unitHealth.IsInvulnerable = false;
            MeleeAttack state;
            if (!melee.TryGetValue(unit, out state))
            {
                if (cooldowns[unit] > 0) return;
                EnemyState target = NearestEnemy(unit.position, attack.detectionRange);
                if (target == null) return;
                state = new MeleeAttack { target = target, flameTrailPosition = unit.position };
                melee.Add(unit, state);
                run.SetAttacking(unit, true);
            }
            if (state.target.root == null || state.target.health <= 0 ||
                FlatDistance(unit.position, run.Commander.position) > attack.detectionRange + run.CurrentTactic.radius)
                state.returning = true;

            if (state.returning)
            {
                Vector3 home = run.FormationPosition(unit);
                run.MoveAttacker(unit, home, attack.returnSpeed, dt);
                if (FlatDistance(unit.position, home) < 0.1f)
                {
                    melee.Remove(unit);
                    run.SetAttacking(unit, false);
                    cooldowns[unit] = AttackInterval(unit, attack);
                }
                return;
            }

            bool chargeInvulnerability = attack.style == AttackDefinition.AttackStyle.Charge &&
                experience != null && experience.HasChargeInvulnerability(unit);
            unitHealth.IsInvulnerable = chargeInvulnerability;
            Vector3 offset = unit.position - state.target.root.position;
            offset.y = 0;
            Vector3 away = offset.sqrMagnitude > 0.0001f ? offset.normalized : -unit.forward;
            Vector3 destination = state.target.root.position + away * attack.stoppingDistance;
            float distance = offset.magnitude;
            bool tooFar = distance > attack.stoppingDistance + attack.stoppingTolerance;
            bool tooClose = attack.maintainDistance && distance < attack.stoppingDistance - attack.stoppingTolerance;
            if (state.windup < 0 && (tooFar || tooClose))
            {
                run.MoveAttacker(unit, destination, attack.approachSpeed, dt);
                if (attack.style == AttackDefinition.AttackStyle.Charge) LeaveFlameTrail(unit, state, false);
                return;
            }
            run.MoveAttacker(unit, unit.position, 0, dt);
            run.FaceTarget(unit, state.target.root.position);
            if (state.windup < 0)
            {
                if (cooldowns[unit] > 0) return;
                foreach (Animator animator in attackers[unit])
                    if (!string.IsNullOrEmpty(attack.animationTrigger)) animator.SetTrigger(attack.animationTrigger);
                state.windup = attack.windup;
            }
            state.windup -= dt;
            if (state.windup > 0) return;

            Vector3 direction = (state.target.root.position - unit.position).normalized;
            float damage = AttackDamage(unit, attack);
            if (attack.style == AttackDefinition.AttackStyle.Charge)
            {
                unitHealth.IsInvulnerable = false;
                LeaveFlameTrail(unit, state, true);
                damage *= MomentumDamageMultiplier(unit);
                bool chainCharge = experience != null && experience.HasChainCharge(unit);
                if (chainCharge) damage *= chainChargeFirstMultiplier * Mathf.Pow(chainChargeDecay, state.chainHits);
                bool killedByCharge = false;
                if (FlatDistance(unit.position, state.target.root.position) <= attack.range)
                {
                    ApplyDamage(state.target, damage, direction, attack.knockback, false, unit);
                    killedByCharge = state.target.health <= 0;
                }
                state.returning = true;
                if (chainCharge && killedByCharge)
                {
                    EnemyState next = FindChainChargeTarget(unit, attack);
                    if (next != null)
                    {
                        state.target = next;
                        state.chainHits++;
                        unitHealth.IsInvulnerable = chargeInvulnerability;
                        state.returning = false;
                        state.windup = -1;
                        cooldowns[unit] = 0;
                        return;
                    }
                }
            }
            else
            {
                if (!TrySpinSlash(unit, attack))
                    ExecuteAttack(unit, state.target.root.position, attack, state.target);
                state.returning = state.target.health <= 0;
            }
            state.windup = -1;
            cooldowns[unit] = AttackInterval(unit, attack);
        }

        private bool TrySpinSlash(Transform unit, AttackDefinition attack)
        {
            int level = experience != null ? experience.SpinSlashLevel(unit) : 0;
            if (level == 0) return false;
            int count = basicAttackCounts.TryGetValue(unit, out int previous) ? previous + 1 : 1;
            if (count < 3) { basicAttackCounts[unit] = count; return false; }
            basicAttackCounts[unit] = 0;
            float radius = spinSlashBaseRadius + (level - 1) * spinSlashRadiusPerLevel;
            float damage = AttackDamage(unit, attack) * spinSlashDamageMultiplier;
            foreach (EnemyState target in enemies)
            {
                if (target.root == null || target.health <= 0 ||
                    FlatDistance(unit.position, target.root.position) > radius) continue;
                Vector3 direction = target.root.position - unit.position;
                direction.y = 0;
                ApplyDamage(target, damage, direction.normalized, 0, false, unit);
            }
            if (spinSlashEffectPrefab == null) return true;
            GameObject effect = Instantiate(spinSlashEffectPrefab, unit.position + Vector3.up * 0.5f,
                spinSlashEffectPrefab.transform.rotation, combatRoot);
            effect.transform.localScale *= radius / spinSlashBaseRadius;
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.loop = false;
                particle.Clear(true);
                particle.Play(true);
            }
            Destroy(effect, spinSlashEffectLifetime);
            return true;
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
            var health = model.GetComponent<UnitHealth>();
            if (health == null) health = model.AddComponent<UnitHealth>();
            health.Initialize(definition.health, new Color(0.95f, 0.2f, 0.2f));
            enemies.Add(new EnemyState { root = model.transform, animator = model.GetComponentInChildren<Animator>(), vitality = health, definition = definition, isBoss = isBoss });
        }
        private void MoveEnemies(float dt)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyState state = enemies[i];
                EnemyDefinition enemy = state.definition;
                if (state.health <= 0)
                {
                    ClearSuppression(state);
                    state.deathTime -= dt;
                    if (state.deathTime <= 0)
                    {
                        if (state.isBoss) BossDefeated = true;
                        Destroy(state.root.gameObject);
                        enemies.RemoveAt(i);
                        foreach (var group in spinningSwords.Values)
                            foreach (SpinningSword sword in group) sword.nextHit.Remove(state);
                    }
                    continue;
                }
                if (state.suppressionRemaining > 0)
                {
                    state.suppressionRemaining = Mathf.Max(0, state.suppressionRemaining - dt);
                    if (state.suppressionRemaining <= 0) ClearSuppression(state);
                }
                state.attackCooldown -= dt;
                if (state.attackTarget != null)
                {
                    state.attackTime += dt;
                    if (!state.attackHit && state.attackTime >= enemy.attackWindup)
                    {
                        state.attackHit = true;
                        Transform victim = state.attackTarget;
                        if (allies.TryGetValue(victim, out var health) && !health.IsDead &&
                            FlatDistance(state.root.position, victim.position) <= enemy.stoppingDistance + 0.15f)
                        {
                            health.TakeDamage(enemy.damage * run.CurrentTactic.receivedDamageMultiplier);
                            if (health.IsDead)
                            {
                                pending.Remove(victim);
                                melee.Remove(victim);
                                run.SetAttacking(victim, false);
                            }
                        }
                    }
                    if (state.attackTime >= Mathf.Max(enemy.attackDuration, enemy.attackWindup)) state.attackTarget = null;
                    continue;
                }
                Transform target = run.Commander;
                float distance = FlatDistance(state.root.position, target.position);
                foreach (var unit in recruitment.Units)
                {
                    if (unit.Key == null || !unit.Key.gameObject.activeSelf || allies[unit.Key].IsDead) continue;
                    float candidate = FlatDistance(state.root.position, unit.Key.position);
                    if (candidate < distance) { distance = candidate; target = unit.Key; }
                }
                Vector3 direction = target.position - state.root.position;
                direction.y = 0;
                float speedMultiplier = state.suppressionRemaining > 0 ? 1f - suppressionSlowPercent : 1f;
                float step = Mathf.Min(enemy.speed * speedMultiplier * dt, Mathf.Max(0, distance - enemy.stoppingDistance));
                if (step > 0)
                {
                    state.root.position += direction.normalized * step;
                    state.root.rotation = Quaternion.RotateTowards(state.root.rotation, Quaternion.LookRotation(direction), enemy.turnSpeed * dt);
                }
                if (state.animator != null && !string.IsNullOrEmpty(enemy.movingParameter))
                    state.animator.SetBool(enemy.movingParameter, step > 0);
                if (FlatDistance(state.root.position, target.position) <= enemy.stoppingDistance + 0.15f &&
                    state.attackCooldown <= 0 && !allies[target].IsDead)
                {
                    state.attackTarget = target;
                    state.attackTime = 0;
                    state.attackHit = false;
                    state.attackCooldown = Mathf.Max(enemy.attackInterval, enemy.attackDuration, enemy.attackWindup);
                    if (direction.sqrMagnitude > 0.0001f) state.root.rotation = Quaternion.LookRotation(direction);
                    if (state.animator != null)
                    {
                        if (!string.IsNullOrEmpty(enemy.movingParameter)) state.animator.SetBool(enemy.movingParameter, false);
                        if (!string.IsNullOrEmpty(enemy.attackState)) state.animator.Play(enemy.attackState, 0, 0);
                    }
                }
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
            // 준비 동작 중 이동한 거리를 반영한다. 관통도 조준 사거리는 동일하다.
            bool projectile = attack.style == AttackDefinition.AttackStyle.StraightProjectile ||
                attack.style == AttackDefinition.AttackStyle.HomingProjectile;
            if (projectile && enemyTarget != null)
            {
                if (enemyTarget.root == null || enemyTarget.health <= 0 ||
                    FlatDistance(source.position, enemyTarget.root.position) > attack.range)
                    enemyTarget = NearestEnemy(source.position, attack.range);
                if (enemyTarget == null) return;
                target = enemyTarget.root.position;
            }
            Vector3 direction = target - source.position;
            direction.y = 0;
            if (direction.sqrMagnitude <= Mathf.Epsilon) direction = source.forward;
            direction.Normalize();
            run.FaceTarget(source, target);
            float damage = AttackDamage(source, attack);
            if (projectile && experience != null && experience.HasChainLightning(source) && chainLightningPrefab != null)
            {
                AttackWithChainLightning(source, enemyTarget, attack, damage);
                return;
            }
            if (attack.style == AttackDefinition.AttackStyle.MeleeArc || attack.style == AttackDefinition.AttackStyle.MeleeLine ||
                attack.style == AttackDefinition.AttackStyle.ApproachArc)
            {
                foreach (EnemyState state in enemies)
                    if (state.health > 0 && InMelee(source.position, direction, state.root.position, attack))
                        ApplyDamage(state, damage, direction, attack.knockback, false, source);
                // 접근 공격은 피해 판정과 별개로 지정된 이펙트를 한 번 재생한다.
                if (attack.style == AttackDefinition.AttackStyle.ApproachArc && attack.projectilePrefab != null)
                {
                    var effect = Instantiate(attack.projectilePrefab,
                        source.position + direction * attack.effectForwardOffset + Vector3.up * attack.height,
                        Quaternion.LookRotation(direction) * attack.projectilePrefab.transform.rotation, combatRoot);
                    effect.transform.localScale *= attack.effectScale;
                    foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var main = particle.main;
                        main.loop = false;
                        particle.Clear(true);
                        particle.Play(true);
                    }
                    var arc = effect.GetComponent<LineRenderer>();
                    if (arc != null)
                    {
                        arc.useWorldSpace = false;
                        for (int i = 0; i < arc.positionCount; i++)
                        {
                            float angle = Mathf.Lerp(-attack.arcAngle / 2, attack.arcAngle / 2,
                                i / (float)(arc.positionCount - 1)) * Mathf.Deg2Rad;
                            arc.SetPosition(i, new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle))
                                * (attack.range / attack.effectScale));
                        }
                    }
                    Destroy(effect, attack.effectLifetime);
                }
                return;
            }
            if (attack.projectilePrefab == null || attack.speed <= 0) return;
            bool piercing = experience != null && experience.HasPiercing(source);
            bool suppression = experience != null && experience.HasSuppression(source);
            bool explosion = experience != null && experience.HasExplosion(source);
            int projectileCount = experience != null ? experience.MultiShotCount(source) : 1;
            float spread = projectileCount > 1 ? experience.MultiShotSpreadAngle : 0;
            for (int i = 0; i < projectileCount; i++)
            {
                // 중앙 1발을 유지하고 좌우 바깥쪽, 안쪽 순서로 추가한다.
                // 4갈래에서도 조준한 적 사이로 모든 화살이 빗나가지 않는다.
                float angle = i == 0 ? 0 : spread * 0.5f / ((i + 1) / 2) * (i % 2 == 1 ? -1 : 1);
                Vector3 projectileDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                float boundaryDistance = DistanceToBoundary(source.position, projectileDirection, run.Ground.bounds);
                float travelDistance = piercing ? boundaryDistance : Mathf.Min(attack.range, boundaryDistance);
                if (travelDistance <= 0) continue;
                Transform visual = Instantiate(attack.projectilePrefab, source.position + Vector3.up * attack.height,
                    Quaternion.LookRotation(projectileDirection), combatRoot).transform;
                shots.Add(new Shot { source = source, visual = visual, direction = projectileDirection, definition = attack,
                    remaining = travelDistance,
                    damage = damage, target = enemyTarget, piercing = piercing,
                    suppression = suppression, explosion = explosion });
                ShotsFired++;
            }
        }
        private static float DistanceToBoundary(Vector3 origin, Vector3 direction, Bounds bounds)
        {
            if (origin.x < bounds.min.x || origin.x > bounds.max.x || origin.z < bounds.min.z || origin.z > bounds.max.z) return 0;
            float x = Mathf.Abs(direction.x) > 0.00001f
                ? ((direction.x > 0 ? bounds.max.x : bounds.min.x) - origin.x) / direction.x : float.PositiveInfinity;
            float z = Mathf.Abs(direction.z) > 0.00001f
                ? ((direction.z > 0 ? bounds.max.z : bounds.min.z) - origin.z) / direction.z : float.PositiveInfinity;
            return Mathf.Max(0, Mathf.Min(x, z));
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
                    if (state.health <= 0 || shot.hitEnemies.Contains(state)) continue;
                    Vector3 offset = state.root.position - start;
                    offset.y = 0;
                    float along = Mathf.Clamp(Vector3.Dot(offset, shot.direction), 0, distance);
                    if ((offset - shot.direction * along).sqrMagnitude <= shot.definition.hitRadius * shot.definition.hitRadius && along < earliest)
                    {
                        if (shot.piercing)
                        {
                            ProcessShotHit(shot, state);
                        }
                        else { hit = state; earliest = along; }
                    }
                }
                shot.visual.position = end;
                shot.remaining -= distance;
                if (hit != null)
                {
                    ProcessShotHit(shot, hit);
                }
                if (hit != null || shot.remaining <= 0) { Destroy(shot.visual.gameObject); shots.RemoveAt(i); }
            }
        }
        private void ProcessShotHit(Shot shot, EnemyState target)
        {
            if (!shot.hitEnemies.Add(target)) return;
            Vector3 impact = target.root.position;
            ApplyDamage(target, shot.damage, shot.direction, shot.definition.knockback, shot.suppression, shot.source);
            if (shot.explosion) Explode(shot, impact);
        }

        private void Explode(Shot shot, Vector3 center)
        {
            if (explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(explosionEffectPrefab,
                    center + Vector3.up * explosionEffectHeight,
                    explosionEffectPrefab.transform.rotation, combatRoot);
                effect.transform.localScale *= explosionEffectScale;
                // 원본은 반복 재생된다. 명중 시에는 복제본만 한 번 재생한다.
                foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particle.main;
                    main.loop = false;
                    particle.Clear(true);
                    particle.Play(true);
                }
                Destroy(effect, explosionEffectLifetime);
            }
            float splashDamage = shot.damage * explosionDamageMultiplier;
            if (splashDamage <= 0) return;
            foreach (EnemyState enemyState in enemies)
            {
                if (enemyState.health <= 0 || shot.hitEnemies.Contains(enemyState) ||
                    shot.splashedEnemies.Contains(enemyState) ||
                    FlatDistance(center, enemyState.root.position) > explosionRadius) continue;
                shot.splashedEnemies.Add(enemyState);
                ApplyDamage(enemyState, splashDamage, shot.direction, 0, false, shot.source);
            }
        }

        private void ApplyDamage(EnemyState target, float damage, Vector3 direction, float knockback,
            bool suppression = false, Transform source = null)
        {
            if (target.health <= 0 || damage <= 0) return;
            Hits++;
            target.vitality.TakeDamage(damage * ShoutArmorMultiplier(target));
            if (bloodEffect != null)
            {
                Quaternion rotation = direction.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(direction) : Quaternion.identity;
                var effect = Instantiate(bloodEffect, target.root.position + Vector3.up * bloodHeight,
                    rotation * bloodEffect.transform.localRotation, combatRoot);
                effect.transform.localScale *= bloodScale;
                Destroy(effect, bloodLifetime);
            }
            if (target.health <= 0)
            {
                ClearSuppression(target);
                ClearShoutDebuff(target);
                Kills++;
                EnemyKilled?.Invoke(target.root.position);
                HealOnKill(source);
                target.attackTarget = null;
                target.deathTime = Mathf.Max(target.definition.deathDuration, target.vitality.DeathDuration + 0.25f);
            }
            else
            {
                if (suppression) ApplySuppression(target);
                if (knockback > 0)
                {
                    Vector3 position = target.root.position + direction * knockback;
                    Bounds bounds = run.Ground.bounds;
                    position.x = Mathf.Clamp(position.x, bounds.min.x + boundaryMargin, bounds.max.x - boundaryMargin);
                    position.z = Mathf.Clamp(position.z, bounds.min.z + boundaryMargin, bounds.max.z - boundaryMargin);
                    target.root.position = position;
                }
            }
        }
        private void HealOnKill(Transform source)
        {
            if (source == null || experience == null || !allies.TryGetValue(source, out UnitHealth health)) return;
            float ratio = experience.BloodthirstHealRatio(source);
            if (health.Heal(health.Maximum * ratio) <= 0 || healingEffectPrefab == null) return;
            GameObject effect = Instantiate(healingEffectPrefab, source.position,
                healingEffectPrefab.transform.rotation, source);
            effect.transform.localScale *= healingEffectScale;
            healingEffects.RemoveAll(item => item == null);
            healingEffects.Add(effect);
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.loop = false;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                particle.Clear(true);
                particle.Play(true);
            }
            Destroy(effect, healingEffectLifetime);
        }
        private void ApplySuppression(EnemyState target)
        {
            // 중복 명중은 감속을 누적하지 않고 지속시간만 갱신한다.
            target.suppressionRemaining = suppressionDuration;
            if (target.suppressionEffect != null || suppressionDebuffPrefab == null) return;
            Transform effect = Instantiate(suppressionDebuffPrefab, target.root).transform;
            effect.localPosition = suppressionDebuffLocalPosition;
            effect.localScale = suppressionDebuffScale;
            target.suppressionEffect = effect;
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Clear(true);
                particle.Play(true);
            }
        }
        private static void ClearSuppression(EnemyState target)
        {
            target.suppressionRemaining = 0;
            if (target.suppressionEffect == null) return;
            target.suppressionEffect.gameObject.SetActive(false);
            Destroy(target.suppressionEffect.gameObject);
            target.suppressionEffect = null;
        }
        private static float FlatDistance(Vector3 a, Vector3 b)
        { a.y = b.y = 0; return Vector3.Distance(a, b); }
        private static float FlatDistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            point.y = start.y = end.y = 0;
            Vector3 path = end - start;
            if (path.sqrMagnitude < 0.0001f) return Vector3.Distance(point, end);
            float t = Mathf.Clamp01(Vector3.Dot(point - start, path) / path.sqrMagnitude);
            return Vector3.Distance(point, start + path * t);
        }
        private void Clear()
        {
            foreach (GameObject effect in healingEffects)
                if (effect != null) { effect.SetActive(false); Destroy(effect); }
            healingEffects.Clear();
            if (combatRoot != null) { combatRoot.gameObject.SetActive(false); Destroy(combatRoot.gameObject); }
            combatRoot = null;
            BossDefeated = false;
            foreach (var unit in melee.Keys) if (unit != null && run != null) run.SetAttacking(unit, false);
            melee.Clear();
            foreach (var ally in allies)
            {
                if (ally.Key == null) continue;
                ally.Key.gameObject.SetActive(true);
                ally.Value.Initialize(ally.Value.Maximum, Color.green);
                ally.Value.enabled = false;
            }
            allies.Clear();
            enemies.Clear(); shots.Clear(); cooldowns.Clear(); weaponStrikeCooldowns.Clear();
            basicAttackCounts.Clear();
            magicTrapCooldowns.Clear(); magicTraps.Clear();
            laserCooldowns.Clear(); lasers.Clear();
            teamShieldCooldowns.Clear();
            ClearTeamAttack(); teamAttackCooldown = -1f;
            ClearTeamAttackSpeed(); teamAttackSpeedCooldown = -1f;
            ClearShouts();
            flameTrails.Clear(); flameTrailTickTime = 0;
            ClearMarchingHorn(); marchingHornCooldown = -1f;
            banners.Clear(); bannerAttackSpeedBonus = 0;
            spinningSwords.Clear(); swordAngle = 0;
            pendingWeaponStrikes.Clear(); attackers.Clear(); pending.Clear();
        }
    }
}

