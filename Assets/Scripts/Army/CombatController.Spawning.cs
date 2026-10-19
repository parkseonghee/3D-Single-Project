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

        private Vector3 AdventureSpawnPosition(Vector3 direction)
        {
            Vector3 center = run.Commander.position;
            Bounds bounds = run.Ground.bounds;
            var camera = run.PlayCamera;
            Vector3 best = center;
            float bestDistance = -1;
            // Try the requested front first; rotate it only when the map edge leaves no room.
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector3 heading = Quaternion.Euler(0, attempt * 45, 0) * direction;
                Vector3 candidate = center + heading * Mathf.Max(10, spawnDistance);
                for (int step = 0; step < 12; step++)
                {
                    candidate.x = Mathf.Clamp(candidate.x, bounds.min.x + boundaryMargin, bounds.max.x - boundaryMargin);
                    candidate.z = Mathf.Clamp(candidate.z, bounds.min.z + boundaryMargin, bounds.max.z - boundaryMargin);
                    candidate.y = center.y;
                    float distance = (candidate - center).sqrMagnitude;
                    Vector3 viewport = camera != null ? camera.WorldToViewportPoint(candidate) : new Vector3(-1, -1, -1);
                    bool visible = viewport.z > 0 && viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1;
                    if (!visible && distance >= 100) return candidate;
                    if (distance > bestDistance) { bestDistance = distance; best = candidate; }
                    candidate += heading * 3;
                }
            }
            return best;
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
