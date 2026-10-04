using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("기마병 진군의 나팔")]
        [SerializeField] private GameObject marchingHornPrefab;
        [SerializeField, Min(0.1f)] private float marchingHornInterval = 10f;
        [SerializeField, Min(0.1f)] private float marchingHornDuration = 5f;
        [SerializeField, Min(0)] private float marchingHornSpeedBonus = 0.5f;
        private float marchingHornCooldown = -1f;
        private float marchingHornRemaining;
        private GameObject marchingHornEffect;

        private void UpdateMarchingHorn(float dt)
        {
            if (marchingHornRemaining > 0)
            {
                marchingHornRemaining = Mathf.Max(0, marchingHornRemaining - dt);
                if (marchingHornRemaining <= 0 || run.CommanderDead) ClearMarchingHorn();
            }
            bool hasCaster = false;
            foreach (var entry in recruitment.Units)
                if (entry.Key != null && entry.Key.gameObject.activeSelf && experience != null &&
                    experience.HasMarchingHorn(entry.Key) && allies.TryGetValue(entry.Key, out var health) && !health.IsDead)
                    hasCaster = true;
            if (!hasCaster) { marchingHornCooldown = -1f; return; }
            if (marchingHornCooldown < 0) marchingHornCooldown = marchingHornInterval;
            marchingHornCooldown = Mathf.Max(0, marchingHornCooldown - dt);
            if (marchingHornCooldown > 0 || marchingHornRemaining > 0 || run.CommanderDead) return;
            marchingHornCooldown = marchingHornInterval;
            marchingHornRemaining = marchingHornDuration;
            run.TeamMoveMultiplier = 1f + marchingHornSpeedBonus;
            if (marchingHornPrefab == null) return;
            float height = 2f;
            foreach (var renderer in run.Commander.GetComponentsInChildren<Renderer>())
                if (!(renderer is ParticleSystemRenderer) && !(renderer is LineRenderer))
                    height = Mathf.Max(height, renderer.bounds.max.y - run.Commander.position.y + 0.4f);
            marchingHornEffect = Instantiate(marchingHornPrefab, run.Commander);
            marchingHornEffect.transform.position = run.Commander.position + Vector3.up * height;
            foreach (var particle in marchingHornEffect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.loop = true;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }

        private void ClearMarchingHorn()
        {
            marchingHornRemaining = 0;
            if (run != null) run.TeamMoveMultiplier = 1f;
            if (marchingHornEffect != null)
            {
                marchingHornEffect.SetActive(false);
                Destroy(marchingHornEffect);
                marchingHornEffect = null;
            }
        }
    }
}
