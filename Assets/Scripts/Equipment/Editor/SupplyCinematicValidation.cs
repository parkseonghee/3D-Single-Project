using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArmySurvivor.Equipment.Editor
{
    public static class SupplyCinematicValidation
    {
        [InitializeOnLoadMethod]
        static void Queue()
        {
            if (!SessionState.GetBool("SupplyCinematicValidated", false))
                EditorApplication.delayCall += Validate;
        }

        [MenuItem("Army Survivor/Equipment/Validate Supply Cinematic")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var data = AssetDatabase.LoadAssetAtPath<EquipmentCatalog>("Assets/Resources/EquipmentCatalog.asset");
            if (data == null || data.supplyFirework == null || data.supplyGateOpen == null)
                throw new InvalidOperationException("Supply cinematic prefab/clip reference missing.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonyTinyPeople/TT_RTS/TT_RTS_Standard/models/buildings/Wall_A_gate.FBX");
            var clone = UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var paths = AnimationUtility.GetCurveBindings(data.supplyGateOpen).Select(x => x.path).Distinct().ToArray();
                foreach (var path in paths)
                    if (!string.IsNullOrEmpty(path) && clone.transform.Find(path) == null)
                        throw new InvalidOperationException("Unbound gate animation path: " + path);
                var door = clone.GetComponentsInChildren<Transform>().First(x => x.name == "Bone_door_A");
                data.supplyGateOpen.SampleAnimation(clone, 0);
                var before = door.localRotation;
                data.supplyGateOpen.SampleAnimation(clone, data.supplyGateOpen.length);
                float angle = Quaternion.Angle(before, door.localRotation);
                if (angle < 10) throw new InvalidOperationException("Gate animation did not open the door.");
                SessionState.SetBool("SupplyCinematicValidated", true);
                Debug.Log($"SUPPLY_CINEMATIC_VALIDATED: firework linked; {paths.Length} animation paths bound; door rotation {angle:F1} degrees.");
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }
    }
}
