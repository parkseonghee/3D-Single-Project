using System.Linq;
using ArmySurvivor.Building;
using ArmySurvivor.Saving;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArmySurvivor.Equipment.Editor
{
    public static class EquipmentSetup
    {
        const string Root = "Assets/EquipmentData";
        const string Weapons = "Assets/ToonyTinyPeople/TT_RTS/TT_RTS_Standard/models/extras/weapons/";
        const string FrostbiteFolder = "Assets/Art/Meshy/Weapons/FrostbiteDagger";
        const string FrostbiteFile = "Meshy_AI_Frostbite_Dagger_20251222_texture.fbx";

        // Resolve Unity's imported main-object ID instead of guessing an FBX fileID in YAML.
        [InitializeOnLoadMethod]
        static void QueueFrostbiteModelLink() => EditorApplication.delayCall += LinkFrostbiteModel;

        static void LinkFrostbiteModel()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            var item = AssetDatabase.LoadAssetAtPath<EquipmentItem>(Root + "/frostbite-dagger.asset");
            if (item == null || item.model != null) return;
            const string source = "Assets/Meshy_AI_Frostbite_Dagger_20251222_texture_fbx";
            if (AssetDatabase.IsValidFolder(source) && !AssetDatabase.IsValidFolder(FrostbiteFolder))
            {
                string error = AssetDatabase.MoveAsset(source, FrostbiteFolder);
                if (!string.IsNullOrEmpty(error)) { Debug.LogError(error); return; }
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FrostbiteFolder + "/" + FrostbiteFile);
            if (model == null) return;
            item.model = model;
            item.previewRotation = model.transform.localEulerAngles;
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
            var catalog = AssetDatabase.LoadAssetAtPath<EquipmentCatalog>("Assets/Resources/EquipmentCatalog.asset");
            Debug.Log("Frostbite equipment model linked. Catalog valid: " + (catalog != null && catalog.IsValid));
        }
        [MenuItem("Army Survivor/Equipment/Install Supply and Equipment")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
            System.IO.Directory.CreateDirectory(Root);
            System.IO.Directory.CreateDirectory("Assets/Resources");
            var catalog = LoadOrCreate<EquipmentCatalog>("Assets/Resources/EquipmentCatalog.asset");
            if (catalog.items == null || catalog.items.Length == 0)
            {
                catalog.sRate = 0.006f;
                catalog.items = new[] {
                    Item("training-blade", "훈련용 검", ItemGrade.C, "w_short_sword.FBX", "신병에게 지급하는 소박한 훈련용 검."),
                    Item("guard-shield", "수비대 방패", ItemGrade.C, "shield_14.FBX", "왕국 수비대의 전투 흔적이 남은 방패."),
                    Item("sage-staff", "현자의 지팡이", ItemGrade.B, "w_staff_D.FBX", "푸른 마력이 깃든 현자의 의장용 지팡이."),
                    Item("royal-blade", "왕실의 대검", ItemGrade.B, "w_TH_claymore.FBX", "왕국의 뛰어난 지휘관에게 수여하던 대검."),
                    Item("aetherstorm-blade", "에테르스톰 블레이드", ItemGrade.A, "Assets/Art/Meshy/Weapons/AetherstormBlade/Meshy_AI_Aetherstorm_Blade_20260701_texture.fbx", "폭풍의 힘을 품은 검. 장비 효과는 추후 구현됩니다."),
                    Item("ornate-magic-wand", "화려한 마법 지팡이", ItemGrade.A, "Assets/Art/Meshy/Weapons/OrnateMagicWand/Meshy_AI_Fantasy_magic_wand_3D_ornate_metallic_20251026_texture.fbx", "금속 장식으로 빛나는 마법 지팡이. 장비 효과는 추후 구현됩니다."),
                    Item("void-shield", "공허의 방패", ItemGrade.A, "Assets/Art/Meshy/Weapons/VoidShield/Meshy_AI_Void_Shield_20260911_texture.fbx", "공허의 기운을 품은 방패. 장비 효과는 추후 구현됩니다."),
                    Item("frostbite-dagger", "프로스트바이트 단검", ItemGrade.S, FrostbiteFolder + "/" + FrostbiteFile, "서리의 기운을 품은 단검. 장비 효과는 추후 구현됩니다.") };
            }
            catalog.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Village Korean SDF.asset");
            if (catalog.supplyBuilding == null)
            {
                var building = LoadOrCreate<BuildingDefinition>(Root+"/SupplyDepot.asset");
                building.displayName = "왕국 보급소"; building.riceCost = 80;
                building.prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonyTinyPeople/TT_RTS/TT_RTS_Standard/models/buildings/Market.FBX");
                building.modelRotation = building.prefab.transform.localEulerAngles;
                building.modelScale = Vector3.one;
                building.footprint = new Vector3(6, 4, 6);
                EditorUtility.SetDirty(building); catalog.supplyBuilding = building;
            }
            if (catalog.ticketPrefab == null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", new Color(1f,.7f,.12f));
                var existing = AssetDatabase.LoadAssetAtPath<Material>(Root+"/Ticket Gold.mat");
                if (existing == null) AssetDatabase.CreateAsset(material,Root+"/Ticket Gold.mat");
                else { Object.DestroyImmediate(material); material=existing; }
                var ticket = GameObject.CreatePrimitive(PrimitiveType.Cube); ticket.name="Draw Ticket Placeholder";
                Object.DestroyImmediate(ticket.GetComponent<Collider>());
                ticket.transform.localScale=new Vector3(.55f,.12f,.8f);
                ticket.GetComponent<Renderer>().sharedMaterial=material;
                catalog.ticketPrefab=PrefabUtility.SaveAsPrefabAsset(ticket,Root+"/Ticket.prefab");
                Object.DestroyImmediate(ticket);
            }
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers=tags.FindProperty("layers"); int layer=LayerMask.NameToLayer("ItemInspect");
            if(layer<0)
            {
                for(int i=31;i>=8;i--) if(string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                { layer=i;layers.GetArrayElementAtIndex(i).stringValue="ItemInspect";break; }
                if(layer<0) throw new System.InvalidOperationException("No free layer for ItemInspect.");
                tags.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            // Supply definition must be registered in both menus that validate saved building IDs.
            foreach(string sceneName in new[]{"VillageScene","StartScene"})
            {
                string path="Assets/Scenes/"+sceneName+".unity";
                var scene=SceneManager.GetSceneByPath(path); bool opened=!scene.isLoaded;
                if(opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                foreach(var root in scene.GetRootGameObjects())
                {
                    foreach(var save in root.GetComponentsInChildren<GameSave>(true)) Register(save,catalog.supplyBuilding);
                    foreach(var palette in root.GetComponentsInChildren<BuildingPalette>(true)) RegisterPalette(palette,catalog.supplyBuilding);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                if(opened) EditorSceneManager.CloseScene(scene,true);
            }
        }
        static EquipmentItem Item(string id,string name,ItemGrade grade,string file,string description)
        {
            var item=LoadOrCreate<EquipmentItem>(Root+"/"+id+".asset");
            item.id=id;item.displayName=name;item.grade=grade;item.description=description;
            item.model=AssetDatabase.LoadAssetAtPath<GameObject>(file.StartsWith("Assets/") ? file : Weapons+file);
            item.previewRotation=item.model.transform.localEulerAngles;
            EditorUtility.SetDirty(item);return item;
        }
        static T LoadOrCreate<T>(string path) where T: ScriptableObject
        {
            var value=AssetDatabase.LoadAssetAtPath<T>(path);
            if(value==null) {value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}
            return value;
        }
        static void Register(GameSave save,BuildingDefinition building)
        {
            var so=new SerializedObject(save);var entries=so.FindProperty("definitions");
            for(int i=0;i<entries.arraySize;i++)
                if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("asset").objectReferenceValue==building)return;
            int index=entries.arraySize;entries.InsertArrayElementAtIndex(index);
            entries.GetArrayElementAtIndex(index).FindPropertyRelative("id").stringValue="supply-depot";
            entries.GetArrayElementAtIndex(index).FindPropertyRelative("asset").objectReferenceValue=building;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void RegisterPalette(BuildingPalette palette,BuildingDefinition building)
        {
            var so=new SerializedObject(palette);var entries=so.FindProperty("options");
            for(int i=0;i<entries.arraySize;i++)
                if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("building").objectReferenceValue==building)return;
            var source=entries.GetArrayElementAtIndex(entries.arraySize-1);
            var button=(UnityEngine.UI.Button)source.FindPropertyRelative("button").objectReferenceValue;
            var icon=(UnityEngine.UI.Image)source.FindPropertyRelative("icon").objectReferenceValue;
            var clone=Object.Instantiate(button.gameObject,button.transform.parent); clone.name="Supply Depot Button";
            foreach(var label in clone.GetComponentsInChildren<TMP_Text>(true))
                label.text=label==clone.GetComponentsInChildren<TMP_Text>(true)[0] ? "왕국 보급소 80 벼" : "뽑기권으로 장비 보급";
            int index=entries.arraySize;entries.InsertArrayElementAtIndex(index);
            var entry=entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("building").objectReferenceValue=building;
            entry.FindPropertyRelative("button").objectReferenceValue=clone.GetComponent<UnityEngine.UI.Button>();
            entry.FindPropertyRelative("icon").objectReferenceValue=clone.GetComponentsInChildren<UnityEngine.UI.Image>(true).First(x=>x.name==icon.name);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
