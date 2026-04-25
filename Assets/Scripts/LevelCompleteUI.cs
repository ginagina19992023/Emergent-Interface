using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text;

/// <summary>
/// Full-screen overlay with stats, score breakdown, final score, and restart. Shown when the player reaches the goal.
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    GameObject overlayRoot;
    Text detailsText;
    Text finalScoreValueText;
    InputField teamNameInput;
    Button submitScoreButton;
    Text submitStatusText;
    Text scoreboardText;
    int finalScoreForSubmission;
    bool scoreSubmittedThisRun;

    void Awake()
    {
        BuildOverlayIfNeeded();
        SetOverlayVisible(false);
    }

    void Update()
    {
        if (overlayRoot == null || !overlayRoot.activeSelf)
            return;

        if (teamNameInput != null && teamNameInput.isFocused)
            return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            Restart();
    }

    public void Show()
    {
        int points = PlayerScore.Instance != null ? PlayerScore.Instance.Score : 0;
        var health = FindFirstObjectByType<PlayerHealth>();
        int healthVal = health != null ? health.CurrentHealth : 0;
        var timer = FindFirstObjectByType<GameTimer>();
        float timeSec = timer != null ? timer.ElapsedSeconds : 0f;

        float safeTime = Mathf.Max(timeSec, 0.0001f);
        float fromPoints = points;
        float fromHealth = healthVal * 100f;
        float fromTime = (1f / safeTime) * 100000f;
        float finalScore = fromPoints + fromHealth + fromTime;

        int pointsRounded = Mathf.RoundToInt(fromPoints);
        int healthRounded = Mathf.RoundToInt(fromHealth);
        int timeContribRounded = Mathf.RoundToInt(fromTime);
        int timeSecRounded = Mathf.RoundToInt(timeSec);
        int finalRounded = Mathf.RoundToInt(finalScore);
        finalScoreForSubmission = finalRounded;
        scoreSubmittedThisRun = false;

        const string yellow = "#FFEB3B";
        if (detailsText != null)
        {
            detailsText.text =
                $"Points: {pointsRounded}  <color={yellow}>+{pointsRounded}</color>\n" +
                $"Lives: {healthVal}  <color={yellow}>+{healthRounded}</color>\n" +
                $"Time: {timeSecRounded} s  <color={yellow}>+{timeContribRounded}</color>";
        }

        if (finalScoreValueText != null)
            finalScoreValueText.text = finalRounded.ToString("N0");

        ResetSubmissionUiState();
        RefreshScoreboardText();
        SetOverlayVisible(true);
        Time.timeScale = 0f;
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

        overlayRoot = new GameObject("LevelCompleteOverlay");
        overlayRoot.transform.SetParent(transform, false);
        RectTransform rootRt = overlayRoot.AddComponent<RectTransform>();
        StretchFull(rootRt);

        GameObject dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform dimRt = dimGo.AddComponent<RectTransform>();
        StretchFull(dimRt);
        Image dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.78f);
        dim.raycastTarget = true;

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.78f);
        titleRt.anchorMax = new Vector2(0.5f, 0.78f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(720f, 80f);
        titleRt.anchoredPosition = Vector2.zero;
        Text title = titleGo.AddComponent<Text>();
        title.font = font;
        title.fontSize = 48;
        title.fontStyle = FontStyle.Bold;
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;
        title.text = "Finished";

        GameObject detailsGo = new GameObject("Details");
        detailsGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform detailsRt = detailsGo.AddComponent<RectTransform>();
        detailsRt.anchorMin = new Vector2(0.5f, 0.62f);
        detailsRt.anchorMax = new Vector2(0.5f, 0.62f);
        detailsRt.pivot = new Vector2(0.5f, 0.5f);
        detailsRt.sizeDelta = new Vector2(760f, 320f);
        detailsRt.anchoredPosition = Vector2.zero;
        detailsText = detailsGo.AddComponent<Text>();
        detailsText.font = font;
        detailsText.fontSize = 26;
        detailsText.color = new Color(0.95f, 0.95f, 0.95f);
        detailsText.supportRichText = true;
        detailsText.alignment = TextAnchor.MiddleCenter;
        detailsText.text =
            "Points: 0  <color=#FFEB3B>+0</color>\n" +
            "Lives: 0  <color=#FFEB3B>+0</color>\n" +
            "Time: 0 s  <color=#FFEB3B>+0</color>";

        GameObject finalLabelGo = new GameObject("FinalScoreLabel");
        finalLabelGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform finalLabelRt = finalLabelGo.AddComponent<RectTransform>();
        finalLabelRt.anchorMin = new Vector2(0.5f, 0.49f);
        finalLabelRt.anchorMax = new Vector2(0.5f, 0.49f);
        finalLabelRt.pivot = new Vector2(0.5f, 0.5f);
        finalLabelRt.sizeDelta = new Vector2(720f, 48f);
        finalLabelRt.anchoredPosition = Vector2.zero;
        Text finalLabel = finalLabelGo.AddComponent<Text>();
        finalLabel.font = font;
        finalLabel.fontSize = 36;
        finalLabel.fontStyle = FontStyle.Bold;
        finalLabel.color = Color.white;
        finalLabel.alignment = TextAnchor.MiddleCenter;
        finalLabel.text = "Final Score";

        GameObject finalValueGo = new GameObject("FinalScoreValue");
        finalValueGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform finalValueRt = finalValueGo.AddComponent<RectTransform>();
        finalValueRt.anchorMin = new Vector2(0.5f, 0.43f);
        finalValueRt.anchorMax = new Vector2(0.5f, 0.43f);
        finalValueRt.pivot = new Vector2(0.5f, 0.5f);
        finalValueRt.sizeDelta = new Vector2(720f, 96f);
        finalValueRt.anchoredPosition = Vector2.zero;
        finalScoreValueText = finalValueGo.AddComponent<Text>();
        finalScoreValueText.font = font;
        finalScoreValueText.fontSize = 64;
        finalScoreValueText.fontStyle = FontStyle.Bold;
        finalScoreValueText.color = new Color(1f, 0.92f, 0.45f);
        finalScoreValueText.alignment = TextAnchor.MiddleCenter;
        finalScoreValueText.text = "0";

        GameObject teamInputGo = new GameObject("TeamNameInput");
        teamInputGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform teamInputRt = teamInputGo.AddComponent<RectTransform>();
        teamInputRt.anchorMin = new Vector2(0.5f, 0.31f);
        teamInputRt.anchorMax = new Vector2(0.5f, 0.31f);
        teamInputRt.pivot = new Vector2(0.5f, 0.5f);
        teamInputRt.sizeDelta = new Vector2(360f, 48f);
        teamInputRt.anchoredPosition = new Vector2(-130f, 0f);
        Image teamInputBg = teamInputGo.AddComponent<Image>();
        teamInputBg.color = new Color(1f, 1f, 1f, 0.95f);
        teamNameInput = teamInputGo.AddComponent<InputField>();
        teamNameInput.lineType = InputField.LineType.SingleLine;
        teamNameInput.characterLimit = 32;

        GameObject teamInputTextGo = new GameObject("Text");
        teamInputTextGo.transform.SetParent(teamInputGo.transform, false);
        RectTransform teamInputTextRt = teamInputTextGo.AddComponent<RectTransform>();
        StretchWithPadding(teamInputTextRt, 14f, 8f);
        Text teamInputText = teamInputTextGo.AddComponent<Text>();
        teamInputText.font = font;
        teamInputText.fontSize = 24;
        teamInputText.color = new Color(0.1f, 0.1f, 0.1f);
        teamInputText.alignment = TextAnchor.MiddleLeft;
        teamInputText.supportRichText = false;

        GameObject teamPlaceholderGo = new GameObject("Placeholder");
        teamPlaceholderGo.transform.SetParent(teamInputGo.transform, false);
        RectTransform teamPlaceholderRt = teamPlaceholderGo.AddComponent<RectTransform>();
        StretchWithPadding(teamPlaceholderRt, 14f, 8f);
        Text teamPlaceholderText = teamPlaceholderGo.AddComponent<Text>();
        teamPlaceholderText.font = font;
        teamPlaceholderText.fontSize = 24;
        teamPlaceholderText.color = new Color(0.4f, 0.4f, 0.4f, 0.9f);
        teamPlaceholderText.alignment = TextAnchor.MiddleLeft;
        teamPlaceholderText.text = "Enter Team Name";

        teamNameInput.textComponent = teamInputText;
        teamNameInput.placeholder = teamPlaceholderText;

        GameObject submitGo = new GameObject("SubmitScoreButton");
        submitGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform submitRt = submitGo.AddComponent<RectTransform>();
        submitRt.anchorMin = new Vector2(0.5f, 0.31f);
        submitRt.anchorMax = new Vector2(0.5f, 0.31f);
        submitRt.pivot = new Vector2(0.5f, 0.5f);
        submitRt.sizeDelta = new Vector2(220f, 48f);
        submitRt.anchoredPosition = new Vector2(190f, 0f);
        submitGo.AddComponent<Image>().color = new Color(0.16f, 0.48f, 0.24f, 1f);
        submitScoreButton = submitGo.AddComponent<Button>();
        submitScoreButton.onClick.AddListener(OnSubmitScoreClicked);

        GameObject submitLabelGo = new GameObject("Text");
        submitLabelGo.transform.SetParent(submitGo.transform, false);
        RectTransform submitLabelRt = submitLabelGo.AddComponent<RectTransform>();
        StretchFull(submitLabelRt);
        Text submitLabel = submitLabelGo.AddComponent<Text>();
        submitLabel.font = font;
        submitLabel.fontSize = 24;
        submitLabel.color = Color.white;
        submitLabel.alignment = TextAnchor.MiddleCenter;
        submitLabel.text = "Submit Score";

        GameObject submitStatusGo = new GameObject("SubmitStatus");
        submitStatusGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform submitStatusRt = submitStatusGo.AddComponent<RectTransform>();
        submitStatusRt.anchorMin = new Vector2(0.5f, 0.265f);
        submitStatusRt.anchorMax = new Vector2(0.5f, 0.265f);
        submitStatusRt.pivot = new Vector2(0.5f, 0.5f);
        submitStatusRt.sizeDelta = new Vector2(760f, 32f);
        submitStatusRt.anchoredPosition = Vector2.zero;
        submitStatusText = submitStatusGo.AddComponent<Text>();
        submitStatusText.font = font;
        submitStatusText.fontSize = 21;
        submitStatusText.alignment = TextAnchor.MiddleCenter;
        submitStatusText.color = new Color(1f, 0.7f, 0.7f, 1f);
        submitStatusText.text = string.Empty;

        GameObject boardLabelGo = new GameObject("ScoreboardLabel");
        boardLabelGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform boardLabelRt = boardLabelGo.AddComponent<RectTransform>();
        boardLabelRt.anchorMin = new Vector2(0.82f, 0.78f);
        boardLabelRt.anchorMax = new Vector2(0.82f, 0.78f);
        boardLabelRt.pivot = new Vector2(0.5f, 0.5f);
        boardLabelRt.sizeDelta = new Vector2(320f, 56f);
        boardLabelRt.anchoredPosition = Vector2.zero;
        Text boardLabel = boardLabelGo.AddComponent<Text>();
        boardLabel.font = font;
        boardLabel.fontSize = 30;
        boardLabel.fontStyle = FontStyle.Bold;
        boardLabel.color = Color.white;
        boardLabel.alignment = TextAnchor.MiddleCenter;
        boardLabel.text = "Scoreboard";

        GameObject boardGo = new GameObject("ScoreboardText");
        boardGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform boardRt = boardGo.AddComponent<RectTransform>();
        boardRt.anchorMin = new Vector2(0.82f, 0.48f);
        boardRt.anchorMax = new Vector2(0.82f, 0.48f);
        boardRt.pivot = new Vector2(0.5f, 0.5f);
        boardRt.sizeDelta = new Vector2(360f, 420f);
        boardRt.anchoredPosition = Vector2.zero;
        scoreboardText = boardGo.AddComponent<Text>();
        scoreboardText.font = font;
        scoreboardText.fontSize = 22;
        scoreboardText.color = new Color(0.95f, 0.95f, 0.95f);
        scoreboardText.alignment = TextAnchor.UpperLeft;
        scoreboardText.horizontalOverflow = HorizontalWrapMode.Wrap;
        scoreboardText.verticalOverflow = VerticalWrapMode.Overflow;
        scoreboardText.text = "No teams submitted yet.";

        GameObject btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.08f);
        btnRt.anchorMax = new Vector2(0.5f, 0.08f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(220f, 56f);
        btnRt.anchoredPosition = Vector2.zero;
        btnGo.AddComponent<Image>().color = new Color(0.22f, 0.42f, 0.55f, 1f);
        Button btn = btnGo.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.32f, 0.52f, 0.65f);
        btn.colors = colors;
        btn.onClick.AddListener(Restart);

        GameObject btnLabelGo = new GameObject("Text");
        btnLabelGo.transform.SetParent(btnGo.transform, false);
        RectTransform labelRt = btnLabelGo.AddComponent<RectTransform>();
        StretchFull(labelRt);
        Text btnText = btnLabelGo.AddComponent<Text>();
        btnText.font = font;
        btnText.fontSize = 28;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.text = "Restart";
    }

    void OnSubmitScoreClicked()
    {
        if (scoreSubmittedThisRun)
        {
            SetSubmitStatus("Score already submitted for this run.", false);
            return;
        }

        string teamName = teamNameInput != null ? teamNameInput.text : string.Empty;
        if (!ScoreboardStore.TryAddEntry(teamName, finalScoreForSubmission, out string error))
        {
            SetSubmitStatus(error, false);
            return;
        }

        scoreSubmittedThisRun = true;
        if (teamNameInput != null)
            teamNameInput.interactable = false;
        if (submitScoreButton != null)
            submitScoreButton.interactable = false;

        SetSubmitStatus("Score submitted successfully.", true);
        RefreshScoreboardText();
    }

    void ResetSubmissionUiState()
    {
        if (teamNameInput != null)
        {
            teamNameInput.text = string.Empty;
            teamNameInput.interactable = true;
        }

        if (submitScoreButton != null)
            submitScoreButton.interactable = true;

        SetSubmitStatus("", true);
    }

    void SetSubmitStatus(string message, bool success)
    {
        if (submitStatusText == null)
            return;

        submitStatusText.text = message;
        submitStatusText.color = success
            ? new Color(0.7f, 1f, 0.7f, 1f)
            : new Color(1f, 0.7f, 0.7f, 1f);
    }

    void RefreshScoreboardText()
    {
        if (scoreboardText == null)
            return;

        var entries = ScoreboardStore.GetEntries();
        if (entries.Count == 0)
        {
            scoreboardText.text = "No teams submitted yet.";
            return;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            builder.Append(i + 1)
                .Append(". ")
                .Append(entry.teamName)
                .Append(" - ")
                .Append(entry.score.ToString("N0"));

            if (i < entries.Count - 1)
                builder.Append('\n');
        }

        scoreboardText.text = builder.ToString();
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void StretchWithPadding(RectTransform rt, float horizontalPadding, float verticalPadding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rt.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
    }
}
