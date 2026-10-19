using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Manual/Tactical Effect")]
    public sealed class TacticalManualEffect : ManualEffectDefinition
    {
        public enum Tactic { FireVolley, WallBreak, RelayVanguard, Pursuit, FormationCounter, FocusedEducation }
        public Tactic tactic;
        [Tooltip("화염: 마법사/궁수, 성벽: 방패/기마, 교대: 병사/방패, 추격: 궁수/기마, 반격: 방패")]
        public UnitDefinition primaryType, secondaryType;
        [Header("발동 연출")]
        public GameObject burstEffect;
        public Vector3 burstScale = Vector3.one;
        [Min(0.1f)] public float visualLifetime = 1.5f;
        [Header("화염 사격")]
        [Min(0.1f)] public float burnDuration = 4;
        [Min(0)] public float burnDps = 12;
        [Min(0.05f)] public float burnTick = 0.5f;
        [Min(0.1f)] public float spreadRadius = 3;
        [Min(0.1f)] public float spreadCooldown = 1;
        [Min(1)] public int maximumBurningEnemies = 40;
        public GameObject burningEffect;
        public Vector3 burningScale = Vector3.one * 0.3f;
        [Header("성벽 돌파")]
        [Min(0)] public float cavalryShield = 40;
        [Min(0.1f)] public float shieldInterval = 8, shieldDuration = 8;
        [Min(0.1f)] public float shockRadius = 3;
        [Min(0)] public float damagePerShield = 1.5f;
        [Header("교대 선봉")]
        [Min(0.1f)] public float relayWindow = 3;
        [Min(0)] public float relayDamageRatio = 0.6f;
        [Min(0.1f)] public float relayCooldown = 0.8f;
        [Header("추격 명령")]
        [Min(0.1f)] public float suppressionSeconds = 3;
        [Min(0)] public float nextChargeBonus = 0.75f;
        [Min(0.1f)] public float chargeBonusDuration = 8;
        [Header("진형 반격")]
        [Min(0)] public int compactFormation = 0, spreadFormation = 2;
        [Min(1)] public float storedDamageCap = 200;
        [Min(0)] public float counterDamageMultiplier = 1.5f;
        [Min(0.1f)] public float counterRadius = 6;
        [Header("집중 교육")]
        [Min(1)] public int consecutiveInvestments = 3;
        [Min(0)] public float focusDamageBonus = 0.6f, focusSpeedBonus = 0.35f;
        [Min(0.1f)] public float focusDuration = 8;
        public override ManualEffect CreateEffect(ManualManager manager)
        {
            switch (tactic)
            {
                case Tactic.FireVolley: return new FireVolleyManual(this, manager);
                case Tactic.WallBreak: return new WallBreakManual(this, manager);
                case Tactic.RelayVanguard: return new RelayVanguardManual(this, manager);
                case Tactic.Pursuit: return new PursuitManual(this, manager);
                case Tactic.FormationCounter: return new FormationCounterManual(this, manager);
                default: return new FocusedEducationManual(this, manager);
            }
        }
        public override string Describe(string fallback)
        {
            switch (tactic)
            {
                case Tactic.FireVolley: return $"마법사 명중 시 {burnDuration:0.#}초 점화\n초당 {burnDps:0.#} 피해\n궁수가 점화된 적 명중 시 반경 {spreadRadius:0.#}에 불길 확산\n대상별 재발동 {spreadCooldown:0.#}초";
                case Tactic.WallBreak: return $"방패병 생존 시 {shieldInterval:0.#}초마다 기마병 보호막 {cavalryShield:0.#}\n돌진 명중 시 보호막을 최대 {cavalryShield:0.#} 소모\n반경 {shockRadius:0.#} 충격파: 소모량 × {damagePerShield:0.#}";
                case Tactic.RelayVanguard: return $"병사와 방패병이 {relayWindow:0.#}초 안에 같은 적을 번갈아 공격\n교대 타격의 {relayDamageRatio:P0} 추가 피해\n대상별 재발동 {relayCooldown:0.#}초";
                case Tactic.Pursuit: return $"궁수 명중 시 {suppressionSeconds:0.#}초 제압\n기마병이 제압된 적을 돌진으로 처치하면\n다음 돌진 피해 +{nextChargeBonus:P0}\n{chargeBonusDuration:0.#}초 안에 1회 · 중첩 없음";
                case Tactic.FormationCounter: return $"밀집 중 아군이 받은 피해·보호막 흡수량 저장 (최대 {storedDamageCap:0.#})\n산개 전환 시 반경 {counterRadius:0.#} 반격\n저장량 × {counterDamageMultiplier:0.#} 피해 · 저장량 소모";
                default: return $"같은 병사에게 경험치 병 {consecutiveInvestments}개 연속 투자\n그 병사 공격력 +{focusDamageBonus:P0}\n공격 속도 +{focusSpeedBonus:P0} · {focusDuration:0.#}초\n새 선봉 발동 시 이전 선봉 강화 종료";
            }
        }
    }
}
