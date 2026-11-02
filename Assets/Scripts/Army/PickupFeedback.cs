using UnityEngine;

namespace ArmySurvivor.Army
{
    // Two persistent emitters per battle, regardless of how many items are collected.
    [DisallowMultipleComponent]
    public sealed class PickupFeedback : MonoBehaviour
    {
        [SerializeField] private GameObject experiencePrefab;
        [SerializeField] private GameObject sealPrefab;
        [SerializeField] private Vector3 offset = new Vector3(0, .8f, 0);
        [SerializeField, Min(.01f)] private float size = .65f;
        [SerializeField, Min(.01f)] private float lifetime = .45f;
        [SerializeField, Min(.01f)] private float minimumInterval = .08f;
        [SerializeField, Range(1, 32)] private int experienceParticles = 12;
        [SerializeField, Range(1, 32)] private int sealParticles = 20;
        private RunController run;
        private Transform effectRoot;
        private ParticleSystem experience, seal;
        private float nextExperience, nextSeal;

        public static PickupFeedback Prepare(RunController owner)
        {
            var feedback = owner.GetComponent<PickupFeedback>();
            if (feedback == null) feedback = owner.gameObject.AddComponent<PickupFeedback>();
            feedback.Initialize();
            return feedback;
        }

        private void Awake() { run = GetComponent<RunController>(); }
        private void OnEnable()
        {
            if (run == null) return;
            run.RunStarted += ResetEffects;
            run.RunEnded += ResetEffects;
        }
        private void Initialize()
        {
            if (effectRoot != null) return;
            effectRoot = new GameObject("Pickup Feedback (reused)").transform;
            effectRoot.SetParent(transform, false);
            effectRoot.gameObject.SetActive(false);
            experience = Create(experiencePrefab, "PickupEffects/ExperiencePickup");
            seal = Create(sealPrefab, "PickupEffects/SealPickup");
            effectRoot.gameObject.SetActive(true);
        }
        private ParticleSystem Create(GameObject prefab, string resource)
        {
            if (prefab == null) prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) { Debug.LogWarning("Missing pickup effect: " + resource, this); return null; }
            var instance = Instantiate(prefab, effectRoot);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localScale = Vector3.one * size;
            var particles = instance.GetComponent<ParticleSystem>();
            if (particles == null) { Destroy(instance); return null; }
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startLifetime = lifetime;
            main.startSpeed = 2.5f;
            main.maxParticles = 64;
            var emission = particles.emission;
            emission.enabled = false; // Emit only on actual successful collection.
            var collision = particles.collision; collision.enabled = false;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }
        public void ExperienceCollected() { Emit(experience, experienceParticles, ref nextExperience); }
        public void SealCollected() { Emit(seal, sealParticles, ref nextSeal); }
        private void Emit(ParticleSystem particles, int count, ref float next)
        {
            if (particles == null || run == null || !run.IsRunning || run.Commander == null || Time.time < next) return;
            next = Time.time + minimumInterval;
            effectRoot.position = run.Commander.position + offset;
            if (!particles.isPlaying) particles.Play(false);
            particles.Emit(count);
        }
        private void LateUpdate()
        {
            if (effectRoot == null || run == null || run.Commander == null) return;
            effectRoot.position = run.Commander.position + offset;
            float speed = run.IsPaused || run.IsChoosingUpgrade ? 0 : 1;
            if (experience != null) { var main = experience.main; main.simulationSpeed = speed; }
            if (seal != null) { var main = seal.main; main.simulationSpeed = speed; }
        }
        private void ResetEffects()
        {
            if (experience != null) experience.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (seal != null) seal.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            nextExperience = nextSeal = 0;
        }
        private void OnDisable()
        {
            if (run != null) { run.RunStarted -= ResetEffects; run.RunEnded -= ResetEffects; }
            ResetEffects();
        }
        private void OnDestroy() { if (effectRoot != null) Destroy(effectRoot.gameObject); }
    }
}
