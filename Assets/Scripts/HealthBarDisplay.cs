using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows <see cref="PlayerHealth"/> as a horizontal fill bar. Assign references or leave empty to build UI at runtime.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class HealthBarDisplay : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Text labelText;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private int fontSize = 22;

    void Awake()
    {
        EnsureUi();
    }

    void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += Refresh;
            Refresh(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
        else
            Refresh(0, 1);
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= Refresh;
    }

    void EnsureUi()
    {
        if (fillImage != null)
            return;

        var root = (RectTransform)transform;

        GameObject panelGo = new GameObject("HealthBarPanel");
        panelGo.transform.SetParent(root, false);
        RectTransform panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(24f, -72f);
        panelRt.sizeDelta = new Vector2(220f, 36f);

        Image panelBg = panelGo.AddComponent<Image>();
        Sprite white = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        panelBg.sprite = white;
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        GameObject labelGo = new GameObject("HealthLabel");
        labelGo.transform.SetParent(panelRt, false);
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(0f, 1f);
        labelRt.anchoredPosition = new Vector2(4f, -2f);
        labelRt.sizeDelta = new Vector2(-8f, 18f);
        labelText = labelGo.AddComponent<Text>();
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Helvetica" }, fontSize);
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.font = font;
        labelText.fontSize = fontSize;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.UpperLeft;

        GameObject fillGo = new GameObject("HealthFill");
        fillGo.transform.SetParent(panelRt, false);
        RectTransform fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 0f);
        fillRt.pivot = new Vector2(0f, 0f);
        fillRt.anchoredPosition = new Vector2(4f, 4f);
        fillRt.sizeDelta = new Vector2(-8f, 14f);
        fillImage = fillGo.AddComponent<Image>();
        fillImage.sprite = white;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.color = new Color(0.25f, 0.85f, 0.35f, 1f);
        fillImage.fillAmount = 1f;
    }

    void Refresh(int current, int max)
    {
        max = Mathf.Max(1, max);
        if (labelText != null)
            labelText.text = $"Health: {current} / {max}";
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01((float)current / max);
    }
}
