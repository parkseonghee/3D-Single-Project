using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        private GameObject TakeEnemyObject(GameObject prefab, Vector3 position)
        {
            if (poolRoot == null)
            {
                poolRoot = new GameObject("Enemy Object Pool").transform;
                poolRoot.SetParent(transform, false);
            }
            if (!enemyObjects.TryGetValue(prefab, out Stack<GameObject> inactive))
                enemyObjects[prefab] = inactive = new Stack<GameObject>();
            GameObject model = null;
            while (inactive.Count > 0 && model == null) model = inactive.Pop();
            if (model == null) model = Instantiate(prefab);
            model.transform.SetParent(combatRoot, false);
            model.transform.SetPositionAndRotation(position, Quaternion.identity);
            model.transform.localScale = prefab.transform.localScale;
            model.name = prefab.name;
            model.SetActive(true);
            // Reset the temporary boss tint when the same prefab is reused as a normal enemy.
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.SetPropertyBlock(null);
            return model;
        }

        private void RecycleEnemyObject(GameObject model, GameObject prefab)
        {
            if (model == null || prefab == null || poolRoot == null) return;
            model.SetActive(false);
            model.transform.SetParent(poolRoot, false);
            if (!enemyObjects.TryGetValue(prefab, out Stack<GameObject> inactive))
                enemyObjects[prefab] = inactive = new Stack<GameObject>();
            inactive.Push(model);
        }

        private Vector3 SurvivalSpawnPosition()
        {
            Vector3 center = run.Commander.position;
            Bounds bounds = run.Ground.bounds;
            float margin = Mathf.Max(0, boundaryMargin);
            Vector3 fallback = center;
            float best = -1;
            for (int attempt = 0; attempt < survivalSettings.spawnPositionAttempts; attempt++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                // The camera viewport is tested directly, so perspective and orthographic cameras both work.
                float radius = survivalSettings.minimumSpawnRadius;
                Vector3 candidate = center + direction * radius;
                for (int step = 0; step < 8 && IsInCamera(candidate); step++)
                    candidate += direction * survivalSettings.cameraOutsideMargin;
                candidate.x = Mathf.Clamp(candidate.x, bounds.min.x + margin, bounds.max.x - margin);
                candidate.z = Mathf.Clamp(candidate.z, bounds.min.z + margin, bounds.max.z - margin);
                candidate.y = center.y;
                if (!IsInCamera(candidate)) return candidate;
                float distance = (candidate - center).sqrMagnitude;
                if (distance > best) { best = distance; fallback = candidate; }
            }
            return fallback; // At a world boundary, no off-screen ground may exist.
        }

        private bool IsInCamera(Vector3 point)
        {
            if (spawnCamera == null) return false;
            Vector3 viewport = spawnCamera.WorldToViewportPoint(point);
            return viewport.z > 0 && viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1;
        }

        private void OnDestroy()
        {
            if (poolRoot != null) Destroy(poolRoot.gameObject);
        }
    }
}
