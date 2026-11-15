using UnityEngine;
using ArmySurvivor.Saving;

namespace ArmySurvivor.Army
{
    public partial class RecruitmentController
    {
        public void CaptureSave(SaveData data, GameSave catalog)
        {
            // Hierarchy 순서가 배치 순서이므로 Dictionary 열거 순서에 의존하지 않는다.
            foreach (Transform unit in soldiersRoot)
                if (Units.TryGetValue(unit, out var definition))
                    data.troops.Add(new TroopRecord { id = catalog.Id(definition), veteranSkills = VeteranKeys(unit) });
            foreach (var unit in reserves)
                data.troops.Add(new TroopRecord { id = catalog.Id(unit.Value), reserve = true, veteranSkills = VeteranKeys(unit.Key) });
        }

        private static System.Collections.Generic.List<string> VeteranKeys(Transform unit)
        {
            var record = VeteranRecord.For(unit, false);
            return record != null ? new System.Collections.Generic.List<string>(record.Skills)
                : new System.Collections.Generic.List<string>();
        }

        private static void RestoreVeteran(Transform unit, TroopRecord record)
        {
            if (record.veteranSkills == null || record.veteranSkills.Count == 0) return;
            var veteran = VeteranRecord.For(unit, true);
            // 알 수 없는 키(삭제된 스킬)는 버린다.
            foreach (string key in record.veteranSkills)
                if (BattleExperience.SkillChoice(key) >= 0) veteran.Add(key);
        }

        private void RestoreSavedTroops()
        {
            var saved = GameSave.TakeTroops();
            if (saved == null) return;
            var catalog = FindFirstObjectByType<GameSave>(FindObjectsInactive.Include);
            foreach (var record in saved)
            {
                var definition = catalog.Resolve<UnitDefinition>(record.id);
                var root = new GameObject(definition.name).transform;
                bool standby = record.reserve || hiredCount >= FormationCap;
                if (standby && reserveRoot == null)
                {
                    reserveRoot = new GameObject("Reserve Troops").transform;
                    reserveRoot.SetParent(transform, false);
                }
                root.SetParent(standby ? reserveRoot : soldiersRoot, false);
                var model = Instantiate(definition.prefab, root);
                model.transform.localPosition = definition.modelOffset;
                model.transform.localRotation = Quaternion.Euler(definition.modelRotation);
                model.transform.localScale = definition.modelScale;
                foreach (var animator in model.GetComponentsInChildren<Animator>()) animator.applyRootMotion = false;
                RestoreVeteran(root, record);
                if (standby) { reserves.Add(root, definition); root.gameObject.SetActive(false); }
                else
                {
                    var point = spawnPoints[hiredCount++];
                    root.SetPositionAndRotation(point.position, point.rotation);
                    Units.Add(root, definition);
                }
            }
        }
    }
}
