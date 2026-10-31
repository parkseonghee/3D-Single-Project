using System;
using UnityEngine;
using UnityEngine.Serialization;
using ArmySurvivor.Building;

namespace ArmySurvivor.Tutorial
{
    public enum TutorialHoleShape { RoundedRectangle, Circle }
    public enum TutorialBubblePlacement { Auto, Above, Below, Left, Right, Center }
    public enum TutorialAdvance { NextButton, TargetClick, Event, Seconds, CommanderDistance }

    [Serializable]
    public sealed class TutorialStep
    {
        public string targetId;
        public TutorialHoleShape holeShape = TutorialHoleShape.RoundedRectangle;
        public Vector2 padding = new Vector2(18, 18);
        [Min(0)] public float cornerRadius = 20;
        [TextArea(1, 2)] public string title;
        [TextArea(2, 5)] public string body;
        public TutorialBubblePlacement bubblePlacement = TutorialBubblePlacement.Auto;
        public bool showAsTopBanner;
        public BuildingDefinition revealBuilding;
        public TutorialAdvance advance = TutorialAdvance.NextButton;
        public string eventKey;
        public bool requireEventTargetMatch;
        [Min(1)] public int requiredEventCount = 1;
        public bool hideWhenTargetMissing;
        [Min(0)] public float seconds;
        [Min(0)] public float fallbackSeconds;
        [Min(0)] public float commanderDistance;
        public bool pauseGame;
        public bool allowCommanderMovementWhilePaused;
        public bool blockOutsideClicks = true;
        public bool allowKeyboard = true;
        public bool showArrow = true;
        [Range(0.05f, 1f)] public float enemyMovementMultiplier = 1f;
        [FormerlySerializedAs("nextButtonIfFocusTargetKilled")]
        public bool showNextButtonAfterJump;
        public bool showNextWhenNoFocus;
    }

    [CreateAssetMenu(menuName = "Army Survivor/Tutorial/Sequence")]
    public sealed class TutorialSequence : ScriptableObject
    {
        public string tutorialId;
        public string startEventKey;
        public bool interruptCurrent;
        public string startTargetId;
        [Min(0)] public float startDistanceFromCommander;
        public string cancelEventKey;
        public bool cancelOnCommanderDeath;
        public string finishEventKey;
        public string jumpEventKey;
        public bool requireJumpTargetMatch;
        public bool cancelIfJumpBeforeStart;
        [Min(0)] public int jumpToStep;
        public TutorialStep[] steps;
    }
}
