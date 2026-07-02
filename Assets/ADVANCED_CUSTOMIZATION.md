# 🔧 Advanced Customization & Technical Reference

## Architecture Overview

### RuntimeUIManager

- **Purpose**: Dynamically creates UI Canvas and all UI elements at runtime
- **No Prefabs Required**: All UI is generated via C# code for flexibility
- **Auto-Discovery**: Finds references to Pendulum and Fluid scripts
- **Modular Design**: Easy to add/remove controls

### OrbitCameraController

- **Purpose**: Provides smooth orbital camera movement around the simulation
- **Physics-Based**: Uses smooth damping for natural feel
- **Collision-Aware**: Optional collision detection prevents camera clipping
- **Preset Views**: NumPad keys for quick angle changes

---

## RuntimeUIManager - Deep Dive

### Class Structure

```csharp
public class RuntimeUIManager : MonoBehaviour
{
    // Public configuration
    public CustomVerletPendulum pendulumController;
    public CustomSPHFluid fluidSimulation;
    public Canvas uiCanvas;

    // Color scheme
    public Color uiBackgroundColor;
    public Color panelColor;
    public Color buttonColor;
    public Color highlightColor;

    // Internal state
    private GameObject uiPanelRoot;
    private Dictionary<string, Slider> sliderDictionary;
    private Dictionary<string, Text> labelDictionary;
}
```

### Key Methods

#### `BuildUIPanel()`

Creates the entire UI hierarchy:

1. Creates a root panel GameObject
2. Sets up layout groups for automatic arrangement
3. Adds title and section headers
4. Dynamically creates sliders for each controlled variable
5. Creates color buttons
6. Adds control information text
7. Creates exit button

#### `AddSlider(string label, float initial, float min, float max, Action<float> callback)`

Creates a labeled slider with real-time value display:

- Creates a container with vertical layout
- Adds descriptive label (bold)
- Adds value display (updates as slider moves)
- Creates the actual slider with visual styling
- Hooks onValueChanged event to callback function

**Example Usage:**

```csharp
AddSlider("Rope Length", 5f, 2f, 15f, (value) =>
{
    pendulumController.ropeLength = value;
    pendulumController.InitializeRope();
});
```

#### `AddColorButton(string label, Color color)`

Creates a clickable button that sets paint color:

- Creates button with the specified color
- Adds label text (emoji + name)
- Hooks onClick to set `paintColor` and `paintColors` array

**To Add More Color Buttons:**

```csharp
AddColorButton("🌈 Cyan", new Color(0.2f, 0.8f, 1f, 1f));
AddColorButton("💜 Pink", new Color(1f, 0.2f, 0.6f, 1f));
```

#### `AddButton(string label, Action onClick, Color backgroundColor)`

Creates a generic button for custom actions:

- Creates button with specified background color
- Handles click events
- Useful for non-slider controls

**Example:**

```csharp
AddButton("🔧 Reset Simulation", () =>
{
    pendulumController.InitializeRope();
    // Add more reset logic here
}, Color.yellow);
```

#### `AddLabel(string text, int fontSize, TextAnchor alignment, bool bold)`

Adds text elements (headers, info text, separators):

- Supports multi-line text with `\n`
- Auto-sizing based on font size and line count
- Supports various text anchors

#### `UpdateSliderLabels()`

Called every frame to update displayed values:

- Reads current slider values
- Updates associated label text
- Provides real-time feedback to user

### Customization Examples

#### Example 1: Add a Reset Button

```csharp
// In BuildUIPanel(), before the Exit button:
AddButton("↻ Reset All Parameters", () =>
{
    if (pendulumController != null)
        pendulumController.InitializeRope();

    if (fluidSimulation != null)
    {
        fluidSimulation.maxParticles = 10000;
        fluidSimulation.viscosity = 0.2f;
        // ... reset other values
    }

    // Reset sliders
    foreach (var slider in sliderDictionary.Values)
    {
        slider.value = slider.minValue + (slider.maxValue - slider.minValue) * 0.5f;
    }
}, new Color(1f, 0.7f, 0.2f, 1f)); // Orange
```

#### Example 2: Add a Recording Toggle

```csharp
// Add a toggle for recording the simulation
GameObject toggleObj = new GameObject("RecordToggle");
toggleObj.transform.SetParent(uiPanelRoot.transform, false);

// ... (setup RectTransform and LayoutElement)

Toggle toggle = toggleObj.AddComponent<Toggle>();
// Configure toggle appearance
toggle.onValueChanged.AddListener((isOn) =>
{
    // Enable/disable recording logic
    Debug.Log("Recording: " + isOn);
});
```

#### Example 3: Add Advanced Physics Parameter

```csharp
// For parameters in CustomVerletPendulum not yet exposed:
AddSlider("Segment Count", pendulumController.segmentCount, 5f, 50f, (value) =>
{
    pendulumController.segmentCount = (int)value;
    pendulumController.InitializeRope();
});
```

#### Example 4: Change Panel Position

