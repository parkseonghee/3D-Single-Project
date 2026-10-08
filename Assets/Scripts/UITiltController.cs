using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Screen Space - Camera 캔버스의 자식 UI 루트를 마우스 위치에 따라 기울인다.
/// Perspective 카메라와 서로 다른 Z 깊이의 레이어를 함께 사용할 때 원근감이 보인다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class UITiltController : MonoBehaviour
{
    [Serializable]
    private struct ParallaxLayer
    {
        public RectTransform target;
        [Tooltip("캔버스 로컬 단위. 카메라에 가까운 레이어일수록 더 크게 설정합니다.")]
        public float depth;
        [Tooltip("마우스가 화면 끝에 있을 때의 X/Y 이동량(캔버스 로컬 단위).")]
        public Vector2 travel;
    }

    [SerializeField, Range(0f, 8f)] private float maxTiltX = 8f;
    [SerializeField, Range(0f, 12f)] private float maxTiltY = 12f;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.18f;
    [SerializeField] private ParallaxLayer[] layers = Array.Empty<ParallaxLayer>();

    private RectTransform root;
    private Quaternion initialRotation;
    private Vector3[] initialLayerPositions;
    private Vector2 currentTilt;
    private Vector2 tiltVelocity;
    private Vector2 currentPointer;
    private Vector2 pointerVelocity;

    private void Awake()
    {
        root = (RectTransform)transform;
        initialRotation = root.localRotation;
        initialLayerPositions = new Vector3[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            if (layers[i].target != null) initialLayerPositions[i] = layers[i].target.localPosition;
    }

    private void LateUpdate()
    {
        // 마우스가 없거나 화면 밖이면 자연스럽게 정면으로 돌아온다.
        Vector2 targetPointer = Vector2.zero;
        Mouse mouse = Mouse.current;
        if (mouse != null && Screen.width > 0 && Screen.height > 0)
        {
            Vector2 position = mouse.position.ReadValue();
            if (position.x >= 0 && position.x <= Screen.width && position.y >= 0 && position.y <= Screen.height)
                targetPointer = new Vector2(position.x / Screen.width * 2f - 1f,
                    position.y / Screen.height * 2f - 1f);
        }

        float delta = Time.unscaledDeltaTime;
        currentPointer = Vector2.SmoothDamp(currentPointer, targetPointer, ref pointerVelocity, smoothTime,
            Mathf.Infinity, delta);
        Vector2 targetTilt = new Vector2(-currentPointer.y * maxTiltX, currentPointer.x * maxTiltY);
        currentTilt = Vector2.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, smoothTime,
            Mathf.Infinity, delta);
        root.localRotation = initialRotation * Quaternion.Euler(currentTilt.x, currentTilt.y, 0f);

        for (int i = 0; i < layers.Length; i++)
        {
            RectTransform layer = layers[i].target;
            if (layer == null || layer == root) continue;
            Vector3 origin = initialLayerPositions[i];
            layer.localPosition = origin + new Vector3(currentPointer.x * layers[i].travel.x,
                currentPointer.y * layers[i].travel.y, layers[i].depth);
        }
    }

    private void OnDisable()
    {
        if (root != null) root.localRotation = initialRotation;
        for (int i = 0; i < layers.Length; i++)
            if (layers[i].target != null && initialLayerPositions != null && i < initialLayerPositions.Length)
                layers[i].target.localPosition = initialLayerPositions[i];
        currentPointer = currentTilt = pointerVelocity = tiltVelocity = Vector2.zero;
    }
}
