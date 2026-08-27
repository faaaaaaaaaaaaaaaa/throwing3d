using System;
using System.Collections;
using UnityEngine;

// Throwing only. Turn ownership lives in TurnManager (like the original 2D game).
public class ThrowManager : MonoBehaviour
{
    public static ThrowManager Instance { get; private set; }

    [Header("Throw Points")]
    [SerializeField] private Transform _zombieHandSpawnPoint;
    [SerializeField] private Transform _humanHandSpawnPoint;
    [SerializeField] private string _playerThrowPointTag = "PlayerLeft_Hand";
    [SerializeField] private string _enemyThrowPointTag = "PlayerRight_Hand";
    [Header("Projectile Prefabs")]
    [SerializeField] private GameObject[] _humanItemPrefabs;
    [SerializeField] private GameObject[] _zombieItemPrefabs;
    [Header("Config")]
    [SerializeField] private float _minThrowPower = 4f;
    [SerializeField] private float _maxThrowPower = 10f;
    [SerializeField, Range(0.1f, 0.5f)] private float _maxPullScreenFraction = 0.28f;
    [SerializeField, Range(10f, 45f)] private float _minAimAngle = 18f;
    [SerializeField, Range(45f, 80f)] private float _maxAimAngle = 70f;
    [SerializeField] private float _windEffect = 1.8f;
    [SerializeField] private float _projectileMass = 0.35f;
    [SerializeField] private PowerBarUI _powerBarUI;
    [SerializeField] private LineRenderer _trajectoryLine;
    [Header("Side Tags")]
    [SerializeField] private string _playerTag = "Adventurer";
    [SerializeField] private string _enemyTag = "Skeleton";
    [SerializeField] private string _headTag = "Head";
    [SerializeField] private string _bodyTag = "Body";
    [Header("Resolve")]
    [SerializeField] private float _resolveDelay = 1.2f;

    private float _charge01;
    private bool _isCharging;
    private bool _chargingAsPlayer = true;
    private Vector2 _dragStartScreen;
    private float _aimAngleDegrees = 38f;
    private float _playerAimAngle = 38f;
    private float _enemyAimAngle = 38f;
    private bool _hasPlayerAim;
    private bool _hasEnemyAim;
    private Action _pendingResolve;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        EnsureTrajectoryLine();
        ResolveSceneReferences();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (UIManager.Instance != null && !UIManager.Instance.IsGameplayActive) return;
        if (TurnManager.Instance == null) return;
        if (TurnManager.Instance.IsWaitingForHit || TurnManager.Instance.IsAiTurn) return;

        bool playerTurn = TurnManager.Instance.CurrentTurn == TurnManager.Turn.Player;
        bool enemyHumanTurn = TurnManager.Instance.CurrentTurn == TurnManager.Turn.Enemy
                              && TurnManager.Instance.NumPlayers == 2;
        if (!playerTurn && !enemyHumanTurn) return;

        if (!TryGetPointer(out Vector2 pointerPosition, out PointerPhase pointerPhase))
            return;

        if (pointerPhase == PointerPhase.Began)
        {
            _isCharging = true;
            _chargingAsPlayer = playerTurn;
            _dragStartScreen = pointerPosition;
            _charge01 = 0f;
            TurnManager.Instance.StopTimer();
            _powerBarUI?.HideTimeWarnings();
            ShowChargeBar(_chargingAsPlayer, 0f);
        }

        if (_isCharging)
        {
            UpdateDragAim(pointerPosition);
            ShowChargeBar(_chargingAsPlayer, _charge01);
            UpdateTrajectoryPreview(_chargingAsPlayer, _charge01, _aimAngleDegrees, null);
        }

