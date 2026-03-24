using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows <see cref="PlayerHealth"/> on a UI <see cref="Text"/>, matching <see cref="ScoreDisplay"/> layout. Assign an existing Text or leave empty to create one at runtime.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class HealthBarDisplay : MonoBehaviour
{
  [SerializeField] private Text healthText;
  [SerializeField] private PlayerHealth playerHealth;
  [SerializeField] private string prefix = "Health: ";
  [SerializeField] private int fontSize = 28;

  void Awake()
  {
    EnsureHealthText();
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

  private void EnsureHealthText()
  {
    if (healthText != null)
      return;

    GameObject textGo = new GameObject("HealthText");
    textGo.transform.SetParent(transform, false);
    RectTransform rt = textGo.AddComponent<RectTransform>();
    rt.anchorMin = new Vector2(0f, 1f);
    rt.anchorMax = new Vector2(0f, 1f);
    rt.pivot = new Vector2(0f, 1f);
    // Stack below ScoreDisplay row (same height as ScoreText: 56).
    rt.anchoredPosition = new Vector2(24f, -80f);
    rt.sizeDelta = new Vector2(480f, 56f);

    healthText = textGo.AddComponent<Text>();
    Font font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Helvetica" }, fontSize);
    if (font == null)
      font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    healthText.font = font;
    healthText.fontSize = fontSize;
    healthText.color = Color.white;
    healthText.alignment = TextAnchor.UpperLeft;
    healthText.text = prefix + "0";
  }

  private void Refresh(int current, int _)
  {
    if (healthText != null)
      healthText.text = prefix + current.ToString();
  }
}
