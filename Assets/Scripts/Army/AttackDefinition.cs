using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Attack Definition")]
    public class AttackDefinition : ScriptableObject
    {
        public enum AttackStyle { StraightProjectile, HomingProjectile, MeleeArc, MeleeLine, Charge, ApproachArc }
        public AttackStyle style;
        [Min(0.1f)] public float range;
        [Min(0.01f)] public float interval;
        [Min(0)] public float damage;
        [Min(0.1f)] public float speed;
        [Min(0.01f)] public float hitRadius;
        public float height;
        public GameObject projectilePrefab;
        public string animationTrigger;
        [Min(0)] public float windup;
        [Range(1, 180)] public float arcAngle = 100;
        [Min(0)] public float knockback;
        [Header("접근 공격")]
        [Min(1)] public float detectionRange = 10;
        [Min(0.1f)] public float approachSpeed = 6;
        [Min(0.1f)] public float returnSpeed = 8;
        [Min(0.1f)] public float stoppingDistance = 1.5f;
        public bool maintainDistance;
        [Min(0)] public float stoppingTolerance = 0.05f;
        [Min(0.1f)] public float effectScale = 1;
        [Min(0)] public float effectForwardOffset;
        [Min(0.1f)] public float effectLifetime = 1f;
        [Header("공격 소리 (비우면 소리 없음)")]
        public AudioClip attackSound;
        [Range(0, 1)] public float attackSoundVolume = 0.5f;
        [Tooltip("소리 파일에서 가장 큰 지점(초). 이 지점이 공격 판정 순간(준비 동작 windup이 끝나는 때)과 겹치도록 그만큼 먼저 재생한다.")]
        [Min(0)] public float attackSoundPeak;
        [Tooltip("같은 소리를 다시 낼 수 있는 최소 간격(초). 여러 병사가 동시에 공격해도 소리가 겹쳐 커지지 않게 한다.")]
        [Min(0)] public float attackSoundInterval = 0.06f;
    }
}
