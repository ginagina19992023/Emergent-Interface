using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// In-game pause: Esc toggles the <see cref="PauseScreenUI"/> overlay (UI Toolkit).
/// Only activates while <see cref="Time.timeScale"/> &gt; 0 so it does not fight other overlays.
/// </summary>
[DefaultExecutionOrder(100)]
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;

    VisualElement pauseRoot;
    Button continueButton;
    Button restartButton;
    InputAction attackAction;
    bool isPaused;
    bool isInitialized;

    void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            GameObject go = GameObject.Find("PauseScreenUI");
            if (go != null)
                uiDocument = go.GetComponent<UIDocument>();
        }

        if (uiDocument != null && uiDocument.rootVisualElement != null)
            uiDocument.rootVisualElement.schedule.Execute(InitializeUi).ExecuteLater(0);
    }

    void OnDisable()
    {
        UnbindUi();
        isInitialized = false;
    }

    void OnDestroy()
    {
        UnbindUi();
    }

    void InitializeUi()
    {
        if (isInitialized || uiDocument == null)
            return;

        VisualElement root = uiDocument.rootVisualElement;
        if (root == null)
            return;

        pauseRoot = root.Q<VisualElement>("PauseScreenRoot");
        continueButton = root.Q<Button>("ContinueButton");
        restartButton = root.Q<Button>("RestartButton");

        if (pauseRoot == null)
        {
            Debug.LogError("PauseMenuUI: PauseScreenRoot missing in PauseScreenUI.uxml.");
            return;
        }

        if (continueButton != null)
            continueButton.clicked += Resume;
        if (restartButton != null)
            restartButton.clicked += Restart;

        SetOverlayVisible(false);
        isInitialized = true;
    }

    void UnbindUi()
    {
        if (continueButton != null)
            continueButton.clicked -= Resume;
        if (restartButton != null)
            restartButton.clicked -= Restart;
    }

    void Update()
    {
        if (!isInitialized)
            return;

        if (attackAction == null)
            TryBindAttackAction();

        if (isPaused)
        {
            if (ShouldResume())
                Resume();
            return;
        }

        if (Time.timeScale > 0f && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Pause();
    }

    void TryBindAttackAction()
    {
        if (attackAction != null)
            return;

        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
            attackAction = playerInput.actions["Attack"];
    }

    bool ShouldResume()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;
        return attackAction != null && attackAction.WasPressedThisFrame();
    }

    void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SetOverlayVisible(true);
    }

    void Resume()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;
        SetOverlayVisible(false);
        ClearHelicopterBufferedInput();
    }

    void Restart()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetOverlayVisible(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    static void ClearHelicopterBufferedInput()
    {
        HelicopterInput hi = FindFirstObjectByType<HelicopterInput>();
        if (hi != null)
            hi.ResetSmoothedState();
    }

    void SetOverlayVisible(bool visible)
    {
        if (pauseRoot == null)
            return;

        if (visible)
            pauseRoot.RemoveFromClassList("pause-screen-hidden");
        else
            pauseRoot.AddToClassList("pause-screen-hidden");
    }
}
