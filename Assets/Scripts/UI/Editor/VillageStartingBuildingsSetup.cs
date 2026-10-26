#if UNITY_EDITOR
using ArmySurvivor.Building;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArmySurvivor.UI.Editor
{
    public static class VillageStartingBuildingsSetup
    {
        [MenuItem("Army Survivor/Village/Restore Starting Resource Buildings")]
        public static void Apply()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/Scenes/VillageScene.unity")
                throw new System.InvalidOperationException("Open VillageScene in Edit Mode first.");
            var controller = Object.FindFirstObjectByType<BuildingPlacementController>();
            var settings = new SerializedObject(controller);
            var parent = (Transform)settings.FindProperty("buildingsRoot").objectReferenceValue;
            var farm = AssetDatabase.LoadAssetAtPath<BuildingDefinition>("Assets/BuildingData/Farm.asset");
            var barracks = AssetDatabase.LoadAssetAtPath<BuildingDefinition>("Assets/BuildingData/Castle.asset");
            var farmRoot = FindOrCreate(parent, "Starting Farm");
            var barracksRoot = FindOrCreate(parent, "Starting Castle");

            // The large scenic castle is independent of the player's movable/savable buildings.
            var landscape = GameObject.Find("Village Castle Landscape").transform;
            var scenicCastle = barracksRoot.transform.Find("Castle");
            if (scenicCastle != null && landscape.Find("Castle") == null)
                scenicCastle.SetParent(landscape, true);

            Configure(controller, farmRoot, farm, new Vector3(-12, 0, -32));
            Configure(controller, barracksRoot, barracks, new Vector3(10, 0, -32));
            var starting = settings.FindProperty("startingBuildings");
            Register(starting, farm, farmRoot);
            Register(starting, barracks, barracksRoot);
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        static GameObject FindOrCreate(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root;
        }
        static void Configure(BuildingPlacementController controller, GameObject root, BuildingDefinition definition, Vector3 position)
        {
            root.SetActive(true);
            root.transform.position = controller.SnapPosition(definition, position);
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(definition.prefab, root.transform);
            model.transform.localPosition = definition.modelOffset;
            model.transform.localRotation = Quaternion.Euler(definition.modelRotation);
            model.transform.localScale = definition.modelScale;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            box.enabled = true; box.isTrigger = false;
            box.size = definition.footprint;
            box.center = Vector3.up * definition.footprint.y * 0.5f;
        }
        static void Register(SerializedProperty entries, BuildingDefinition definition, GameObject root)
        {
            int index = -1;
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("definition").objectReferenceValue == definition) { index = i; break; }
            if (index < 0) { index = entries.arraySize; entries.InsertArrayElementAtIndex(index); }
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("definition").objectReferenceValue = definition;
            entry.FindPropertyRelative("sceneObject").objectReferenceValue = root;
        }
    }
}
#endif
