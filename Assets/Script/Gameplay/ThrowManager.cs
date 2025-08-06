using UnityEngine;

public class ThrowManager : MonoBehaviour
{
    [SerializeField] private Transform _zombieHandSpawnPoint;
    [SerializeField] private Transform _humanHandSpawnPoint;
    [SerializeField] private GameObject[] _humanItemPrefabs;
    [SerializeField] private GameObject[] _zombieItemPrefabs;
    [SerializeField] private float _minThrowPower = 4f;
    [SerializeField] private float _maxThrowPower = 8f;
    [SerializeField] private float _chargeSpeed = 18f; // unit/sec
    [SerializeField] private float _throwAngleY = 6f;
    [SerializeField] private bool _isHuman = true;
    [SerializeField] private PowerBarUI _powerBarUI;
    private float _currentPower;
    private bool _isCharging;
    private const string HumanTag = "Human";
    private const string ZombieTag = "Zombie";

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

    public void ThrowItem(float force, bool isHuman)
    {
        // select prefab from random
        Transform spawnPoint = isHuman ? _humanHandSpawnPoint : _zombieHandSpawnPoint;
        GameObject[] prefabs = isHuman ? _humanItemPrefabs : _zombieItemPrefabs;
        string ownerTag = isHuman ? "Human" : "Zombie";

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        GameObject item = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        // set ownerTag + set OnHit callback
        var projectile = item.GetComponent<ProjectileItem>();
        if (projectile != null)
        {
            projectile.OwnerTag = ownerTag;
            projectile.OnHit = (ProjectileItem.HitType hitType, string hitTag) =>
            {
                print($"OnHit: hitType={hitType}, hitTag={hitTag}");
                if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

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
            };
        }

        // add power
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 throwDir = (isHuman ? Vector3.left : Vector3.right) * force
                 + Vector3.up * _throwAngleY;
            rb.AddForce(throwDir, ForceMode.Impulse);

            Debug.DrawRay(spawnPoint.position, throwDir.normalized * 2, Color.red, 1f);
        }
    }
}