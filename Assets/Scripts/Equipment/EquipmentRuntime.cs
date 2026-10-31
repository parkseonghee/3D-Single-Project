using System.Collections.Generic;
using ArmySurvivor.Army;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArmySurvivor.Tutorial;

namespace ArmySurvivor.Equipment
{
    public sealed class EquipmentRuntime : MonoBehaviour
    {
        readonly List<Transform> drops = new List<Transform>();
        readonly Dictionary<Transform, float> pickupReadyAt = new Dictionary<Transform, float>();
        EquipmentCatalog catalog;
        RunController run;
        CombatController combat;
        EquipmentUI ui;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
        }
        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "VillageScene" && scene.name != "PlayScene") return;
            foreach (var root in scene.GetRootGameObjects()) if (root.GetComponent<EquipmentRuntime>() != null) return;
            var rootObject = new GameObject("Equipment System");
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            rootObject.AddComponent<EquipmentRuntime>();
        }
        void Start()
        {
            catalog = Resources.Load<EquipmentCatalog>("EquipmentCatalog");
            if (catalog == null) { Debug.LogError("EquipmentCatalog is missing.", this); enabled = false; return; }
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (run == null) run = root.GetComponentInChildren<RunController>(true);
                if (combat == null) combat = root.GetComponentInChildren<CombatController>(true);
            }
            ui = gameObject.AddComponent<EquipmentUI>();
            ui.Initialize(catalog, run);
            if (combat != null) combat.EnemyKilled += Drop;
            if (run != null) { run.RunStarted += ClearDrops; run.RunEnded += ClearDrops; }
        }
        void Drop(Vector3 position)
        {
            if (run == null || !run.IsRunning || catalog.ticketPrefab == null || Random.value >= catalog.ticketDropChance) return;
            var item = Instantiate(catalog.ticketPrefab, transform);
            item.name = "Draw Ticket";
            // Experience bottles use the enemy's death position; keep the seal visible beside them.
            Vector3 fromCommander = position - run.Commander.position;
            fromCommander.y = 0;
            Vector3 side = fromCommander.sqrMagnitude > 0.001f
                ? Vector3.Cross(Vector3.up, fromCommander.normalized) : Vector3.right;
            Vector3 ticketPosition = position + side * catalog.ticketSpawnSeparation;
            item.transform.position = new Vector3(ticketPosition.x, run.Ground.bounds.max.y + 0.7f, ticketPosition.z);
            var target = item.GetComponent<TutorialTarget>();
            if (target == null) target = item.AddComponent<TutorialTarget>();
            target.SetId("DrawTicket");
            drops.Add(item.transform);
            pickupReadyAt[item.transform] = Time.time + catalog.ticketPickupDelay;
            TutorialEvents.Raise("TicketDropped", item.transform);
        }
        void Update()
        {
            if (run == null || !run.IsRunning || run.IsPaused || run.IsChoosingUpgrade || run.CommanderDead) return;
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var item = drops[i];
                if (item == null) { if (!ReferenceEquals(item, null)) pickupReadyAt.Remove(item); drops.RemoveAt(i); continue; }
                var target = run.Commander.position; target.y = item.position.y;
                float distance = Vector3.Distance(item.position, target);
                if (distance <= catalog.pickupRadius && Time.time >= pickupReadyAt[item])
                {
                    EquipmentInventory.AddTickets(1);
                    TutorialEvents.Raise("TicketCollected", item);
                    pickupReadyAt.Remove(item);
                    Destroy(item.gameObject); drops.RemoveAt(i); continue;
                }
                if (distance < catalog.attractionRadius)
                    item.position = Vector3.MoveTowards(item.position, target, catalog.attractionSpeed * Time.deltaTime);
                item.Rotate(0, 90 * Time.deltaTime, 0, Space.World);
            }
        }
        void ClearDrops() { foreach (var item in drops) if (item != null) Destroy(item.gameObject); drops.Clear(); pickupReadyAt.Clear(); }
#if UNITY_EDITOR
        [ContextMenu("Testing/Grant 10 Tickets (Play Mode)")]
        void GrantTestTickets() { if (Application.isPlaying) EquipmentInventory.AddTickets(10); }
#endif
        void OnDestroy()
        {
            if (combat != null) combat.EnemyKilled -= Drop;
            if (run != null) { run.RunStarted -= ClearDrops; run.RunEnded -= ClearDrops; }
            ClearDrops();
        }
    }
}