```csharp
// In BuildUIPanel(), replace the anchor/offset code:

// TOP-RIGHT CORNER
uiPanelRect.anchorMin = new Vector2(1, 1);
uiPanelRect.anchorMax = new Vector2(1, 1);
uiPanelRect.offsetMin = new Vector2(-420, -1000);
uiPanelRect.offsetMax = new Vector2(-20, -20);

// TOP-LEFT CORNER
uiPanelRect.anchorMin = new Vector2(0, 1);
uiPanelRect.anchorMax = new Vector2(0, 1);
uiPanelRect.offsetMin = new Vector2(20, -1000);
uiPanelRect.offsetMax = new Vector2(420, -20);

// CENTER SCREEN
uiPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
uiPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
uiPanelRect.offsetMin = new Vector2(-210, -500);
uiPanelRect.offsetMax = new Vector2(210, 500);
```

---

## OrbitCameraController - Deep Dive

### Class Structure

```csharp
public class OrbitCameraController : MonoBehaviour
{
    // Configuration
    public Transform orbitTarget;
    public float distance = 15f;
    public float minDistance = 3f;
    public float maxDistance = 50f;
    public float rotationSpeed = 2f;
    public float zoomSpeed = 5f;

    // Internal state
    private float currentYaw = 0f;
    private float currentPitch = 45f;
    private float currentDistance;
    private Camera mainCamera;
    private Vector3 panOffset;
}
```

### Key Methods

#### `HandleMouseInput()`

Processes right-click rotation and middle-click panning:

- Tracks mouse position changes
- Updates target yaw/pitch based on mouse delta
- Applies rotation constraints if enabled
- Manages pan offset for middle-click panning

**Customization:**

```csharp
// Change from right-click to left-click rotation:
// In HandleMouseInput(), change:
if (Input.GetMouseButtonDown(0))  // 0 = Left, 1 = Right, 2 = Middle
{
    isRightMouseDown = true;
    // ...
}
```

#### `HandleKeyboardInput()`

Processes arrow key input for rotation:

- Arrow keys provide constant rotation when held
- NumPad keys trigger preset camera angles
- Can be extended with more preset views

**Add a New Preset:**

```csharp
if (Input.GetKeyDown(KeyCode.Keypad5))
{
    targetPitch = 30f;
    targetYaw = -45f;  // Custom angle
}
```

#### `HandleZoom()`

Processes mouse scroll wheel:

- Smooth zoom with constraints
- Clamped between min/max distance

**Change Zoom Speed:**

```csharp
// Slower zoom
targetDistance -= scrollInput * 2f;  // was 5f

// Faster zoom
targetDistance -= scrollInput * 10f;  // was 5f
```

#### `UpdateCameraPosition()`

Main update loop that:

1. Applies rotation smoothing (Lerp)
2. Applies zoom smoothing (Lerp)
3. Calculates desired position from spherical coordinates
4. Checks for collisions
5. Updates camera position and rotation

**Understanding Smoothing:**

```csharp
// Smoothing factor between 0 (instant) and 1 (no movement)
currentYaw = Mathf.Lerp(currentYaw, targetYaw, 0.1f);

// Lower value = more responsive, higher value = smoother
// 0.1 = 90% smooth (responsive)
// 0.5 = 50% smooth (medium)
// 0.9 = 10% smooth (very smooth but sluggish)
```

#### `CheckCollisions(Vector3 from, Vector3 to)`

Optional collision detection prevents camera clipping:

- Casts ray from target to desired position
- Clamps camera distance if collision detected
- Respects collision layer mask

**Enable Collisions:**

```csharp
public bool useCollisionDetection = true;
public LayerMask collisionLayers = ~0;  // All layers
```

### Customization Examples

#### Example 1: Change Rotation Mouse Button

```csharp
// Change to left-click (button 0) instead of right-click (button 1)
if (Input.GetMouseButtonDown(0))
{
    isRightMouseDown = true;
}
if (Input.GetMouseButtonUp(0))
{
    isRightMouseDown = false;
}
```

#### Example 2: Add Smooth Position Transition

```csharp
// Add a method for animated camera transitions
public void TransitionToView(float targetYaw, float targetPitch, float targetDist, float duration)
{
    // This would require a coroutine and Time.deltaTime tracking
    // Implementation left as exercise
}
```

#### Example 3: Add Invert-Y Option (Like Some Games)

```csharp
[Header("Input Preferences")]
public bool invertYAxis = false;

// In HandleMouseInput():
if (isRightMouseDown)
{
    Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

    targetYaw += mouseDelta.x * rotationSpeed * 0.1f;

    // Invert Y if preference is set
    float yDelta = invertYAxis ? mouseDelta.y : -mouseDelta.y;
    targetPitch += yDelta * rotationSpeed * 0.1f;

    // ...
}
```

#### Example 4: Add Freelook Mode

```csharp
[Header("Freelook Mode")]
public bool enableFreelookMode = false;

// In Update():
if (enableFreelookMode && Input.GetKeyDown(KeyCode.F))
{
    // Switch to freelook where camera doesn't orbit target
    // Implementation would require major refactoring
}
```

