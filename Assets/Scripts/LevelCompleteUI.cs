using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen overlay with final score and restart. Shown when the player reaches the goal.
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    GameObject overlayRoot;
    Text scoreText;

    void Awake()
    {
        BuildOverlayIfNeeded();
        SetOverlayVisible(false);
    }

    public void Show(int finalScore)
    {
        if (scoreText != null)
            scoreText.text = "Final score: " + finalScore.ToString();
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
        titleRt.anchorMin = new Vector2(0.5f, 0.62f);
        titleRt.anchorMax = new Vector2(0.5f, 0.62f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(720f, 100f);
        titleRt.anchoredPosition = Vector2.zero;
        Text title = titleGo.AddComponent<Text>();
        title.font = font;
        title.fontSize = 48;
        title.fontStyle = FontStyle.Bold;
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;
        title.text = "Finished";

        GameObject scoreGo = new GameObject("FinalScore");
        scoreGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform scoreRt = scoreGo.AddComponent<RectTransform>();
        scoreRt.anchorMin = new Vector2(0.5f, 0.52f);
        scoreRt.anchorMax = new Vector2(0.5f, 0.52f);
        scoreRt.pivot = new Vector2(0.5f, 0.5f);
        scoreRt.sizeDelta = new Vector2(720f, 56f);
        scoreRt.anchoredPosition = Vector2.zero;
        scoreText = scoreGo.AddComponent<Text>();
        scoreText.font = font;
        scoreText.fontSize = 32;
        scoreText.color = new Color(0.95f, 0.95f, 0.95f);
        scoreText.alignment = TextAnchor.MiddleCenter;
        scoreText.text = "Final score: 0";

        GameObject btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.38f);
        btnRt.anchorMax = new Vector2(0.5f, 0.38f);
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
