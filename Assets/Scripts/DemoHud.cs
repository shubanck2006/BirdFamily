using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DemoHud : MonoBehaviour
{
    private static readonly Color Ink = new Color32(15, 36, 33, 255);
    private static readonly Color Panel = new Color32(19, 43, 39, 246);
    private static readonly Color Edge = new Color32(92, 125, 105, 255);
    private static readonly Color Cream = new Color32(255, 247, 222, 255);
    private static readonly Color Muted = new Color32(188, 210, 186, 255);
    private static readonly Color Mint = new Color32(190, 220, 156, 255);
    private static readonly Color Gold = new Color32(255, 203, 112, 255);
    private static readonly Color Well = new Color32(10, 30, 28, 255);

    private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    private static readonly Vector2 TopRight = Vector2.one;
    private static readonly Vector2 BottomLeft = Vector2.zero;
    private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    private static readonly Vector2 LeftCenter = new Vector2(0f, 0.5f);
    private static readonly Vector2 RightCenter = new Vector2(1f, 0.5f);

    private readonly List<UnityEngine.Object> artwork = new List<UnityEngine.Object>();
    private readonly List<RaycastResult> raycastHits = new List<RaycastResult>();
    private readonly Image[] chickSlots = new Image[5];
    private readonly Image[] chickIcons = new Image[5];

    private Font font;
    private Sprite rounded;
    private Sprite chick;
    private Sprite feather;
    private Sprite nest;
    private GraphicRaycaster raycaster;
    private RectTransform run;
    private RectTransform modal;
    private RectTransform modalFade;
    private RectTransform modalContent;
    private RectTransform briefing;
    private RectTransform paused;
    private RectTransform results;
    private CanvasGroup runGroup;
    private CanvasGroup toastGroup;
    private Text toast;
    private Text objective;
    private Text count;
    private Text timer;
    private Text hint;
    private Text pauseSummary;
    private Text finalTime;
    private Text parStatus;
    private Text parGoal;
    private float toastRemaining;
    private int previousRescued = -1;
    private int previousTotal = -1;
    private int previousSeconds = -1;
    private bool previousReturning;
    private Rect previousSafeArea;
    private Vector2Int previousScreenSize;

    public void Build(Action play, Action pause, Action resume, Action restart, float parTime)
    {
#if UNITY_2022_2_OR_NEWER
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        rounded = MakeSprite("Rounded panel", 32, (x, y) =>
        {
            Vector2 corner = new Vector2(Mathf.Max(0f, Mathf.Abs(x) - 0.42f),
                Mathf.Max(0f, Mathf.Abs(y) - 0.42f));
            return corner.sqrMagnitude <= 0.58f * 0.58f;
        }, 11);
        chick = MakeSprite("Chick silhouette", 40, (x, y) =>
            Ellipse(x, y, -0.15f, -0.25f, 0.64f, 0.48f) ||
            Ellipse(x, y, 0.31f, 0.31f, 0.38f, 0.40f) ||
            (x > 0.57f && x < 0.98f && Mathf.Abs(y - 0.26f) < (0.98f - x) * 0.47f));
        feather = MakeSprite("Feather silhouette", 48, (x, y) =>
        {
            float length = (x + y) * 0.7071f;
            float side = (x - y) * 0.7071f;
            return (Mathf.Abs(side) < 0.045f && length > -1.1f && length < 1.02f) ||
                (Ellipse(length, side, 0.23f, 0f, 0.76f, 0.34f) &&
                (side > 0.09f || Mathf.Abs(length + side * 1.05f - 0.21f) > 0.078f));
        });
        nest = MakeSprite("Nest silhouette", 40, (x, y) =>
            (Ellipse(x, y, 0f, -0.16f, 0.94f, 0.50f) &&
                (y < -0.07f || Mathf.Abs(x) > 0.66f) &&
                (Mathf.Abs(y + 0.23f + 0.08f * x) > 0.042f) &&
                (Mathf.Abs(y + 0.44f - 0.12f * x) > 0.041f)) ||
            Ellipse(x, y, -0.25f, 0.14f, 0.23f, 0.40f) ||
            Ellipse(x, y, 0.28f, 0.13f, 0.23f, 0.36f));

        if (EventSystem.current == null && FindObjectOfType<EventSystem>() == null)
        {
            GameObject input = new GameObject("Demo input", typeof(EventSystem), typeof(StandaloneInputModule));
            input.transform.SetParent(transform, false);
        }

        var canvasObject = new GameObject("Bird Family HUD", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.4f;
        raycaster = canvasObject.GetComponent<GraphicRaycaster>();

        run = Stretch("Flight HUD", canvas.transform);
        runGroup = run.gameObject.AddComponent<CanvasGroup>();
        BuildFlightHud(pause);

        modal = Stretch("Menu layer", canvas.transform);
        FullImage("Meadow shade", modal, new Color32(6, 23, 23, 102));
        var vignette = FullImage("Text backdrop", modal, new Color32(8, 26, 24, 226));
        vignette.sprite = MakeSprite("Left fade", 64, (x, y) => true, gradient: true);
        modalFade = vignette.rectTransform;
        modalFade.pivot = Vector2.one * 0.5f;
        modalFade.offsetMin = Vector2.zero;
        modalFade.offsetMax = Vector2.zero;

        modalContent = Stretch("Safe menu content", modal);
        briefing = CreateCard("Flight briefing", modalContent, 430, Application.isMobilePlatform ? 502 : 476);
        BuildBriefing(briefing, play, parTime);
        paused = CreateCard("Pause menu", modalContent, 430, Application.isMobilePlatform ? 408 : 370);
        BuildPause(paused, resume, restart);
        results = CreateCard("Flight results", modalContent, 430, Application.isMobilePlatform ? 430 : 404, true);
        BuildResults(results, restart);

        ApplySafeArea();
        UpdateRun(0, chickSlots.Length, 0f, false);
        ShowBriefing();
    }

    public void ShowBriefing()
    {
        ClearNotification();
        OpenMenu(briefing, false);
    }

    public void ShowRun()
    {
        modal.gameObject.SetActive(false);
        run.gameObject.SetActive(true);
        runGroup.alpha = 1f;
        runGroup.blocksRaycasts = true;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void ShowPause()
    {
        pauseSummary.text = count.text + " chicks gathered  /  " + timer.text + " elapsed";
        OpenMenu(paused, true);
    }

    public void ShowResults(float seconds, float parTime)
    {
        int centiseconds = Mathf.CeilToInt(seconds * 100f);
        finalTime.text = $"{FormatClock(centiseconds / 100)}.{centiseconds % 100:00}";
        parGoal.text = FormatClock(Mathf.FloorToInt(parTime));
        bool madePar = seconds <= parTime;
        parStatus.text = madePar ? "You met the target time." : "Try bringing everyone home within the target time.";
        parStatus.color = madePar ? Gold : Muted;
        OpenMenu(results, false);
    }

    public void UpdateRun(int rescued, int total, float seconds, bool returning)
    {
        int wholeSeconds = Mathf.FloorToInt(seconds);
        if (wholeSeconds != previousSeconds)
        {
            timer.text = FormatClock(wholeSeconds);
            previousSeconds = wholeSeconds;
        }

        if (rescued == previousRescued && total == previousTotal && returning == previousReturning)
            return;

        objective.text = returning ? "Return to the nest" : "Gather the chicks";
        hint.text = returning ? "Fly into the glowing nest ring with your flock." : "Fly close to a chick to bring it along.";
        count.text = $"{rescued} / {total}";
        count.color = returning ? Mint : Gold;
        for (int index = 0; index < chickSlots.Length; index++)
        {
            bool joined = index < rescued;
            chickSlots[index].gameObject.SetActive(index < total);
            chickSlots[index].color = joined ? Gold : Well;
            chickIcons[index].color = joined ? Ink : Muted;
        }
        previousRescued = rescued;
        previousTotal = total;
        previousReturning = returning;
    }

    public void Notify(string message)
    {
        toast.text = message;
        toastGroup.alpha = 1f;
        toastRemaining = 3.2f;
    }

    public void ClearNotification()
    {
        toastRemaining = 0f;
        toastGroup.alpha = 0f;
    }

    public bool IsPointerOverControl(Vector2 screenPosition)
    {
        if (modal.gameObject.activeSelf)
            return true;
        if (EventSystem.current == null)
            return false;
        raycastHits.Clear();
        raycaster.Raycast(new PointerEventData(EventSystem.current) { position = screenPosition }, raycastHits);
        return raycastHits.Count > 0;
    }

    private void Update()
    {
        if (modal == null)
            return;
        ApplySafeArea();
        if (modal.gameObject.activeSelf || toastRemaining <= 0f)
            return;
        toastRemaining = Mathf.Max(0f, toastRemaining - Time.unscaledDeltaTime);
        toastGroup.alpha = Mathf.Clamp01(toastRemaining / 0.45f);
    }

    private void BuildFlightHud(Action pause)
    {
        RectTransform mission = Card("Mission tracker", run, TopLeft, 28, -26, 344, 116);
        Label("Mission name", mission, 19, -12, 202, 20, "THE WAY HOME", 13, Muted, FontStyle.Bold);
        objective = Label("Current objective", mission, 18, -32, 304, 29, "", 23, Cream, FontStyle.Bold);
        count = Label("Flock count", mission, 260, -68, 65, 30, "", 21, Gold, FontStyle.Bold, TextAnchor.MiddleRight);
        for (int index = 0; index < chickSlots.Length; index++)
        {
            chickSlots[index] = Fill("Chick progress", mission, TopLeft, 18 + index * 39, -73, 32, 26, Well);
            chickIcons[index] = Icon("Chick", chickSlots[index].transform, 7, -4, 18, chick, Muted);
        }

        int pauseWidth = Application.isMobilePlatform ? 82 : 48;
        int headerHeight = Application.isMobilePlatform ? 82 : 60;
        int headerInset = (headerHeight - 60) / 2;
        RectTransform timeplate = Card("Flight time", run, TopRight, -36 - pauseWidth, -26, 110, headerHeight);
        Label("Time caption", timeplate, 0, -5 - headerInset, 110, 19, "ELAPSED", 11, Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
        timer = Label("Elapsed time", timeplate, 0, -23 - headerInset, 110, 29, "", 25, Cream, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform pauseButton = MakeButton("Pause flight", run, TopRight, -28, -26, pauseWidth, headerHeight, Panel, pause);
        Fill("Pause bar", pauseButton, TopLeft, pauseWidth / 2 - 7, -14 - headerInset, 4, 15, Cream, false);
        Fill("Pause bar", pauseButton, TopLeft, pauseWidth / 2 + 3, -14 - headerInset, 4, 15, Cream, false);
        Label("Pause shortcut", pauseButton, 0, -34 - headerInset, pauseWidth, 18,
            Application.isMobilePlatform ? "Pause" : "Esc", 12, Muted, FontStyle.Bold, TextAnchor.MiddleCenter);

        RectTransform guidance = Card("Flight guidance", run, BottomLeft, 28, 26, 350, 64);
        Label("Steering controls", guidance, 15, -7, 325, 20,
            Application.isMobilePlatform ? "TAP THE MEADOW TO STEER" : "CLICK TO STEER     •     WASD / ARROWS",
            12, Gold, FontStyle.Bold);
        hint = Label("Flight tip", guidance, 15, -29, 325, 24, "", 15, Cream);

        RectTransform notice = Card("Flight notification", run, BottomCenter, 0, 26, 376, 45);
        toastGroup = notice.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastGroup.blocksRaycasts = false;
        toastGroup.interactable = false;
        toast = Label("Notification text", notice, 8, 0, 360, 45, "", 15, Cream, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    private void BuildBriefing(RectTransform parent, Action play, float parTime)
    {
        Icon("Feather", parent, 24, -25, 34, feather, Gold);
        Label("Mission name", parent, 67, -26, 329, 28, "THE WAY HOME", 14, Gold, FontStyle.Bold);
        Label("Game title", parent, 24, -59, 382, 75, "Bird Family", 56, Cream, FontStyle.Bold);
        Label("Opening line", parent, 26, -139, 370, 44,
            "Five chicks are scattered across the meadow.", 19, Cream);

        Fill("Divider", parent, TopLeft, 26, -198, 378, 1, Edge, false);
        ObjectiveRow(parent, -211, chick, "Gather the flock", "Fly near all five chicks. They'll follow you.");
        ObjectiveRow(parent, -269, nest, "Bring everyone home", "Lead the flock back to the glowing nest.");

        Label("Controls", parent, 26, -337, 380, 21,
            Application.isMobilePlatform ? "Tap the meadow to steer." : "Click to steer. WASD or arrows also work.",
            14, Muted);
        TextButton(parent, -376, "Take flight", Gold, Ink, play, Application.isMobilePlatform ? null : "SPACE");
        Label("Timing objective", parent, 26, Application.isMobilePlatform ? -464 : -438, 376, 20,
            $"For a quick flight, aim for {parTime:0.#} seconds.", 13, Muted);
    }

    private void BuildPause(RectTransform parent, Action resume, Action restart)
    {
        Icon("Feather", parent, 24, -25, 32, feather, Gold);
        Label("Pause title", parent, 24, -65, 380, 66, "Flight paused", 44, Cream, FontStyle.Bold);
        pauseSummary = Label("Flight status", parent, 26, -138, 378, 29, "", 18, Muted);
        Fill("Divider", parent, TopLeft, 26, -188, 378, 1, Edge, false);
        TextButton(parent, -212, "Resume flight", Gold, Ink, resume, Application.isMobilePlatform ? null : "ESC");
        TextButton(parent, Application.isMobilePlatform ? -302 : -283, "Restart flight", Well, Cream, restart);
    }

    private void BuildResults(RectTransform parent, Action restart)
    {
        Icon("Nest", parent, 24, -24, 37, nest, Gold);
        Label("Result title", parent, 24, -65, 380, 64, "Flock reunited", 44, Cream, FontStyle.Bold);
        Label("Result description", parent, 26, -134, 375, 26, "All five chicks are home.", 19, Cream);

        RectTransform flight = Fill("Flight time tile", parent, TopLeft, 26, -185, 184, 76, Well).rectTransform;
        RectTransform target = Fill("Target time tile", parent, TopLeft, 220, -185, 184, 76, Well).rectTransform;
        Label("Flight time caption", flight, 12, -7, 160, 20, "FLIGHT TIME", 12, Muted, FontStyle.Bold);
        Label("Target time caption", target, 12, -7, 160, 20, "TARGET TIME", 12, Muted, FontStyle.Bold);
        finalTime = Label("Final time", flight, 12, -29, 160, 38, "", 29, Cream, FontStyle.Bold);
        parGoal = Label("Target time", target, 12, -29, 160, 38, "", 29, Cream, FontStyle.Bold);
        parStatus = Label("Target result", parent, 26, -271, 378, 25, "", 15, Muted);
        TextButton(parent, -325, "Fly again", Gold, Ink, restart);
    }

    private void ObjectiveRow(RectTransform parent, int y, Sprite symbol, string title, string details)
    {
        RectTransform badge = Fill("Objective symbol background", parent, TopLeft, 26, y - 5, 42, 42, Well).rectTransform;
        Icon("Objective symbol", badge, 8, -8, 26, symbol, Gold);
        Label("Objective title", parent, 81, y, 325, 24, title, 17, Cream, FontStyle.Bold);
        Label("Objective instructions", parent, 81, y - 25, 325, 24, details, 15, Muted);
    }

    private void TextButton(RectTransform parent, int y, string caption, Color fill, Color textColor,
        Action onClick, string shortcut = null)
    {
        int height = Application.isMobilePlatform ? 80 : 54;
        RectTransform button = MakeButton(caption, parent, TopLeft, 26, y, 378, height, fill, onClick);
        Label("Button label", button, 16, 0, 270, height, caption, 20, textColor, FontStyle.Bold);
        if (shortcut != null)
        {
            Color background = Color.Lerp(fill, textColor, 0.16f);
            Fill("Shortcut keycap", button, TopLeft, 303, -14, 59, 26, background);
            Label("Keyboard shortcut", button, 303, -14, 59, 26, shortcut, 12, textColor, FontStyle.Bold,
                TextAnchor.MiddleCenter);
        }
    }

    private RectTransform MakeButton(string name, Transform parent, Vector2 anchor, float x, float y,
        float width, float height, Color fill, Action onClick)
    {
        Image surface = Fill(name, parent, anchor, x, y, width, height, fill);
        surface.raycastTarget = true;
        var button = surface.gameObject.AddComponent<Button>();
        button.targetGraphic = surface;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color32(255, 245, 219, 255),
            pressedColor = new Color32(212, 218, 199, 255),
            selectedColor = Color.white,
            disabledColor = new Color32(177, 177, 177, 255),
            colorMultiplier = 1f,
            fadeDuration = 0.12f
        };
        button.onClick.AddListener(() => onClick?.Invoke());
        return surface.rectTransform;
    }

    private void OpenMenu(RectTransform page, bool showFlightHud)
    {
        run.gameObject.SetActive(showFlightHud);
        runGroup.alpha = 0.45f;
        runGroup.blocksRaycasts = false;
        modal.gameObject.SetActive(true);
        modalFade.localScale = new Vector3(page == results ? -1f : 1f, 1f, 1f);
        briefing.gameObject.SetActive(page == briefing);
        paused.gameObject.SetActive(page == paused);
        results.gameObject.SetActive(page == results);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void ApplySafeArea()
    {
        var screenSize = new Vector2Int(Screen.width, Screen.height);
        Rect safeArea = Screen.safeArea;
        if (screenSize.x <= 0 || screenSize.y <= 0 ||
            (safeArea == previousSafeArea && screenSize == previousScreenSize))
            return;

        Vector2 minimum = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
        Vector2 maximum = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
        run.anchorMin = modalContent.anchorMin = minimum;
        run.anchorMax = modalContent.anchorMax = maximum;
        run.offsetMin = run.offsetMax = Vector2.zero;
        modalContent.offsetMin = modalContent.offsetMax = Vector2.zero;
        previousSafeArea = safeArea;
        previousScreenSize = screenSize;
    }

    private RectTransform CreateCard(string name, Transform parent, int width, int height, bool right = false)
    {
        RectTransform card = Card(name, parent, right ? RightCenter : LeftCenter, right ? -50 : 50, 10, width, height);
        Fill("Card accent", card, TopLeft, 26, -1, 83, 3, Gold, false);
        return card;
    }

    private RectTransform Card(string name, Transform parent, Vector2 anchor, float x, float y, float width, float height)
    {
        Image borderImage = Fill(name, parent, anchor, x, y, width, height, Edge);
        borderImage.fillCenter = false;
        RectTransform border = borderImage.rectTransform;
        Fill("Panel surface", border, TopLeft, 1, -1, width - 2, height - 2, Panel);
        return border;
    }

    private Image Icon(string name, Transform parent, float x, float y, float size, Sprite sprite, Color color)
    {
        Image image = Fill(name, parent, TopLeft, x, y, size, size, color, false);
        image.sprite = sprite;
        return image;
    }

    private Image Fill(string name, Transform parent, Vector2 anchor, float x, float y, float width, float height,
        Color color, bool useCorners = true)
    {
        RectTransform rect = Place(name, parent, anchor, x, y, width, height);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (useCorners)
        {
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
        }
        return image;
    }

    private Image FullImage(string name, Transform parent, Color color)
    {
        var image = Stretch(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text Label(string name, Transform parent, float x, float y, float width, float height, string content,
        int size, Color color, FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        RectTransform rect = Place(name, parent, TopLeft, x, y, width, height);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.fontStyle = style;
        label.text = content;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Stretch(string name, Transform parent)
    {
        RectTransform rect = Place(name, parent, Vector2.zero, 0, 0, 0, 0);
        rect.anchorMax = Vector2.one;
        return rect;
    }

    private static RectTransform Place(string name, Transform parent, Vector2 anchor, float x, float y,
        float width, float height)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, y);
        return rect;
    }

    private static string FormatClock(int seconds)
    {
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private static bool Ellipse(float x, float y, float centerX, float centerY, float radiusX, float radiusY)
    {
        float dx = (x - centerX) / radiusX;
        float dy = (y - centerY) / radiusY;
        return dx * dx + dy * dy <= 1f;
    }

    private Sprite MakeSprite(string name, int size, Func<float, float, bool> inside, int border = 0, bool gradient = false)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int coverage = 0;
                for (int subY = 0; subY < 4; subY++)
                {
                    for (int subX = 0; subX < 4; subX++)
                    {
                        float sampleX = ((x + (subX + 0.5f) / 4f) / size) * 2f - 1f;
                        float sampleY = ((y + (subY + 0.5f) / 4f) / size) * 2f - 1f;
                        if (inside(sampleX, sampleY))
                            coverage++;
                    }
                }
                float fade = gradient ? 1f - Mathf.SmoothStep(0f, 1f, x / (size * 0.8f)) : 1f;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(coverage * 255f / 16f * fade));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f,
            0, SpriteMeshType.FullRect, Vector4.one * border);
        sprite.name = name;
        artwork.Add(sprite);
        artwork.Add(texture);
        return sprite;
    }

    private void OnDestroy()
    {
        foreach (UnityEngine.Object asset in artwork)
            Destroy(asset);
    }
}
