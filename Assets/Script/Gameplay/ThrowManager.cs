using System;
using UnityEngine;

public class ThrowManager : MonoBehaviour
{
    [Header("Throw Points")]
    [SerializeField] private Transform _zombieHandSpawnPoint;
    [SerializeField] private Transform _humanHandSpawnPoint;
    [Header("Projectile Prefabs")]
    [SerializeField] private GameObject[] _humanItemPrefabs;
    [SerializeField] private GameObject[] _zombieItemPrefabs;
    [Header("Config")]
    [SerializeField] private float _minThrowPower = 4f;
    [SerializeField] private float _maxThrowPower = 8f;
    [SerializeField] private float _chargeSpeed = 18f; // unit/sec
    [SerializeField] private float _throwAngleY = 6f;
    [SerializeField] private float windEffect = 2f; // not use
    [SerializeField] private bool _isHuman = true;
    [SerializeField] private PowerBarUI _powerBarUI;
    private float _currentPower;
    private bool _isCharging;
    private const string HumanTag = "Human";
    private const string ZombieTag = "Zombie";
    private string _turnTag;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _isCharging = true;
            _currentPower = _minThrowPower;
            _powerBarUI.ShowHumanPowerBar(true);
        }

        if (_isCharging)
        {
            _currentPower += _chargeSpeed * Time.deltaTime;
            _currentPower = Mathf.Clamp(_currentPower, _minThrowPower, _maxThrowPower);

            // update UI
            float t = (_currentPower - _minThrowPower) / (_maxThrowPower - _minThrowPower);
            _powerBarUI.SetHumanPower(t); // chargePercent = 0-1

        }

        if (Input.GetMouseButtonUp(0) && _isCharging)
        {
            _isCharging = false;
            _powerBarUI.ShowHumanPowerBar(false);
            ThrowItem(_currentPower, true);

        }
    }

    public void ThrowItem(float force, bool isHuman) // TODO: add them when have turn system --> Action onHitCallback)
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;
        WindManager.Instance.RandomWind();
        // select prefab from random
        Transform spawnPoint = isHuman ? _humanHandSpawnPoint : _zombieHandSpawnPoint;
        GameObject[] prefabs = isHuman ? _humanItemPrefabs : _zombieItemPrefabs;
        string ownerTag = isHuman ? "Human" : "Zombie";

        GameObject prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
        GameObject item = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        // set ownerTag + set OnHit callback
        var projectile = item.GetComponent<ProjectileItem>();
        if (projectile != null)
        {
            projectile.OwnerTag = ownerTag;
            projectile.OnHit = (ProjectileItem.HitType hitType, string hitTag) =>
            {
                _turnTag = hitTag;
                print($"OnHit: hitType={hitType}, hitTag={hitTag}");

                int damage = -1;
                if (hitType == ProjectileItem.HitType.Head)
                {
                    damage = GameManager.Instance.HeadshotDamage;

                    if (hitTag == HumanTag)
                    {
                        GameManager.Instance.HitHuman(damage);
                    }
                    else if (hitTag == ZombieTag)
                    {
                        GameManager.Instance.HitZombie(damage);
                    }
                }
                else
                {
                    damage = GameManager.Instance.BodyshotDamage;

                    if (hitTag == HumanTag)
                    {
                        GameManager.Instance.HitHuman(damage);
                    }
                    else if (hitTag == ZombieTag)
                    {
                        GameManager.Instance.HitZombie(damage);
                    }
                }
                // TODO: add sfx vfx animation
                // onHitCallback?.Invoke();
            };

        }

        // add power
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            float angle = _throwAngleY; // เช่น 10 องศา
            Vector3 baseDir = isHuman ? Vector3.left : Vector3.right;
            Vector3 throwDir = Quaternion.AngleAxis(angle, Vector3.forward) * baseDir;

            float wind = WindManager.Instance ? WindManager.Instance.windForce : 0f;
            bool windRight = WindManager.Instance && WindManager.Instance.windDirection == WindDirection.Right;
            double finalPower = force;

            float curMin = _minThrowPower, curMax = _maxThrowPower;
            if ((ownerTag == HumanTag && windRight) || (ownerTag == ZombieTag && !windRight))
            {
                finalPower -= wind * 1f;
            }
            else
            {
                finalPower += wind * 3f;
            }
            finalPower = Mathf.Clamp01((float)finalPower);

            float throwForce = Mathf.Lerp(_minThrowPower, _maxThrowPower, (float)finalPower);
            rb.AddForce(throwDir.normalized * throwForce, ForceMode.Impulse);

            Debug.DrawRay(spawnPoint.position, throwDir.normalized * 2, Color.red, 1f);
        }
    }
}