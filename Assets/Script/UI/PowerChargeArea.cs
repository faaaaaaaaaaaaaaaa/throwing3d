using UnityEngine;
using UnityEngine.EventSystems;

// Invisible button placed over a character. Holding it charges that side's throw.
// ThrowManager gates by whose turn it is, so tapping the enemy on your turn does nothing.
public class PowerChargeArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private bool _isPlayerSide = true;

    // Whether this area is currently held down. Check this to know if the area is "on".
    public bool IsPressed { get; private set; }
    public bool IsPlayerSide => _isPlayerSide;

    public void OnPointerDown(PointerEventData eventData) => SetPressed(true);

    public void OnPointerUp(PointerEventData eventData) => SetPressed(false);

    public bool ContainsScreenPoint(Vector2 screenPoint)
    {
        var rect = transform as RectTransform;
        if (rect == null) return false;

        var canvas = GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, camera);
    }

    // Release if the area is hidden mid-hold (e.g. turn ends) so charge can't stick.
    private void OnDisable() => SetPressed(false);

    private void SetPressed(bool pressed)
    {
        IsPressed = pressed;
        ThrowManager.Instance?.SetChargeInput(_isPlayerSide, pressed);
    }
}