#### Example 5: Add Follow Speed

```csharp
[Header("Camera Behavior")]
public float cameraFollowDistance = 1.5f;

// In UpdateCameraPosition():
// If target moved, smoothly follow it
Vector3 targetCenter = orbitTarget.position + Vector3.up * cameraFollowDistance;
```

---

## Integration with CustomVerletPendulum

### Accessible Parameters

```csharp
// Read/Write
public int segmentCount;
public float ropeLength;
public float ropeWidth;
public Vector3 gravity;
public float airResistance;
public float torsionSpringK;
public float torsionDampingB;
public float orbitalPushSpeed;

// Read-Only
private List<RopeNode> nodesList;
private LineRenderer lineRenderer;
```

### How to Modify Parameters

**Option 1: Direct Assignment (Simple)**

```csharp
pendulumController.gravity = new Vector3(0, -20f, 0);
pendulumController.ropeLength = 10f;
```

**Option 2: Component Slider**

```csharp
AddSlider("New Parameter", initialValue, min, max, (value) =>
{
    pendulumController.newVariable = value;
    // If you need to reinitialize:
    pendulumController.InitializeRope();
});
```

### Performance Impact

- **gravity**: No reinitialization needed, immediate effect
- **airResistance**: Immediate effect
- **ropeLength**: Requires `InitializeRope()` call
- **torsionSpringK/torsionDampingB**: Immediate effect, no reinitialization
- **segmentCount**: Requires `InitializeRope()` call (expensive)

---

## Integration with CustomSPHFluid

### Accessible Parameters

```csharp
// Particle Properties
public int maxParticles;
public float smoothingRadius;
public float targetDensity;
public float pressureStiffness;
public float viscosity;
public Vector3 gravity;

// Rendering
public float particleVisualScale;
public Material particleInstancingMaterial;

// Emission
public float emissionRate;
public float exitVelocity;

// Paint Colors
public Color paintColor;
public Color[] paintColors;
public bool randomizePaintColors;
```

### How to Modify Parameters

**Safe Runtime Changes:**

```csharp
fluidSimulation.viscosity = 0.5f;  // Immediate
fluidSimulation.emissionRate = 150f;  // Immediate
fluidSimulation.paintColor = Color.red;  // Immediate

// Change paint colors array
if (fluidSimulation.paintColors != null)
{
    fluidSimulation.paintColors[0] = Color.red;
}
```

**Potentially Expensive Changes:**

```csharp
// These may require GPU buffer reallocation
fluidSimulation.maxParticles = 20000;
fluidSimulation.smoothingRadius = 1.5f;
```

---

## Event System (Advanced)

### Creating a Custom Event System

If you want UI to trigger external events:

```csharp
using UnityEngine.Events;

public class SimulationEventManager : MonoBehaviour
{
    public static UnityEvent<float> OnGravityChanged = new UnityEvent<float>();
    public static UnityEvent<Color> OnColorChanged = new UnityEvent<Color>();
    public static UnityEvent OnReset = new UnityEvent();
}

// In RuntimeUIManager:
AddSlider("Gravity Y", gravity.y, -20f, 0f, (value) =>
{
    pendulumController.gravity = new Vector3(0, value, 0);
    SimulationEventManager.OnGravityChanged.Invoke(value);
});
```

---

## Performance Considerations

### UI Performance

- **Overhead**: Minimal - all UI is created once at startup
- **Update**: Only labels update each frame (O(n) where n = number of sliders)
- **Solution**: Consider UI pooling if you have hundreds of sliders

### Camera Performance

- **Calculation**: Sphere-to-Cartesian conversion each frame
- **Smoothing**: Lerp operations are very cheap
- **Collision**: Optional raycast, can be expensive if overdone

### Optimization Tips

```csharp
// Disable collision detection if not needed
useCollisionDetection = false;

// Reduce update frequency if needed
if (Time.frameCount % 2 == 0)
{
    UpdateCameraPosition();
}

// Cache camera reference
private Camera mainCamera;  // Already done in this script
```

---

## Debugging

### Enable Debug Logging

```csharp
// In RuntimeUIManager.Start():
Debug.Log("UIManager initialized");
Debug.Log($"Pendulum Reference: {pendulumController}");
Debug.Log($"Fluid Reference: {fluidSimulation}");

// In OrbitCameraController.Update():
if (Input.GetKeyDown(KeyCode.D))
{
    Debug.Log($"Yaw: {currentYaw}, Pitch: {currentPitch}, Distance: {currentDistance}");
}
```

### Visual Debugging

```csharp
// In OrbitCameraController
void OnDrawGizmosSelected()
{
    if (orbitTarget != null)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(orbitTarget.position, 0.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(orbitTarget.position, transform.position);
    }
}
```

---

## Conclusion

Both scripts are designed to be:

- **Self-contained**: No external dependencies
- **Modular**: Easy to add/remove features
- **Well-commented**: Every method has documentation
- **Flexible**: Can be extended for your specific needs

Feel free to modify, extend, and customize these scripts to fit your specific requirements!

**Happy coding! 🚀**
