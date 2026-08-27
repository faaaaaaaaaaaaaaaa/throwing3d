using UnityEngine;

// Login is intentionally out of the MVP loop (there is no backend yet). This just lets the optional
// login screen advance to the main menu.
// ponytail: wire real auth here later if it earns its keep; the plan keeps login out of v1.
public class LoginManager : MonoBehaviour
{
    // Hook to the login screen's Continue / Guest button.
    public void Continue()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowMainMenu();
    }
}
