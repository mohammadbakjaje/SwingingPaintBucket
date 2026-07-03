using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Professional Runtime UI Manager with Setup Menu (English Interface)
/// - Displays a setup menu on startup (Time.timeScale = 0)
/// - Allows configuration of simulation parameters before starting
/// - Provides a clean, minimal runtime interface during simulation
/// - Small settings button to return to menu during gameplay
/// - Directly binds all UI controls to CustomSPHFluid and CustomVerletPendulum public properties
/// </summary>
public class RuntimeUIManager : MonoBehaviour
{
    [Header("References")]
    public CustomVerletPendulum pendulumController;
    public CustomSPHFluid fluidSimulation;

    [Header("Color Theme")]
    public Color primaryColor = new Color(0.2f, 0.4f, 0.7f, 1f);      // Professional blue
    public Color secondaryColor = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark background
    public Color accentColor = new Color(1f, 0.5f, 0.2f, 1f);        // Orange accent
    public Color textColor = Color.white;

    // UI References
    private Canvas mainCanvas;
    private GameObject setupMenuPanel;
    private GameObject runtimeSettingsButton;
    private bool simulationStarted = false;

    private int desiredColorCount = 1;
    private readonly List<Color> selectedColors = new List<Color>();
    private readonly Dictionary<Color, Outline> colorSelectionOutlines = new Dictionary<Color, Outline>();

    void Start()
    {
        // Create or find Canvas
        if (mainCanvas == null)
        {
            CreateCanvasHierarchy();
        }

        // Build Setup Menu (appears first, pauses simulation)
        BuildSetupMenu();

        if (fluidSimulation != null)
            fluidSimulation.ClearPaintColors();

        // Pause simulation until user clicks Start
        Time.timeScale = 0f;
        simulationStarted = false;
    }

    void Update()
    {
        if (simulationStarted && Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }
    }

