using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class BattleExperience
    {
        [Header("경험치 병 성능 / 경계")]
        [SerializeField, Min(8)] private int maximumVisibleBottles = 128;
        [SerializeField, Min(0)] private int prewarmBottles = 32;
        [SerializeField, Min(1)] private int creationsPerFrame = 4;
        [SerializeField, Min(.1f)] private float bottleMergeCellSize = 2;
        [SerializeField, Min(0)] private float bottleBoundaryInset = .3f;
        [SerializeField, Min(.01f)] private float bottleAnimationInterval = .0333f;
        [SerializeField, Min(1)] private float bottleAnimationDistance = 30;
        private readonly Stack<Transform> bottlePool = new Stack<Transform>();
        private readonly Dictionary<Transform, int> bottleAmounts = new Dictionary<Transform, int>();
        private readonly Dictionary<Vector2Int, Transform> bottleCells = new Dictionary<Vector2Int, Transform>();
        private readonly Dictionary<Transform, Vector2Int> dropCells = new Dictionary<Transform, Vector2Int>();
        private Transform poolRoot;
        private CommanderProgression pickupProgression;
        private PickupFeedback pickupFeedback;
        private int fieldBottleCount, creationFrame = -1, creationsThisFrame;
        private float animationElapsed;

        private void EnsureDropRoots()
        {
            if (dropRoot == null) { dropRoot = new GameObject("Experience Drops").transform; dropRoot.SetParent(transform, false); }
            if (poolRoot == null) { poolRoot = new GameObject("Experience Bottle Pool").transform; poolRoot.SetParent(transform, false); poolRoot.gameObject.SetActive(false); }
            pickupProgression = GetComponent<CommanderProgression>();
            pickupFeedback = PickupFeedback.Prepare(run);
        }
        private Transform CreatePooledBottle()
        {
            var bottle = Instantiate(bottlePrefab, poolRoot).transform;
            var target = bottle.GetComponent<ArmySurvivor.Tutorial.TutorialTarget>();
            if (target == null) target = bottle.gameObject.AddComponent<ArmySurvivor.Tutorial.TutorialTarget>();
            target.SetId("ExpBottle");
            foreach (var renderer in bottle.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            bottle.gameObject.SetActive(false);
            return bottle;
        }
        private IEnumerator WarmBottlePool()
        {
            while (run.IsRunning && bottlePool.Count + drops.Count < Mathf.Min(prewarmBottles, maximumVisibleBottles))
            {
                ResetCreationBudget();
                while (creationsThisFrame < creationsPerFrame && bottlePool.Count + drops.Count < Mathf.Min(prewarmBottles, maximumVisibleBottles))
                { bottlePool.Push(CreatePooledBottle()); creationsThisFrame++; }
                yield return null;
            }
        }
        private void ResetCreationBudget()
        {
            if (creationFrame == Time.frameCount) return;
            creationFrame = Time.frameCount; creationsThisFrame = 0;
        }
        private void SpawnPooledBottle(Vector3 position)
        {
            if (!run.IsRunning || dropRoot == null) return;
            // Drops and movement share exactly the same reachable boundary, including formation margin.
            position = run.ClampCommanderPosition(position, bottleBoundaryInset);
            position.y = bottleWorldY;
            float cellSize = Mathf.Max(.1f, bottleMergeCellSize);
            var cell = new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.z / cellSize));
            fieldBottleCount++;
            if (bottleCells.TryGetValue(cell, out var existing) && existing != null)
            { bottleAmounts[existing]++; return; }
            ResetCreationBudget();
            if (drops.Count > 0 && (drops.Count >= maximumVisibleBottles || (bottlePool.Count == 0 && creationsThisFrame >= creationsPerFrame)))
            {
                Transform nearest = drops[0]; float nearestDistance = float.MaxValue;
                foreach (var candidate in drops)
                {
                    float distance = (candidate.position-position).sqrMagnitude;
                    if (distance < nearestDistance) { nearest = candidate; nearestDistance = distance; }
                }
                // Aggregate value, never discard rewards when the visual budget is exhausted.
                bottleAmounts[nearest]++; return;
            }
            Transform bottle;
            if (bottlePool.Count > 0) bottle = bottlePool.Pop();
            else { bottle = CreatePooledBottle(); creationsThisFrame++; }
            bottle.SetPositionAndRotation(position, Quaternion.identity);
            bottle.SetParent(dropRoot, true); bottle.gameObject.SetActive(true);
            drops.Add(bottle); bottleAmounts[bottle] = 1; bottleCells[cell] = bottle; dropCells[bottle] = cell;
            ArmySurvivor.Tutorial.TutorialEvents.Raise("ExpBottleDropped", bottle);
        }
        private void AnimatePooledBottles(float dt)
        {
            if (!run.IsRunning || run.IsPaused || run.IsChoosingUpgrade || IsChoosingUpgrade || dt <= 0) return;
            animationElapsed += dt;
            if (animationElapsed < bottleAnimationInterval) return;
            float angle = bottleRotationSpeed * animationElapsed; animationElapsed = 0;
            Vector3 commander = run.Commander.position;
            float distanceSquared = bottleAnimationDistance * bottleAnimationDistance;
            foreach (var bottle in drops)
                if ((bottle.position-commander).sqrMagnitude <= distanceSquared)
                    bottle.Rotate(Vector3.up, angle, Space.World);
        }
        private void CollectPooledBottles()
        {
            if (!run.IsRunning || run.IsPaused || run.IsChoosingUpgrade || IsChoosingUpgrade) return;
            int collected = 0;
            float radius = pickupRadius * (pickupProgression != null ? pickupProgression.PickupMultiplier : 1);
            Vector3 commander = run.Commander.position;
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                Transform bottle = drops[i]; Vector3 offset = bottle.position - commander; offset.y = 0;
                if (offset.sqrMagnitude > radius * radius) continue;
                collected += bottleAmounts[bottle];
                bottleCells.Remove(dropCells[bottle]); dropCells.Remove(bottle); bottleAmounts.Remove(bottle);
                // Unordered removal avoids shifting the whole list for every pickup.
                int last = drops.Count-1; drops[i] = drops[last]; drops.RemoveAt(last);
                bottle.gameObject.SetActive(false); bottle.SetParent(poolRoot, true); bottlePool.Push(bottle);
            }
            if (collected == 0) return;
            fieldBottleCount -= collected; Bottles += collected;
            if (pickupFeedback != null) pickupFeedback.ExperienceCollected();
            OnBottlesCollected?.Invoke(collected);
            ArmySurvivor.Tutorial.TutorialEvents.Raise("ExpBottleCollected");
            RefreshUI();
        }
        private void ReleaseAllBottles()
        {
            StopAllCoroutines();
            foreach (var bottle in drops) if (bottle != null)
            { bottle.gameObject.SetActive(false); bottle.SetParent(poolRoot, true); bottlePool.Push(bottle); }
            drops.Clear(); bottleAmounts.Clear(); bottleCells.Clear(); dropCells.Clear();
            fieldBottleCount = 0; animationElapsed = 0;
        }
    }
}
