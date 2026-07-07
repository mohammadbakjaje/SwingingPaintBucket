# 🎬 Runtime UI & Camera System - Architecture Diagram

## System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        SWINGING PAINT BUCKET                        │
│                      Standalone .EXE Application                    │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                ┌───────────────────┼───────────────────┐
                │                   │                   │
        ┌───────▼──────────┐ ┌──────▼──────────┐ ┌────▼──────────────┐
        │  GAME LOGIC      │ │  RENDERING     │ │  USER INPUT      │
        │                  │ │                │ │                  │
        │ CustomVerlet     │ │ Main Camera    │ │ Keyboard         │
        │ Pendulum         │ │                │ │ Mouse           │
        │                  │ │ LineRenderer   │ │ Scroll Wheel    │
        │ CustomSPHFluid   │ │                │ │                  │
        │                  │ │ Particles      │ │                  │
        │ PureMathPaint    │ │ Visuals        │ │                  │
        │ Canvas           │ │                │ │                  │
        └────────┬─────────┘ └────────┬───────┘ └────────┬─────────┘
                 │                    │                  │
                 └────────────────────┼──────────────────┘
                                      │
                ┌─────────────────────┼─────────────────────┐
                │                     │                     │
        ┌───────▼────────────┐  ┌────▼──────────────┐ ┌────▼───────────┐
        │  RUNTIME UI        │  │  ORBIT CAMERA    │ │  SIMULATION    │
        │  MANAGER           │  │  CONTROLLER      │ │  EXECUTION     │
        │                    │  │                  │ │                │
        │ ┌─────────────────┐│  │ ┌──────────────┐ │ │ Update Loop    │
        │ │ Canvas          ││  │ │ Yaw/Pitch    │ │ │ (FixedUpdate)  │
        │ │                 ││  │ │ Calculation  │ │ │                │
        │ │ Physics Sliders ││  │ │              │ │ │ Physics        │
        │ │ - Gravity       ││  │ │ Smooth Lerp  │ │ │ Calculations   │
        │ │ - Rope Length   ││  │ │              │ │ │ Particle Sim   │
        │ │ - Viscosity     ││  │ │ Collision    │ │ │ Texture Paint  │
        │ │ - etc.          ││  │ │ Detection    │ │ │                │
        │ │                 ││  │ │              │ │ │                │
        │ │ Color Buttons   ││  │ │ Preset Views │ │ │                │
        │ │                 ││  │ │              │ │ │                │
        │ │ Info/Controls   ││  │ │              │ │ │                │
        │ │ Exit Button     ││  │ │              │ │ │                │
        │ └─────────────────┘│  │ └──────────────┘ │ │                │
        │                    │  │                  │ │                │
        └────────┬───────────┘  └─────────┬────────┘ └────────┬───────┘
                 │                        │                   │
                 │ (reads/writes)         │ (positions)       │ (ticks)
                 │                        │                   │
        ┌────────▼────────────────────────▼───────────────────▼────────┐
        │                    SCENE GAME OBJECTS                        │
        │                                                              │
        │  Pendulum Root                                               │
        │  ├── Rope (LineRenderer)                                     │
        │  ├── Bucket                                                  │
        │  │   └── Fluid Simulation                                    │
        │  │       ├── Particles (GPU)                                 │
        │  │       └── Canvas/Floor                                    │
        │  │                                                           │
        │  Main Camera (with OrbitCameraController)                    │
        │  UIManager (with RuntimeUIManager)                           │
        └──────────────────────────────────────────────────────────────┘
```

---

## Data Flow - Physics Controls Example

```
User Input (Right-Click + Mouse Move)
        ↓
OrbitCameraController.HandleMouseInput()
        ↓
Update currentYaw/currentPitch
        ↓
UpdateCameraPosition()
        ↓
Camera Position Quaternion Updated
        ↓
RaycastHit(collisionDetection)
        ↓
Display Visual Feedback


User Input (Slider Drag)
        ↓
RuntimeUIManager.AddSlider() → onValueChanged Listener
        ↓
Modify pendulumController.gravity
        ↓
CustomVerletPendulum.FixedUpdate()
        ↓
