# 🎨 Runtime UI & Camera Controller Setup Guide

## Overview

This guide will help you integrate the **RuntimeUIManager** and **OrbitCameraController** scripts into your Swinging Paint Bucket application for a professional, interactive experience in your Windows standalone build.

---

## Part 1: Setting Up the Orbit Camera Controller ✅

### Step 1: Add the Script to Your Main Camera

1. **In the Unity Editor**, locate your **Main Camera** in the Hierarchy
2. **Drag and drop** `OrbitCameraController.cs` to the **Assets** folder
3. **Select** the Main Camera and in the **Inspector**, click **Add Component**
4. **Search** for "OrbitCameraController" and **add it**

### Step 2: Configure Camera Settings

In the **OrbitCameraController** component inspector, set the following:

| Parameter              | Recommended Value                                       | Purpose                                  |
| ---------------------- | ------------------------------------------------------- | ---------------------------------------- |
| **Orbit Target**       | Your Pendulum Anchor (the root of CustomVerletPendulum) | The point around which the camera orbits |
| **Distance**           | 15                                                      | Starting distance from target            |
| **Min Distance**       | 3                                                       | How close you can zoom                   |
| **Max Distance**       | 50                                                      | How far you can zoom                     |
| **Rotation Speed**     | 2                                                       | Mouse rotation sensitivity               |
| **Zoom Speed**         | 5                                                       | Mouse scroll sensitivity                 |
| **Pan Speed**          | 0.02                                                    | Middle-click pan speed                   |
| **Min Vertical Angle** | -80                                                     | Lowest pitch angle                       |
| **Max Vertical Angle** | 80                                                      | Highest pitch angle                      |

### Step 3: Test Camera Controls

**Save the scene and enter Play Mode**. Test the following controls:

| Control                              | Action                     |
| ------------------------------------ | -------------------------- |
| **Right Mouse Button + Move Mouse**  | Rotate around the pendulum |
| **Mouse Scroll Wheel**               | Zoom in/out                |
| **Middle Mouse Button + Move Mouse** | Pan the view               |
| **Arrow Keys**                       | Rotate using keyboard      |
| **Numpad 8**                         | Top-down view              |
| **Numpad 2**                         | Side view                  |
| **Numpad 7**                         | Isometric view             |

---

## Part 2: Setting Up the Runtime UI Manager ✅

### Step 1: Create an Empty GameObject for UI Management

1. **Right-click** in the Hierarchy → **Create Empty**
2. **Name it** "UIManager"
3. **Drag and drop** `RuntimeUIManager.cs` to the **Assets** folder
4. **Add the script** to the UIManager GameObject

### Step 2: Assign References in the RuntimeUIManager

**Select the UIManager** and in the Inspector:

1. **Pendulum Controller**: Drag the GameObject containing `CustomVerletPendulum`
2. **Fluid Simulation**: Drag the GameObject containing `CustomSPHFluid`
3. **UI Canvas**: Leave empty (the script will create one automatically)

### Step 3: Customize UI Colors (Optional)

In the RuntimeUIManager inspector, you can customize:

| Setting                 | Default        | Purpose                |
| ----------------------- | -------------- | ---------------------- |
| **UI Background Color** | Dark Gray      | Panel background tint  |
| **Panel Color**         | Very Dark Gray | Main panel color       |
| **Button Color**        | Light Blue     | Button colors          |
| **Highlight Color**     | Bright Blue    | Button highlight color |
| **UI Starts Visible**   | true           | Show UI on startup     |
| **Toggle UI Key**       | Tab            | Key to hide/show UI    |

### Step 4: Test the UI

**Enter Play Mode** and verify:

- ✅ A UI panel appears in the **bottom-left corner**
- ✅ All sliders respond to mouse input
- ✅ Color buttons change the paint color
- ✅ **TAB key** hides/shows the UI
- ✅ **Exit** button works (will quit the app)

---

## Part 3: How to Link UI Elements to Your Scripts 📝

The **RuntimeUIManager** automatically creates connections to your scripts. Here's how it works:

### Physics Controls Linking

For **CustomVerletPendulum**, the UI creates sliders for:

```csharp
// Gravity control (Y axis)
slider.onValueChanged.AddListener((value) =>
{
    Vector3 g = pendulumController.gravity;
    pendulumController.gravity = new Vector3(g.x, value, g.z);
});

// Rope Length control
slider.onValueChanged.AddListener((value) =>
{
    pendulumController.ropeLength = value;
    pendulumController.InitializeRope();
});

// Air Resistance, Orbital Push, Torsion parameters...
```

### Fluid Controls Linking

For **CustomSPHFluid**, sliders control:

```csharp
slider.onValueChanged.AddListener((value) =>
{
    fluidSimulation.maxParticles = (int)value;
});

// Smoothing Radius, Viscosity, Pressure, Emission Rate...
```

### Paint Color Linking

Color buttons directly set the paint color:

```csharp
button.onClick.AddListener(() =>
{
    fluidSimulation.paintColor = selectedColor;
    if (fluidSimulation.paintColors != null && fluidSimulation.paintColors.Length > 0)
    {
        fluidSimulation.paintColors[0] = selectedColor;
    }
});
```

---

## Part 4: Custom UI Extensions 🎨

