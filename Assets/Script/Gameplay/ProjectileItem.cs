using UnityEngine;

public class ProjectileItem : MonoBehaviour
{
    public enum HitType { Head, Body, Ground, Wall }
    public string OwnerTag;
    public System.Action<HitType, string> OnHit;

    private bool hasHit = false;

    private string FindCharacterTag(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag("Human") || t.CompareTag("Zombie"))
                return t.tag;
            t = t.parent;
        }
        return null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        // ไม่ชนตัวเอง
        Transform t = collision.transform;
        bool isOwner = false;
        while (t != null)
        {
            if (t.CompareTag(OwnerTag))
            {
                isOwner = true;
                break;
            }
            t = t.parent;
        }
        if (isOwner)
        {
            hasHit = false;
            return;
        }
        hasHit = true;

        string targetCharTag = FindCharacterTag(collision.transform);
        Debug.Log("Hit with : " + collision.gameObject.tag + " | CharacterTag: " + targetCharTag);

        if (collision.gameObject.CompareTag("Head"))
        {
            OnHit?.Invoke(HitType.Head, targetCharTag);
        }
        else if (collision.gameObject.CompareTag("Body"))
        {
            OnHit?.Invoke(HitType.Body, targetCharTag);
        }
        else
        {
            OnHit?.Invoke(HitType.Ground, targetCharTag);
        }
        var col = GetComponent<Collider>();
        //if (col) col.enabled = false;
        Destroy(gameObject, 0.5f);
    }
}