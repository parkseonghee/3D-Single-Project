using UnityEngine;

namespace ArmySurvivor.Army
{
    // 데미지 숫자 표시 수치. PlayScene의 DamageNumbers가 참조한다.
    [CreateAssetMenu(menuName = "Army Survivor/Damage Number Settings")]
    public class DamageNumberSettings : ScriptableObject
    {
        [Header("표시 시간과 움직임")]
        [Min(0.1f)] public float lifetime = 0.7f;
        [Tooltip("사라질 때까지 위로 떠오르는 거리 (캔버스 단위)")]
        [Min(0)] public float riseDistance = 45f;
        [Tooltip("적 발밑 기준 숫자를 띄울 높이 (월드 단위)")]
        public Vector3 worldOffset = new Vector3(0, 1.8f, 0);
        [Tooltip("겹침을 줄이기 위한 좌우 무작위 흔들림 (캔버스 단위)")]
        [Min(0)] public float horizontalJitter = 18f;

        [Header("화면 정리")]
        [Tooltip("동시에 보이는 숫자 최대 개수. 넘치면 가장 오래된 숫자를 재사용한다.")]
        [Min(1)] public int maxActive = 40;
        [Tooltip("같은 적이 이 시간 안에 다시 맞으면 기존 숫자에 더한다.")]
        [Min(0)] public float mergeWindow = 0.25f;
        [Tooltip("이보다 작은 피해는 표시하지 않는다.")]
        [Min(0)] public float minimumDamage = 0.5f;

        [Header("모양")]
        public Color normalColor = new Color(1f, 0.97f, 0.9f);
        [Tooltip("마을 건물 강화 보너스를 받은 병력의 피해")]
        public Color boostedColor = new Color(1f, 0.78f, 0.3f);
        [Min(1)] public float fontSize = 30f;
        [Tooltip("이 피해 이상이면 글자를 키운다.")]
        [Min(0)] public float bigHitDamage = 60f;
        [Min(1)] public float bigHitScale = 1.35f;
    }
}