Rope Physics Recalculation
        ↓
LineRenderer Updated
        ↓
Visual Change on Screen
```

---

## Component Dependencies

```
OrbitCameraController
  ├── Requires: Camera component on same GameObject
  ├── Requires: Transform (orbitTarget)
  ├── Optional: Physics (for collision detection)
  └── Reads: Input.GetMouseButtonDown/Up, Input.GetAxis, Input.GetKey

RuntimeUIManager
  ├── Requires: CustomVerletPendulum reference
  ├── Requires: CustomSPHFluid reference
  ├── Creates: Canvas (if not provided)
  ├── Creates: All UI elements (Sliders, Buttons, Labels)
  └── Reads: References & modifies their public variables

CustomVerletPendulum
  ├── Uses: LineRenderer (for rope visualization)
  ├── References: bucketTransform, fluidSimulationObject
  └── Can be modified by: RuntimeUIManager sliders

CustomSPHFluid
  ├── Uses: ComputeShader (GPU particles)
  ├── Uses: Materials & Meshes
  ├── References: bucketTransform, floorRenderer
  └── Can be modified by: RuntimeUIManager sliders & color buttons
```

---

## Update Order

```
Frame Update Sequence:
1. Input Processing (Input.GetMouseButtonDown, etc.)
   ├── OrbitCameraController.Update()
   │   ├── HandleMouseInput()
   │   ├── HandleKeyboardInput()
   │   ├── HandleZoom()
   │   └── UpdateCameraPosition()
   │
   └── RuntimeUIManager.Update()
       └── UpdateSliderLabels()

2. Physics Update (FixedUpdate)
   ├── CustomVerletPendulum.FixedUpdate()
   │   ├── Verlet Integration
   │   ├── Constraint Solving
   │   └── Position Update
   │
   └── CustomSPHFluid.FixedUpdate()
       ├── SPH Particle Updates
       ├── Density & Pressure
       ├── Force Calculations
       └── Particle Position Update

3. Rendering (LateUpdate)
   ├── LineRenderer.SetPosition() (rope)
   ├── ParticleRenderer Updates
   └── Canvas UI Rendering

4. Display Rendering
   └── Screen Draw
```

---

## Message Flow - Changing Paint Color

```
User clicks "Red" Color Button
        ↓
Button.onClick.Invoke()
        ↓
RuntimeUIManager.AddColorButton() lambda executes:
    {
        fluidSimulation.paintColor = new Color(1f, 0.2f, 0.2f, 1f);
        fluidSimulation.paintColors[0] = new Color(1f, 0.2f, 0.2f, 1f);
    }
        ↓
CustomSPHFluid.GetPaintColor() now returns Red
        ↓
Next emission:
    fluidSimulation.SpawnFluidParticles()
        ↓
Particle visual color set to Red
        ↓
Next frame:
    RenderInstancedParticles() renders particles in Red
        ↓
Screen shows Red paint instead of Blue
```

---

## UI Hierarchy (Runtime Generated)

```
Canvas (RuntimeUICanvas)
├── GraphicRaycaster
├── CanvasScaler
└── UIPanel (RectTransform)
    ├── VerticalLayoutGroup
    ├── Title (Text: "PAINT SIMULATION UI")
    │
    ├── Separator (Image: gray line)
    │
    ├── Section Header (Text: "🎯 PHYSICS CONTROLS")
    │
    ├── Gravity Y (Container)
    │   ├── Label (Text: "Gravity Y")
    │   ├── Value (Text: "-9.81")
    │   └── Slider
    │       ├── Background (Image)
    │       ├── Fill (Image)
    │       └── Handle (Image)
    │
    ├── Rope Length (Container)
    │   ├── Label (Text: "Rope Length")
    │   ├── Value (Text: "5.00")
    │   └── Slider (...)
    │
    ├── [More Sliders...]
    │
    ├── Separator
    ├── Section Header (Text: "💧 FLUID CONTROLS")
    ├── [Fluid Sliders...]
    │
    ├── Separator
    ├── Section Header (Text: "🎨 PAINT CUSTOMIZATION")
    ├── Color Button "Red"
    ├── Color Button "Green"
    ├── Color Button "Blue"
    ├── [More Color Buttons...]
    │
    ├── Separator
    ├── Info Text (Text: "Use Arrow Keys/Mouse...")
    ├── Separator
    │
    └── Exit Button
        ├── Image (Color: Red)
        └── Text (Text: "⛔ EXIT APPLICATION")
