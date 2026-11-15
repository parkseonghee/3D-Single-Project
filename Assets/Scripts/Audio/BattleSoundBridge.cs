using ArmySurvivor.Army;
using UnityEngine;

namespace ArmySurvivor.Audio
{
    // 전투 씬의 기존 이벤트를 듣고 효과음과 배경음을 바꾼다. 전투 코드는 소리를 몰라도 된다.
    public sealed class BattleSoundBridge : MonoBehaviour
    {
        [SerializeField] private RunController run;
        [SerializeField] private CombatController combat;
        [SerializeField] private BattleExperience experience;

        private UnitHealth commanderHealth;
        private bool commanderDown;

        private Bgm BattleMusic => GameModeManager.Current == GameMode.Survival ? Bgm.Survival : Bgm.Battle;

        private void OnEnable()
        {
            run.RunStarted += RunStarted;
            run.RunEnded += RunEnded;
            combat.EnemyDamaged += EnemyDamaged;
            combat.EnemyDefeated += EnemyDefeated;
            combat.SpecialWaveAnnounced += SpecialWave;
            combat.BossSpawned += BossSpawned;
            combat.EliteSpawned += EliteSpawned;
            if (experience != null)
            {
                experience.UnitLeveledUp += UnitLeveledUp;
                experience.OnBottlesCollected += BottlesCollected;
            }
        }

        private void OnDisable()
        {
            run.RunStarted -= RunStarted;
            run.RunEnded -= RunEnded;
            combat.EnemyDamaged -= EnemyDamaged;
            combat.EnemyDefeated -= EnemyDefeated;
            combat.SpecialWaveAnnounced -= SpecialWave;
            combat.BossSpawned -= BossSpawned;
            combat.EliteSpawned -= EliteSpawned;
            if (experience != null)
            {
                experience.UnitLeveledUp -= UnitLeveledUp;
                experience.OnBottlesCollected -= BottlesCollected;
            }
            if (commanderHealth != null) commanderHealth.OnDamageTaken -= CommanderDamaged;
        }

        private void Start()
        {
            if (run.Commander != null && run.Commander.TryGetComponent(out commanderHealth))
                commanderHealth.OnDamageTaken += CommanderDamaged;
            if (!run.IsRunning) GameAudio.PlayMusic(Bgm.Preparation);
        }

        private void RunStarted()
        {
            commanderDown = false;
            GameAudio.PlayMusic(BattleMusic);
            GameAudio.Play(Sfx.WaveStart);
        }

        private void RunEnded() => GameAudio.PlayMusic(Bgm.Preparation);

        private void EnemyDamaged(Transform enemy, Vector3 position, float damage, bool building) => GameAudio.Play(Sfx.Hit);

        private void EnemyDefeated(bool elite, bool boss)
        {
            if (boss)
            {
                GameAudio.Play(Sfx.BossDeath);
                if (run.IsRunning) GameAudio.PlayMusic(BattleMusic);
            }
            else GameAudio.Play(elite ? Sfx.EliteDeath : Sfx.EnemyDeath);
        }

        private void SpecialWave(string name) => GameAudio.Play(Sfx.SpecialWave);

        private void BossSpawned()
        {
            GameAudio.Play(Sfx.BossAppear);
            GameAudio.PlayMusic(Bgm.Boss);
        }

        private void EliteSpawned() => GameAudio.Play(Sfx.EliteAppear);
        private void UnitLeveledUp(Transform unit, int level) => GameAudio.Play(Sfx.UnitLevelUp);
        private void BottlesCollected(int count) => GameAudio.Play(Sfx.ExpPickup);

        private void CommanderDamaged(UnitHealth health, float lost, float absorbed)
        {
            if (health.IsDead)
            {
                if (!commanderDown) GameAudio.Play(Sfx.CommanderDeath);
                commanderDown = true;
                return;
            }
            if (lost > 0) GameAudio.Play(Sfx.CommanderHurt);
        }
    }
}
