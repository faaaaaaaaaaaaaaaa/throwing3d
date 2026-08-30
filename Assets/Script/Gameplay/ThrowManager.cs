using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

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
    [SerializeField] private float _minThrowPower = 2.4f;
    [SerializeField] private float _maxThrowPower = 4.6f;
    [SerializeField] private float _powerThrowBonus = 1.2f;
    [SerializeField] private float _fullChargeSeconds = 1.4f;
    [SerializeField] private float _lobAngleDegrees = 38f;
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

    public float FullChargeSeconds => _fullChargeSeconds;

    private float _charge01;
    private bool _isCharging;
    private bool _chargingAsPlayer = true;
    private Action _pendingResolve;

    // Charge input comes from the invisible PowerChargeArea over each character.
    // If no area exists in the scene we fall back to reading the raw pointer.
    private bool _areaPlayerDown;
    private bool _areaEnemyDown;
    private bool _hasChargeArea;
    private bool _wasInputDown;
    private PowerChargeArea[] _chargeAreas;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        EnsureTrajectoryLine();
        ResolveSceneReferences();
        RefreshChargeAreas();
    }

    // Called by PowerChargeArea (the invisible button over a character).
    public void SetChargeInput(bool isPlayerSide, bool isDown)
    {
        _hasChargeArea = true;
        if (isPlayerSide) _areaPlayerDown = isDown;
        else _areaEnemyDown = isDown;
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
        if (!playerTurn && !enemyHumanTurn)
        {
            _wasInputDown = false;
            return;
        }

        bool inputDown = GetChargeInputDown(playerTurn);
        ChargeEdges(_wasInputDown, inputDown, out bool began, out bool ended);
        _wasInputDown = inputDown;

        if (began)
        {
            _isCharging = true;
            _chargingAsPlayer = playerTurn;
            _charge01 = 0f;
            TurnManager.Instance.StopTimer();
            _powerBarUI?.HideTimeWarnings();
            ShowChargeBar(_chargingAsPlayer, 0f);
        }

        if (_isCharging)
        {
            _charge01 = Mathf.Clamp01(_charge01 + Time.deltaTime / Mathf.Max(0.35f, _fullChargeSeconds));
            ShowChargeBar(_chargingAsPlayer, _charge01);
            UpdateTrajectoryPreview(_chargingAsPlayer, _charge01, _lobAngleDegrees, null);

            if (_charge01 >= 1f)
                ConfirmCharge();
        }

        if (ended && _isCharging)
            ConfirmCharge();
    }

    // Press/release edges from a held-state bool (hold-to-charge input model).
    public static void ChargeEdges(bool wasDown, bool isDown, out bool began, out bool ended)
    {
        began = isDown && !wasDown;
        ended = !isDown && wasDown;
    }

    private bool GetChargeInputDown(bool playerTurn)
    {
        if (_hasChargeArea)
        {
            bool eventInputDown = playerTurn ? _areaPlayerDown : _areaEnemyDown;
            return eventInputDown || IsRawPointerInsideChargeArea(playerTurn);
        }

        // ponytail: fallback for scenes without a PowerChargeArea (mainly editor/mouse testing).
        // Skips taps that land on other UI so pause/item buttons don't start a charge.
        // Ceiling: touch IsPointerOverGameObject() uses the last pointer, not per-finger.
        if (!TryGetRawPointer(out _)) return false;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
        return true;
    }

    private void RefreshChargeAreas()
    {
        _chargeAreas = FindObjectsByType<PowerChargeArea>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        _hasChargeArea = _chargeAreas.Length > 0;
    }

    private bool IsRawPointerInsideChargeArea(bool playerTurn)
    {
        if (!TryGetRawPointer(out Vector2 screenPoint)) return false;

        if (_chargeAreas == null || _chargeAreas.Length == 0)
            RefreshChargeAreas();

        foreach (PowerChargeArea area in _chargeAreas)
        {
            if (area == null || !area.isActiveAndEnabled) continue;
            if (area.IsPlayerSide != playerTurn) continue;
            if (area.ContainsScreenPoint(screenPoint)) return true;
        }
        return false;
    }

    private static bool TryGetRawPointer(out Vector2 screenPoint)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            screenPoint = touch.position;
            return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
        }

        screenPoint = Input.mousePosition;
        return Input.GetMouseButton(0);
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
        if (isPlayer) _powerBarUI?.ShowHumanPowerBar(false);
        else _powerBarUI?.ShowZombiePowerBar(false);
        TurnManager.Instance.OnPowerConfirmed(power, isPlayer);
    }

    public void ConfigureFromLevel(LevelConfig cfg)
    {
        ResolveSceneReferences();
        _minThrowPower = cfg.minThrowPower;
        _maxThrowPower = cfg.maxThrowPower;
        // Higher chargeSpeed = shorter hold window (matches original JSON curve).
        _fullChargeSeconds = Mathf.Lerp(1.6f, 0.85f, (cfg.chargeSpeed - 12f) / 6f);

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
        if (specialType == "PowerThrow") maxForce += _powerThrowBonus;

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
            Vector3 throwDir = ComputeLobDirection(spawnPoint.position, targetPoint.position, _lobAngleDegrees);
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
        if (specialType == "PowerThrow") maxForce += _powerThrowBonus;

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
