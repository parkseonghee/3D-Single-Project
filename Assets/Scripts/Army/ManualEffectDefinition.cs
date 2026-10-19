using UnityEngine;

namespace ArmySurvivor.Army
{
    public abstract class ManualEffectDefinition : ScriptableObject
    {
        public virtual int MinimumSoldiers => 1;
        public virtual string Describe(string fallback) => fallback;
        public abstract ManualEffect CreateEffect(ManualManager manager);
    }

    // Runtime state belongs to this instance, never to the shared asset.
    public abstract class ManualEffect
    {
        public abstract void OnBattleStart();
        public abstract void OnBattleEnd();
        public virtual void Tick(float deltaTime) { }
    }
}