        if (pointerPhase == PointerPhase.Ended && _isCharging)
            ConfirmCharge();
    }

    private enum PointerPhase { Began, Held, Ended }

    private static bool TryGetPointer(out Vector2 position, out PointerPhase phase)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            position = touch.position;
            phase = touch.phase == TouchPhase.Began
                ? PointerPhase.Began
                : touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled
                    ? PointerPhase.Ended
                    : PointerPhase.Held;
            return true;
        }

        position = Input.mousePosition;
        if (Input.GetMouseButtonDown(0)) phase = PointerPhase.Began;
        else if (Input.GetMouseButtonUp(0)) phase = PointerPhase.Ended;
        else if (Input.GetMouseButton(0)) phase = PointerPhase.Held;
        else
        {
            phase = PointerPhase.Held;
            return false;
        }
        return true;
    }

    private void UpdateDragAim(Vector2 pointerPosition)
    {
        // Angry Birds gesture: pull opposite the throw. Pull distance controls force; pulling
        // downward raises the launch angle.
        Vector2 pull = _dragStartScreen - pointerPosition;
        float maxPullPixels = Mathf.Min(Screen.width, Screen.height) * _maxPullScreenFraction;
        ComputeDragAim(
            pull,
            maxPullPixels,
            _minAimAngle,
            _maxAimAngle,
            out _charge01,
            out _aimAngleDegrees);
    }

    public static void ComputeDragAim(
        Vector2 pull,
        float maxPullPixels,
        float minAngle,
        float maxAngle,
        out float power01,
        out float angleDegrees)
    {
        power01 = Mathf.Clamp01(pull.magnitude / Mathf.Max(1f, maxPullPixels));
        float verticalShare = Mathf.Clamp01(pull.y / Mathf.Max(1f, pull.magnitude));
        angleDegrees = Mathf.Lerp(minAngle, maxAngle, verticalShare);
    }

    private void ShowChargeBar(bool isPlayer, float t)
    {
        if (isPlayer)
        {
            _powerBarUI?.ShowHumanPowerBar(true);
            _powerBarUI?.SetHumanPower(t);
        }
        else
        {
            _powerBarUI?.ShowZombiePowerBar(true);
            _powerBarUI?.SetZombiePower(t);
        }
    }

    private void ConfirmCharge()
    {
        if (!_isCharging) return;
        _isCharging = false;
        HideTrajectoryPreview();

        float power = _charge01;
        bool isPlayer = _chargingAsPlayer;
        if (isPlayer)
        {
            _playerAimAngle = _aimAngleDegrees;
            _hasPlayerAim = true;
        }
        else
        {
            _enemyAimAngle = _aimAngleDegrees;
            _hasEnemyAim = true;
        }
        if (isPlayer) _powerBarUI?.ShowHumanPowerBar(false);
        else _powerBarUI?.ShowZombiePowerBar(false);
        TurnManager.Instance.OnPowerConfirmed(power, isPlayer);
    }

    public void ConfigureFromLevel(LevelConfig cfg)
    {
        ResolveSceneReferences();
        _minThrowPower = cfg.minThrowPower;
        _maxThrowPower = cfg.maxThrowPower;

        StopAllCoroutines();
        _isCharging = false;
        _pendingResolve = null;
        HideTrajectoryPreview();
        _powerBarUI?.HideBothBars();
        _powerBarUI?.HideTimeWarnings();
    }

    // power01 = 0..1 charge. specialType: null | "PowerThrow" | "DoubleAttack"
    public void ThrowCharged(bool isPlayer, float power01, string specialType, Action onResolved)
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver)
        {
            onResolved?.Invoke();
            return;
        }

        ResolveSceneReferences();

        Transform spawnPoint = isPlayer ? _humanHandSpawnPoint : _zombieHandSpawnPoint;
        Transform targetPoint = isPlayer ? _zombieHandSpawnPoint : _humanHandSpawnPoint;
        GameObject[] prefabs = GetPrefabsForSide(isPlayer);
        string ownerTag = isPlayer ? _playerTag : _enemyTag;
        if (spawnPoint == null || targetPoint == null || prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("ThrowManager: missing throw points or throwable prefabs.");
            onResolved?.Invoke();
            return;
        }

        GameObject prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
        GameObject item = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        float maxForce = _maxThrowPower;
        if (specialType == "PowerThrow") maxForce += 3f;

        Rigidbody rb = EnsureRigidbody(item);
        var projectile = EnsureProjectileItem(item);
        if (projectile != null)
        {
            projectile.OwnerTag = ownerTag;
            projectile.SideTags = new[] { _playerTag, _enemyTag };
            projectile.HeadTag = _headTag;
            projectile.BodyTag = _bodyTag;
            projectile.OnHit = (hitType, hitTag) => ApplyHit(hitType, hitTag, isPlayer, specialType);
        }

        if (rb != null)
        {
            float throwForce = Mathf.Lerp(_minThrowPower, maxForce, Mathf.Clamp01(power01));
            float angle = isPlayer && _hasPlayerAim
                ? _playerAimAngle
                : !isPlayer && _hasEnemyAim
                    ? _enemyAimAngle
                    : 38f;
            Vector3 throwDir = ComputeLobDirection(spawnPoint.position, targetPoint.position, angle);
            rb.mass = _projectileMass;
            rb.AddForce(throwDir * throwForce, ForceMode.Impulse);

            if (projectile != null && WindManager.Instance != null)
            {
                float windSign = WindManager.Instance.windDirection == WindDirection.Right ? 1f : -1f;
                projectile.WindAcceleration =
                    Vector3.right * (WindManager.Instance.windForce * _windEffect * windSign);
            }
        }

        _pendingResolve = onResolved;
        StartCoroutine(ResolveAfterDelay(onResolved));
    }

    private GameObject[] GetPrefabsForSide(bool isPlayer)
    {
        GameObject[] own = isPlayer ? _humanItemPrefabs : _zombieItemPrefabs;
        if (own != null && own.Length > 0) return own;

        // ponytail: while there are only a few throwables authored, the empty side reuses the
        // other side's pool. Upgrade path: split to side-specific pools once art choices settle.
        GameObject[] fallback = isPlayer ? _zombieItemPrefabs : _humanItemPrefabs;
        return fallback != null && fallback.Length > 0 ? fallback : own;
    }

    private static ProjectileItem EnsureProjectileItem(GameObject item)
    {
        var projectile = item.GetComponent<ProjectileItem>();
        return projectile != null ? projectile : item.AddComponent<ProjectileItem>();
    }

    private Rigidbody EnsureRigidbody(GameObject item)
    {
        var rb = item.GetComponent<Rigidbody>();
        if (rb == null) rb = item.AddComponent<Rigidbody>();

        rb.mass = Mathf.Max(0.05f, _projectileMass);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (item.GetComponentInChildren<Collider>() == null)
        {
            var col = item.AddComponent<SphereCollider>();
            col.radius = 0.25f;
        }

        return rb;
    }

    private void ResolveSceneReferences()
    {
        if (_humanHandSpawnPoint == null)
            _humanHandSpawnPoint = FindTransformWithTag(_playerThrowPointTag);
        if (_zombieHandSpawnPoint == null)
            _zombieHandSpawnPoint = FindTransformWithTag(_enemyThrowPointTag);
        if (_powerBarUI == null)
            _powerBarUI = FindAnyObjectByType<PowerBarUI>();
    }

    private static Transform FindTransformWithTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return null;

        try
        {
            GameObject go = GameObject.FindWithTag(tagName);
            return go != null ? go.transform : null;
        }
        catch (UnityException)
        {
            return null;
        }
    }

    public static Vector3 ComputeLobDirection(Vector3 from, Vector3 toward, float lobAngleDegrees)
    {
        Vector3 flat = toward - from;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.right;
        flat.Normalize();

        float up = Mathf.Tan(lobAngleDegrees * Mathf.Deg2Rad);
        return (flat + Vector3.up * up).normalized;
    }

    private void UpdateTrajectoryPreview(
        bool isPlayer,
        float power01,
        float angleDegrees,
        string specialType)
    {
        if (_trajectoryLine == null) return;

        Transform spawnPoint = isPlayer ? _humanHandSpawnPoint : _zombieHandSpawnPoint;
        Transform targetPoint = isPlayer ? _zombieHandSpawnPoint : _humanHandSpawnPoint;
        if (spawnPoint == null || targetPoint == null) return;

        float maxForce = _maxThrowPower;
        if (specialType == "PowerThrow") maxForce += 3f;

        float throwForce = Mathf.Lerp(_minThrowPower, maxForce, Mathf.Clamp01(power01));
        Vector3 dir = ComputeLobDirection(spawnPoint.position, targetPoint.position, angleDegrees);
        Vector3 velocity = dir * (throwForce / _projectileMass);
        Vector3 acceleration = Physics.gravity;

        if (WindManager.Instance != null)
        {
            float windSign = WindManager.Instance.windDirection == WindDirection.Right ? 1f : -1f;
            acceleration += Vector3.right * (WindManager.Instance.windForce * _windEffect * windSign);
        }

        const int steps = 28;
        const float stepTime = 0.07f;
        _trajectoryLine.positionCount = steps;
        _trajectoryLine.enabled = true;

        Vector3 pos = spawnPoint.position;
        Vector3 vel = velocity;
        for (int i = 0; i < steps; i++)
        {
            _trajectoryLine.SetPosition(i, pos);
            vel += acceleration * stepTime;
            pos += vel * stepTime;
        }
    }

    private void HideTrajectoryPreview()
    {
        if (_trajectoryLine == null) return;
        _trajectoryLine.enabled = false;
        _trajectoryLine.positionCount = 0;
    }

    private void EnsureTrajectoryLine()
    {
        if (_trajectoryLine != null) return;

        var go = new GameObject("TrajectoryPreview");
        go.transform.SetParent(transform, false);
        _trajectoryLine = go.AddComponent<LineRenderer>();
        _trajectoryLine.useWorldSpace = true;
        _trajectoryLine.startWidth = 0.07f;
        _trajectoryLine.endWidth = 0.02f;
        _trajectoryLine.numCapVertices = 4;
        _trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
        _trajectoryLine.startColor = new Color(1f, 0.86f, 0.25f, 0.75f);
        _trajectoryLine.endColor = new Color(1f, 0.45f, 0.15f, 0.35f);
        _trajectoryLine.enabled = false;
    }

    private IEnumerator ResolveAfterDelay(Action onResolved)
    {
        yield return new WaitForSeconds(_resolveDelay);
        if (_pendingResolve == onResolved) _pendingResolve = null;
        onResolved?.Invoke();
    }

    private void ApplyHit(ProjectileItem.HitType hitType, string hitTag, bool isPlayerShooter, string specialType)
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        bool hitEnemy = hitTag == _enemyTag;
        bool hitPlayer = hitTag == _playerTag;
        bool landed = hitType == ProjectileItem.HitType.Head || hitType == ProjectileItem.HitType.Body;
        if (!landed) return;

        int damage;
        if (specialType == "DoubleAttack")
            damage = GameManager.Instance.DoubleAttackDamage;
        else if (specialType == "PowerThrow")
            damage = GameManager.Instance.PowerThrowDamage;
        else
            damage = hitType == ProjectileItem.HitType.Head
                ? GameManager.Instance.HeadshotDamage
                : GameManager.Instance.BodyshotDamage;

        if (isPlayerShooter && hitEnemy) GameManager.Instance.HitEnemy(damage);
        else if (!isPlayerShooter && hitPlayer) GameManager.Instance.HitPlayer(damage);
    }
}
