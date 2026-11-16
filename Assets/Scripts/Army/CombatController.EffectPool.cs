using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    // 피격 이펙트·투사체처럼 전투 중 초당 수십 번 생기는 오브젝트를 매번 만들고 지우지 않고 재사용한다.
    // 몬스터가 많을 때 생성/파괴와 메모리 정리로 생기는 순간 끊김을 줄인다.
    public partial class CombatController
    {
        private sealed class PooledEffect
        {
            public GameObject prefab;
            public Vector3 scale;
            public ParticleSystem[] particles;
            public TrailRenderer[] trails;
        }
        private struct TimedEffect { public GameObject effect; public float until; }

        private readonly Dictionary<GameObject, Stack<GameObject>> effectPool = new Dictionary<GameObject, Stack<GameObject>>();
        private readonly Dictionary<GameObject, PooledEffect> effectInfo = new Dictionary<GameObject, PooledEffect>();
        private readonly List<TimedEffect> timedEffects = new List<TimedEffect>();
        private Transform effectPoolRoot;

        private Transform EffectPoolRoot
        {
            get
            {
                if (effectPoolRoot == null)
                {
                    effectPoolRoot = new GameObject("Effect Pool").transform;
                    effectPoolRoot.SetParent(transform, false);
                }
                return effectPoolRoot;
            }
        }

        // Instantiate(prefab, position, rotation, combatRoot)와 같다. 재사용한 오브젝트는 크기·파티클·궤적을 처음 상태로 되돌린다.
        private GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject effect = null;
            if (effectPool.TryGetValue(prefab, out var inactive))
                while (inactive.Count > 0 && effect == null) effect = inactive.Pop();
            if (effect == null)
            {
                effect = Instantiate(prefab, position, rotation, combatRoot);
                effectInfo[effect] = new PooledEffect
                {
                    prefab = prefab,
                    scale = effect.transform.localScale,
                    particles = effect.GetComponentsInChildren<ParticleSystem>(true),
                    trails = effect.GetComponentsInChildren<TrailRenderer>(true)
                };
                return effect;
            }
            var info = effectInfo[effect];
            effect.transform.SetParent(combatRoot, false);
            effect.transform.SetPositionAndRotation(position, rotation);
            effect.transform.localScale = info.scale;
            effect.SetActive(true);
            foreach (var trail in info.trails) trail.Clear();
            foreach (var particle in info.particles)
            {
                particle.Clear(false);
                if (particle.main.playOnAwake) particle.Play(false);
            }
            return effect;
        }

        // Destroy(effect) 대신 쓴다. 풀에서 만든 것이 아니면 그대로 파괴한다.
        private void ReleaseEffect(GameObject effect)
        {
            if (effect == null) return;
            if (!effectInfo.TryGetValue(effect, out var info)) { effect.SetActive(false); Destroy(effect); return; }
            effect.SetActive(false);
            effect.transform.SetParent(EffectPoolRoot, false);
            if (!effectPool.TryGetValue(info.prefab, out var inactive))
                effectPool[info.prefab] = inactive = new Stack<GameObject>();
            inactive.Push(effect);
        }

        // Destroy(effect, seconds) 대신 쓴다.
        private void ReleaseEffectAfter(GameObject effect, float seconds)
        {
            if (effect == null) return;
            timedEffects.Add(new TimedEffect { effect = effect, until = Time.time + seconds });
        }

        private void UpdateTimedEffects()
        {
            float now = Time.time;
            for (int i = timedEffects.Count - 1; i >= 0; i--)
            {
                if (timedEffects[i].until > now) continue;
                ReleaseEffect(timedEffects[i].effect);
                int last = timedEffects.Count - 1;
                timedEffects[i] = timedEffects[last];
                timedEffects.RemoveAt(last);
            }
        }

        // 전투 정리 때 combatRoot가 통째로 파괴되기 전에 쓰던 이펙트를 풀로 되돌린다.
        private void ReleaseAllEffects()
        {
            foreach (var timed in timedEffects) ReleaseEffect(timed.effect);
            timedEffects.Clear();
            foreach (var shot in shots) if (shot.visual != null) ReleaseEffect(shot.visual.gameObject);
            foreach (var shot in enemyProjectiles) if (shot.visual != null) ReleaseEffect(shot.visual.gameObject);
        }
    }
}
