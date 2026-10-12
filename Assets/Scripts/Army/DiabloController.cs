using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArmySurvivor.Army
{
    [RequireComponent(typeof(RunController), typeof(CombatController), typeof(DiabloHud))]
    public sealed class DiabloController : MonoBehaviour
    {
        [Header("상태")]
        [SerializeField, Min(0.1f)] private float duration = 10;
        [SerializeField, Range(0.3f, 0.5f)] private float absorptionSeconds = 0.4f;
        [SerializeField, Range(0.1f, 0.2f)] private float hitStopSeconds = 0.15f;
        [SerializeField, Min(0.1f)] private float sizeMultiplier = 1.5f;
        [SerializeField, Min(0.1f)] private float moveMultiplier = 1.5f;
        [SerializeField, Range(0, 0.99f)] private float damageReduction = 0.8f;
        [SerializeField] private GameObject auraPrefab;
        [SerializeField] private Vector3 auraOffset = new Vector3(0, 0.05f, 0);
        [SerializeField, Min(0.01f)] private float auraScale = 1;
        [Header("이동 화살 — 기존 궁수 AttackDefinition")]
        [SerializeField] private AttackDefinition arrow;
        [SerializeField, Min(1)] private int arrowCount = 12;
        [SerializeField, Min(0.1f)] private float arrowDistance = 2;
        [SerializeField, Min(0)] private float arrowDamage = 35;
        [Header("좌클릭 화염")]
        [SerializeField] private GameObject firePrefab;
        [SerializeField, Min(1)] private int fireCount = 3;
        [SerializeField, Range(0, 180)] private float fireSpread = 40;
        [SerializeField, Min(0.05f)] private float fireCooldown = 0.3f;
        [SerializeField, Min(0)] private float fireDamage = 50;
        [SerializeField, Min(0.1f)] private float fireSpeed = 12;
        [SerializeField, Min(0.1f)] private float fireRange = 15;
        [SerializeField, Min(0.01f)] private float fireHitRadius = 0.7f;
        [SerializeField] private float fireHeight = 0.8f;
        [SerializeField, Min(0.01f)] private float fireScale = 1f;
        [SerializeField] private string fireAttackState = "Base Layer.Attack";
        [SerializeField, Min(0)] private float attackBlendSeconds = 0.03f;
        private Animator[] commanderAnimators;
        [Header("기존 메테오")]
        [SerializeField, Min(0.1f)] private float meteorInterval = 1;
        [SerializeField, Min(0.1f)] private float meteorRadius = 15;
        [SerializeField, Min(0)] private float meteorDamageMultiplier = 2;
        private RunController run;
        private CombatController combat;
        private RecruitmentController recruitment;
        private DiabloHud hud;
        private UnitHealth health;
        private AttackDefinition fire;
        private GameObject aura, absorptionRoot;
        private Vector3 originalScale, lastPosition;
        private float absorptionTime, distance, fireTimer, meteorTimer;
        private float stopRemaining, savedTimeScale;
        private bool stopPending, ownsStop;
        private sealed class Absorbed
        {
            public Transform unit, proxy;
            public Vector3 position, scale;
            public bool active;
        }
        private readonly List<Absorbed> absorbed = new List<Absorbed>();
        public bool Active { get; private set; }
        public float Remaining { get; private set; }
        public float Duration => duration;
        public float MeteorCooldown => meteorTimer;
        public float MeteorInterval => meteorInterval;
        public bool IsConfigured => arrow != null && arrow.projectilePrefab != null && firePrefab != null && auraPrefab != null;

        private void Awake()
        {
            run = GetComponent<RunController>(); combat = GetComponent<CombatController>();
            recruitment = GetComponent<RecruitmentController>(); hud = GetComponent<DiabloHud>();
        }
        private void OnEnable() { run.RunEnded += End; }
        public void Activate()
        {
            if (!run.IsRunning || run.CommanderDead || !IsConfigured) return;
            if (Active) { Remaining = duration; return; }
            Active = true; Remaining = duration; run.IsDiablo = true;
            health = run.Commander.GetComponent<UnitHealth>();
            commanderAnimators = run.Commander.GetComponentsInChildren<Animator>();
            originalScale = run.Commander.localScale;
            run.Commander.localScale = originalScale * sizeMultiplier;
            run.DiabloMoveMultiplier = moveMultiplier; health.DiabloDamageReduction = damageReduction;
            combat.SuspendArmyForDiablo();
            absorptionRoot = new GameObject("Diablo Absorption Visuals");
            absorptionRoot.SetActive(false);
            foreach (var unit in recruitment.Units.Keys)
            {
                if (unit == null || !unit.gameObject.activeSelf) continue;
                var state = new Absorbed { unit = unit, position = unit.position, scale = unit.localScale, active = unit.gameObject.activeSelf };
                // Inactive parent prevents gameplay callbacks on visual copies.
                state.proxy = Instantiate(unit.gameObject, absorptionRoot.transform).transform;
                foreach (var script in state.proxy.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
                foreach (var collider in state.proxy.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                foreach (var body in state.proxy.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                foreach (var canvas in state.proxy.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
                foreach (var animator in state.proxy.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                state.proxy.SetPositionAndRotation(unit.position, unit.rotation);
                state.proxy.localScale = state.scale;
                absorbed.Add(state); unit.gameObject.SetActive(false);
            }
            absorptionRoot.SetActive(true);
            aura = Instantiate(auraPrefab, run.Commander);
            aura.transform.localPosition = auraOffset; aura.transform.localScale = Vector3.one * auraScale;
            foreach (var particle in aura.GetComponentsInChildren<ParticleSystem>(true))
            { var main = particle.main; main.loop = true; }
            if (fire == null) fire = ScriptableObject.CreateInstance<AttackDefinition>();
            fire.style = AttackDefinition.AttackStyle.StraightProjectile; fire.projectilePrefab = firePrefab;
            fire.speed = fireSpeed; fire.range = fireRange; fire.hitRadius = fireHitRadius; fire.height = fireHeight;
            absorptionTime = distance = fireTimer = 0; meteorTimer = meteorInterval;
            lastPosition = run.Commander.position; stopPending = true;
            hud.Show(this, health);
        }

        private void Update()
        {
            if (!Active) return;
            if (!run.IsRunning || run.CommanderDead) { End(); return; }
            if (stopPending && !run.IsPaused && !run.IsChoosingUpgrade)
            {
                stopPending = false; ownsStop = true; stopRemaining = hitStopSeconds;
                savedTimeScale = Time.timeScale; Time.timeScale = 0; run.IsChoosingUpgrade = true;
            }
            if (ownsStop)
            {
                stopRemaining -= Time.unscaledDeltaTime;
                if (stopRemaining <= 0) ReleaseStop();
                return;
            }
            if (run.IsPaused || run.IsChoosingUpgrade) { lastPosition = run.Commander.position; return; }
            float dt = Time.deltaTime;
            Remaining = Mathf.Max(0, Remaining - dt);
            absorptionTime += dt;
            foreach (var state in absorbed)
                if (state.proxy != null)
                {
                    float t = Mathf.Clamp01(absorptionTime / absorptionSeconds);
                    state.proxy.position = Vector3.Lerp(state.position, run.Commander.position + Vector3.up, t * t);
                    state.proxy.localScale = state.scale * (1 - t);
                }
            if (absorptionRoot != null && absorptionTime >= absorptionSeconds)
            { Destroy(absorptionRoot); absorptionRoot = null; }
            if (Remaining <= 0) { End(); return; }
            Vector3 delta = run.Commander.position - lastPosition; delta.y = 0;
            distance += delta.magnitude; lastPosition = run.Commander.position;
            if (distance >= arrowDistance)
            {
                distance %= arrowDistance;
                for (int i = 0; i < arrowCount; i++)
                    combat.FireDiabloProjectile(arrow, Quaternion.Euler(0, i * 360f / arrowCount, 0) * Vector3.forward, arrowDamage, 1, true);
            }
            fireTimer = Mathf.Max(0, fireTimer - dt);
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed && fireTimer <= 0 && !combat.DiabloPointerBlocked(mouse.position.ReadValue()))
            {
                var camera = Camera.main;
                var plane = new Plane(Vector3.up, run.Commander.position);
                if (camera != null)
                {
                    Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
                    if (plane.Raycast(ray, out float enter))
                    {
                        Vector3 direction = ray.GetPoint(enter) - run.Commander.position;
                        if (direction.sqrMagnitude < 0.001f) return;
                        run.FaceTarget(run.Commander, ray.GetPoint(enter));
                        PlayFireAttack();
                        for (int i = 0; i < fireCount; i++)
                            combat.FireDiabloProjectile(fire, Quaternion.Euler(0, fireCount == 1 ? 0 : fireSpread * (i / (float)(fireCount - 1) - 0.5f), 0) * direction, fireDamage, fireScale);
                        fireTimer = fireCooldown;
                    }
                }
            }
            meteorTimer -= dt;
            if (meteorTimer <= 0) { combat.CastDiabloMeteor(meteorRadius, meteorDamageMultiplier); meteorTimer = meteorInterval; }
        }
        private void ReleaseStop()
        {
            if (!ownsStop) return;
            ownsStop = false; Time.timeScale = savedTimeScale; run.IsChoosingUpgrade = false;
        }
        private void PlayFireAttack()
        {
            int state = Animator.StringToHash(fireAttackState);
            foreach (var animator in commanderAnimators)
            {
                if (animator == null || !animator.isActiveAndEnabled || !animator.HasState(0, state)) continue;
                // Complete one swing per volley instead of repeatedly truncating a slow clip.
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    if (clip.name == "CommanderAttack")
                        animator.SetFloat("FireAttackSpeed", clip.length / Mathf.Max(0.05f, fireCooldown));
                animator.CrossFadeInFixedTime(state, attackBlendSeconds, 0, 0);
            }
        }
        public void End()
        {
            ReleaseStop(); stopPending = false;
            if (!Active) return;
            Active = false; Remaining = 0; run.IsDiablo = false; run.DiabloMoveMultiplier = 1;
            if (run.Commander != null) run.Commander.localScale = originalScale;
            if (health != null) health.DiabloDamageReduction = 0;
            if (aura != null) Destroy(aura);
            if (absorptionRoot != null) Destroy(absorptionRoot);
            foreach (var state in absorbed)
                if (state.unit != null)
                {
                    state.unit.localScale = state.scale;
                    state.unit.position = run.FormationPosition(state.unit);
                    state.unit.gameObject.SetActive(state.active);
                    run.SetAttacking(state.unit, false);
                }
            absorbed.Clear(); hud.Hide();
        }
        private void OnDisable() { run.RunEnded -= End; End(); }
        private void OnDestroy() { if (fire != null) Destroy(fire); }
    }
}
