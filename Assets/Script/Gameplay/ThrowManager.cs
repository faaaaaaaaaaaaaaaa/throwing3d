using UnityEngine;

public class ThrowManager : MonoBehaviour
{
    [SerializeField] private Transform _humanHandSpawnPoint;
    [SerializeField] private GameObject[] _humanItemPrefabs;
    [SerializeField] private float _minThrowPower = 4f;
    [SerializeField] private float _maxThrowPower = 8f;
    [SerializeField] private float _chargeSpeed = 18f; // หน่วย/วินาที
    [SerializeField] private float _throwAngleY = 6f;
    [SerializeField] private bool _isHuman;
    [SerializeField] private PowerBarUI _powerBarUI;
    private float _currentPower;
    private bool _isCharging;

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
            ThrowItem(_currentPower);
        }
    }

    public void ThrowItem(float force)
    {
        // select prefab from random
        GameObject prefab = _humanItemPrefabs[Random.Range(0, _humanItemPrefabs.Length)];
        GameObject item = Instantiate(prefab, _humanHandSpawnPoint.position, Quaternion.identity);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 throwDir = (_isHuman ? Vector3.left : Vector3.right) * force
                 + Vector3.up * _throwAngleY;
            rb.AddForce(throwDir, ForceMode.Impulse);

            Debug.DrawRay(_humanHandSpawnPoint.position, throwDir.normalized * 2, Color.red, 1f);
        }
    }
}