using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public class UnitHealth : MonoBehaviour
    {
        public event System.Action<UnitHealth> OnHpChanged;
        public event System.Action<UnitHealth, float, float> OnDamageTaken; // health lost, shield absorbed
        public float Maximum { get; private set; }
        public float Current { get; private set; }
        public bool IsDead => Current <= 0;
        public float DeathDuration { get; private set; }
        public bool DeathFinished => IsDead && Time.time >= deathEndsAt;
        private float deathEndsAt;
        private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");
        private Color barColor;
        private float barHeight;
        private RectTransform healthBar;
        private RectTransform fill;
        private RectTransform shieldFill;
        private GameObject shieldEffect;
        private float shieldExpiresAt;
        private float shieldMaximum;
        private float baseShield;
        public float Shield => baseShield + ManualShieldAmount;
        private sealed class ExternalShield { public float amount, maximum, until; }
        private readonly System.Collections.Generic.Dictionary<object, ExternalShield> manualShields =
            new System.Collections.Generic.Dictionary<object, ExternalShield>();
        private readonly System.Collections.Generic.List<object> expiredShields = new System.Collections.Generic.List<object>();
        private float ManualShieldAmount
        {
            get { float total = 0; foreach (var shield in manualShields.Values)
                    if (Time.time < shield.until) total += shield.amount; return total; }
        }
        public void GiveManualShield(object source, float amount, float duration)
        {
            if (IsDead || amount <= 0 || duration <= 0) return;
            manualShields[source] = new ExternalShield { amount = amount, maximum = amount, until = Time.time + duration };
            RefreshBar();
        }
        public void RemoveManualShield(object source) { manualShields.Remove(source); RefreshBar(); }
        public float ConsumeShield(float amount)
            => ConsumeShield(amount, true);
        private float ConsumeShield(float amount, bool notify)
        {
            if (Time.time >= shieldExpiresAt) ClearShield();
            float remaining = Mathf.Max(0, amount), consumed = 0;
            foreach (var shield in manualShields.Values)
            {
                if (Time.time >= shield.until) continue;
                float used = Mathf.Min(remaining, shield.amount);
                shield.amount -= used; remaining -= used; consumed += used;
            }
            float fromBase = Mathf.Min(remaining, baseShield);
            baseShield -= fromBase; consumed += fromBase;
            if (baseShield <= 0) ClearShield();
            if (notify) RefreshBar();
            return consumed;
        }
        public float DamageReduction { get; set; }
        public float DiabloDamageReduction { get; set; }
        public bool IsInvulnerable { get; set; }
        private float rewardInvulnerableUntil;
        public void GrantTemporaryInvulnerability(float seconds) =>
            rewardInvulnerableUntil = Mathf.Max(rewardInvulnerableUntil, Time.time + seconds);

        public void GiveShield(float amount, float duration, GameObject effectPrefab)
        {
            if (IsDead || amount <= 0 || duration <= 0) return;
            baseShield = Mathf.Max(baseShield, amount);
            shieldMaximum = baseShield;
            shieldExpiresAt = Time.time + duration;
            if (shieldEffect == null && effectPrefab != null)
            {
                shieldEffect = Instantiate(effectPrefab, transform);
                shieldEffect.transform.localPosition = Vector3.zero;
            }
            if (shieldEffect != null)
            {
                // 보호막 지속 시간에 맞춰 한 번만 재생한다. 재부여 시 남은 시간도 갱신한다.
                var particles = shieldEffect.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var particle in particles)
                {
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = particle.main;
                    main.loop = false;
                    main.duration = duration;
                    main.startLifetime = duration;
                }
                foreach (var particle in particles) particle.Play(false);
            }
            RefreshBar();
        }

        private void ClearShield()
        {
            baseShield = 0;
            if (shieldEffect != null)
            {
                shieldEffect.SetActive(false);
                Destroy(shieldEffect);
                shieldEffect = null;
            }
            if (shieldFill != null) shieldFill.gameObject.SetActive(ManualShieldAmount > 0);
        }

        private void Update()
        {
            if (baseShield > 0 && Time.time >= shieldExpiresAt) { ClearShield(); RefreshBar(); }
            if (manualShields.Count > 0)
            {
                expiredShields.Clear();
                foreach (var pair in manualShields) if (Time.time >= pair.Value.until) expiredShields.Add(pair.Key);
                foreach (var key in expiredShields) manualShields.Remove(key);
                if (expiredShields.Count > 0) RefreshBar();
            }
        }

        public void Initialize(float maximum, Color color)
        {
            DamageReduction = 0;
            DiabloDamageReduction = 0;
            IsInvulnerable = false;
            rewardInvulnerableUntil = 0;
            manualShields.Clear();
            ClearShield();
            if (Maximum > 0 && IsDead)
                foreach (var animator in GetComponentsInChildren<Animator>())
                {
                    animator.Rebind();
                    animator.Update(0);
                }
            Maximum = Mathf.Max(1, maximum);
            Current = Maximum;
            barColor = color;
            barHeight = 2;
            foreach (Renderer model in GetComponentsInChildren<Renderer>())
                barHeight = Mathf.Max(barHeight, model.bounds.max.y - transform.position.y + 0.3f);
            enabled = true;
            if (healthBar == null) CreateBar();
            fill.GetComponent<Image>().color = barColor;
            RefreshBar();
        }

        public void Revive(float healthRatio)
        {
            if (!IsDead) return;
            gameObject.SetActive(true);
            Current = Maximum * Mathf.Clamp(healthRatio, .01f, 1);
            DeathDuration = 0; deathEndsAt = 0;
            foreach (var animator in GetComponentsInChildren<Animator>())
            {
                animator.Rebind(); animator.Update(0);
            }
            enabled = true;
            RefreshBar();
        }

        public void TakeDamage(float damage)
        {
            if (IsDead || IsInvulnerable || Time.time < rewardInvulnerableUntil) return;
            if (baseShield > 0 && Time.time >= shieldExpiresAt) ClearShield();
            damage = Mathf.Max(0, damage) * (1f - Mathf.Clamp01(DamageReduction)) * (1f - Mathf.Clamp01(DiabloDamageReduction));
            float previous = Current;
            float absorbed = ConsumeShield(damage, false);
            Current = Mathf.Max(0, Current - (damage - absorbed));
            if (IsDead) PlayDeath();
            RefreshBar();
            if (damage > 0) OnDamageTaken?.Invoke(this, previous - Current, absorbed);
        }

        public float Heal(float amount)
        {
            if (IsDead || amount <= 0) return 0;
            float recovered = Mathf.Min(amount, Maximum - Current);
            Current += recovered;
            if (recovered > 0) RefreshBar();
            return recovered;
        }

        private void PlayDeath()
        {
            DeathDuration = 0;
            foreach (var animator in GetComponentsInChildren<Animator>())
            {
                if (!animator.HasState(0, DeathState)) continue;
                foreach (var parameter in animator.parameters)
                    if (parameter.type == AnimatorControllerParameterType.Trigger) animator.ResetTrigger(parameter.nameHash);
                // 사망은 진행 중인 공격을 즉시 끊고, 마지막 자세를 유지한다.
                animator.Play(DeathState, 0, 0);
                animator.Update(0);
                DeathDuration = Mathf.Max(DeathDuration, animator.GetCurrentAnimatorStateInfo(0).length);
            }
            deathEndsAt = Time.time + DeathDuration + 0.25f;
        }

        private void CreateBar()
        {
            var root = new GameObject("World Health Bar", typeof(RectTransform), typeof(Canvas), typeof(Image));
            root.layer = 5;
            healthBar = root.GetComponent<RectTransform>();
            healthBar.SetParent(transform, false);
            healthBar.sizeDelta = new Vector2(120, 14);
            healthBar.localScale = Vector3.one * 0.01f;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = -100;
            var background = root.GetComponent<Image>();
            background.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            background.raycastTarget = false;
            var inside = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            inside.layer = 5;
            fill = inside.GetComponent<RectTransform>();
            fill.SetParent(healthBar, false);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(2, 2);
            fill.offsetMax = new Vector2(-2, -2);
            inside.GetComponent<Image>().raycastTarget = false;
            var shield = new GameObject("Shield Fill", typeof(RectTransform), typeof(Image));
            shield.layer = 5;
            shieldFill = shield.GetComponent<RectTransform>();
            shieldFill.SetParent(healthBar, false);
            shieldFill.anchorMin = new Vector2(0, 1);
            shieldFill.anchorMax = new Vector2(1, 1);
            shieldFill.offsetMin = new Vector2(0, 3);
            shieldFill.offsetMax = new Vector2(0, 9);
            shield.GetComponent<Image>().color = Color.white;
            shield.GetComponent<Image>().raycastTarget = false;
            shield.SetActive(false);
        }

        private void RefreshBar()
        {
            OnHpChanged?.Invoke(this);
            if (healthBar == null) return;
            healthBar.gameObject.SetActive(enabled && !IsDead);
            fill.anchorMax = new Vector2(Mathf.Clamp01(Current / Maximum), 1);
            shieldFill.gameObject.SetActive(Shield > 0);
            float totalMaximum = baseShield > 0 ? shieldMaximum : 0;
            foreach (var shield in manualShields.Values) if (Time.time < shield.until) totalMaximum += shield.maximum;
            shieldFill.anchorMax = new Vector2(totalMaximum > 0 ? Mathf.Clamp01(Shield / totalMaximum) : 0, 1);
        }

        private void LateUpdate()
        {
            if (healthBar == null || IsDead) return;
            var camera = Camera.main;
            if (camera == null) return;
            healthBar.position = transform.position + Vector3.up * barHeight;
            healthBar.rotation = camera.transform.rotation;
        }

        private void OnDisable()
        {
            IsInvulnerable = false;
            manualShields.Clear();
            ClearShield();
            if (healthBar != null) healthBar.gameObject.SetActive(false);
        }
    }
}
