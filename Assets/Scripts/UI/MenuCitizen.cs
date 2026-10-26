using UnityEngine;

namespace ArmySurvivor.UI
{
    // Decorative only: never registers with the army, combat, or save systems.
    public sealed class MenuCitizen : MonoBehaviour
    {
        [SerializeField] private Transform[] route;
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.3f;
        [SerializeField, Min(0f)] private float pauseSeconds = 1.8f;
        [SerializeField, Min(1f)] private float turnSpeed = 240f;
        [SerializeField] private Animator animator;
        [SerializeField] private int startingWaypoint;
        private int target;
        private float wait;
        private static readonly int Walking = Animator.StringToHash("Walking");

        public void Configure(Transform[] points, Animator body, float speed, int start)
        {
            route = points;
            animator = body;
            walkSpeed = speed;
            startingWaypoint = start;
        }

        private void OnEnable()
        {
            if (route == null || route.Length < 2) return;
            target = (startingWaypoint + 1) % route.Length;
            wait = (startingWaypoint % 3) * 0.4f;
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }

        private void Update()
        {
            if (route == null || route.Length < 2 || route[target] == null) return;
            float dt = Time.unscaledDeltaTime;
            if (wait > 0f)
            {
                wait -= dt;
                if (animator != null) animator.SetBool(Walking, false);
                return;
            }
            Vector3 destination = route[target].position;
            Vector3 delta = destination - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.01f)
            {
                target = (target + 1) % route.Length;
                wait = pauseSeconds;
                return;
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(delta), turnSpeed * dt);
            transform.position = Vector3.MoveTowards(transform.position, destination, walkSpeed * dt);
            if (animator != null) animator.SetBool(Walking, true);
        }

        private void OnDrawGizmosSelected()
        {
            if (route == null || route.Length < 2) return;
            Gizmos.color = new Color(1f, 0.8f, 0.25f);
            for (int i = 0; i < route.Length; i++)
                if (route[i] != null && route[(i + 1) % route.Length] != null)
                    Gizmos.DrawLine(route[i].position, route[(i + 1) % route.Length].position);
        }
    }
}
