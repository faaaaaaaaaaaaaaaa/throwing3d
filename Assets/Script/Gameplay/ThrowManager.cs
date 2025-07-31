using UnityEngine;

public class ThrowManager : MonoBehaviour
{
    [SerializeField] private Transform _humanHandSpawnPoint;
    [SerializeField] private GameObject[] _humanItemPrefabs;
    [SerializeField] private float _throwPower = 10f;
    [SerializeField] private bool _isHuman;

    private void Update()
    {
        // test on pc
        if (Input.GetMouseButtonDown(0))
        {
            ThrowItem();
        }
    }

    public void ThrowItem()
    {
        // select prefab from random
        GameObject prefab = _humanItemPrefabs[Random.Range(0, _humanItemPrefabs.Length)];
        // create spawn point
        GameObject item = Instantiate(prefab, _humanHandSpawnPoint.position, Quaternion.identity);
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // shoot them out
            Vector3 throwDir = _isHuman ? Vector3.left : Vector3.right;

            rb.AddForce(throwDir * _throwPower, ForceMode.Impulse);

            // Optional: Debug direction
            Debug.DrawRay(_humanHandSpawnPoint.position, throwDir * 2, Color.red, 1f);
        }
    }
}