using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField, Range(0f, 0.25f)] private float _throwReleaseDelay = 0.173f;

    private static readonly int ThrowState = Animator.StringToHash("Throw");
    private bool _hasThrowState;

    public float ThrowReleaseDelay => _throwReleaseDelay;

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
}
