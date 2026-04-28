using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen overlay with stats, score breakdown, final score, and restart. Shown when the player reaches the goal.
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    [Header("Time Bonus Tuning")]
    [SerializeField] float targetTimeSec = 200f;
    [SerializeField] float maxTimeBonus = 9000f;
    [SerializeField] float minTimeBonus = 0f;
    [Header("Health Bonus Tuning")]
    [SerializeField] float healthScoreMultiplier = 200f;

    GameObject overlayRoot;
    Font overlayFont;
    Text detailsText;
    Text finalScoreValueText;
    InputField teamNameInput;
    Button submitScoreButton;
    Text submitStatusText;

    RectTransform scoreboardListRoot;
    Text scoreboardEmptyText;
    Button editModeButton;
    Text editModeButtonText;
    InputField renameInput;
    Button renameButton;
    Button deleteButton;
    Text editStatusText;

    int finalScoreForSubmission;
    bool scoreSubmittedThisRun;
    bool isEditMode;
    int selectedScoreboardIndex = -1;

    void Awake()
    {
        BuildOverlayIfNeeded();
        SetOverlayVisible(false);
    }

    void Update()
    {
        if (overlayRoot == null || !overlayRoot.activeSelf)
            return;

        if ((teamNameInput != null && teamNameInput.isFocused) ||
            (renameInput != null && renameInput.isFocused))
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

        float fromPoints = points;
        float fromHealth = healthVal * healthScoreMultiplier;
        float fromTime = ComputeTimeBonus(timeSec);
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
                $"Score: {pointsRounded}  <color={yellow}>+{pointsRounded}</color>\n" +
                $"Lives: {healthVal}  <color={yellow}>+{healthRounded}</color>\n" +
                $"Time: {timeSecRounded} s  <color={yellow}>+{timeContribRounded}</color>";
        }

        if (finalScoreValueText != null)
            finalScoreValueText.text = finalRounded.ToString("N0");

        ResetSubmissionUiState();
        ResetEditUiState();
        RefreshScoreboardList();
        SetOverlayVisible(true);
        Time.timeScale = 0f;
    }

    float ComputeTimeBonus(float timeSec)
    {
        float safeTargetTime = Mathf.Max(0.0001f, targetTimeSec);
        float clampedTime = Mathf.Max(0f, timeSec);
        float normalized = Mathf.Clamp01(1f - (clampedTime / safeTargetTime));
        return Mathf.Lerp(minTimeBonus, maxTimeBonus, normalized);
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

        overlayFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Helvetica" }, 28);
        if (overlayFont == null)
            overlayFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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
        title.font = overlayFont;
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
        detailsText.font = overlayFont;
        detailsText.fontSize = 26;
        detailsText.color = new Color(0.95f, 0.95f, 0.95f);
        detailsText.supportRichText = true;
        detailsText.alignment = TextAnchor.MiddleCenter;

        GameObject finalLabelGo = new GameObject("FinalScoreLabel");
        finalLabelGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform finalLabelRt = finalLabelGo.AddComponent<RectTransform>();
        finalLabelRt.anchorMin = new Vector2(0.5f, 0.49f);
        finalLabelRt.anchorMax = new Vector2(0.5f, 0.49f);
        finalLabelRt.pivot = new Vector2(0.5f, 0.5f);
        finalLabelRt.sizeDelta = new Vector2(720f, 48f);
        finalLabelRt.anchoredPosition = Vector2.zero;
        Text finalLabel = finalLabelGo.AddComponent<Text>();
        finalLabel.font = overlayFont;
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
        finalScoreValueText.font = overlayFont;
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
        teamInputGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);
        teamNameInput = teamInputGo.AddComponent<InputField>();
        teamNameInput.lineType = InputField.LineType.SingleLine;
        teamNameInput.characterLimit = 32;

        GameObject teamInputTextGo = new GameObject("Text");
        teamInputTextGo.transform.SetParent(teamInputGo.transform, false);
        RectTransform teamInputTextRt = teamInputTextGo.AddComponent<RectTransform>();
        StretchWithPadding(teamInputTextRt, 14f, 8f);
        Text teamInputText = teamInputTextGo.AddComponent<Text>();
        teamInputText.font = overlayFont;
        teamInputText.fontSize = 24;
        teamInputText.color = new Color(0.1f, 0.1f, 0.1f);
        teamInputText.alignment = TextAnchor.MiddleLeft;
        teamInputText.supportRichText = false;

        GameObject teamPlaceholderGo = new GameObject("Placeholder");
        teamPlaceholderGo.transform.SetParent(teamInputGo.transform, false);
        RectTransform teamPlaceholderRt = teamPlaceholderGo.AddComponent<RectTransform>();
        StretchWithPadding(teamPlaceholderRt, 14f, 8f);
        Text teamPlaceholderText = teamPlaceholderGo.AddComponent<Text>();
        teamPlaceholderText.font = overlayFont;
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
        CreateButtonLabel(submitGo.transform, "Submit Score", 24);

        GameObject submitStatusGo = new GameObject("SubmitStatus");
        submitStatusGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform submitStatusRt = submitStatusGo.AddComponent<RectTransform>();
        submitStatusRt.anchorMin = new Vector2(0.5f, 0.265f);
        submitStatusRt.anchorMax = new Vector2(0.5f, 0.265f);
        submitStatusRt.pivot = new Vector2(0.5f, 0.5f);
        submitStatusRt.sizeDelta = new Vector2(760f, 32f);
        submitStatusRt.anchoredPosition = Vector2.zero;
        submitStatusText = submitStatusGo.AddComponent<Text>();
        submitStatusText.font = overlayFont;
        submitStatusText.fontSize = 21;
        submitStatusText.alignment = TextAnchor.MiddleCenter;
        submitStatusText.color = new Color(1f, 0.7f, 0.7f, 1f);
        submitStatusText.text = string.Empty;

        GameObject editModeGo = new GameObject("EditModeButton");
        editModeGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform editModeRt = editModeGo.AddComponent<RectTransform>();
        editModeRt.anchorMin = new Vector2(0.1f, 0.08f);
        editModeRt.anchorMax = new Vector2(0.1f, 0.08f);
        editModeRt.pivot = new Vector2(0.5f, 0.5f);
        editModeRt.sizeDelta = new Vector2(240f, 52f);
        editModeRt.anchoredPosition = Vector2.zero;
        editModeGo.AddComponent<Image>().color = new Color(0.35f, 0.25f, 0.2f, 1f);
        editModeButton = editModeGo.AddComponent<Button>();
        editModeButton.onClick.AddListener(ToggleEditMode);
        editModeButtonText = CreateButtonLabel(editModeGo.transform, "Edit Mode: Off", 23);

        GameObject boardLabelGo = new GameObject("ScoreboardLabel");
        boardLabelGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform boardLabelRt = boardLabelGo.AddComponent<RectTransform>();
        boardLabelRt.anchorMin = new Vector2(0.82f, 0.78f);
        boardLabelRt.anchorMax = new Vector2(0.82f, 0.78f);
        boardLabelRt.pivot = new Vector2(0.5f, 0.5f);
        boardLabelRt.sizeDelta = new Vector2(320f, 56f);
        boardLabelRt.anchoredPosition = Vector2.zero;
        Text boardLabel = boardLabelGo.AddComponent<Text>();
        boardLabel.font = overlayFont;
        boardLabel.fontSize = 30;
        boardLabel.fontStyle = FontStyle.Bold;
        boardLabel.color = Color.white;
        boardLabel.alignment = TextAnchor.MiddleCenter;
        boardLabel.text = "Scoreboard";

        GameObject boardPanelGo = new GameObject("ScoreboardPanel");
        boardPanelGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform boardPanelRt = boardPanelGo.AddComponent<RectTransform>();
        boardPanelRt.anchorMin = new Vector2(0.82f, 0.46f);
        boardPanelRt.anchorMax = new Vector2(0.82f, 0.46f);
        boardPanelRt.pivot = new Vector2(0.5f, 0.5f);
        boardPanelRt.sizeDelta = new Vector2(360f, 450f);
        boardPanelRt.anchoredPosition = Vector2.zero;

        GameObject boardListGo = new GameObject("ScoreboardList");
        boardListGo.transform.SetParent(boardPanelGo.transform, false);
        scoreboardListRoot = boardListGo.AddComponent<RectTransform>();
        scoreboardListRoot.anchorMin = new Vector2(0f, 1f);
        scoreboardListRoot.anchorMax = new Vector2(1f, 1f);
        scoreboardListRoot.pivot = new Vector2(0.5f, 1f);
        scoreboardListRoot.anchoredPosition = new Vector2(0f, -8f);
        scoreboardListRoot.sizeDelta = new Vector2(-24f, 260f);
        VerticalLayoutGroup layout = boardListGo.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 6f;

        GameObject emptyTextGo = new GameObject("ScoreboardEmptyText");
        emptyTextGo.transform.SetParent(boardPanelGo.transform, false);
        RectTransform emptyTextRt = emptyTextGo.AddComponent<RectTransform>();
        emptyTextRt.anchorMin = new Vector2(0f, 0.52f);
        emptyTextRt.anchorMax = new Vector2(1f, 0.92f);
        emptyTextRt.offsetMin = new Vector2(12f, 0f);
        emptyTextRt.offsetMax = new Vector2(-12f, 0f);
        scoreboardEmptyText = emptyTextGo.AddComponent<Text>();
        scoreboardEmptyText.font = overlayFont;
        scoreboardEmptyText.fontSize = 21;
        scoreboardEmptyText.color = new Color(0.95f, 0.95f, 0.95f);
        scoreboardEmptyText.alignment = TextAnchor.MiddleCenter;
        scoreboardEmptyText.text = "No teams submitted yet.";

        GameObject renameInputGo = new GameObject("RenameInput");
        renameInputGo.transform.SetParent(boardPanelGo.transform, false);
        RectTransform renameInputRt = renameInputGo.AddComponent<RectTransform>();
        renameInputRt.anchorMin = new Vector2(0f, 0.18f);
        renameInputRt.anchorMax = new Vector2(1f, 0.29f);
        renameInputRt.offsetMin = new Vector2(12f, 0f);
        renameInputRt.offsetMax = new Vector2(-12f, 0f);
        renameInputGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);
        renameInput = renameInputGo.AddComponent<InputField>();
        renameInput.lineType = InputField.LineType.SingleLine;
        renameInput.characterLimit = 32;

        GameObject renameTextGo = new GameObject("Text");
        renameTextGo.transform.SetParent(renameInputGo.transform, false);
        RectTransform renameTextRt = renameTextGo.AddComponent<RectTransform>();
        StretchWithPadding(renameTextRt, 10f, 8f);
        Text renameText = renameTextGo.AddComponent<Text>();
        renameText.font = overlayFont;
        renameText.fontSize = 20;
        renameText.color = new Color(0.1f, 0.1f, 0.1f);
        renameText.alignment = TextAnchor.MiddleLeft;

        GameObject renamePlaceholderGo = new GameObject("Placeholder");
        renamePlaceholderGo.transform.SetParent(renameInputGo.transform, false);
        RectTransform renamePlaceholderRt = renamePlaceholderGo.AddComponent<RectTransform>();
        StretchWithPadding(renamePlaceholderRt, 10f, 8f);
        Text renamePlaceholder = renamePlaceholderGo.AddComponent<Text>();
        renamePlaceholder.font = overlayFont;
        renamePlaceholder.fontSize = 20;
        renamePlaceholder.color = new Color(0.4f, 0.4f, 0.4f, 0.9f);
        renamePlaceholder.alignment = TextAnchor.MiddleLeft;
        renamePlaceholder.text = "Select team, then type new name";
        renameInput.textComponent = renameText;
        renameInput.placeholder = renamePlaceholder;

        GameObject renameBtnGo = new GameObject("RenameButton");
        renameBtnGo.transform.SetParent(boardPanelGo.transform, false);
        RectTransform renameBtnRt = renameBtnGo.AddComponent<RectTransform>();
        renameBtnRt.anchorMin = new Vector2(0f, 0.08f);
        renameBtnRt.anchorMax = new Vector2(0.48f, 0.16f);
        renameBtnRt.offsetMin = new Vector2(12f, 0f);
        renameBtnRt.offsetMax = new Vector2(-4f, 0f);
        renameBtnGo.AddComponent<Image>().color = new Color(0.2f, 0.4f, 0.65f, 1f);
        renameButton = renameBtnGo.AddComponent<Button>();
        renameButton.onClick.AddListener(ApplyRenameForSelectedEntry);
        CreateButtonLabel(renameBtnGo.transform, "Rename", 18);

        GameObject deleteBtnGo = new GameObject("DeleteButton");
        deleteBtnGo.transform.SetParent(boardPanelGo.transform, false);
        RectTransform deleteBtnRt = deleteBtnGo.AddComponent<RectTransform>();
        deleteBtnRt.anchorMin = new Vector2(0.52f, 0.08f);
        deleteBtnRt.anchorMax = new Vector2(1f, 0.16f);
        deleteBtnRt.offsetMin = new Vector2(4f, 0f);
        deleteBtnRt.offsetMax = new Vector2(-12f, 0f);
        deleteBtnGo.AddComponent<Image>().color = new Color(0.65f, 0.23f, 0.23f, 1f);
        deleteButton = deleteBtnGo.AddComponent<Button>();
        deleteButton.onClick.AddListener(DeleteSelectedEntry);
        CreateButtonLabel(deleteBtnGo.transform, "Delete", 18);

        GameObject editStatusGo = new GameObject("EditStatus");
        editStatusGo.transform.SetParent(boardPanelGo.transform, false);
        RectTransform editStatusRt = editStatusGo.AddComponent<RectTransform>();
        editStatusRt.anchorMin = new Vector2(0f, 0f);
        editStatusRt.anchorMax = new Vector2(1f, 0.07f);
        editStatusRt.offsetMin = new Vector2(12f, 0f);
        editStatusRt.offsetMax = new Vector2(-12f, 0f);
        editStatusText = editStatusGo.AddComponent<Text>();
        editStatusText.font = overlayFont;
        editStatusText.fontSize = 16;
        editStatusText.alignment = TextAnchor.MiddleCenter;
        editStatusText.color = new Color(1f, 0.7f, 0.7f, 1f);

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
        CreateButtonLabel(btnGo.transform, "Restart", 28);
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
        RefreshScoreboardList();
    }

    void ToggleEditMode()
    {
        isEditMode = !isEditMode;
        selectedScoreboardIndex = -1;
        if (renameInput != null)
            renameInput.text = string.Empty;
        SetEditStatus(isEditMode ? "Edit mode enabled. Click a team to edit." : "", true);
        RefreshEditControls();
        RefreshScoreboardList();
    }

    void ApplyRenameForSelectedEntry()
    {
        if (!isEditMode || selectedScoreboardIndex < 0)
            return;

        IReadOnlyList<ScoreboardStore.ScoreboardEntry> entries = ScoreboardStore.GetEntries();
        if (selectedScoreboardIndex >= entries.Count)
        {
            SetEditStatus("Selection is no longer valid.", false);
            selectedScoreboardIndex = -1;
            RefreshScoreboardList();
            return;
        }

        string oldName = entries[selectedScoreboardIndex].teamName;
        string newName = renameInput != null ? renameInput.text : string.Empty;
        if (!ScoreboardStore.TryRenameEntry(oldName, newName, out string error))
        {
            SetEditStatus(error, false);
            return;
        }

        string normalized = ScoreboardStore.NormalizeTeamName(newName);
        IReadOnlyList<ScoreboardStore.ScoreboardEntry> refreshed = ScoreboardStore.GetEntries();
        selectedScoreboardIndex = -1;
        for (int i = 0; i < refreshed.Count; i++)
        {
            if (ScoreboardStore.NormalizeTeamName(refreshed[i].teamName) == normalized)
            {
                selectedScoreboardIndex = i;
                break;
            }
        }

        SetEditStatus("Team renamed.", true);
        RefreshScoreboardList();
    }

    void DeleteSelectedEntry()
    {
        if (!isEditMode || selectedScoreboardIndex < 0)
            return;

        IReadOnlyList<ScoreboardStore.ScoreboardEntry> entries = ScoreboardStore.GetEntries();
        if (selectedScoreboardIndex >= entries.Count)
        {
            SetEditStatus("Selection is no longer valid.", false);
            selectedScoreboardIndex = -1;
            RefreshScoreboardList();
            return;
        }

        string selectedName = entries[selectedScoreboardIndex].teamName;
        if (!ScoreboardStore.TryDeleteEntry(selectedName, out string error))
        {
            SetEditStatus(error, false);
            return;
        }

        selectedScoreboardIndex = -1;
        if (renameInput != null)
            renameInput.text = string.Empty;
        SetEditStatus("Team deleted.", true);
        RefreshScoreboardList();
    }

    void OnScoreboardEntryClicked(int index)
    {
        if (!isEditMode)
            return;

        IReadOnlyList<ScoreboardStore.ScoreboardEntry> entries = ScoreboardStore.GetEntries();
        if (index < 0 || index >= entries.Count)
            return;

        selectedScoreboardIndex = index;
        if (renameInput != null)
            renameInput.text = entries[index].teamName;
        SetEditStatus($"Selected: {entries[index].teamName}", true);
        RefreshEditControls();
        RefreshScoreboardList();
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

    void ResetEditUiState()
    {
        isEditMode = false;
        selectedScoreboardIndex = -1;
        if (renameInput != null)
            renameInput.text = string.Empty;
        SetEditStatus("", true);
        RefreshEditControls();
    }

    void RefreshEditControls()
    {
        if (editModeButtonText != null)
            editModeButtonText.text = isEditMode ? "Edit Mode: On" : "Edit Mode: Off";

        if (renameInput != null)
            renameInput.gameObject.SetActive(isEditMode);
        if (renameButton != null)
            renameButton.gameObject.SetActive(isEditMode);
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(isEditMode);
        if (editStatusText != null)
            editStatusText.gameObject.SetActive(isEditMode);

        bool canEditSelection = isEditMode && selectedScoreboardIndex >= 0;
        if (renameInput != null)
            renameInput.interactable = canEditSelection;
        if (renameButton != null)
            renameButton.interactable = canEditSelection;
        if (deleteButton != null)
            deleteButton.interactable = canEditSelection;
    }

    void RefreshScoreboardList()
    {
        if (scoreboardListRoot == null)
            return;

        IReadOnlyList<ScoreboardStore.ScoreboardEntry> entries = ScoreboardStore.GetEntries();

        for (int i = scoreboardListRoot.childCount - 1; i >= 0; i--)
            Destroy(scoreboardListRoot.GetChild(i).gameObject);

        if (entries.Count == 0)
        {
            if (scoreboardEmptyText != null)
                scoreboardEmptyText.gameObject.SetActive(true);
            selectedScoreboardIndex = -1;
            RefreshEditControls();
            return;
        }

        if (scoreboardEmptyText != null)
            scoreboardEmptyText.gameObject.SetActive(false);
        if (selectedScoreboardIndex >= entries.Count)
            selectedScoreboardIndex = -1;

        for (int i = 0; i < entries.Count; i++)
        {
            ScoreboardStore.ScoreboardEntry entry = entries[i];
            GameObject rowGo = new GameObject($"Entry_{i + 1}");
            rowGo.transform.SetParent(scoreboardListRoot, false);

            LayoutElement rowLayout = rowGo.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 44f;

            Image rowImage = rowGo.AddComponent<Image>();
            bool isSelected = i == selectedScoreboardIndex;
            rowImage.color = isSelected ? new Color(0.28f, 0.36f, 0.52f, 0.9f) : new Color(1f, 1f, 1f, 0.08f);

            Button rowButton = rowGo.AddComponent<Button>();
            rowButton.interactable = isEditMode;
            int capturedIndex = i;
            rowButton.onClick.AddListener(() => OnScoreboardEntryClicked(capturedIndex));

            GameObject rowTextGo = new GameObject("Text");
            rowTextGo.transform.SetParent(rowGo.transform, false);
            RectTransform rowTextRt = rowTextGo.AddComponent<RectTransform>();
            StretchWithPadding(rowTextRt, 10f, 6f);
            Text rowText = rowTextGo.AddComponent<Text>();
            rowText.font = overlayFont;
            rowText.fontSize = 19;
            rowText.color = Color.white;
            rowText.alignment = TextAnchor.UpperLeft;
            rowText.text = $"{i + 1}. {entry.teamName} - {entry.score:N0}";
        }

        RefreshEditControls();
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

    void SetEditStatus(string message, bool success)
    {
        if (editStatusText == null)
            return;

        editStatusText.text = message;
        editStatusText.color = success
            ? new Color(0.7f, 1f, 0.7f, 1f)
            : new Color(1f, 0.7f, 0.7f, 1f);
    }

    Text CreateButtonLabel(Transform parent, string text, int fontSize)
    {
        GameObject labelGo = new GameObject("Text");
        labelGo.transform.SetParent(parent, false);
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        StretchFull(labelRt);
        Text label = labelGo.AddComponent<Text>();
        label.font = overlayFont;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = text;
        return label;
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
