using System;
using UnityEngine;

namespace ArmySurvivor.Audio
{
    // 코드가 소리를 부르는 이름. 실제 파일과 음량은 SoundLibrary 에셋에서 연결한다.
    public enum Sfx
    {
        UiClick,
        Hit,
        EnemyDeath,
        EliteDeath,
        BossDeath,
        EliteAppear,
        BossAppear,
        CommanderHurt,
        CommanderDeath,
        ExpPickup,
        SealPickup,
        UnitLevelUp,
        WaveStart,
        SpecialWave,
        Victory,
        Defeat,
        NewRecord,
        SupplyDraw,
        SupplyRare,
        BuildingPlace,
        Recruit,
        MeteorFall,
        MeteorImpact,
        DiabloFire,
        ShieldAura,
        Shout,
        ChainLightning,
        WeaponStrike,
        ArrowExplosion,
    }

    public enum Bgm { None, Title, Village, Preparation, Battle, Survival, Boss }

    [CreateAssetMenu(menuName = "Army Survivor/Audio/Sound Library")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public class SfxEntry
        {
            public Sfx id;
            [Tooltip("여러 개를 넣으면 재생할 때마다 무작위로 고른다. 비어 있으면 소리 없이 넘어간다.")]
            public AudioClip[] clips;
            [Range(0, 1)] public float volume = 1f;
            [Tooltip("재생마다 이 범위에서 음높이를 무작위로 정한다.")]
            public Vector2 pitch = new Vector2(0.95f, 1.05f);
            [Tooltip("같은 소리를 다시 낼 수 있는 최소 간격(초). 타격처럼 한 프레임에 여러 번 나는 소리가 겹쳐 터지지 않게 한다.")]
            [Min(0)] public float minInterval = 0.05f;
        }

        [Serializable]
        public class BgmEntry
        {
            public Bgm id;
            public AudioClip clip;
            [Range(0, 1)] public float volume = 0.6f;
        }

        [Serializable]
        public class SceneBgm
        {
            public string scene;
            public Bgm bgm;
        }

        [Tooltip("동시에 낼 수 있는 효과음 수. 넘치면 가장 오래된 소리를 끊는다.")]
        [Min(1)] public int sfxVoices = 24;
        [Tooltip("배경음 교체 시 서로 겹쳐 바뀌는 시간(초)")]
        [Min(0)] public float bgmFadeSeconds = 1.2f;
        public SfxEntry[] sfx;
        public BgmEntry[] bgm;
        [Tooltip("씬에 들어가면 먼저 트는 배경음. 전투 씬은 전투 상태에 따라 다시 바뀐다.")]
        public SceneBgm[] scenes;

        public SfxEntry Find(Sfx id) => sfx == null ? null : Array.Find(sfx, x => x != null && x.id == id);
        public BgmEntry Find(Bgm id) => bgm == null ? null : Array.Find(bgm, x => x != null && x.id == id);
        public Bgm SceneMusic(string scene)
        {
            var entry = scenes == null ? null : Array.Find(scenes, x => x != null && x.scene == scene);
            return entry?.bgm ?? Bgm.None;
        }
    }
}
