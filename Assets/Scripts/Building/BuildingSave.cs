using UnityEngine;
using ArmySurvivor.Saving;

namespace ArmySurvivor.Building
{
    public partial class BuildingPlacementController
    {
        public void CaptureSave(SaveData data, GameSave catalog)
        {
            data.rice = Rice; data.gold = Gold; data.special = SpecialResource;
            foreach (var pair in placedBuildings)
                data.buildings.Add(new BuildingRecord { id = catalog.Id(pair.Value.definition),
                    position = pair.Key.transform.position, level = pair.Value.level, starting = pair.Value.isStarting });
            // 철거한 건물도 누적 구매 횟수에는 남으므로 별도로 기록한다.
            foreach (var pair in buildCounts)
                data.purchases.Add(new CountRecord { id = catalog.Id(pair.Key), count = pair.Value });
        }

        public void RestoreSave(SaveData data, GameSave catalog)
        {
            Cancel();
            foreach (var pair in placedBuildings) { pair.Key.SetActive(false); Destroy(pair.Key); }
            placedBuildings.Clear(); occupiedCells.Clear(); buildCounts.Clear();
            foreach (var record in data.buildings)
            {
                var definition = catalog.Resolve<BuildingDefinition>(record.id);
                var root = new GameObject(definition.displayName);
                root.transform.SetParent(buildingsRoot, false);
                root.transform.position = record.position;
                var model = Instantiate(definition.prefab, root.transform);
                model.transform.localPosition = definition.modelOffset;
                model.transform.localRotation = Quaternion.Euler(definition.modelRotation);
                model.transform.localScale = definition.modelScale;
                var box = root.AddComponent<BoxCollider>();
                box.size = definition.footprint;
                box.center = Vector3.up * definition.footprint.y * 0.5f;
                placedBuildings[root] = new BuildingState { definition = definition, level = record.level, isStarting = record.starting };
                Occupy(GetArea(definition, record.position));
            }
            foreach (var record in data.purchases) buildCounts[catalog.Resolve<BuildingDefinition>(record.id)] = record.count;
            Rice = data.rice; Gold = data.gold; SpecialResource = data.special;
        }
    }

    public partial class RiceHarvest
    {
        public void CaptureSave(SaveData data)
        {
            data.day = Day; data.dailyTotal = DailyTotal; data.remainingCount = RemainingCount;
            data.remainingRice = RemainingRice; data.portion = portion;
        }
        public void RestoreSave(SaveData data)
        {
            Day = data.day; DailyTotal = data.dailyTotal; RemainingCount = data.remainingCount;
            RemainingRice = data.remainingRice; portion = data.portion;
            Refresh();
        }
    }
}
