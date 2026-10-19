using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public abstract class TacticalManualRuntime : ManualEffect
    {
        protected readonly TacticalManualEffect data;
        protected readonly ManualManager manager;
        protected float clock;
        private float lastAnnouncement = -10;
        protected TacticalManualRuntime(TacticalManualEffect data, ManualManager manager)
        { this.data = data; this.manager = manager; }
        protected bool IsType(Transform unit, UnitDefinition type) => unit != null && type != null && manager.Experience.DefinitionFor(unit) == type;
        protected bool Alive(Transform unit) => unit != null && unit.gameObject.activeInHierarchy &&
            unit.TryGetComponent<UnitHealth>(out var health) && !health.IsDead;
        protected bool HasLiving(UnitDefinition type)
        {
            foreach (var unit in manager.Experience.TrackedUnits) if (IsType(unit, type) && Alive(unit)) return true;
            return false;
        }
        protected void Burst(string text, Vector3 position, Transform follow = null)
        {
            manager.PlayVisual(data.burstEffect, position, data.burstScale, data.visualLifetime, follow);
            if (clock - lastAnnouncement >= 1) { manager.Announce(text); lastAnnouncement = clock; }
        }
        public override void Tick(float deltaTime) { clock += Mathf.Max(0, deltaTime); }
    }

    public sealed class FireVolleyManual : TacticalManualRuntime
    {
        private sealed class Burn { public float remaining, accumulated, spreadAt; public Transform source; public GameObject visual; }
        private readonly Dictionary<Transform, Burn> burning = new Dictionary<Transform, Burn>();
        public FireVolleyManual(TacticalManualEffect data, ManualManager manager) : base(data, manager) { }
        public override void OnBattleStart() { manager.Combat.OnManualEligibleHit += Hit; Show(); }
        private void Ignite(Transform target, Transform source)
        {
            if (!manager.Combat.ManualEnemyAlive(target)) return;
            if (!burning.TryGetValue(target, out var burn))
            {
                if (burning.Count >= Mathf.Max(1, data.maximumBurningEnemies)) return;
                burn = new Burn(); burning.Add(target, burn);
            }
            burn.remaining = Mathf.Max(0.1f, data.burnDuration); burn.source = source;
            if (burn.visual == null) burn.visual = manager.PlayVisual(data.burningEffect, target.position + Vector3.up * 0.3f,
                data.burningScale, burn.remaining, target);
        }
        private void Hit(CombatController.ManualHit hit)
        {
            if (IsType(hit.Source, data.primaryType)) { Ignite(hit.Target, hit.Source); Show(); return; }
            if (!IsType(hit.Source, data.secondaryType) || !burning.TryGetValue(hit.Target, out var burn) || clock < burn.spreadAt) return;
            burn.spreadAt = clock + Mathf.Max(0.1f, data.spreadCooldown);
            foreach (var target in manager.Combat.ManualEnemiesInRadius(hit.Position, data.spreadRadius)) Ignite(target, burn.source);
            Burst("화염 사격!", hit.Position); Show();
        }
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            foreach (var target in new List<Transform>(burning.Keys))
            {
                var burn = burning[target];
                if (!manager.Combat.ManualEnemyAlive(target)) { Remove(target); continue; }
                float slice = Mathf.Min(deltaTime, burn.remaining);
                burn.remaining -= slice; burn.accumulated += slice;
                if (burn.remaining > 0 && burn.visual == null)
                    burn.visual = manager.PlayVisual(data.burningEffect, target.position + Vector3.up * 0.3f,
                        data.burningScale, burn.remaining, target);
                if (burn.accumulated >= Mathf.Max(0.05f, data.burnTick) || burn.remaining <= 0)
                { manager.Combat.ManualDamage(target, data.burnDps * burn.accumulated, burn.source); burn.accumulated = 0; }
                if (burn.remaining <= 0) Remove(target);
            }
            Show();
        }
        private void Remove(Transform target)
        { if (burning[target].visual != null) Object.Destroy(burning[target].visual); burning.Remove(target); }
        private void Show() => manager.PublishStatus($"점화 {burning.Count}명 · 궁수 명중 시 확산");
        public override void OnBattleEnd()
        {
            manager.Combat.OnManualEligibleHit -= Hit;
            foreach (var burn in burning.Values) if (burn.visual != null) Object.Destroy(burn.visual);
            burning.Clear();
        }
    }

    public sealed class WallBreakManual : TacticalManualRuntime
    {
        private float nextShield;
        private readonly HashSet<UnitHealth> recipients = new HashSet<UnitHealth>();
        public WallBreakManual(TacticalManualEffect data, ManualManager manager) : base(data, manager) { }
        public override void OnBattleStart() { manager.Combat.OnManualEligibleHit += Hit; Grant(); }
        private void Grant()
        {
            nextShield = clock + Mathf.Max(0.1f, data.shieldInterval);
            if (!HasLiving(data.primaryType)) return;
            foreach (var unit in manager.Experience.TrackedUnits)
                if (IsType(unit, data.secondaryType) && Alive(unit))
                {
                    var health = unit.GetComponent<UnitHealth>(); recipients.Add(health);
                    health.GiveManualShield(this, data.cavalryShield, data.shieldDuration);
                    manager.PlayVisual(data.burstEffect, unit.position, data.burstScale * 0.4f, data.visualLifetime, unit);
                }
        }
        private void Hit(CombatController.ManualHit hit)
        {
            if (!hit.ChargeImpact || !IsType(hit.Source, data.secondaryType) || !Alive(hit.Source)) return;
            float used = hit.Source.GetComponent<UnitHealth>().ConsumeShield(data.cavalryShield);
            if (used <= 0) return;
            manager.Combat.ManualAreaDamage(hit.Position, data.shockRadius, used * data.damagePerShield, hit.Source);
            Burst("성벽 돌파!", hit.Position);
        }
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime); if (clock >= nextShield) Grant();
            manager.PublishStatus(HasLiving(data.primaryType) ? $"기마병 보호막 보급 {Mathf.Max(0, nextShield-clock):0.0}초" : "방패병 전투불능 · 보호막 보급 중단",
                0, 0, Mathf.Max(0, nextShield-clock), data.shieldInterval);
        }
        public override void OnBattleEnd()
        {
            manager.Combat.OnManualEligibleHit -= Hit;
            foreach (var health in recipients) if (health != null) health.RemoveManualShield(this);
            recipients.Clear();
        }
    }

    public sealed class RelayVanguardManual : TacticalManualRuntime
    {
        private struct LastHit { public bool primary; public float at, readyAt; }
        private readonly Dictionary<Transform, LastHit> history = new Dictionary<Transform, LastHit>();
        private int activations;
        public RelayVanguardManual(TacticalManualEffect data, ManualManager manager) : base(data, manager) { }
        public override void OnBattleStart() { manager.Combat.OnManualEligibleHit += Hit; Show(); }
        private void Hit(CombatController.ManualHit hit)
        {
            bool primary = IsType(hit.Source, data.primaryType);
            if ((!primary && !IsType(hit.Source, data.secondaryType)) || hit.Killed) return;
            history.TryGetValue(hit.Target, out var previous);
            bool trigger = history.ContainsKey(hit.Target) && previous.primary != primary &&
                clock - previous.at <= data.relayWindow && clock >= previous.readyAt;
            history[hit.Target] = new LastHit { primary = primary, at = clock,
                readyAt = trigger ? clock + data.relayCooldown : previous.readyAt };
            if (!trigger) return;
            manager.Combat.ManualDamage(hit.Target, hit.Damage * data.relayDamageRatio, hit.Source);
            activations++; Burst("교대 선봉!", hit.Position); Show();
        }
        private void Show() => manager.PublishStatus($"교대 연계 {activations}회 · 병사 / 방패병");
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            foreach (var target in new List<Transform>(history.Keys))
                if (!manager.Combat.ManualEnemyAlive(target) || clock-history[target].at > Mathf.Max(data.relayWindow, data.relayCooldown)) history.Remove(target);
        }
        public override void OnBattleEnd() { manager.Combat.OnManualEligibleHit -= Hit; history.Clear(); }
    }

    public sealed class PursuitManual : TacticalManualRuntime
    {
        private readonly Dictionary<Transform, float> empowered = new Dictionary<Transform, float>();
        public PursuitManual(TacticalManualEffect data, ManualManager manager) : base(data, manager) { }
        public override void OnBattleStart() { manager.Combat.OnManualEligibleHit += Hit; Show(); }
        private void Hit(CombatController.ManualHit hit)
        {
            if (IsType(hit.Source, data.primaryType) && !hit.Killed) manager.Combat.ManualSuppress(hit.Target, data.suppressionSeconds);
            if (!hit.ChargeImpact || !IsType(hit.Source, data.secondaryType)) return;
            empowered.Remove(hit.Source);
            if (hit.Killed && hit.WasSuppressed)
            {
                manager.Combat.SetManualCharge(this, hit.Source, data.nextChargeBonus, data.chargeBonusDuration);
                empowered[hit.Source] = clock + data.chargeBonusDuration;
                Burst("추격 명령!", hit.Source.position, hit.Source);
            }
            Show();
        }
        private void Show() => manager.PublishStatus($"추격 준비 {empowered.Count}명 · 제압된 적 돌진 처치");
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            foreach (var unit in new List<Transform>(empowered.Keys))
                if (!Alive(unit) || clock >= empowered[unit]) empowered.Remove(unit);
            Show();
        }
        public override void OnBattleEnd() { manager.Combat.OnManualEligibleHit -= Hit; manager.Combat.RemoveManualCharges(this); empowered.Clear(); }
    }

    public sealed class FormationCounterManual : TacticalManualRuntime
    {
        private float stored;
        private readonly List<UnitHealth> subscribed = new List<UnitHealth>();
        public FormationCounterManual(TacticalManualEffect data, ManualManager manager) : base(data, manager) { }
        public override void OnBattleStart()
        {
            manager.Run.OnFormationChanged += Formation;
            Subscribe(manager.Run.Commander);
            foreach (var unit in manager.Experience.TrackedUnits) Subscribe(unit);
            Show();
        }
        private void Subscribe(Transform unit)
        {
            if (unit != null && unit.TryGetComponent<UnitHealth>(out var health))
            { subscribed.Add(health); health.OnDamageTaken += Damage; }
        }
        private void Damage(UnitHealth health, float lost, float absorbed)
        {
            if (manager.Run.TacticIndex != data.compactFormation || !HasLiving(data.primaryType)) return;
            stored = Mathf.Min(data.storedDamageCap, stored + lost + absorbed); Show();
        }
        private void Formation(int previous, int current)
        {
            if (current != data.spreadFormation || stored <= 0 || !HasLiving(data.primaryType) || manager.Run.CommanderDead) return;
            float damage = stored * data.counterDamageMultiplier; stored = 0;
            manager.Combat.ManualAreaDamage(manager.Run.Commander.position, data.counterRadius, damage, manager.Run.Commander);
            Burst("진형 반격!", manager.Run.Commander.position); Show();
        }
        private void Show() => manager.PublishStatus($"밀집 축적 {stored:0}/{data.storedDamageCap:0} · 산개로 반격", 0, 0, stored, data.storedDamageCap);
        public override void OnBattleEnd()
        {
            manager.Run.OnFormationChanged -= Formation;
            foreach (var health in subscribed) if (health != null) health.OnDamageTaken -= Damage;
            subscribed.Clear(); stored = 0;
        }
    }

    public sealed class FocusedEducationManual : TacticalManualRuntime
    {
        private Transform last, empowered;
        private int count;
        private float remaining;
        public FocusedEducationManual(TacticalManualEffect data, ManualManager manager) : base(data, manager) { }
        public override void OnBattleStart() { manager.Experience.OnExpBottleInvested += Invest; Show(); }
        private void Invest(Transform unit)
        {
            count = last == unit ? count + 1 : 1; last = unit;
            if (count >= Mathf.Max(1, data.consecutiveInvestments))
            {
                empowered = unit; remaining = Mathf.Max(0.1f, data.focusDuration); count = 0; last = null;
                manager.Combat.SetManualBuff(this, data.focusDamageBonus, data.focusSpeedBonus, unit);
                Burst("집중 교육!", unit.position, unit);
            }
            Show();
        }
        private void Show()
        {
            var ids = new List<int>(); if (last != null && count > 0) ids.Add(last.GetInstanceID());
            manager.PublishStatus(remaining > 0 ? $"선봉 강화 {remaining:0.0}초 · 연속 투자 {count}/{data.consecutiveInvestments}"
                : $"같은 병사 연속 투자 {count}/{data.consecutiveInvestments}", count, data.consecutiveInvestments, remaining, data.focusDuration);
            manager.MarkPortraits(ids);
        }
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (remaining <= 0) return;
            remaining = Mathf.Max(0, remaining - deltaTime);
            if (empowered == null || (empowered.TryGetComponent<UnitHealth>(out var hp) && hp.IsDead)) remaining = 0;
            if (remaining <= 0) manager.Combat.RemoveManualBuff(this);
            Show();
        }
        public override void OnBattleEnd() { manager.Experience.OnExpBottleInvested -= Invest; manager.Combat.RemoveManualBuff(this); last = null; empowered = null; remaining = 0; }
    }
}
