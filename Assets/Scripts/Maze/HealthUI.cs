using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class HealthUI : MonoBehaviour
{
    [SerializeField] private AIHealth aiHealth;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField, Min(8f)] private float screenEdgeMargin = 24f;
    [SerializeField, Min(16f)] private float heartSize = 38f;
    [SerializeField, Min(0f)] private float heartSpacing = 7f;
    [SerializeField, Min(180f)] private float playerBarWidth = 460f;
    [SerializeField, Min(12f)] private float playerBarHeight = 24f;
    [SerializeField, Min(8f)] private float bottomMargin = 26f;

    private Canvas canvas;
    private Image[] hearts;
    private Image healthFill;
    private TMP_Text playerHealthLabel;
    private GameObject gameOverOverlay;
    private TMP_Text gameOverReason;
    private Button restartButton;
    private Sprite whiteSprite;
    private Sprite heartSprite;
    private Texture2D whiteTexture;
    private Texture2D heartTexture;
    private float lastCanvasWidth = -1f;

    private void Awake()
    {
        EnsureCanvas();
        EnsureEventSystem();
        BuildHud();
    }

    private void Start()
    {
        if (fontAsset == null && TMP_Settings.instance != null)
        {
            fontAsset = TMP_Settings.defaultFontAsset;
        }

        if (aiHealth == null)
        {
            aiHealth = FindAnyObjectByType<AIHealth>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (aiHealth != null)
        {
            aiHealth.HealthChanged += UpdateAIHealth;
            UpdateAIHealth(aiHealth.CurrentHealth, aiHealth.MaxHealth);
        }
        else
        {
            Debug.LogError("HealthUI could not find AIHealth in the scene.", this);
        }

        if (playerHealth != null)
        {
            playerHealth.HealthChanged += UpdatePlayerHealth;
            UpdatePlayerHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
        else
        {
            Debug.LogError("HealthUI could not find PlayerHealth in the scene.", this);
        }

        LayoutPlayerBar();
    }

    private void OnDestroy()
    {
        if (aiHealth != null)
        {
            aiHealth.HealthChanged -= UpdateAIHealth;
        }

        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdatePlayerHealth;
        }

        if (whiteSprite != null)
        {
            Destroy(whiteSprite);
        }

        if (heartSprite != null)
        {
            Destroy(heartSprite);
        }

        if (whiteTexture != null)
        {
            Destroy(whiteTexture);
        }

        if (heartTexture != null)
        {
            Destroy(heartTexture);
        }
    }

    private void Update()
    {
        if (canvas != null && !Mathf.Approximately(lastCanvasWidth, canvas.pixelRect.width))
        {
            LayoutPlayerBar();
        }
    }

    public void ShowGameOver(string reason)
    {
        gameOverReason.text = reason;
        gameOverOverlay.SetActive(true);
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void EnsureCanvas()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }

    private void BuildHud()
    {
        CreateHeartDisplay();
        CreatePlayerHealthBar();
        CreateGameOverOverlay();
    }

    private void CreateHeartDisplay()
    {
        RectTransform panel = CreateRect("AI Hearts", transform);
        SetAnchors(panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        panel.anchoredPosition = new Vector2(-screenEdgeMargin, -screenEdgeMargin);
        panel.sizeDelta = new Vector2(3f * heartSize + 2f * heartSpacing + 24f, heartSize + 20f);

        hearts = new Image[AIHealth.MaximumHearts];
        for (int i = 0; i < hearts.Length; i++)
        {
            RectTransform heartRect = CreateRect("Heart " + (i + 1), panel);
            SetAnchors(heartRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f));
            heartRect.sizeDelta = new Vector2(heartSize, heartSize);
            heartRect.anchoredPosition = new Vector2(-12f - i * (heartSize + heartSpacing), 0f);
            hearts[i] = SetImage(heartRect.gameObject, Color.red, GetHeartSprite());
            hearts[i].preserveAspect = true;
            hearts[i].raycastTarget = false;
        }
    }

    private void CreatePlayerHealthBar()
    {
        RectTransform panel = CreateRect("Player Health", transform);
        SetAnchors(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        panel.anchoredPosition = new Vector2(0f, bottomMargin);
        panel.sizeDelta = new Vector2(playerBarWidth, 66f);
        SetImage(panel.gameObject, new Color(0.055f, 0.045f, 0.06f, 0.84f), GetWhiteSprite());

        RectTransform backgroundRect = CreateRect("Health Bar Background", panel);
        SetAnchors(backgroundRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        backgroundRect.offsetMin = new Vector2(9f, -playerBarHeight - 8f);
        backgroundRect.offsetMax = new Vector2(-9f, -8f);
        SetImage(backgroundRect.gameObject, new Color(0.16f, 0.16f, 0.18f, 1f), GetWhiteSprite());

        RectTransform fillRect = CreateRect("Health Bar Fill", backgroundRect);
        SetAnchors(fillRect, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f));
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        healthFill = SetImage(fillRect.gameObject, new Color(0.2f, 0.82f, 0.35f), GetWhiteSprite());
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFill.fillAmount = 1f;
        healthFill.raycastTarget = false;

        RectTransform labelRect = CreateRect("Player HP Text", panel);
        SetAnchors(labelRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
        labelRect.anchoredPosition = new Vector2(0f, 5f);
        labelRect.sizeDelta = new Vector2(-18f, 24f);
        playerHealthLabel = CreateText("Player HP", labelRect, 17f, TextAlignmentOptions.Center);
        playerHealthLabel.text = "100 / 100 HP";
    }

    private void CreateGameOverOverlay()
    {
        RectTransform overlayRect = CreateRect("Game Over Overlay", transform);
        SetAnchors(overlayRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImage = SetImage(overlayRect.gameObject, new Color(0.015f, 0.012f, 0.025f, 0.82f), GetWhiteSprite());
        overlayImage.raycastTarget = true;
        gameOverOverlay = overlayRect.gameObject;
        gameOverOverlay.SetActive(false);

        RectTransform titleRect = CreateRect("Game Over Title", overlayRect);
        SetAnchors(titleRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        titleRect.anchoredPosition = new Vector2(0f, 88f);
        titleRect.sizeDelta = new Vector2(600f, 72f);
        CreateText("Game Over", titleRect, 52f, TextAlignmentOptions.Center).text = "GAME OVER";

        RectTransform reasonRect = CreateRect("Game Over Reason", overlayRect);
        SetAnchors(reasonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        reasonRect.anchoredPosition = new Vector2(0f, 28f);
        reasonRect.sizeDelta = new Vector2(680f, 44f);
        gameOverReason = CreateText("Game Over Reason Text", reasonRect, 25f, TextAlignmentOptions.Center);

        RectTransform buttonRect = CreateRect("Restart Button", overlayRect);
        SetAnchors(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        buttonRect.anchoredPosition = new Vector2(0f, -55f);
        buttonRect.sizeDelta = new Vector2(220f, 54f);
        Image buttonImage = SetImage(buttonRect.gameObject, new Color(0.62f, 0.12f, 0.16f, 1f), GetWhiteSprite());
        restartButton = buttonRect.gameObject.AddComponent<Button>();
        restartButton.targetGraphic = buttonImage;
        restartButton.onClick.AddListener(RestartCurrentScene);

        RectTransform buttonLabelRect = CreateRect("Restart Label", buttonRect);
        SetAnchors(buttonLabelRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        buttonLabelRect.offsetMin = Vector2.zero;
        buttonLabelRect.offsetMax = Vector2.zero;
        CreateText("Restart Text", buttonLabelRect, 23f, TextAlignmentOptions.Center).text = "RESTART";
    }

    private void UpdateAIHealth(int current, int maximum)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].color = i < current
                ? new Color(0.92f, 0.12f, 0.2f, 1f)
                : new Color(0.24f, 0.24f, 0.27f, 1f);
        }
    }

    private void UpdatePlayerHealth(float current, float maximum)
    {
        float normalizedHealth = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        healthFill.fillAmount = normalizedHealth;
        healthFill.color = normalizedHealth > 0.6f
            ? new Color(0.2f, 0.82f, 0.35f, 1f)
            : normalizedHealth > 0.3f
                ? new Color(0.96f, 0.72f, 0.16f, 1f)
                : new Color(0.9f, 0.16f, 0.19f, 1f);
        playerHealthLabel.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(maximum) + " HP";
    }

    private void LayoutPlayerBar()
    {
        if (canvas == null || healthFill == null)
        {
            return;
        }

        RectTransform panel = healthFill.transform.parent.parent as RectTransform;
        if (panel != null)
        {
            float availableWidth = Mathf.Max(180f, canvas.GetComponent<RectTransform>().rect.width - 2f * screenEdgeMargin);
            panel.sizeDelta = new Vector2(Mathf.Min(playerBarWidth, availableWidth), panel.sizeDelta.y);
        }

        lastCanvasWidth = canvas.pixelRect.width;
    }

    private TMP_Text CreateText(string objectName, RectTransform parent, float size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        if (text.font == null)
        {
            Debug.LogError("HealthUI requires a TMP font asset. Assign LiberationSans SDF in the Inspector.", this);
        }

        return text;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
    }

    private static Image SetImage(GameObject target, Color color, Sprite sprite)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.AddComponent<Image>();
        }

        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
        {
            whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whiteTexture.name = "Health UI White Pixel";
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            whiteSprite = Sprite.Create(whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        return whiteSprite;
    }

    private Sprite GetHeartSprite()
    {
        if (heartSprite != null)
        {
            return heartSprite;
        }

        const int textureSize = 32;
        heartTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        heartTexture.name = "Health UI Heart Placeholder";
        Color32[] pixels = new Color32[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float normalizedX = ((x + 0.5f) / textureSize * 2f - 1f) * 1.4f;
                float normalizedY = ((y + 0.5f) / textureSize * 2f - 1f) * 1.4f;
                float sum = normalizedX * normalizedX + normalizedY * normalizedY - 1f;
                float heartShape = sum * sum * sum
                    - normalizedX * normalizedX * normalizedY * normalizedY * normalizedY;
                pixels[y * textureSize + x] = heartShape <= 0f
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        heartTexture.SetPixels32(pixels);
        heartTexture.filterMode = FilterMode.Bilinear;
        heartTexture.Apply();
        heartSprite = Sprite.Create(
            heartTexture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
        heartSprite.name = "Health UI Heart Placeholder";
        return heartSprite;
    }
}
