using System;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public interface IBossBehavior
    {
        void OnSpawn(BossUnit boss);
        void OnPhaseChange(BossUnit boss, int phase);
        void OnDeath(BossUnit boss);
    }

    public sealed class BossUnit : MonoBehaviour
    {
        public event Action<BossUnit> OnBossDefeated;
        public UnitHealth Health { get; private set; }
        public bool IsAlive => active && Health != null && !Health.IsDead;
        private IBossBehavior[] behaviors;
        private bool active;
        private int phase;

        public void Initialize(UnitHealth health)
        {
            Health = health;
            behaviors = GetComponents<IBossBehavior>();
            phase = 0;
            active = true;
            foreach (var behavior in behaviors) behavior.OnSpawn(this);
        }

        public void ChangePhase(int nextPhase)
        {
            if (!active || nextPhase == phase) return;
            phase = nextPhase;
            foreach (var behavior in behaviors) behavior.OnPhaseChange(this, phase);
        }

        private void Update()
        {
            if (!active || Health == null || !Health.IsDead) return;
            active = false;
            foreach (var behavior in behaviors) behavior.OnDeath(this);
            OnBossDefeated?.Invoke(this);
        }

        private void OnDisable() => active = false;
    }
}
