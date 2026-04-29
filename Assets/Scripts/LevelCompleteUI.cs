using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Full-screen overlay with stats, score breakdown, final score, and restart. Shown when the player reaches the goal.
/// Uses UI Toolkit for the interface.
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] UIDocument endScreenDocument;

    [Header("Time Bonus Tuning")]
    [SerializeField] float targetTimeSec = 200f;
    [SerializeField] float maxTimeBonus = 9000f;
    [SerializeField] float minTimeBonus = 0f;

    [Header("Health Bonus Tuning")]
    [SerializeField] float healthScoreMultiplier = 200f;

    VisualElement root;
    VisualElement endScreenRoot;
    Label pointsLabel;
    Label pointsBonus;
    Label livesLabel;
    Label livesBonus;
    Label timeLabel;
    Label timeBonus;
    Label totalScoreValue;
    TextField teamNameInput;
    Button submitButton;
    Label submitStatus;
    Button restartButton;

    ScrollView scoreboardList;
    Label scoreboardEmptyText;
    Button editModeButton;
    VisualElement editControls;
    TextField renameInput;
    Button renameButton;
    Button deleteButton;
    Label editStatus;

    int finalScoreForSubmission;
    bool scoreSubmittedThisRun;
    bool isEditMode;
    int selectedScoreboardIndex = -1;
    bool isVisible;
    bool isInitialized;

    void OnEnable()
    {
        TryFindUIDocument();

        if (endScreenDocument == null)
        {
            Debug.LogError("LevelCompleteUI: No UIDocument found. Please assign one in the inspector or ensure an 'EndScreenUI' GameObject exists.");
            return;
        }

        root = endScreenDocument.rootVisualElement;
        if (root != null)
        {
            root.schedule.Execute(InitializeUI).ExecuteLater(0);
        }
    }

    void TryFindUIDocument()
    {
        if (endScreenDocument != null)
            return;

        endScreenDocument = GetComponent<UIDocument>();
        if (endScreenDocument != null)
            return;

        GameObject endScreenGo = GameObject.Find("EndScreenUI");
        if (endScreenGo != null)
            endScreenDocument = endScreenGo.GetComponent<UIDocument>();
    }

    void InitializeUI()
    {
        if (isInitialized)
            return;

        root = endScreenDocument.rootVisualElement;
        QueryElements();
        BindEvents();
        SetOverlayVisible(false);
        isInitialized = true;
    }

    void QueryElements()
    {
        if (root == null)
            return;

        endScreenRoot = root.Q<VisualElement>("EndScreenRoot");
        pointsLabel = root.Q<Label>("PointsLabel");
        pointsBonus = root.Q<Label>("PointsBonus");
        livesLabel = root.Q<Label>("LivesLabel");
        livesBonus = root.Q<Label>("LivesBonus");
        timeLabel = root.Q<Label>("TimeLabel");
        timeBonus = root.Q<Label>("TimeBonus");
        totalScoreValue = root.Q<Label>("TotalScoreValue");

        teamNameInput = root.Q<TextField>("TeamNameInput");
        submitButton = root.Q<Button>("SubmitButton");
        submitStatus = root.Q<Label>("SubmitStatus");
        restartButton = root.Q<Button>("RestartButton");

        scoreboardList = root.Q<ScrollView>("ScoreboardList");
        scoreboardEmptyText = root.Q<Label>("ScoreboardEmptyText");
        editModeButton = root.Q<Button>("EditModeButton");
        editControls = root.Q<VisualElement>("EditControls");
        renameInput = root.Q<TextField>("RenameInput");
        renameButton = root.Q<Button>("RenameButton");
        deleteButton = root.Q<Button>("DeleteButton");
        editStatus = root.Q<Label>("EditStatus");
    }

    void BindEvents()
    {
        if (submitButton != null)
            submitButton.clicked += OnSubmitScoreClicked;

        if (restartButton != null)
            restartButton.clicked += Restart;

        if (editModeButton != null)
            editModeButton.clicked += ToggleEditMode;

        if (renameButton != null)
            renameButton.clicked += ApplyRenameForSelectedEntry;

        if (deleteButton != null)
            deleteButton.clicked += DeleteSelectedEntry;
    }

    void OnDestroy()
    {
        if (submitButton != null)
            submitButton.clicked -= OnSubmitScoreClicked;

        if (restartButton != null)
            restartButton.clicked -= Restart;

        if (editModeButton != null)
            editModeButton.clicked -= ToggleEditMode;

        if (renameButton != null)
            renameButton.clicked -= ApplyRenameForSelectedEntry;

        if (deleteButton != null)
            deleteButton.clicked -= DeleteSelectedEntry;
    }

    void Update()
    {
        if (!isVisible)
            return;

        bool teamInputFocused = teamNameInput != null && teamNameInput.focusController?.focusedElement == teamNameInput;
        bool renameInputFocused = renameInput != null && renameInput.focusController?.focusedElement == renameInput;

        if (teamInputFocused || renameInputFocused)
            return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            Restart();
    }

    public void Show()
    {
        TryFindUIDocument();

        if (endScreenDocument == null)
        {
            Debug.LogError("LevelCompleteUI.Show(): No UIDocument found!");
            return;
        }

        if (!isInitialized)
        {
            root = endScreenDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("LevelCompleteUI.Show(): rootVisualElement is null!");
                return;
            }
            QueryElements();
            BindEvents();
            isInitialized = true;
        }

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

        if (pointsLabel != null)
            pointsLabel.text = $"Points: {pointsRounded}";
        if (pointsBonus != null)
            pointsBonus.text = $"+{pointsRounded}";

        if (livesLabel != null)
            livesLabel.text = $"Lives left: {healthVal}";
        if (livesBonus != null)
            livesBonus.text = $"+{healthRounded}";

        if (timeLabel != null)
            timeLabel.text = $"Completion time: {timeSecRounded}s";
        if (timeBonus != null)
            timeBonus.text = $"+{timeContribRounded}";

        if (totalScoreValue != null)
            totalScoreValue.text = finalRounded.ToString("N0");

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
        isVisible = visible;
        if (endScreenRoot != null)
        {
            if (visible)
                endScreenRoot.RemoveFromClassList("end-screen-hidden");
            else
                endScreenRoot.AddToClassList("end-screen-hidden");
        }
        else
        {
            Debug.LogWarning($"LevelCompleteUI.SetOverlayVisible({visible}): endScreenRoot is null!");
        }
    }

    void OnSubmitScoreClicked()
    {
        if (scoreSubmittedThisRun)
        {
            SetSubmitStatus("Score already submitted for this run.", false);
            return;
        }

        string teamName = teamNameInput != null ? teamNameInput.value : string.Empty;
        if (!ScoreboardStore.TryAddEntry(teamName, finalScoreForSubmission, out string error))
        {
            SetSubmitStatus(error, false);
            return;
        }

        scoreSubmittedThisRun = true;
        if (teamNameInput != null)
            teamNameInput.SetEnabled(false);
        if (submitButton != null)
            submitButton.SetEnabled(false);

        SetSubmitStatus("Score submitted successfully.", true);
        RefreshScoreboardList();
    }

    void ToggleEditMode()
    {
        isEditMode = !isEditMode;
        selectedScoreboardIndex = -1;
        if (renameInput != null)
            renameInput.value = string.Empty;
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
        string newName = renameInput != null ? renameInput.value : string.Empty;
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
            renameInput.value = string.Empty;
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
            renameInput.value = entries[index].teamName;
        SetEditStatus($"Selected: {entries[index].teamName}", true);
        RefreshEditControls();
        RefreshScoreboardList();
    }

    void ResetSubmissionUiState()
    {
        if (teamNameInput != null)
        {
            teamNameInput.value = string.Empty;
            teamNameInput.SetEnabled(true);
        }

        if (submitButton != null)
            submitButton.SetEnabled(true);

        SetSubmitStatus("", true);
    }

    void ResetEditUiState()
    {
        isEditMode = false;
        selectedScoreboardIndex = -1;
        if (renameInput != null)
            renameInput.value = string.Empty;
        SetEditStatus("", true);
        RefreshEditControls();
    }

    void RefreshEditControls()
    {
        if (editModeButton != null)
            editModeButton.text = isEditMode ? "Edit Mode: On" : "Edit Mode: Off";

        if (editControls != null)
        {
            editControls.RemoveFromClassList("edit-controls-visible");
            if (isEditMode)
                editControls.AddToClassList("edit-controls-visible");
        }

        bool canEditSelection = isEditMode && selectedScoreboardIndex >= 0;
        if (renameInput != null)
            renameInput.SetEnabled(canEditSelection);
        if (renameButton != null)
            renameButton.SetEnabled(canEditSelection);
        if (deleteButton != null)
            deleteButton.SetEnabled(canEditSelection);
    }

    void RefreshScoreboardList()
    {
        if (scoreboardList == null)
            return;

        scoreboardList.Clear();

        IReadOnlyList<ScoreboardStore.ScoreboardEntry> entries = ScoreboardStore.GetEntries();

        if (entries.Count == 0)
        {
            if (scoreboardEmptyText != null)
                scoreboardEmptyText.style.display = DisplayStyle.Flex;
            selectedScoreboardIndex = -1;
            RefreshEditControls();
            return;
        }

        if (scoreboardEmptyText != null)
            scoreboardEmptyText.style.display = DisplayStyle.None;
        if (selectedScoreboardIndex >= entries.Count)
            selectedScoreboardIndex = -1;

        for (int i = 0; i < entries.Count; i++)
        {
            ScoreboardStore.ScoreboardEntry entry = entries[i];
            int capturedIndex = i;
            bool isSelected = i == selectedScoreboardIndex;

            VisualElement row = new VisualElement();
            row.AddToClassList("scoreboard-entry");
            if (isSelected)
                row.AddToClassList("scoreboard-entry-selected");

            Label nameLabel = new Label($"{i + 1}. {entry.teamName}");
            nameLabel.AddToClassList("scoreboard-entry-text");
            row.Add(nameLabel);

            Label scoreLabel = new Label(entry.score.ToString("N0"));
            scoreLabel.AddToClassList("scoreboard-entry-score");
            row.Add(scoreLabel);

            row.RegisterCallback<ClickEvent>(evt => OnScoreboardEntryClicked(capturedIndex));

            scoreboardList.Add(row);
        }

        RefreshEditControls();
    }

    void SetSubmitStatus(string message, bool success)
    {
        if (submitStatus == null)
            return;

        submitStatus.text = message;
        submitStatus.RemoveFromClassList("submit-status-success");
        if (success && !string.IsNullOrEmpty(message))
            submitStatus.AddToClassList("submit-status-success");
    }

    void SetEditStatus(string message, bool success)
    {
        if (editStatus == null)
            return;

        editStatus.text = message;
        editStatus.RemoveFromClassList("edit-status-success");
        if (success && !string.IsNullOrEmpty(message))
            editStatus.AddToClassList("edit-status-success");
    }
}
