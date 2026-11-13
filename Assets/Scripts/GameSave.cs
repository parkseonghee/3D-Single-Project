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
        public string gameMode; // absent in legacy saves => Adventure
        public int rice, gold, day, dailyTotal, remainingCount, remainingRice, portion;
        public List<BuildingRecord> buildings = new List<BuildingRecord>();
        public List<CountRecord> purchases = new List<CountRecord>();
        public List<TroopRecord> troops = new List<TroopRecord>();
        public ArmySurvivor.Equipment.EquipmentSave equipment;
        // 재도전권(기획서 8). 필드가 없는 이전 저장은 campaignId 빈 값, retryTokens -1로 읽혀 새 회차로 처리한다.
        public string campaignId;
        public int retryTokens = -1;
        public int retryTokensMax;
        // 출전 후 전투가 끝나기 전에 게임을 종료하면 true로 남는다. 이어하기 시 재도전권 1장을 쓴다.
        public bool battleInProgress;
    }
    [Serializable] public class BuildingRecord { public string id; public Vector3 position; public int level; public bool starting; }
    [Serializable] public class CountRecord { public string id; public int count; }
    // veteranSkills: BattleExperience.SkillKey 문자열. 필드가 없는 이전 저장은 빈 목록으로 읽힌다.
    [Serializable] public class TroopRecord { public string id; public bool reserve; public List<string> veteranSkills = new List<string>(); }

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
        [Header("모험 회차 · 재도전권 (기획서 8)")]
        [SerializeField, Min(0)] private int retryTokensPerCampaign = 3;
        private const string AutoSaveDirectory = "Saves";
        private const string AutoSaveFile = "autosave.json";
        private int confirmSlot = -1;
        private bool loading;
        private static SaveData pending;
        private static List<TroopRecord> pendingTroops;
        public static void ClearPending() { pending = null; pendingTroops = null; ArmySurvivor.Equipment.EquipmentInventory.Reset(); CampaignState.Clear(); }
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
                ArmySurvivor.Equipment.EquipmentInventory.Restore(data.equipment);
                if (!string.IsNullOrEmpty(data.campaignId) && data.retryTokens >= 0)
                    CampaignState.Restore(data.campaignId, data.retryTokens, Mathf.Max(data.retryTokensMax, data.retryTokens));
                else CampaignState.StartNew(retryTokensPerCampaign); // 이전 형식 저장
                // 수동 슬롯·이전 형식에서 불러온 경우에도 이어하기 기준을 이 아침으로 맞춘다.
                if (GameModeManager.Current == GameMode.Adventure) WriteAutoSave();
            }
            else if (village != null && GameModeManager.Current == GameMode.Adventure && !CampaignState.Active)
            {
                // 새 모험 회차: Day 1 아침 자동 저장.
                CampaignState.StartNew(retryTokensPerCampaign);
                WriteAutoSave();
            }
            RefreshSlots();
        }

        // ---------- 자동 저장(아침 저장) ----------
        public static string AutoSavePath =>
            Path.Combine(Application.persistentDataPath, AutoSaveDirectory, AutoSaveFile);
        public static bool HasAutoSave => File.Exists(AutoSavePath);

        // Day 진입(마을 도착) 시점의 상태를 기록한다. 보상·벼 갱신·베테랑 선택이 끝난 뒤 호출한다.
        public bool WriteAutoSave()
        {
            if (village == null || GameModeManager.Current != GameMode.Adventure || !CampaignState.Active) return false;
            try
            {
                WriteFile(AutoSavePath, Capture());
                return true;
            }
            catch (Exception e) { Debug.LogWarning("자동 저장 실패: " + e.Message, this); return false; }
        }

        private static SaveData ReadAutoSave()
        {
            try
            {
                if (!HasAutoSave) return null;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(AutoSavePath));
                return data != null && data.version == 1 && data.day >= 1 && data.troops != null ? data : null;
            }
            catch (Exception e) { Debug.LogWarning("자동 저장 읽기 실패: " + e.Message); return null; }
        }

        // 시작 화면 표시용. battleInProgress면 이어하기 시 재도전권 1장이 차감된다.
        public static (int day, int tokens, bool battleInProgress)? AutoSaveSummary()
        {
            var data = ReadAutoSave();
            if (data == null) return null;
            return (data.day, Mathf.Max(0, data.retryTokens), data.battleInProgress);
        }

        public static void DeleteAutoSave()
        {
            try { if (HasAutoSave) File.Delete(AutoSavePath); }
            catch (Exception e) { Debug.LogWarning("자동 저장 삭제 실패: " + e.Message); }
        }

        // 출전 시 true, 승리 확정 시 false. 전투 중 종료 후 이어하기를 판정한다.
        public static void MarkBattleInProgress(bool value)
        {
            var data = ReadAutoSave();
            if (data == null || data.battleInProgress == value) return;
            data.battleInProgress = value;
            try { WriteFile(AutoSavePath, data); }
            catch (Exception e) { Debug.LogWarning("자동 저장 갱신 실패: " + e.Message); }
        }

        // 사망·중도 복귀 시 즉시 호출한다. 파일에 먼저 기록해 강제 종료로 되돌릴 수 없게 한다.
        // true: 1장 사용, 아침 저장으로 재시작 가능. false: 남은 재도전권 없음(또는 저장 없음).
        public static bool ConsumeRetryToken(out int remaining)
        {
            remaining = 0;
            var data = ReadAutoSave();
            if (data == null || data.retryTokens <= 0) { CampaignState.SetTokens(0); return false; }
            data.retryTokens--;
            data.battleInProgress = false;
            try { WriteFile(AutoSavePath, data); }
            catch (Exception e) { Debug.LogWarning("재도전권 기록 실패: " + e.Message); return false; }
            remaining = data.retryTokens;
            CampaignState.SetTokens(remaining);
            return true;
        }

        public enum ContinueResult { Loaded, LoadedAfterPenalty, CampaignEnded, Missing }

        // 시작 화면 '이어하기'와 재도전 버튼이 같은 경로로 아침 상태를 불러온다.
        public static ContinueResult LoadAutoSave(string sceneName)
        {
            var data = ReadAutoSave();
            if (data == null || !Application.CanStreamedLevelBeLoaded(sceneName)) return ContinueResult.Missing;
            var result = ContinueResult.Loaded;
            if (data.battleInProgress)
            {
                // 전투 중 종료: 사망과 같게 재도전권 1장을 쓴다. 남은 장수가 없으면 회차 종료.
                if (data.retryTokens <= 0) { DeleteAutoSave(); CampaignState.Clear(); return ContinueResult.CampaignEnded; }
                data.retryTokens--;
                data.battleInProgress = false;
                try { WriteFile(AutoSavePath, data); }
                catch (Exception e) { Debug.LogWarning("재도전권 기록 실패: " + e.Message); return ContinueResult.Missing; }
                result = ContinueResult.LoadedAfterPenalty;
            }
            pending = data;
            pendingTroops = null;
            GameModeManager.Select(GameMode.Adventure);
            GameModeManager.RecordAdventureDay(data.day);
            Time.timeScale = 1f;
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            return result;
        }

        private static void WriteFile(string path, SaveData data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path + ".tmp", JsonUtility.ToJson(data, true));
            // 임시 파일을 완성한 뒤 교체하여 쓰기 중 실패 시 기존 저장을 보존한다.
            if (File.Exists(path)) File.Replace(path + ".tmp", path, null);
            else File.Move(path + ".tmp", path);
        }

        private SaveData Capture()
        {
            village.Cancel();
            var data = new SaveData { savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                gameMode = GameModeManager.Current.ToString() };
            village.CaptureSave(data, this);
            harvest.CaptureSave(data);
            data.equipment = ArmySurvivor.Equipment.EquipmentInventory.Capture();
            var army = FindFirstObjectByType<RecruitmentController>(FindObjectsInactive.Include);
            if (army != null) army.CaptureSave(data, this);
            else if (pendingTroops != null) data.troops.AddRange(pendingTroops);
            data.campaignId = CampaignState.Id;
            data.retryTokens = CampaignState.Active ? CampaignState.RetryTokens : -1;
            data.retryTokensMax = CampaignState.MaxRetryTokens;
            return data;
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
            if (data == null || data.version != 1 || data.day < 1 || data.rice < 0 || data.gold < 0 ||
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
                WriteFile(SlotPath(index), Capture());
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
                GameModeManager.Select(Enum.TryParse(data.gameMode, out GameMode savedMode)
                    ? savedMode : GameMode.Adventure);
                if (GameModeManager.Current == GameMode.Adventure)
                    GameModeManager.RecordAdventureDay(data.day);
                pendingTroops = null;
                loading = true;
                RefreshSlots();
                SceneManager.LoadSceneAsync(villageScene, LoadSceneMode.Single);
            }
            catch (Exception e) { loading = false; status.text = failureText; Debug.LogWarning(e.Message, this); }
        }
    }
}
