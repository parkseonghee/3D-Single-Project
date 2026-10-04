using ArmySurvivor.Building;
using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Unit Definition")]
    public class UnitDefinition : ScriptableObject
    {
        public GameObject prefab;
        public string displayName;
        public bool canLearnPiercing;
        public bool canLearnMultiShot;
        public bool canLearnFocus;
        public bool canLearnSuppression;
        public bool canLearnExplosion;
        public bool canLearnWeaponStrike;
        public bool canLearnBloodthirst;
        public bool canLearnSpinningSword;
        public bool canLearnSpinSlash;
        public bool canLearnBanner;
        public bool canLearnMagicTrap;
        public bool canLearnLaserAOE;
        public bool canLearnChainLightning;
        public bool canLearnTeamShield;
        public bool canLearnTeamAttack;
        public bool canLearnIntimidatingShout;
        public bool canLearnIronWall;
        public bool canLearnTeamAttackSpeed;
        public bool canLearnChainCharge;
        public bool canLearnFlameHooves;
        public bool canLearnChargeInvulnerability;
        public bool canLearnMarchingHorn;
        public bool canLearnMomentum;
        public bool canLearnManaCycle;
        [Min(1)] public float health = 150;
        public AttackDefinition attack;
        public BuildingDefinition requiredBuilding;
        [Min(0)] public int riceCost;
        public Vector3 modelOffset;
        public Vector3 modelRotation;
        public Vector3 modelScale = Vector3.one;
    }
}