```

---

## Camera Transform Calculation

```
Input: currentYaw (degrees), currentPitch (degrees), currentDistance (units)

Step 1: Create Rotation Quaternion
    rotation = Quaternion.Euler(currentPitch, currentYaw, 0°)

Step 2: Calculate Offset Vector
    // Start from "back" direction and rotate
    offset = rotation * Vector3.back  // Vector3.back = (0, 0, 1)
    // This gives us: (-sin(yaw)*cos(pitch), sin(pitch), cos(yaw)*cos(pitch))

Step 3: Scale by Distance
    offset = offset * currentDistance

Step 4: Add Pan Offset
    offset = offset + panOffset

Step 5: Calculate Final Position
    cameraWorldPosition = orbitTarget.position + offset

Step 6: Apply Collision Detection (Optional)
    if useCollisionDetection:
        cameraWorldPosition = CheckCollisions(orbitTarget, cameraWorldPosition)

Step 7: Set Transform
    transform.position = cameraWorldPosition
    transform.LookAt(orbitTarget.position + panOffset, Vector3.up)
```

---

## Slider Value Flow

```
User Drags Slider ─→ Slider.value changes (0.0 - 1.0)
                 ↓
        onValueChanged.Invoke(sliderValue)
                 ↓
    Lambda function executes with mapped value:

    Input: slider.value (0.0 - 1.0)
    Formula: actualValue = minValue + slider.value * (maxValue - minValue)
    Example: 0.5 * (15 - 2) = 0.5 * 13 = 6.5
                 ↓
    pendulumController.ropeLength = 6.5
                 ↓
    Next frame Update():
        labelText.text = "6.5"  (UpdateSliderLabels)
                 ↓
    Next frame FixedUpdate():
        CustomVerletPendulum recalculates rope positions with new length
                 ↓
    Screen shows updated rope
```

---

## Collision Detection Raycast

```
Camera Desired Position Calculation:
    desiredPos = orbitTarget + (rotation * Vector3.back) * distance

Collision Check:
    rayStart = orbitTarget.position
    rayDirection = (desiredPos - rayStart).normalized
    rayLength = Vector3.Distance(rayStart, desiredPos)

    Physics.Raycast(rayStart, rayDirection, out hit, rayLength, layers)

If Collision Detected:
    // Move camera just before the collision point
    finalPos = rayStart + rayDirection * (hit.distance - 0.5f)

If No Collision:
    finalPos = desiredPos

Apply Final Position:
    transform.position = finalPos
```

---

## Input Event Sequence (One Frame)

```
Frame Start
    │
    ├─ OnGUI() / Update() [Frame-dependent]
    │   ├─ Input.GetKeyDown(KeyCode) - returns true only this frame
    │   ├─ Input.GetMouseButtonDown(0) - returns true only this frame
    │   ├─ Input.mousePosition - current position
    │   └─ Input.GetAxis("Mouse ScrollWheel") - delta scroll
    │
    ├─ Update() [Called once per frame]
    │   ├─ OrbitCameraController.Update()
    │   │   ├─ HandleMouseInput() - processes mouse buttons & position
    │   │   ├─ HandleKeyboardInput() - processes arrow keys
    │   │   └─ HandleZoom() - processes scroll wheel
    │   │
    │   └─ RuntimeUIManager.Update()
    │       └─ UpdateSliderLabels() - refreshes value displays
    │
    ├─ FixedUpdate() [Fixed timestep for physics]
    │   ├─ CustomVerletPendulum.FixedUpdate()
    │   └─ CustomSPHFluid.FixedUpdate()
    │
    ├─ LateUpdate() [After all updates]
    │   ├─ Camera updates position/rotation
    │   └─ UI Canvas renders
    │
    └─ OnGUI() / Rendering
        └─ Screen drawn with all visual changes
```

---

**This diagram helps understand how all components interact and data flows through the system!**
