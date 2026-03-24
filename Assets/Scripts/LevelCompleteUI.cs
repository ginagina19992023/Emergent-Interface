using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen overlay with stats, score breakdown, final score, and restart. Shown when the player reaches the goal.
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    GameObject overlayRoot;
    Text detailsText;
    Text finalScoreValueText;

    void Awake()
    {
        BuildOverlayIfNeeded();
        SetOverlayVisible(false);
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
        float fromTime = (1f / safeTime) * 10000f;
        float finalScore = fromPoints + fromHealth + fromTime;
        float finalScoreRounded = (float)Math.Round((double)finalScore, 1);

        if (detailsText != null)
        {
            detailsText.text =
                $"Points: {points}\n" +
                $"Health: {healthVal}\n" +
                $"Time: {timeSec:F2} s\n\n" +
                $"From points: +{fromPoints:N0}\n" +
                $"From health: +{fromHealth:N0}\n" +
                $"From time: +{fromTime:N1}";
        }

        if (finalScoreValueText != null)
            finalScoreValueText.text = finalScoreRounded.ToString("N1");

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
        detailsRt.anchorMin = new Vector2(0.5f, 0.52f);
        detailsRt.anchorMax = new Vector2(0.5f, 0.52f);
        detailsRt.pivot = new Vector2(0.5f, 0.5f);
        detailsRt.sizeDelta = new Vector2(760f, 320f);
        detailsRt.anchoredPosition = Vector2.zero;
        detailsText = detailsGo.AddComponent<Text>();
        detailsText.font = font;
        detailsText.fontSize = 26;
        detailsText.color = new Color(0.95f, 0.95f, 0.95f);
        detailsText.alignment = TextAnchor.MiddleCenter;
        detailsText.text = "Points: 0\nHealth: 0\nTime: 0.00 s";

        GameObject finalLabelGo = new GameObject("FinalScoreLabel");
        finalLabelGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform finalLabelRt = finalLabelGo.AddComponent<RectTransform>();
        finalLabelRt.anchorMin = new Vector2(0.5f, 0.30f);
        finalLabelRt.anchorMax = new Vector2(0.5f, 0.30f);
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
        finalValueRt.anchorMin = new Vector2(0.5f, 0.20f);
        finalValueRt.anchorMax = new Vector2(0.5f, 0.20f);
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

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
