using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Full-screen tutorial/start overlay shown on scene load. Gameplay is paused until dismissed via Play or Space.
/// </summary>
public class StartTutorialUI : MonoBehaviour
{
    GameObject overlayRoot;
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

        Canvas parentCanvas = FindFirstObjectByType<Canvas>();
        GameObject host = new GameObject("StartTutorialUI");
        if (parentCanvas != null)
            host.transform.SetParent(parentCanvas.transform, false);
        else
        {
            Canvas canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            host.AddComponent<CanvasScaler>();
            host.AddComponent<GraphicRaycaster>();
        }

        host.AddComponent<StartTutorialUI>();

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }
    }

    void Awake()
    {
        BuildOverlayIfNeeded();
        Show();
    }

    void Update()
    {
        if (overlayRoot == null || !overlayRoot.activeSelf)
            return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
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
        if (overlayRoot != null)
            overlayRoot.SetActive(visible);
    }

    void BuildOverlayIfNeeded()
    {
        if (overlayRoot != null)
            return;

        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Helvetica" }, 28);
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        overlayRoot = new GameObject("StartTutorialOverlay");
        overlayRoot.transform.SetParent(transform, false);
        RectTransform rootRt = overlayRoot.AddComponent<RectTransform>();
        StretchFull(rootRt);

        GameObject dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform dimRt = dimGo.AddComponent<RectTransform>();
        StretchFull(dimRt);
        Image dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.82f);
        dim.raycastTarget = true;

        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(920f, 820f);
        panelRt.anchoredPosition = Vector2.zero;
        Image panel = panelGo.AddComponent<Image>();
        panel.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(840f, 80f);
        titleRt.anchoredPosition = new Vector2(0f, -24f);
        Text title = titleGo.AddComponent<Text>();
        title.font = font;
        title.fontSize = 50;
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(1f, 0.93f, 0.35f);
        title.alignment = TextAnchor.MiddleCenter;
        title.text = "HOW TO PLAY";

        GameObject objectiveGo = new GameObject("Objective");
        objectiveGo.transform.SetParent(panelGo.transform, false);
        RectTransform objectiveRt = objectiveGo.AddComponent<RectTransform>();
        objectiveRt.anchorMin = new Vector2(0.5f, 1f);
        objectiveRt.anchorMax = new Vector2(0.5f, 1f);
        objectiveRt.pivot = new Vector2(0.5f, 1f);
        objectiveRt.sizeDelta = new Vector2(840f, 280f);
        objectiveRt.anchoredPosition = new Vector2(0f, -98f);
        Text objective = objectiveGo.AddComponent<Text>();
        objective.font = font;
        objective.fontSize = 31;
        objective.color = new Color(0.95f, 0.97f, 1f);
        objective.supportRichText = true;
        objective.alignment = TextAnchor.UpperLeft;
        objective.text =
            "Complete the course and aim for the highest score.\n\n" +
            "Your final score is based on:\n" +
            "- Score from crystals and gates\n" +
            "- Lives left\n" +
            "- Time left";

        GameObject targetsGo = new GameObject("Targets");
        targetsGo.transform.SetParent(panelGo.transform, false);
        RectTransform targetsRt = targetsGo.AddComponent<RectTransform>();
        targetsRt.anchorMin = new Vector2(0.5f, 1f);
        targetsRt.anchorMax = new Vector2(0.5f, 1f);
        targetsRt.pivot = new Vector2(0.5f, 1f);
        targetsRt.sizeDelta = new Vector2(400f, 120f);
        targetsRt.anchoredPosition = new Vector2(-210f, -360f);
        Text targets = targetsGo.AddComponent<Text>();
        targets.font = font;
        targets.fontSize = 29;
        targets.color = new Color(0.97f, 0.98f, 1f);
        targets.supportRichText = true;
        targets.alignment = TextAnchor.UpperLeft;
        targets.text = "<b>Crystals</b>\n- Destroy them by shooting";

        GameObject gatesGo = new GameObject("Gates");
        gatesGo.transform.SetParent(panelGo.transform, false);
        RectTransform gatesRt = gatesGo.AddComponent<RectTransform>();
        gatesRt.anchorMin = new Vector2(0.5f, 1f);
        gatesRt.anchorMax = new Vector2(0.5f, 1f);
        gatesRt.pivot = new Vector2(0.5f, 1f);
        gatesRt.sizeDelta = new Vector2(400f, 140f);
        gatesRt.anchoredPosition = new Vector2(210f, -360f);
        Text gates = gatesGo.AddComponent<Text>();
        gates.font = font;
        gates.fontSize = 29;
        gates.color = new Color(0.97f, 0.98f, 1f);
        gates.supportRichText = true;
        gates.alignment = TextAnchor.UpperLeft;
        gates.text =
            "<b>Gates</b>\n" +
            "- Go trough them to get points\n" +
            "- Blue gates give you a speed boost";

        GameObject dangerGo = new GameObject("Damage");
        dangerGo.transform.SetParent(panelGo.transform, false);
        RectTransform dangerRt = dangerGo.AddComponent<RectTransform>();
        dangerRt.anchorMin = new Vector2(0.5f, 1f);
        dangerRt.anchorMax = new Vector2(0.5f, 1f);
        dangerRt.pivot = new Vector2(0.5f, 1f);
        dangerRt.sizeDelta = new Vector2(840f, 72f);
        dangerRt.anchoredPosition = new Vector2(0f, -500f);
        Text danger = dangerGo.AddComponent<Text>();
        danger.font = font;
        danger.fontSize = 29;
        danger.color = new Color(1f, 0.78f, 0.78f);
        danger.supportRichText = true;
        danger.alignment = TextAnchor.UpperLeft;
        danger.text = "<b>Damage</b>\n- You lose a life if you bump into objects\n - Also watch out for the wizards' attacks!";

        GameObject controlsGo = new GameObject("Controls");
        controlsGo.transform.SetParent(panelGo.transform, false);
        RectTransform controlsRt = controlsGo.AddComponent<RectTransform>();
        controlsRt.anchorMin = new Vector2(0.5f, 1f);
        controlsRt.anchorMax = new Vector2(0.5f, 1f);
        controlsRt.pivot = new Vector2(0.5f, 1f);
        controlsRt.sizeDelta = new Vector2(840f, 142f);
        controlsRt.anchoredPosition = new Vector2(0f, -610f);
        Text controls = controlsGo.AddComponent<Text>();
        controls.font = font;
        controls.fontSize = 27;
        controls.color = new Color(0.95f, 0.97f, 1f);
        controls.supportRichText = true;
        controls.alignment = TextAnchor.UpperLeft;
        controls.text =
            "<b>Controls</b>\n" +
            "- Up/Down -> Biking\n" +
            "- Shoot -> Punch\n" +
            "- Steering -> Wheel";

        GameObject btnGo = new GameObject("PlayButton");
        btnGo.transform.SetParent(panelGo.transform, false);
        RectTransform btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.sizeDelta = new Vector2(230f, 62f);
        btnRt.anchoredPosition = new Vector2(0f, 10f);
        btnGo.AddComponent<Image>().color = new Color(0.20f, 0.52f, 0.30f);
        Button playButton = btnGo.AddComponent<Button>();
        ColorBlock colors = playButton.colors;
        colors.highlightedColor = new Color(0.28f, 0.63f, 0.37f);
        playButton.colors = colors;
        playButton.onClick.AddListener(StartGame);

        GameObject btnTextGo = new GameObject("Text");
        btnTextGo.transform.SetParent(btnGo.transform, false);
        RectTransform btnTextRt = btnTextGo.AddComponent<RectTransform>();
        StretchFull(btnTextRt);
        Text playText = btnTextGo.AddComponent<Text>();
        playText.font = font;
        playText.fontSize = 30;
        playText.fontStyle = FontStyle.Bold;
        playText.color = Color.white;
        playText.alignment = TextAnchor.MiddleCenter;
        playText.text = "Play";
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