### Adding More Sliders

To add additional sliders to control other variables, modify the `BuildUIPanel()` method:

```csharp
// In RuntimeUIManager.cs, around line 150, add:
AddSlider(
    label: "My Parameter",
    initialValue: myValue,
    minValue: 0f,
    maxValue: 100f,
    onValueChanged: (value) =>
    {
        myScript.myVariable = value;
    }
);
```

### Adding More Color Buttons

Add more color presets:

```csharp
// In BuildUIPanel(), add more of these:
AddColorButton("🟦 Cyan", new Color(0.2f, 0.8f, 1f, 1f));
AddColorButton("🟥 Magenta", new Color(1f, 0.2f, 0.8f, 1f));
```

### Changing the UI Position

To move the UI panel, modify the anchors in the `BuildUIPanel()` method:

```csharp
// Current: Bottom-left corner
uiPanelRect.anchorMin = new Vector2(0, 0);   // Bottom-left
uiPanelRect.anchorMax = new Vector2(0, 0);   // Bottom-left
uiPanelRect.offsetMin = new Vector2(20, 20); // 20px from corner
uiPanelRect.offsetMax = new Vector2(420, 1000); // Size

// For top-right corner instead:
uiPanelRect.anchorMin = new Vector2(1, 1);   // Top-right
uiPanelRect.anchorMax = new Vector2(1, 1);   // Top-right
uiPanelRect.offsetMin = new Vector2(-420, -1000); // Negative sizes
uiPanelRect.offsetMax = new Vector2(-20, -20);
```

---

## Part 5: Building the Final .EXE 🎮

### Pre-Build Checklist

1. ✅ **UIManager** GameObject is in your **Startup Scene**
2. ✅ **Main Camera** has **OrbitCameraController** attached
3. ✅ **All references** are properly assigned in both scripts
4. ✅ **Test the application** in Play Mode to ensure everything works
5. ✅ **Verify** that the UI is visible and functional

### Build Settings

1. **File → Build Settings**
2. **Select your Scene** (it should show in "Scenes in Build")
3. **Target Platform**: Windows
4. **Architecture**: x86_64
5. **Uncheck** "Development Build" for final release
6. **Click "Build"** and choose a folder (e.g., `Builds/`)

### Running the .EXE

After building:

1. Navigate to the **Build folder**
2. **Double-click** the `.exe` file
3. The application should start **in fullscreen**
4. ✅ Use **right-click + mouse** to rotate the camera
5. ✅ Use **scroll wheel** to zoom
6. ✅ Use **arrow keys** for alternative camera control
7. ✅ Press **TAB** to toggle UI visibility
8. ✅ Adjust parameters with the UI sliders
9. ✅ Click **EXIT** button or **Alt+F4** to close

---

## Part 6: Troubleshooting 🔧

### Issue: UI doesn't appear

**Solution:**

- Verify UIManager has both scripts attached
- Check that references are assigned (Pendulum & Fluid)
- Ensure the scene is being built with the UI objects

### Issue: Camera doesn't orbit

**Solution:**

- Verify Main Camera has OrbitCameraController script
- Make sure **Orbit Target** is assigned to the pendulum root
- Check that the camera is using proper frustum culling

### Issue: Sliders don't affect simulation

**Solution:**

- Verify Pendulum & Fluid references in UIManager
- Check Console for errors in Unity Editor
- Ensure CustomVerletPendulum and CustomSPHFluid are active in scene

### Issue: App crashes on exit

**Solution:**

- The Exit button calls `Application.Quit()` which is safe
- If crashing, check the Console for errors
- Verify no scripts are trying to access destroyed objects

---

## Part 7: Performance Optimization 📊

For optimal performance in your standalone build:

1. **Disable unnecessary GameObjects** in the Hierarchy
2. **Set Quality Settings**:
   - **Edit → Project Settings → Quality**
   - Set to **High** for best visuals
   - Disable V-Sync if performance is an issue

3. **Camera Culling**:
   - Set Main Camera culling to only necessary layers
   - Disable shadow casting on non-essential objects

4. **UI Optimization**:
   - The UI is built at runtime to minimize file size
   - Consider caching frequently-used UI elements

---

## Part 8: Summary of Key Controls 🎮

| Action                | Control                     |
| --------------------- | --------------------------- |
| **Rotate Camera**     | Right Mouse Button + Move   |
| **Zoom Camera**       | Mouse Scroll Wheel          |
| **Pan Camera**        | Middle Mouse Button + Move  |
| **Rotate (Keyboard)** | Arrow Keys                  |
| **Top-Down View**     | Numpad 8                    |
| **Side View**         | Numpad 2                    |
| **Isometric View**    | Numpad 7                    |
| **Toggle UI**         | TAB Key                     |
| **Exit App**          | Click Exit Button or Alt+F4 |

---

## Additional Notes

- **All UI elements are created dynamically at runtime**, so you don't need to manually build a Canvas hierarchy
- **The scripts are fully commented** for easy customization
- **Both scripts use only built-in Unity components** (no external dependencies)
- **The UI is responsive** and works at any screen resolution

---

If you need to modify the script behavior or add new features, the code is well-structured with clear method names and comments. Feel free to customize as needed!

**Happy simulating! 🎨✨**
