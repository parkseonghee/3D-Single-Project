using UnityEngine;

namespace ArmySurvivor.Equipment
{
    public sealed class SupplySlidingGate : MonoBehaviour
    {
        [Tooltip("Assign DoorPivot transforms, not the door meshes.")]
        public Transform doorA, doorB;
        public float openAngleA = 90f, openAngleB = -90f;
        public AnimationClip openA, openB;
        public bool Ready => doorA && doorB && openA && openB;
        public void Sample(float progress)
        {
            if (!Ready) return;
            openA.SampleAnimation(doorA.gameObject, Mathf.Clamp01(progress) * openA.length);
            openB.SampleAnimation(doorB.gameObject, Mathf.Clamp01(progress) * openB.length);
        }
    }
}
