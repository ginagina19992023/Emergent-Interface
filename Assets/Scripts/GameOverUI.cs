using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Listens for <see cref="PlayerHealth"/> death, shows a full-screen overlay with a restart button, and reloads the active scene.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Font uiFont;

    GameObject overlayRoot;

    void Awake()
    {
        BuildOverlayIfNeeded();
        SetOverlayVisible(false);
    }

    void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.OnPlayerDied += HandlePlayerDied;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnPlayerDied -= HandlePlayerDied;
    }

    void Update()
    {
        if (overlayRoot == null || !overlayRoot.activeSelf)
            return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            Restart();
    }

    void HandlePlayerDied()
    {
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

        Font font = uiFont;
        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Helvetica" }, 28);
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        overlayRoot = new GameObject("GameOverOverlay");
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
        titleRt.anchorMin = new Vector2(0.5f, 0.55f);
        titleRt.anchorMax = new Vector2(0.5f, 0.55f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(640f, 120f);
        titleRt.anchoredPosition = Vector2.zero;
        Text title = titleGo.AddComponent<Text>();
        title.font = font;
        title.fontSize = 52;
        title.fontStyle = FontStyle.Normal;
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;
        title.text = "Game Over";

        GameObject btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(overlayRoot.transform, false);
        RectTransform btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.42f);
        btnRt.anchorMax = new Vector2(0.5f, 0.42f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(240f, 50f);
        btnRt.anchoredPosition = Vector2.zero;
        Image btnImage = btnGo.AddComponent<Image>();
        btnImage.sprite = CreateRoundedRectSprite(256, 64, 12);
        btnImage.type = Image.Type.Sliced;
        btnImage.color = Color.white;
        Button btn = btnGo.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        Color primary = new Color(186f / 255f, 66f / 255f, 167f / 255f, 1f);
        colors.normalColor = primary;
        colors.highlightedColor = new Color(primary.r, primary.g, primary.b, 0.8f);
        colors.pressedColor = new Color(primary.r * 0.88f, primary.g * 0.88f, primary.b * 0.88f, 1f);
        colors.selectedColor = primary;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.2f;
        btn.colors = colors;
        btn.onClick.AddListener(Restart);

        GameObject btnLabelGo = new GameObject("Text");
        btnLabelGo.transform.SetParent(btnGo.transform, false);
        RectTransform labelRt = btnLabelGo.AddComponent<RectTransform>();
        StretchFull(labelRt);
        Text btnText = btnLabelGo.AddComponent<Text>();
        btnText.font = font;
        btnText.fontSize = 26;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.text = "RESTART";
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>9-sliced white rounded rect for uGUI; tinted by <see cref="Image.color"/> / button ColorBlock.</summary>
    static Sprite CreateRoundedRectSprite(int width, int height, int cornerRadius)
    {
        cornerRadius = Mathf.Clamp(cornerRadius, 1, Mathf.Min(width, height) / 2 - 1);
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inside = IsInsideRoundedRect(x, y, width, height, cornerRadius);
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        var border = new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, border);
    }

    static bool IsInsideRoundedRect(int x, int y, int w, int h, int r)
    {
        if (x >= r && x < w - r)
            return true;
        if (y >= r && y < h - r)
            return true;

        float fx = x + 0.5f;
        float fy = y + 0.5f;

        if (x < r && y < r)
        {
            float dx = fx - r;
            float dy = fy - r;
            return dx * dx + dy * dy <= r * r;
        }
        if (x >= w - r && y < r)
        {
            float dx = fx - (w - r);
            float dy = fy - r;
            return dx * dx + dy * dy <= r * r;
        }
        if (x < r && y >= h - r)
        {
            float dx = fx - r;
            float dy = fy - (h - r);
            return dx * dx + dy * dy <= r * r;
        }
        if (x >= w - r && y >= h - r)
        {
            float dx = fx - (w - r);
            float dy = fy - (h - r);
            return dx * dx + dy * dy <= r * r;
        }
        return false;
    }
}
