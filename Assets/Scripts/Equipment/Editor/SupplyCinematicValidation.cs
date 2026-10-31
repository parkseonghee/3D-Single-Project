using System;
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
            if (data == null || data.supplyFirework == null ||
                string.IsNullOrWhiteSpace(data.supplyDoorAPath) || string.IsNullOrWhiteSpace(data.supplyDoorBPath))
                throw new InvalidOperationException("Supply cinematic references or door paths missing.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonyTinyPeople/TT_RTS/TT_RTS_Standard/models/buildings/Wall_A_gate.FBX");
            if (prefab == null) throw new InvalidOperationException("Supply gate prefab missing.");
            var clone = UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var doorA = clone.transform.Find(data.supplyDoorAPath);
                var doorB = clone.transform.Find(data.supplyDoorBPath);
                if (doorA == null || doorB == null || doorA == doorB)
                    throw new InvalidOperationException("Supply door paths do not locate two distinct gate leaves.");
                float angleA = Quaternion.Angle(Quaternion.Euler(data.supplyDoorAClosedEuler), Quaternion.Euler(data.supplyDoorAOpenEuler));
                float angleB = Quaternion.Angle(Quaternion.Euler(data.supplyDoorBClosedEuler), Quaternion.Euler(data.supplyDoorBOpenEuler));
                if (angleA < 10 || angleB < 10)
                    throw new InvalidOperationException("Supply door closed/open rotations do not visibly differ.");
                if (data.supplyDoorStart * data.supplySequenceDuration + data.supplyDoorDuration > data.supplySequenceDuration)
                    throw new InvalidOperationException("Supply doors cannot finish opening before the cinematic ends.");
                SessionState.SetBool("SupplyCinematicValidated", true);
                Debug.Log($"SUPPLY_CINEMATIC_VALIDATED: firework linked; door paths found; door rotations {angleA:F1}/{angleB:F1} degrees.");
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }
    }
}
