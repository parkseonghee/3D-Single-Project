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

        public void Initialize(float maximum, Color color)
        {
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
            if (IsDead) return;
            Current = Mathf.Max(0, Current - Mathf.Max(0, damage));
            if (IsDead) PlayDeath();
            RefreshBar();
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
        }

        private void RefreshBar()
        {
            if (healthBar == null) return;
            healthBar.gameObject.SetActive(enabled && !IsDead);
            fill.anchorMax = new Vector2(Mathf.Clamp01(Current / Maximum), 1);
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
            if (healthBar != null) healthBar.gameObject.SetActive(false);
        }
    }
}
