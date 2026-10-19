using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Manual/Equal Education Effect")]
    public sealed class EqualEducationEffect : ManualEffectDefinition
    {
        public enum Reactivation { RefreshDuration, Stack }
        [Min(1)] public int differentSoldiersRequired = 3;
        [Min(0)] public float damageBonus = 0.3f;
        [Min(0)] public float attackSpeedBonus = 0.2f;
        [Min(0.1f)] public float duration = 8f;
        public Reactivation reactivation = Reactivation.RefreshDuration;
        public GameObject buffEffect;
        public Vector3 effectOffset = new Vector3(0, 0.1f, 0);
        public Vector3 effectScale = Vector3.one;
        [Min(0.1f)] public float effectLifetime = 1.5f;
        public override ManualEffect CreateEffect(ManualManager manager) => new Runtime(this, manager);
        public override int MinimumSoldiers => Mathf.Max(1, differentSoldiersRequired);
        public override string Describe(string fallback) =>
            $"서로 다른 병사 {MinimumSoldiers}명에게 연속으로 경험치 병 투자\n\n전 병사 공격력 +{damageBonus:P0}\n공격 속도 +{attackSpeedBonus:P0} · {duration:0.#}초\n" +
            (reactivation == Reactivation.RefreshDuration ? "재발동 시 지속 시간 갱신" : "재발동 시 효과 중첩 · 시간 갱신");

        public sealed class Runtime : ManualEffect
        {
            private readonly EqualEducationEffect data;
            private readonly ManualManager manager;
            // Instance IDs survive a recorded unit dying or being destroyed.
            private readonly HashSet<int> invested = new HashSet<int>();
            private float remaining;
            private int stacks;
            public Runtime(EqualEducationEffect data, ManualManager manager)
            { this.data = data; this.manager = manager; }
            public override void OnBattleStart()
            {
                manager.Experience.OnExpBottleInvested += Invested;
                Publish();
            }
            private void Invested(Transform unit)
            {
                int id = unit.GetInstanceID();
                if (invested.Contains(id)) invested.Clear();
                invested.Add(id);
                if (invested.Count >= Mathf.Max(1, data.differentSoldiersRequired))
                {
                    stacks = data.reactivation == Reactivation.Stack && remaining > 0 ? stacks + 1 : 1;
                    remaining = Mathf.Max(0.1f, data.duration);
                    manager.Combat.SetManualBuff(this, data.damageBonus * stacks, data.attackSpeedBonus * stacks);
                    invested.Clear();
                    manager.PlayActivation(data);
                }
                Publish();
            }
            public override void Tick(float deltaTime)
            {
                if (remaining <= 0) return;
                remaining = Mathf.Max(0, remaining - deltaTime);
                if (remaining <= 0) { stacks = 0; manager.Combat.RemoveManualBuff(this); }
                Publish();
            }
            private void Publish() => manager.Publish(invested, Mathf.Max(1, data.differentSoldiersRequired), remaining, data.duration, stacks);
            public override void OnBattleEnd()
            {
                manager.Experience.OnExpBottleInvested -= Invested;
                manager.Combat.RemoveManualBuff(this);
                invested.Clear(); remaining = 0; stacks = 0;
                Publish();
            }
        }
    }
}
