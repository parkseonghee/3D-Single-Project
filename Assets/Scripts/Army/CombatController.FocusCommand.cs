using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("집중 구역 명령")]
        [SerializeField] private Camera focusInputCamera;
        [SerializeField] private LineRenderer focusRingTemplate;
        [SerializeField, Min(0.5f)] private float focusRadius = 5f;
        [SerializeField, Min(1)] private int focusMaxAttackersPerEnemy = 2;
        [SerializeField, Min(0.1f)] private float focusRallySpeed = 8f;
        [SerializeField, Range(0f, 0.9f)] private float focusSlotRadiusFraction = 0.55f;
        [SerializeField, Range(0f, 1f)] private float focusCavalryPerimeterFraction = 0.8f;
        [SerializeField, Min(0f)] private float focusCavalryInterceptBand = 1.5f;
        [SerializeField, Min(0.01f)] private float focusArrivalTolerance = 0.2f;
        [SerializeField, Min(0f)] private float focusEdgeInset = 0.35f;
        [SerializeField, Range(12, 128)] private int focusRingSegments = 64;
        [SerializeField] private float focusRingHeight = 0.08f;
        [SerializeField] private Color focusRingColor = new Color(1f, 0.72f, 0.2f, 0.9f);

        private readonly List<RaycastResult> focusUiHits = new List<RaycastResult>();
        private readonly HashSet<Transform> focusAssignedUnits = new HashSet<Transform>();
        private LineRenderer focusRing;
        private Vector3 focusCenter;
        private bool focusActive;
        private int focusGeneration;

        public bool HasFocusCommand => focusActive;
        public Vector3 FocusCenter => focusCenter;

        private void UpdateFocusCommand(float dt)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && !PointerOverInteractiveUI(mouse.position.ReadValue()))
            {
                if (mouse.rightButton.wasPressedThisFrame) ClearFocusCommand();
                else if (mouse.leftButton.wasPressedThisFrame && focusInputCamera != null && run.Ground != null)
                {
                    Ray ray = focusInputCamera.ScreenPointToRay(mouse.position.ReadValue());
                    if (run.Ground.Raycast(ray, out RaycastHit hit, focusInputCamera.farClipPlane))
                        SetFocusCommand(hit.point);
                }
            }

            if (focusActive) MoveFocusedSoldiers(dt);
        }

        private bool PointerOverInteractiveUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;
            focusUiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                { position = screenPosition }, focusUiHits);
            foreach (RaycastResult hit in focusUiHits)
            {
                if (hit.gameObject == null) continue;
                if (hit.gameObject.GetComponentInParent<Selectable>() != null ||
                    hit.gameObject.GetComponentInParent<ScrollRect>() != null)
                    return true;
            }
            return false;
        }

        private void SetFocusCommand(Vector3 point)
        {
            CancelCurrentAttacks();
            Bounds groundBounds = run.Ground.bounds;
            point.x = Mathf.Clamp(point.x, groundBounds.min.x + focusRadius,
                groundBounds.max.x - focusRadius);
            point.z = Mathf.Clamp(point.z, groundBounds.min.z + focusRadius,
                groundBounds.max.z - focusRadius);
            focusCenter = point;
            focusActive = true;
            focusGeneration++;
            foreach (var entry in recruitment.Units)
                if (entry.Key != null && entry.Key.gameObject.activeSelf &&
                    allies.TryGetValue(entry.Key, out UnitHealth health) && !health.IsDead)
                    run.SetAttacking(entry.Key, IsFocusRallyUnit(entry.Value.attack));
            if (focusRing == null && focusRingTemplate != null && combatRoot != null)
            {
                focusRing = Instantiate(focusRingTemplate, combatRoot);
                focusRing.gameObject.name = "Focus Area Ring";
                focusRing.gameObject.SetActive(true);
                focusRing.useWorldSpace = false;
                focusRing.loop = true;
                focusRing.positionCount = focusRingSegments;
                focusRing.startColor = focusRingColor;
                focusRing.endColor = focusRingColor;
                for (int i = 0; i < focusRingSegments; i++)
                {
                    float angle = i * Mathf.PI * 2f / focusRingSegments;
                    focusRing.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * focusRadius);
                }
            }
            if (focusRing != null)
            {
                focusRing.transform.SetPositionAndRotation(point + Vector3.up * focusRingHeight, Quaternion.identity);
                focusRing.transform.localScale = Vector3.one;
            }
        }

        private void ClearFocusCommand()
        {
            if (focusActive)
            {
                CancelCurrentAttacks();
                foreach (var entry in recruitment.Units)
                    if (entry.Key != null) run.SetAttacking(entry.Key, false);
                focusGeneration++;
            }
            focusActive = false;
            if (focusRing != null)
            {
                focusRing.gameObject.SetActive(false);
                Destroy(focusRing.gameObject);
                focusRing = null;
            }
        }

        private void CancelCurrentAttacks()
        {
            pending.Clear();
            melee.Clear();
        }

        private Vector3 FocusSlot(Transform unit)
        {
            float fraction = IsFocusCavalry(unit) ? focusCavalryPerimeterFraction :
                focusSlotRadiusFraction;
            float radius = Mathf.Min(focusRadius * fraction,
                Mathf.Max(0f, focusRadius - focusEdgeInset));
            return run.FormationPosition(unit, focusCenter, radius);
        }

        private bool IsFocusCavalry(Transform unit)
        {
            return recruitment.Units.TryGetValue(unit, out UnitDefinition definition) &&
                definition.attack != null &&
                definition.attack.style == AttackDefinition.AttackStyle.Charge;
        }

        private static bool IsFocusRallyUnit(AttackDefinition attack)
        {
            if (attack == null) return false;
            return attack.style != AttackDefinition.AttackStyle.StraightProjectile &&
                attack.style != AttackDefinition.AttackStyle.HomingProjectile;
        }

        private bool FocusUnitReady(Transform unit)
        {
            return !focusActive ||
                (recruitment.Units.TryGetValue(unit, out UnitDefinition definition) &&
                 !IsFocusRallyUnit(definition.attack)) ||
                FlatDistance(unit.position, FocusSlot(unit)) <= focusArrivalTolerance;
        }

        private void MoveFocusedSoldiers(float dt)
        {
            foreach (var entry in recruitment.Units)
            {
                Transform unit = entry.Key;
                if (unit == null || !unit.gameObject.activeSelf || melee.ContainsKey(unit) ||
                    !IsFocusRallyUnit(entry.Value.attack) ||
                    !allies.TryGetValue(unit, out UnitHealth health) || health.IsDead) continue;
                Vector3 slot = FocusSlot(unit);
                run.MoveAttacker(unit, FlatDistance(unit.position, slot) <= focusArrivalTolerance
                    ? unit.position : slot, focusRallySpeed, dt);
            }
        }

        private Vector3 ClampToFocus(Vector3 point)
        {
            Vector3 offset = point - focusCenter;
            offset.y = 0;
            float allowedRadius = Mathf.Max(0, focusRadius - focusEdgeInset);
            if (offset.sqrMagnitude > allowedRadius * allowedRadius)
                point = focusCenter + offset.normalized * allowedRadius;
            return point;
        }

        private EnemyState SelectEnemyForUnit(Transform unit, float range, out bool focused)
        {
            focused = false;
            if (!focusActive) return NearestEnemy(unit.position, range);
            bool cavalry = IsFocusCavalry(unit);
            EnemyState chosen = cavalry ? FindCommandTarget(unit, range, true, true) : null;
            if (chosen == null) chosen = FindCommandTarget(unit, range, true, false);
            if (chosen == null && cavalry) chosen = FindCommandTarget(unit, range, false, true);
            if (chosen == null) chosen = FindCommandTarget(unit, range, false, false);
            focused = chosen != null;
            return chosen;
        }

        private EnemyState FindCommandTarget(Transform unit, float range, bool respectLimit,
            bool cavalryPerimeter)
        {
            EnemyState best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (EnemyState candidate in enemies)
            {
                if (candidate.root == null || candidate.health <= 0) continue;
                float distance = FlatDistance(unit.position, candidate.root.position);
                if (distance > range) continue;
                float centerDistance = FlatDistance(focusCenter, candidate.root.position);
                if (centerDistance > focusRadius ||
                    (cavalryPerimeter && centerDistance < focusRadius - focusCavalryInterceptBand)) continue;
                if (respectLimit && !candidate.isBoss &&
                    CountFocusAttackers(candidate, unit) >= focusMaxAttackersPerEnemy) continue;
                if (distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        private EnemyState NearestFocusEnemy(Vector3 origin, float range)
        {
            EnemyState best = null;
            float bestDistance = range;
            foreach (EnemyState candidate in enemies)
            {
                if (candidate.root == null || candidate.health <= 0 ||
                    FlatDistance(focusCenter, candidate.root.position) > focusRadius) continue;
                float distance = FlatDistance(origin, candidate.root.position);
                if (distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        private int CountFocusAttackers(EnemyState target, Transform except)
        {
            focusAssignedUnits.Clear();
            foreach (var entry in pending)
                if (entry.Key != except && entry.Value.focusAssigned &&
                    entry.Value.focusGeneration == focusGeneration && entry.Value.enemyTarget == target)
                    focusAssignedUnits.Add(entry.Key);
            foreach (var entry in melee)
                if (entry.Key != except && !entry.Value.returning && entry.Value.focusAssigned &&
                    entry.Value.focusGeneration == focusGeneration && entry.Value.target == target)
                    focusAssignedUnits.Add(entry.Key);
            foreach (Shot shot in shots)
                if (shot.source != except && shot.focusAssigned && shot.focusGeneration == focusGeneration &&
                    shot.target == target)
                    focusAssignedUnits.Add(shot.source);
            return focusAssignedUnits.Count;
        }

        private bool FocusTargetInRange(Transform unit, EnemyState target, float range)
        {
            if (!focusActive || target == null || target.root == null || target.health <= 0 ||
                FlatDistance(unit.position, target.root.position) > range) return false;
            return FlatDistance(focusCenter, target.root.position) <= focusRadius;
        }
    }
}
