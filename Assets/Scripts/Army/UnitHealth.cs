using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public class UnitHealth : MonoBehaviour
    {
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
        public float Shield { get; private set; }
        public float DamageReduction { get; set; }
        public bool IsInvulnerable { get; set; }

        public void GiveShield(float amount, float duration, GameObject effectPrefab)
        {
            if (IsDead || amount <= 0 || duration <= 0) return;
            Shield = Mathf.Max(Shield, amount);
            shieldMaximum = Shield;
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
            Shield = 0;
            if (shieldEffect != null)
            {
                shieldEffect.SetActive(false);
                Destroy(shieldEffect);
                shieldEffect = null;
            }
            if (shieldFill != null) shieldFill.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Shield > 0 && Time.time >= shieldExpiresAt) ClearShield();
        }

        public void Initialize(float maximum, Color color)
        {
            DamageReduction = 0;
            IsInvulnerable = false;
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

        public void TakeDamage(float damage)
        {
            if (IsDead || IsInvulnerable) return;
            if (Shield > 0 && Time.time >= shieldExpiresAt) ClearShield();
            damage = Mathf.Max(0, damage) * (1f - Mathf.Clamp01(DamageReduction));
            float absorbed = Mathf.Min(Shield, damage);
            Shield -= absorbed;
            if (Shield <= 0) ClearShield();
            Current = Mathf.Max(0, Current - (damage - absorbed));
            if (IsDead) PlayDeath();
            RefreshBar();
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
            if (healthBar == null) return;
            healthBar.gameObject.SetActive(enabled && !IsDead);
            fill.anchorMax = new Vector2(Mathf.Clamp01(Current / Maximum), 1);
            shieldFill.gameObject.SetActive(Shield > 0);
            shieldFill.anchorMax = new Vector2(shieldMaximum > 0 ? Shield / shieldMaximum : 0, 1);
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
            ClearShield();
            if (healthBar != null) healthBar.gameObject.SetActive(false);
        }
    }
}
