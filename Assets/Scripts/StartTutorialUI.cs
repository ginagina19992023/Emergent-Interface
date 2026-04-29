using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the scene's existing start screen object. Gameplay is paused until
/// Space or Shoot/Attack is pressed, then the screen is hidden.
/// </summary>
public class StartTutorialUI : MonoBehaviour
{
    [SerializeField] GameObject startScreenUI;

    InputAction attackAction;
    static bool sceneHookRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterSceneHook()
    {
        if (sceneHookRegistered)
            return;

        SceneManager.sceneLoaded += HandleSceneLoaded;
        sceneHookRegistered = true;
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstanceExists();
    }

    static void EnsureInstanceExists()
    {
        if (FindFirstObjectByType<StartTutorialUI>() != null)
            return;

        GameObject host = new GameObject("StartTutorialUI");
        host.AddComponent<StartTutorialUI>();
    }

    void Awake()
    {
        if (startScreenUI == null)
            startScreenUI = GameObject.Find("Start Screen UI");

        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
            attackAction = playerInput.actions["Attack"];

        Show();
    }

    void Update()
    {
        if (startScreenUI == null || !startScreenUI.activeSelf)
            return;

        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool shootPressed = attackAction != null && attackAction.WasPressedThisFrame();
        if (spacePressed || shootPressed)
            StartGame();
    }

    void Show()
    {
        SetOverlayVisible(true);
        Time.timeScale = 0f;
    }

    void StartGame()
    {
        SetOverlayVisible(false);
        Time.timeScale = 1f;
    }

    void SetOverlayVisible(bool visible)
    {
        if (startScreenUI != null)
            startScreenUI.SetActive(visible);
    }
}
