using System.Collections;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField, Range(0f, 1f)] private float _throwReleaseNormalizedTime = 0.578f;
    [SerializeField] private Transform _handReleasePoint;

    private static readonly int ThrowState = Animator.StringToHash("Throw");
    private bool _hasThrowState;
    private float _throwClipLength = 1.37f;

    public float ThrowReleaseNormalizedTime => _throwReleaseNormalizedTime;
    public Transform HandReleasePoint => _handReleasePoint;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogWarning($"CharacterAnimator: no Animator found under {name}.", this);
            return;
        }

        _hasThrowState = _animator.HasState(0, ThrowState);
        Debug.Assert(_hasThrowState,
            $"CharacterAnimator: Animator on {name} needs a state named Throw.", this);

        CacheThrowClipLength();
        ResolveHandReleasePoint();
    }

    public bool PlayThrow()
    {
        if (_animator == null || !_animator.isActiveAndEnabled || !_hasThrowState)
            return false;

        // Start the pose this frame; waiting for a trigger transition made the projectile lead the arm.
        _animator.Play(ThrowState, 0, 0f);
        _animator.Update(0f);
        return true;
    }

    public IEnumerator WaitForThrowRelease()
    {
        if (_animator == null || !_hasThrowState)
            yield break;

        const int layer = 0;
        float timeout = Mathf.Max(0.5f, _throwClipLength + 0.25f);
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(layer);
            if (state.shortNameHash == ThrowState &&
                state.normalizedTime >= _throwReleaseNormalizedTime)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning(
            $"CharacterAnimator: timed out waiting for throw release at {_throwReleaseNormalizedTime:P0} on {name}.",
            this);
    }

    private void CacheThrowClipLength()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null) return;

        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip == null || clip.name != "Throw") continue;
            _throwClipLength = Mathf.Max(0.1f, clip.length);
            return;
        }
    }

    private void ResolveHandReleasePoint()
    {
        if (_handReleasePoint != null) return;

        _handReleasePoint = FindChildTransform(transform, "handslot.l");
        if (_handReleasePoint == null)
            _handReleasePoint = FindChildTransform(transform, "handslot.r");
        if (_handReleasePoint == null)
            _handReleasePoint = FindChildTransform(transform, "hand.l");
        if (_handReleasePoint == null)
            _handReleasePoint = FindChildTransform(transform, "hand.r");
    }

    private static Transform FindChildTransform(Transform root, string childName)
    {
        if (root == null) return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }
}
