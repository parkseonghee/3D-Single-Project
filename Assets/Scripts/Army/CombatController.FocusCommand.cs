using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ArmySurvivor.Tutorial;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("집중 구역 명령")]
        [SerializeField] private Camera focusInputCamera;
        [SerializeField, Min(0)] private float focusClickPaddingPixels = 14f;
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
        private EnemyState focusTarget;
        private int focusGeneration;

        public bool HasFocusCommand => focusActive;
        public Vector3 FocusCenter => focusCenter;

        private void UpdateFocusCommand(float dt)
        {
            if (run.IsDiablo) return;
            Mouse mouse = Mouse.current;
            if (mouse != null && !PointerOverInteractiveUI(mouse.position.ReadValue()))
            {
                if (mouse.leftButton.wasPressedThisFrame && focusActive)
                {
                    ClearFocusCommand();
                    TutorialEvents.Raise("FocusTargetCleared");
                }
                else if (mouse.rightButton.wasPressedThisFrame && focusInputCamera != null)
                {
                    EnemyState enemy = FindEnemyAtScreenPoint(mouse.position.ReadValue());
                    if (enemy != null)
                    {
                        focusTarget = enemy;
                        SetFocusCommand(enemy.root.position);
                        TutorialEvents.Raise("FocusTargetSet", enemy.root);
                    }
                }
            }

            if (focusActive)
            {
                if (focusTarget != null && focusTarget.root != null && focusTarget.health > 0)
                {
                    focusCenter = focusTarget.root.position;
                    if (focusRing != null) focusRing.transform.position = focusCenter + Vector3.up * focusRingHeight;
                }
                MoveFocusedSoldiers(dt);
            }
        }

        private EnemyState FindEnemyAtScreenPoint(Vector2 screenPoint)
        {
            EnemyState selected = null;
            float nearestDepth = float.PositiveInfinity;
            foreach (EnemyState enemy in enemies)
            {
                if (enemy.root == null || enemy.health <= 0) continue;
                Renderer[] renderers = enemy.root.GetComponentsInChildren<Renderer>();
                bool hasBounds = false;
                Bounds bounds = default;
                foreach (Renderer renderer in renderers)
                {
                    if (!renderer.enabled || renderer is ParticleSystemRenderer) continue;
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (!hasBounds) continue;
                Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                float depth = float.PositiveInfinity;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1,
                            (corner & 4) == 0 ? -1 : 1));
                    Vector3 screen = focusInputCamera.WorldToScreenPoint(point);
                    if (screen.z <= 0) continue;
                    min = Vector2.Min(min, screen);
                    max = Vector2.Max(max, screen);
                    depth = Mathf.Min(depth, screen.z);
                }
                if (float.IsInfinity(min.x)) continue;
                Rect hitArea = Rect.MinMaxRect(min.x - focusClickPaddingPixels, min.y - focusClickPaddingPixels,
                    max.x + focusClickPaddingPixels, max.y + focusClickPaddingPixels);
                if (!hitArea.Contains(screenPoint) || depth >= nearestDepth) continue;
                selected = enemy;
                nearestDepth = depth;
            }
            return selected;
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
                if (hit.gameObject.GetComponent<TutorialSpotlight>() != null) return true;
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
                TutorialTarget marker = focusRing.GetComponent<TutorialTarget>();
                if (marker == null) marker = focusRing.gameObject.AddComponent<TutorialTarget>();
                marker.SetId("FocusMarker");
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
            focusTarget = null;
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
            if (focusTarget != null && focusTarget.root != null && focusTarget.health > 0)
            {
                bool ranged = recruitment.Units.TryGetValue(unit, out UnitDefinition own) &&
                    !IsFocusRallyUnit(own.attack);
                if (ranged || FlatDistance(unit.position, focusTarget.root.position) <= range)
                {
                    focused = true;
                    return focusTarget;
                }
            }
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
                (IsFocusRallyUnit(recruitment.Units[unit].attack) &&
                 FlatDistance(unit.position, target.root.position) > range)) return false;
            return FlatDistance(focusCenter, target.root.position) <= focusRadius;
        }
    }
}
