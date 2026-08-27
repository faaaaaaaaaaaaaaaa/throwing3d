using UnityEngine;

public class ProjectileItem : MonoBehaviour
{
    public enum HitType { Head, Body, Ground, Wall }
    public string OwnerTag;
    public System.Action<HitType, string> OnHit;
    public Vector3 WindAcceleration;

    // Tags are populated by ThrowManager at spawn, so the projectile is theme-agnostic. Defaults
    // suit the dungeon theme (Adventurer vs Skeleton) if used standalone.
    public string[] SideTags = { "Adventurer", "Skeleton" };
    public string HeadTag = "Head";
    public string BodyTag = "Body";

    private bool hasHit;
    private Rigidbody cachedRigidbody;

    private void Awake() => cachedRigidbody = GetComponent<Rigidbody>();

    private void FixedUpdate()
    {
        if (!hasHit && cachedRigidbody != null && WindAcceleration.sqrMagnitude > 0f)
            cachedRigidbody.AddForce(WindAcceleration, ForceMode.Acceleration);
    }

    private string FindCharacterTag(Transform t)
    {
        while (t != null)
        {
            foreach (var side in SideTags)
                if (!string.IsNullOrEmpty(side) && t.CompareTag(side)) return t.tag;
            t = t.parent;
        }
        return null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        // Don't count hitting our own thrower.
        Transform t = collision.transform;
        while (t != null)
        {
            if (!string.IsNullOrEmpty(OwnerTag) && t.CompareTag(OwnerTag)) return;
            t = t.parent;
        }
        hasHit = true;

        string targetCharTag = FindCharacterTag(collision.transform);

        if (collision.gameObject.CompareTag(HeadTag))
            OnHit?.Invoke(HitType.Head, targetCharTag);
        else if (collision.gameObject.CompareTag(BodyTag))
            OnHit?.Invoke(HitType.Body, targetCharTag);
        else
            OnHit?.Invoke(HitType.Ground, targetCharTag);

        Destroy(gameObject, 0.5f);
    }
}
