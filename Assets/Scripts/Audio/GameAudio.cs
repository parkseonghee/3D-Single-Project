using System.Collections.Generic;
using ArmySurvivor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArmySurvivor.Audio
{
    // 모든 씬에서 하나만 유지하는 소리 재생기. Resources/SoundLibrary 에셋이 있으면 게임 시작 때 자동으로 만들어진다.
    // 화면 요소가 아니므로 씬에 배치하지 않는다. 음량은 GameSettings 값을 따른다.
    public sealed class GameAudio : MonoBehaviour
    {
        private const string LibraryPath = "SoundLibrary";
        private static GameAudio instance;

        private SoundLibrary library;
        private readonly AudioSource[] music = new AudioSource[2];
        private readonly float[] musicBase = new float[2];
        private int activeMusic;
        private Bgm currentBgm = Bgm.None;
        private float fade = 1f;
        private AudioSource[] voices;
        private float[] voiceBase;
        private int nextVoice;
        private readonly Dictionary<Sfx, SoundLibrary.SfxEntry> entries = new Dictionary<Sfx, SoundLibrary.SfxEntry>();
        private readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private PointerEventData pointer;
        private EventSystem pointerSystem;

        public static Bgm CurrentBgm => instance != null ? instance.currentBgm : Bgm.None;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            var library = Resources.Load<SoundLibrary>(LibraryPath);
            if (library == null) { Debug.LogWarning("Resources/SoundLibrary 에셋이 없어 소리를 재생하지 않습니다."); return; }
            var go = new GameObject("Game Audio");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameAudio>();
            instance.Init(library);
        }

        private void Init(SoundLibrary source)
        {
            library = source;
            for (int i = 0; i < music.Length; i++)
            {
                music[i] = gameObject.AddComponent<AudioSource>();
                music[i].loop = true;
                music[i].playOnAwake = false;
                music[i].ignoreListenerPause = true;
            }
            voices = new AudioSource[library.sfxVoices];
            voiceBase = new float[voices.Length];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            if (library.sfx != null)
                foreach (var entry in library.sfx)
                    if (entry != null && !entries.ContainsKey(entry.id)) entries.Add(entry.id, entry);
            GameSettings.Changed += RefreshVolumes;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        // 첫 씬은 이 오브젝트가 만들어지는 시점과 씬 로드 알림 순서가 엇갈릴 수 있어 첫 프레임에 한 번 더 맞춘다.
        private void Start()
        {
            if (currentBgm == Bgm.None) SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= RefreshVolumes;
            SceneManager.sceneLoaded -= SceneLoaded;
            if (instance == this) instance = null;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            var bgm = library.SceneMusic(scene.name);
            if (bgm != Bgm.None) SwitchMusic(bgm);
        }

        public static void Play(Sfx id, float volumeScale = 1f)
        {
            if (instance != null) instance.PlayInternal(id, volumeScale);
        }

        public static void PlayMusic(Bgm id)
        {
            if (instance != null) instance.SwitchMusic(id);
        }

        private void PlayInternal(Sfx id, float volumeScale)
        {
            if (!entries.TryGetValue(id, out var entry) || entry.clips == null || entry.clips.Length == 0) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < entry.minInterval) return;
            var clip = entry.clips[Random.Range(0, entry.clips.Length)];
            if (clip == null) return;
            lastPlayed[id] = now;
            int index = FreeVoice();
            var voice = voices[index];
            voiceBase[index] = entry.volume * volumeScale;
            voice.clip = clip;
            voice.pitch = Random.Range(Mathf.Min(entry.pitch.x, entry.pitch.y), Mathf.Max(entry.pitch.x, entry.pitch.y));
            voice.volume = voiceBase[index] * GameSettings.SfxVolume;
            voice.Play();
        }

        // 쉬는 음원을 먼저 쓰고, 모두 재생 중이면 차례대로 가장 오래된 것을 끊는다.
        private int FreeVoice()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                int index = (nextVoice + i) % voices.Length;
                if (!voices[index].isPlaying) { nextVoice = (index + 1) % voices.Length; return index; }
            }
            int oldest = nextVoice;
            nextVoice = (nextVoice + 1) % voices.Length;
            return oldest;
        }

        private void SwitchMusic(Bgm id)
        {
            if (id == currentBgm) return;
            currentBgm = id;
            var entry = library.Find(id);
            var clip = entry?.clip;
            if (clip != null && music[activeMusic].clip == clip && music[activeMusic].isPlaying) return;
            activeMusic = 1 - activeMusic;
            var next = music[activeMusic];
            next.Stop();
            next.clip = clip;
            musicBase[activeMusic] = entry != null ? entry.volume : 0f;
            if (clip != null) next.Play();
            fade = library.bgmFadeSeconds <= 0 ? 1f : 0f;
            ApplyMusicVolume();
        }

        private void Update()
        {
            if (fade < 1f)
            {
                fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / library.bgmFadeSeconds);
                ApplyMusicVolume();
                if (fade >= 1f) music[1 - activeMusic].Stop();
            }
            ClickSound();
        }

        // 버튼마다 소리를 연결하지 않고, 눌린 위치의 활성 UI 조작 요소를 찾아 공통 클릭음을 낸다.
        private void ClickSound()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || EventSystem.current == null) return;
            if (!entries.TryGetValue(Sfx.UiClick, out var entry) || entry.clips == null || entry.clips.Length == 0) return;
            if (pointer == null || pointerSystem != EventSystem.current) { pointerSystem = EventSystem.current; pointer = new PointerEventData(pointerSystem); }
            pointer.position = mouse.position.ReadValue();
            uiHits.Clear();
            EventSystem.current.RaycastAll(pointer, uiHits);
            if (uiHits.Count == 0) return;
            var selectable = uiHits[0].gameObject.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.IsInteractable()) PlayInternal(Sfx.UiClick, 1f);
        }

        private void ApplyMusicVolume()
        {
            float volume = GameSettings.BgmVolume;
            music[activeMusic].volume = musicBase[activeMusic] * volume * fade;
            music[1 - activeMusic].volume = musicBase[1 - activeMusic] * volume * (1f - fade);
        }

        private void RefreshVolumes()
        {
            ApplyMusicVolume();
            for (int i = 0; i < voices.Length; i++) voices[i].volume = voiceBase[i] * GameSettings.SfxVolume;
        }
    }
}