    /// <summary>
    /// Creates Canvas hierarchy if it doesn't exist
    /// </summary>
    private void CreateCanvasHierarchy()
    {
        GameObject canvasObj = new GameObject("RuntimeUICanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();

        // Configure CanvasScaler for proper DPI scaling with TextMeshPro
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    /// <summary>
    /// Builds the professional setup menu that appears on startup
    /// </summary>
    private void BuildSetupMenu()
    {
        // Create main panel
        setupMenuPanel = new GameObject("SetupMenuPanel");
        setupMenuPanel.transform.SetParent(mainCanvas.transform, false);
        RectTransform panelRect = setupMenuPanel.AddComponent<RectTransform>();

        // Full screen overlay
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Semi-transparent background
        Image panelBg = setupMenuPanel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        // Create center content panel
        GameObject contentPanel = new GameObject("ContentPanel");
        contentPanel.transform.SetParent(setupMenuPanel.transform, false);
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();

        // Center on screen
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(600, 800);
        contentRect.anchoredPosition = Vector2.zero;

        // Content panel styling
        Image contentBg = contentPanel.AddComponent<Image>();
        contentBg.color = secondaryColor;

        // Add rounded corners appearance with border
        Outline outline = contentPanel.AddComponent<Outline>();
        outline.effectColor = primaryColor;
        outline.effectDistance = new Vector2(2, 2);

        VerticalLayoutGroup contentLayout = contentPanel.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(40, 40, 40, 40);
        contentLayout.spacing = 25f;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        LayoutElement contentLayout2 = contentPanel.AddComponent<LayoutElement>();
        contentLayout2.preferredHeight = 800;
        contentLayout2.flexibleWidth = 1f;

        // Title: Paint Simulation
        AddTitleLabel(contentPanel, "Paint Simulation", 28, bold: true);

        // Separator
        AddSeparator(contentPanel);

        // Section 1: Max Particles
        AddSectionHeader(contentPanel, "Max Particles");
        if (fluidSimulation != null)
        {
            AddSlider(contentPanel,
                "Particle Count",
                fluidSimulation.maxParticles,
                1000f, 50000f,
                (value) =>
                {
                    fluidSimulation.maxParticles = (int)value;
                });
        }

        AddSeparator(contentPanel);

        // Section 2: Color Selection
        AddSectionHeader(contentPanel, "Paint Colors");
        AddColorSelector(contentPanel);

        AddSeparator(contentPanel);

        // Section 3: Canvas Type
        AddSectionHeader(contentPanel, "Canvas Material");
        if (fluidSimulation != null)
        {
            AddCanvasTypeSelector(contentPanel);
        }

        AddSeparator(contentPanel);

        // Spacing before buttons
        AddSpacer(contentPanel, 20);

        // Start Simulation Button
        AddStartButton(contentPanel);
    }

    /// <summary>
    /// Creates the small settings button visible during simulation
    /// </summary>
    private void CreateRuntimeSettingsButton()
    {
        runtimeSettingsButton = new GameObject("RuntimeSettingsButton");
        runtimeSettingsButton.transform.SetParent(mainCanvas.transform, false);
        RectTransform buttonRect = runtimeSettingsButton.AddComponent<RectTransform>();

        // Top-right corner
        buttonRect.anchorMin = new Vector2(1, 1);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.sizeDelta = new Vector2(120, 50);
        buttonRect.anchoredPosition = new Vector2(-20, -20);

        // Button styling
        Image buttonImage = runtimeSettingsButton.AddComponent<Image>();
        buttonImage.color = primaryColor;

        Button button = runtimeSettingsButton.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = primaryColor;
        colors.highlightedColor = primaryColor * 1.2f;
        colors.pressedColor = primaryColor * 0.8f;
        button.colors = colors;
        button.targetGraphic = buttonImage;

        // Button text using TextMeshProUGUI
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(runtimeSettingsButton.transform, false);

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Settings ⚙";
        buttonText.fontSize = 18;
        buttonText.alignment = TMPro.TextAlignmentOptions.Center;
        buttonText.color = textColor;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Click handler
        button.onClick.AddListener(() => ReturnToMenu());

        // Initially hidden
        runtimeSettingsButton.SetActive(false);
    }

    /// <summary>
    /// Adds a slider to the setup menu
    /// </summary>
    private void AddSlider(GameObject parent, string label, float initialValue, float minValue, float maxValue, System.Action<float> onValueChanged)
    {
        // Container
        GameObject sliderContainer = new GameObject(label + "Container");
        sliderContainer.transform.SetParent(parent.transform, false);
        RectTransform containerRect = sliderContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(0, 70f);

        LayoutElement layoutElement = sliderContainer.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 70f;
        layoutElement.flexibleWidth = 1f;

        VerticalLayoutGroup containerLayout = sliderContainer.AddComponent<VerticalLayoutGroup>();
        containerLayout.childForceExpandWidth = true;
        containerLayout.childForceExpandHeight = false;
        containerLayout.spacing = 5f;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(sliderContainer.transform, false);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 16;
        labelText.alignment = TextAlignmentOptions.BottomRight;
        labelText.color = textColor;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(0, 20f);

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 20f;
        labelLayout.flexibleWidth = 1f;

        // Slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(sliderContainer.transform, false);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = initialValue;
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0, 20f);

        LayoutElement sliderLayout = sliderObj.AddComponent<LayoutElement>();
        sliderLayout.preferredHeight = 20f;
        sliderLayout.flexibleWidth = 1f;

        // Slider background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Slider fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(sliderObj.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = primaryColor;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;

        // Slider handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(sliderObj.transform, false);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = accentColor;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0, 0.5f);
        handleRect.anchorMax = new Vector2(0, 0.5f);
        handleRect.sizeDelta = new Vector2(12f, 26f);

        slider.handleRect = handleRect;

        slider.onValueChanged.AddListener((value) =>
        {
            onValueChanged?.Invoke(value);
        });
    }

    /// <summary>
    /// Adds color count selector and multi-color palette buttons.
    /// </summary>
    private void AddColorSelector(GameObject parent)
    {
        AddSlider(parent,
            "Number of Colors",
            desiredColorCount,
            1f, 6f,
            (value) =>
            {
                desiredColorCount = Mathf.RoundToInt(value);
                while (selectedColors.Count > desiredColorCount)
                    selectedColors.RemoveAt(selectedColors.Count - 1);
                UpdateColorSelectionVisuals();
                SyncPaintColorsToSimulation();
            });

        List<Color> colorPalette = new List<Color>
        {
            new Color(1f, 0.2f, 0.2f, 1f),    // Red
            new Color(0.2f, 0.9f, 0.2f, 1f),  // Green
            new Color(0.2f, 0.5f, 1f, 1f),    // Blue
            new Color(1f, 1f, 0.2f, 1f),      // Yellow
            new Color(1f, 0.2f, 1f, 1f),      // Purple
            new Color(1f, 0.6f, 0.2f, 1f),    // Orange
        };

        GameObject colorGrid = new GameObject("ColorGrid");
        colorGrid.transform.SetParent(parent.transform, false);
        RectTransform gridRect = colorGrid.AddComponent<RectTransform>();
        gridRect.sizeDelta = new Vector2(0, 90f);

        LayoutElement gridLayout = colorGrid.AddComponent<LayoutElement>();
        gridLayout.preferredHeight = 90f;
        gridLayout.flexibleWidth = 1f;

        GridLayoutGroup grid = colorGrid.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.spacing = new Vector2(10, 10);
        grid.cellSize = new Vector2(150, 35);

        colorSelectionOutlines.Clear();
        foreach (Color color in colorPalette)
        {
            AddColorButton(colorGrid, color);
        }
    }

    private void ToggleColorSelection(Color color)
    {
        if (selectedColors.Contains(color))
        {
            selectedColors.Remove(color);
        }
        else if (desiredColorCount == 1)
        {
            selectedColors.Clear();
            selectedColors.Add(color);
        }
        else if (selectedColors.Count < desiredColorCount)
        {
            selectedColors.Add(color);
        }

        UpdateColorSelectionVisuals();
        SyncPaintColorsToSimulation();
    }

    private void UpdateColorSelectionVisuals()
    {
        foreach (var kvp in colorSelectionOutlines)
        {
            if (kvp.Value != null)
                kvp.Value.enabled = selectedColors.Contains(kvp.Key);
        }
    }

    private void SyncPaintColorsToSimulation()
    {
        if (fluidSimulation == null) return;

        if (selectedColors.Count == 0)
            fluidSimulation.ClearPaintColors();
        else
            fluidSimulation.SetPaintColors(selectedColors.ToArray(), selectedColors.Count);
    }

    /// <summary>
    /// Adds a single color button with multi-select support.
    /// </summary>
    private void AddColorButton(GameObject parent, Color color)
    {
        GameObject buttonObj = new GameObject("ColorButton");
        buttonObj.transform.SetParent(parent.transform, false);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;

        Outline selectionOutline = buttonObj.AddComponent<Outline>();
        selectionOutline.effectColor = Color.white;
        selectionOutline.effectDistance = new Vector2(3f, -3f);
        selectionOutline.enabled = false;
        colorSelectionOutlines[color] = selectionOutline;

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        button.colors = colors;
        button.targetGraphic = buttonImage;

        button.onClick.AddListener(() => ToggleColorSelection(color));
    }

    /// <summary>
    /// Adds canvas type selector - directly updates fluidSimulation.surfaceMaterial
    /// </summary>
    private void AddCanvasTypeSelector(GameObject parent)
    {
        // Container for canvas type buttons
        GameObject canvasGrid = new GameObject("CanvasTypeGrid");
        canvasGrid.transform.SetParent(parent.transform, false);
        RectTransform gridRect = canvasGrid.AddComponent<RectTransform>();
        gridRect.sizeDelta = new Vector2(0, 50f);

        LayoutElement gridLayout = canvasGrid.AddComponent<LayoutElement>();
        gridLayout.preferredHeight = 50f;
        gridLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup grid = canvasGrid.AddComponent<HorizontalLayoutGroup>();
        grid.spacing = 10;
        grid.childForceExpandWidth = true;
        grid.childForceExpandHeight = false;

        // Canvas type options
        string[] canvasTypes = { "Cloth", "Wood", "Metal" };
        CustomSPHFluid.CanvasMaterialType[] materialTypes = new[]
        {
            CustomSPHFluid.CanvasMaterialType.Cloth,
            CustomSPHFluid.CanvasMaterialType.Wood,
            CustomSPHFluid.CanvasMaterialType.Metal
        };

        for (int i = 0; i < canvasTypes.Length; i++)
        {
            int index = i;
            AddCanvasTypeButton(canvasGrid, canvasTypes[i], primaryColor, () =>
            {
                if (fluidSimulation != null)
                    fluidSimulation.UpdateCanvasMaterial(materialTypes[index]);
            });
        }
    }

    /// <summary>
    /// Adds a canvas type button - directly updates fluidSimulation.surfaceMaterial
    /// </summary>
    private void AddCanvasTypeButton(GameObject parent, string label, Color color, System.Action onClick)
    {
        GameObject buttonObj = new GameObject("CanvasTypeButton");
        buttonObj.transform.SetParent(parent.transform, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(0, 40f);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        button.colors = colors;
        button.targetGraphic = buttonImage;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = label;
        buttonText.fontSize = 16;
        buttonText.alignment = TMPro.TextAlignmentOptions.Center;
        buttonText.color = textColor;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        LayoutElement buttonLayout = buttonObj.AddComponent<LayoutElement>();
        buttonLayout.flexibleWidth = 1f;
        buttonLayout.preferredHeight = 40f;

        // Direct binding to onClick action
        button.onClick.AddListener(() => onClick?.Invoke());
    }

    /// <summary>
    /// Adds the "Start Simulation" button
    /// </summary>
    private void AddStartButton(GameObject parent)
    {
        GameObject buttonObj = new GameObject("StartSimulationButton");
        buttonObj.transform.SetParent(parent.transform, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(0, 50f);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = accentColor;

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = accentColor;
        colors.highlightedColor = accentColor * 1.2f;
        colors.pressedColor = accentColor * 0.8f;
        button.colors = colors;
        button.targetGraphic = buttonImage;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Start Simulation";
        buttonText.fontSize = 18;
        buttonText.alignment = TMPro.TextAlignmentOptions.Center;
        buttonText.color = textColor;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        LayoutElement buttonLayout = buttonObj.AddComponent<LayoutElement>();
        buttonLayout.flexibleWidth = 1f;
        buttonLayout.preferredHeight = 50f;

        button.onClick.AddListener(() => StartSimulation());
    }

    /// <summary>
    /// Starts the simulation and hides the setup menu
    /// </summary>
    private void StartSimulation()
    {
        // Hide setup menu
        setupMenuPanel.SetActive(false);

        // Create runtime settings button if not exists
        if (runtimeSettingsButton == null)
        {
            CreateRuntimeSettingsButton();
        }

        runtimeSettingsButton.SetActive(true);

        // Resume simulation
        Time.timeScale = 1f;
        simulationStarted = true;

        Debug.Log("Paint Simulation Started");
    }

    /// <summary>
    /// Returns to setup menu
    /// </summary>
    private void ReturnToMenu()
    {
        // Show setup menu
        setupMenuPanel.SetActive(true);

        // Hide runtime button
        if (runtimeSettingsButton != null)
        {
            runtimeSettingsButton.SetActive(false);
        }

        // Pause simulation
        Time.timeScale = 0f;
        simulationStarted = false;

        Debug.Log("Returned to Setup Menu");
    }

    /// <summary>
    /// Adds a title label
    /// </summary>
    private void AddTitleLabel(GameObject parent, string text, int fontSize, bool bold = false)
    {
        GameObject labelObj = new GameObject("TitleLabel");
        labelObj.transform.SetParent(parent.transform, false);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = text;
        labelText.fontSize = fontSize;
        labelText.alignment = TMPro.TextAlignmentOptions.Center;
        labelText.color = accentColor;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(0, fontSize * 3f);

        LayoutElement layoutElement = labelObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = fontSize * 3f;
        layoutElement.flexibleWidth = 1f;
    }

    /// <summary>
    /// Adds a section header
    /// </summary>
    private void AddSectionHeader(GameObject parent, string text)
    {
        GameObject headerObj = new GameObject("SectionHeader");
        headerObj.transform.SetParent(parent.transform, false);

        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = text;
        headerText.fontSize = 16;
        headerText.alignment = TextAlignmentOptions.BottomRight;
        headerText.color = primaryColor;

        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(0, 25f);

        LayoutElement layoutElement = headerObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 25f;
        layoutElement.flexibleWidth = 1f;
    }

    /// <summary>
    /// Adds a separator line
    /// </summary>
    private void AddSeparator(GameObject parent)
    {
        GameObject separatorObj = new GameObject("Separator");
        separatorObj.transform.SetParent(parent.transform, false);

        Image separatorImage = separatorObj.AddComponent<Image>();
        separatorImage.color = new Color(0.4f, 0.4f, 0.4f, 0.3f);

        RectTransform separatorRect = separatorObj.GetComponent<RectTransform>();
        separatorRect.sizeDelta = new Vector2(0, 1f);

        LayoutElement layoutElement = separatorObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 1f;
        layoutElement.flexibleWidth = 1f;
    }

    /// <summary>
    /// Adds spacer for layout
    /// </summary>
    private void AddSpacer(GameObject parent, float height)
    {
        GameObject spacerObj = new GameObject("Spacer", typeof(RectTransform));
        spacerObj.transform.SetParent(parent.transform, false);

        RectTransform spacerRect = spacerObj.GetComponent<RectTransform>();
        spacerRect.sizeDelta = new Vector2(0, height);

        LayoutElement layoutElement = spacerObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
        layoutElement.flexibleWidth = 1f;
    }
}

