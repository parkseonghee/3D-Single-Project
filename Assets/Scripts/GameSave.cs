using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ArmySurvivor.Building;
using ArmySurvivor.Army;

namespace ArmySurvivor.Saving
{
    // JSON에는 실행 중 오브젝트 참조 대신 고정 ID와 값만 기록한다.
    [Serializable] public class SaveData
    {
        public int version = 1;
        public string savedAt;
        public int rice, gold, special, day, dailyTotal, remainingCount, remainingRice, portion;
        public List<BuildingRecord> buildings = new List<BuildingRecord>();
        public List<CountRecord> purchases = new List<CountRecord>();
        public List<TroopRecord> troops = new List<TroopRecord>();
    }
    [Serializable] public class BuildingRecord { public string id; public Vector3 position; public int level; public bool starting; }
    [Serializable] public class CountRecord { public string id; public int count; }
    [Serializable] public class TroopRecord { public string id; public bool reserve; }

    public class GameSave : MonoBehaviour
    {
        [Serializable] public class Definition { public string id; public ScriptableObject asset; }
        [Serializable] public class Slot { public TMP_Text summary; public Button save; public Button load; }
        [SerializeField] private Definition[] definitions;
        [SerializeField] private Slot[] slots;
        [SerializeField] private BuildingPlacementController village;
        [SerializeField] private RiceHarvest harvest;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text status;
        [SerializeField] private string villageScene;
        [SerializeField] private string directoryName = "Saves";
        [SerializeField] private string emptyText = "빈 슬롯";
        [SerializeField] private string damagedText = "불러올 수 없는 저장";
        [SerializeField] private string summaryFormat = "Day {0}  ·  벼 {1}  ·  골드 {2}\n{3}";
        [SerializeField] private string savedText = "저장했습니다.";
        [SerializeField] private string overwriteText = "같은 슬롯의 저장 버튼을 다시 누르면 덮어씁니다.";
        [SerializeField] private string failureText = "저장 파일을 처리하지 못했습니다.";
        private int confirmSlot = -1;
        private bool loading;
        private static SaveData pending;
        private static List<TroopRecord> pendingTroops;
        public static void ClearPending() { pending = null; pendingTroops = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ClearPending();
        public string Id(ScriptableObject asset)
        {
            foreach (var entry in definitions) if (entry.asset == asset) return entry.id;
            throw new InvalidDataException("저장 정의가 등록되지 않았습니다.");
        }
        public T Resolve<T>(string id) where T : ScriptableObject
        {
            foreach (var entry in definitions) if (entry.id == id && entry.asset is T value) return value;
            throw new InvalidDataException("저장 정의를 찾을 수 없습니다.");
        }
        private IEnumerator Start()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                slots[i].save.onClick.AddListener(() => SaveSlot(index));
                slots[i].load.onClick.AddListener(() => LoadSlot(index));
            }
            // 각 게임 시스템의 Awake/Start 초기화가 끝난 뒤 복원한다.
            yield return null;
            if (village != null && pending != null)
            {
                var data = pending;
                pending = null;
                village.RestoreSave(data, this);
                harvest.RestoreSave(data);
                pendingTroops = data.troops;
            }
            RefreshSlots();
        }
        public static List<TroopRecord> TakeTroops()
        {
            var result = pendingTroops;
            pendingTroops = null;
            return result;
        }
        public void Open() { confirmSlot = -1; status.text = ""; panel.SetActive(true); RefreshSlots(); }
        public void Close() { confirmSlot = -1; panel.SetActive(false); }
        public string SlotPath(int index) => Path.Combine(Application.persistentDataPath, directoryName, "slot-" + (index + 1) + ".json");
        private SaveData Read(int index)
        {
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SlotPath(index)));
            if (data == null || data.version != 1 || data.day < 1 || data.rice < 0 || data.gold < 0 || data.special < 0 ||
                data.dailyTotal < 0 || data.remainingRice < 0 || data.remainingRice > data.dailyTotal || data.remainingCount < 0 || data.portion < 0 ||
                data.buildings == null || data.purchases == null || data.troops == null) throw new InvalidDataException();
            foreach (var b in data.buildings)
            {
                if (b == null || b.level < 1 || !float.IsFinite(b.position.x) || !float.IsFinite(b.position.y) || !float.IsFinite(b.position.z)) throw new InvalidDataException();
                if (!Resolve<BuildingDefinition>(b.id).IsValid) throw new InvalidDataException();
            }
            foreach (var p in data.purchases) { if (p == null || p.count < 0) throw new InvalidDataException(); Resolve<BuildingDefinition>(p.id); }
            foreach (var t in data.troops) { if (t == null || Resolve<UnitDefinition>(t.id).prefab == null) throw new InvalidDataException(); }
            return data;
        }
        public void RefreshSlots()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].save.interactable = village != null && !loading;
                slots[i].load.interactable = false;
                if (!File.Exists(SlotPath(i))) { slots[i].summary.text = emptyText; continue; }
                try
                {
                    var data = Read(i);
                    slots[i].summary.text = string.Format(summaryFormat, data.day, data.rice, data.gold, data.savedAt);
                    slots[i].load.interactable = !loading;
                }
                catch (Exception) { slots[i].summary.text = damagedText; }
            }
        }
        public void SaveSlot(int index)
        {
            if (loading || village == null || !village.gameObject.activeInHierarchy || index < 0 || index >= slots.Length) return;
            if (File.Exists(SlotPath(index)) && confirmSlot != index)
            { confirmSlot = index; status.text = overwriteText; return; }
            confirmSlot = -1;
            try
            {
                village.Cancel();
                var data = new SaveData { savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                village.CaptureSave(data, this);
                harvest.CaptureSave(data);
                var army = FindFirstObjectByType<RecruitmentController>(FindObjectsInactive.Include);
                if (army != null) army.CaptureSave(data, this);
                else if (pendingTroops != null) data.troops.AddRange(pendingTroops);
                var path = SlotPath(index);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(data, true));
                // 임시 파일을 완성한 뒤 교체하여 쓰기 중 실패 시 기존 저장을 보존한다.
                if (File.Exists(path)) File.Replace(path + ".tmp", path, null);
                else File.Move(path + ".tmp", path);
                status.text = savedText;
                RefreshSlots();
            }
            catch (Exception e) { status.text = failureText; Debug.LogWarning(e.Message, this); }
        }
        public void LoadSlot(int index)
        {
            if (loading || index < 0 || index >= slots.Length) return;
            try
            {
                var data = Read(index);
                if (!Application.CanStreamedLevelBeLoaded(villageScene)) throw new InvalidDataException();
                pending = data;
                pendingTroops = null;
                loading = true;
                RefreshSlots();
                SceneManager.LoadSceneAsync(villageScene, LoadSceneMode.Single);
            }
            catch (Exception e) { loading = false; status.text = failureText; Debug.LogWarning(e.Message, this); }
        }
    }
}
