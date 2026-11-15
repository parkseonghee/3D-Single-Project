using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ArmySurvivor.Army
{
    // 아군이 적에게 준 피해를 숫자로 띄운다. 원본 텍스트(template)는 씬에 배치하고 여기서 복제해 재사용한다.
    public class DamageNumbers : MonoBehaviour
    {
        private const string EnabledKey = "Settings.DamageNumbers";
        // 시작 화면 설정 토글과 공유하는 사용자 설정. 기본값은 켜짐.
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            set { PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        [SerializeField] private DamageNumberSettings settings;
        [SerializeField] private CombatController combat;
        [SerializeField] private Camera worldCamera;
        [Tooltip("숫자를 담는 영역. 전투 UI 아래에 두면 전투 밖에서는 자동으로 숨는다.")]
        [SerializeField] private RectTransform container;
        [SerializeField] private TMP_Text template;

        private class Number
        {
            public TMP_Text text;
            public Transform target;
            public Vector3 world;
            public float age, damage, jitter;
            public bool boosted;
        }

        private readonly List<Number> active = new List<Number>();
        private readonly Stack<Number> pool = new Stack<Number>();
        private Canvas canvas;

        private void Awake()
        {
            canvas = container != null ? container.GetComponentInParent<Canvas>() : null;
            if (template != null) template.gameObject.SetActive(false);
        }

        private void OnEnable() { if (combat != null) combat.EnemyDamaged += Show; }
        private void OnDisable()
        {
            if (combat != null) combat.EnemyDamaged -= Show;
            // 전투가 끝나면 남은 숫자를 정리한다.
            for (int i = active.Count - 1; i >= 0; i--) Release(i);
        }

        private void Show(Transform target, Vector3 position, float damage, bool boosted)
        {
            if (!Enabled || settings == null || template == null || damage < settings.minimumDamage) return;
            // 같은 적이 짧은 시간에 연속으로 맞으면 숫자 하나로 합쳐 화면을 덜 어지럽힌다.
            foreach (var existing in active)
                if (existing.target == target && target != null && existing.age < settings.mergeWindow)
                {
                    existing.damage += damage;
                    existing.boosted |= boosted;
                    existing.age = 0;
                    existing.world = position + settings.worldOffset;
                    Style(existing);
                    return;
                }
            if (active.Count >= settings.maxActive) Release(0);
            var number = pool.Count > 0 ? pool.Pop() : Create();
            number.target = target;
            number.world = position + settings.worldOffset;
            number.damage = damage;
            number.boosted = boosted;
            number.age = 0;
            number.jitter = Random.Range(-settings.horizontalJitter, settings.horizontalJitter);
            number.text.gameObject.SetActive(true);
            Style(number);
            active.Add(number);
            Place(number);
        }

        private Number Create()
        {
            var text = Instantiate(template, container);
            text.name = "Damage Number";
            text.raycastTarget = false;
            return new Number { text = text };
        }

        private void Style(Number number)
        {
            number.text.text = Mathf.RoundToInt(number.damage).ToString();
            number.text.color = number.boosted ? settings.boostedColor : settings.normalColor;
            number.text.fontSize = settings.fontSize * (number.damage >= settings.bigHitDamage ? settings.bigHitScale : 1f);
        }

        private void LateUpdate()
        {
            if (active.Count == 0) return;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var number = active[i];
                // 일시정지·카드 선택 중(timeScale 0)에는 함께 멈춘다.
                number.age += Time.deltaTime;
                if (number.age >= settings.lifetime) { Release(i); continue; }
                Place(number);
            }
        }

        private void Place(Number number)
        {
            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return;
            Vector3 screen = cam.WorldToScreenPoint(number.world);
            bool visible = screen.z > 0;
            number.text.enabled = visible;
            if (!visible) return;
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, uiCamera, out Vector2 local);
            float t = number.age / settings.lifetime;
            ((RectTransform)number.text.transform).anchoredPosition = local + new Vector2(number.jitter, settings.riseDistance * t);
            // 후반 40% 동안 서서히 사라진다.
            var color = number.text.color;
            color.a = t < 0.6f ? 1f : Mathf.Clamp01((1f - t) / 0.4f);
            number.text.color = color;
        }

        private void Release(int index)
        {
            var number = active[index];
            active.RemoveAt(index);
            number.target = null;
            if (number.text != null)
            {
                number.text.gameObject.SetActive(false);
                pool.Push(number);
            }
        }
    }
}
