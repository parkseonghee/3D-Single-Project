using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class CardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Serializable]
        public sealed class Config
        {
            [Min(0f)] public float enterStagger = 0.1f;
            [Min(0.01f)] public float enterDuration = 0.42f;
            [Range(0.5f, 1f)] public float enterScale = 0.78f;
            [Min(0f)] public float enterOffset = 22f;
            [Range(0f, 15f)] public float enterRotation = 6f;
            [Min(0f)] public float floatHeight = 6f;
            [Min(0.1f)] public float floatHalfPeriod = 1.6f;
            [Min(0.01f)] public float hoverDuration = 0.16f;
            [Range(1f, 1.2f)] public float hoverScale = 1.07f;
            [Min(0f)] public float hoverRise = 12f;
            [Min(0.01f)] public float punchDuration = 0.26f;
            [Range(0f, 0.3f)] public float punchScale = 0.13f;
            [Min(0.01f)] public float otherFadeDuration = 0.2f;
        }

        private RectTransform rect;
        private CanvasGroup canvasGroup;
        private Button button;
        private Config config;
        private Action onSelected;
        private Vector2 homePosition;
        private Quaternion homeRotation;
        private Sequence entranceTween;
        private Tween floatTween;
        private Sequence hoverTween;
        private Tween selectionTween;
        private Tween fadeTween;
        private bool ready;

        public void Configure(Button legacyButton, Action selected, Config motionConfig)
        {
            rect = (RectTransform)transform;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();
            GetComponent<Image>().raycastTarget = true;
            if (legacyButton != null && legacyButton.gameObject != gameObject)
                legacyButton.gameObject.SetActive(false);
            config = motionConfig;
            onSelected = selected;
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
            button.interactable = false;
        }

        public void PlayEntrance(int order)
        {
            KillTweens();
            ready = false;
            button.interactable = false;
            homePosition = rect.anchoredPosition;
            homeRotation = rect.localRotation;
            rect.anchoredPosition = homePosition + Vector2.down * config.enterOffset;
            rect.localScale = Vector3.one * config.enterScale;
            rect.localRotation = homeRotation * Quaternion.Euler(0f, 0f,
                (order % 2 == 0 ? -1f : 1f) * config.enterRotation);
            canvasGroup.alpha = 0f;

            entranceTween = DOTween.Sequence().SetUpdate(true)
                .SetDelay(order * config.enterStagger)
                .Append(rect.DOScale(1f, config.enterDuration).SetEase(Ease.OutBack).SetUpdate(true))
                .Join(rect.DOAnchorPos(homePosition, config.enterDuration).SetEase(Ease.OutCubic).SetUpdate(true))
                .Join(rect.DOLocalRotateQuaternion(homeRotation, config.enterDuration)
                    .SetEase(Ease.OutCubic).SetUpdate(true))
                .Join(canvasGroup.DOFade(1f, config.enterDuration).SetEase(Ease.OutQuad).SetUpdate(true))
                .OnComplete(() =>
                {
                    ready = true;
                    button.interactable = true;
                    StartFloating();
                });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!ready || !button.interactable) return;
            KillTween(ref floatTween);
            AnimateHover(config.hoverScale, homePosition.y + config.hoverRise, false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!ready || !button.interactable) return;
            AnimateHover(1f, homePosition.y, true);
        }

        public void LockInteraction()
        {
            ready = false;
            button.interactable = false;
            KillTween(ref entranceTween);
            KillTween(ref floatTween);
            KillTween(ref hoverTween);
        }

        public void PlaySelection(Action afterPunch)
        {
            LockInteraction();
            KillTween(ref selectionTween);
            selectionTween = rect.DOPunchScale(Vector3.one * config.punchScale,
                    config.punchDuration, 6, 0.7f)
                .SetUpdate(true)
                .OnComplete(() => afterPunch?.Invoke());
        }

        public void FadeOut(Action completed)
        {
            LockInteraction();
            KillTween(ref fadeTween);
            fadeTween = canvasGroup.DOFade(0f, config.otherFadeDuration)
                .SetUpdate(true)
                .OnComplete(() => completed?.Invoke());
        }

        private void HandleClick()
        {
            if (ready) onSelected?.Invoke();
        }

        private void AnimateHover(float targetScale, float targetY, bool resumeFloat)
        {
            KillTween(ref hoverTween);
            hoverTween = DOTween.Sequence().SetUpdate(true)
                .Append(rect.DOScale(targetScale, config.hoverDuration)
                    .SetEase(Ease.OutQuad).SetUpdate(true))
                .Join(rect.DOAnchorPosY(targetY, config.hoverDuration)
                    .SetEase(Ease.OutQuad).SetUpdate(true));
            if (resumeFloat) hoverTween.OnComplete(StartFloating);
        }

        private void StartFloating()
        {
            if (!ready || !isActiveAndEnabled) return;
            KillTween(ref floatTween);
            if (config.floatHeight <= 0f) return;
            floatTween = rect.DOAnchorPosY(homePosition.y + config.floatHeight,
                    config.floatHalfPeriod)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void OnDisable()
        {
            KillTweens();
            ready = false;
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.localRotation = homeRotation;
                rect.anchoredPosition = homePosition;
            }
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        private void KillTweens()
        {
            KillTween(ref entranceTween);
            KillTween(ref floatTween);
            KillTween(ref hoverTween);
            KillTween(ref selectionTween);
            KillTween(ref fadeTween);
        }

        private static void KillTween<T>(ref T tween) where T : Tween
        {
            if (tween != null && tween.IsActive()) tween.Kill();
            tween = null;
        }
    }
}
