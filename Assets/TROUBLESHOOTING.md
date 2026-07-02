# 🆘 Troubleshooting Checklist

## Pre-Setup Verification

### Before Attaching Scripts

- [ ] Main Camera exists in scene
- [ ] CustomVerletPendulum script exists in project
- [ ] CustomSPHFluid script exists in project
- [ ] Both scripts have all required public fields
- [ ] Pendulum has Transform and LineRenderer
- [ ] Fluid Simulation has bucket reference
- [ ] No compilation errors in Console

---

## Camera Issues

### Problem: Camera doesn't respond to mouse input

**Quick Diagnosis:**

1. Select Main Camera in Hierarchy
2. Check if OrbitCameraController component exists
3. Check if OrbitCameraController has Orbit Target assigned
4. Look at Console for errors

**Solutions:**

```
☑ Add OrbitCameraController to Main Camera:
  - Select Main Camera
  - Add Component → OrbitCameraController

☑ Assign Orbit Target:
  - Select Main Camera
  - In OrbitCameraController, find "Orbit Target" field
  - Drag the pendulum root GameObject into it

☑ Check Script Compilation:
  - Are there any errors in Console?
  - If yes, fix them first

☑ Verify Input System:
  - Is Input System enabled in Project Settings?
  - File → Build Settings → Player Settings → Input
```

### Problem: Camera rotates too fast or too slow

**Solution:**

```
Adjust in OrbitCameraController:
- Rotation Speed: 2 (default)
  Lower = slower, Higher = faster
- Keyboard Rotation Speed: 30 (default)
  Lower = slower, Higher = faster
```

### Problem: Camera clips through objects

**Solution:**

```
In OrbitCameraController:
☑ Enable Collision Detection:
  useCollisionDetection = true

☑ Set Collision Layers:
  collisionLayers = All layers or specific layer mask

☑ If still clipping, adjust:
  - Reduce the collision offset (find "0.5f" in CheckCollisions method)
  - Increase minDistance to prevent camera getting too close
```

### Problem: Zoom with scroll wheel doesn't work

**Solution:**

```
☑ Check if Mouse ScrollWheel is blocked by UI
  - Click on viewport area (not on UI panel)
  - Try scrolling again

☑ If still not working:
  - In OrbitCameraController, check HandleZoom()
  - Verify zoomSpeed is not 0

☑ Test Input:
  - Open Console
  - Run this code: Debug.Log(Input.GetAxis("Mouse ScrollWheel"));
  - Check if values change when scrolling
```

### Problem: Camera stuck in one position

**Solution:**

```
☑ Check if lerp smoothing is too high:
  rotationSmoothness = 0.1 (try this value)
  zoomSmoothness = 0.15 (try this value)

  If too smooth (0.9+), camera moves very slowly

☑ Check if frame rate is low:
  - Open Stats panel while running
  - If FPS < 30, optimize scene

☑ Verify Update() is being called:
  - Add Debug.Log in OrbitCameraController.Update()
  - Check Console for spam
```

---

## UI Panel Issues

### Problem: UI panel doesn't appear

**Quick Diagnosis:**

1. Check if UIManager exists in scene
2. Check if RuntimeUIManager component is attached
3. Try pressing TAB (default toggle key)
4. Check Console for errors

**Solutions:**

```
☑ Verify UIManager exists:
  - Check Hierarchy for "UIManager" GameObject
  - If not there, create: Right-click → Create Empty → Name "UIManager"

☑ Add RuntimeUIManager script:
  - Select UIManager
  - Add Component → RuntimeUIManager

☑ Check UI visibility:
  - Press TAB to toggle visibility
  - If still not visible, check if it's off-screen

☑ Verify references:
  - In RuntimeUIManager Inspector:
    - Pendulum Controller should have value
    - Fluid Simulation should have value
  - If empty, drag GameObjects into these fields

☑ Check startup setting:
  - In RuntimeUIManager: uiStartsVisible = true
```

### Problem: Sliders appear but don't do anything

**Quick Diagnosis:**

1. Check Console for errors
2. Verify pendulum/fluid references are assigned
3. Check if values are being read in scripts

**Solutions:**

```
☑ Verify references in UIManager Inspector:
  - Drag actual GameObject (not just Transform)
  - Make sure GameObject has the script component

☑ Check Console for errors:
  - Look for NullReferenceException
  - Look for missing component warnings

☑ Test manually:
  - In Play Mode, open Console
  - Select Main Camera
  - Manually set: pendulum.ropeLength = 8f;
  - Did rope visual change?
  - If yes, something is wrong with UI linking
  - If no, something is wrong with the script itself

☑ Check if scripts are public:
  - All variables being controlled must be public
  - Example: public float ropeLength;
  - If private or [SerializeField], UI can't access

☑ For complex parameters:
  - Some changes require reinitialization
  - Example: ropeLength needs pendulum.InitializeRope()
  - Check SETUP_GUIDE.md for special cases
```

### Problem: Color buttons don't change paint color

**Quick Diagnosis:**

1. Verify fluid simulation reference is assigned
2. Check if paintColor variable is public
3. Check Console for errors

**Solutions:**

```
☑ In RuntimeUIManager Inspector:
  - Verify "Fluid Simulation" field is filled
  - If not, drag the GameObject with CustomSPHFluid

☑ In CustomSPHFluid.cs:
  - Verify this line exists: public Color paintColor;
  - If not, the color feature may be disabled

☑ Check for paint color initialization:
  - Make sure paintColors array has at least 1 element
  - In CustomSPHFluid Start():
    if (paintColors == null || paintColors.Length == 0)
        paintColors = new Color[] { paintColor };

☑ Test manually in Console:
  - fluidSimulation.paintColor = Color.red;
  - Did particles start emitting in red?
  - If yes, UI linking has an issue
  - If no, something else is wrong

☑ Check emission:
  - Are particles actually being emitted?
  - If no particles visible, fix that first
  - Then fix color
```

### Problem: TAB key doesn't toggle UI visibility

**Solution:**

```
☑ Check key mapping in RuntimeUIManager:
  - toggleUIKey = KeyCode.Tab (default)
  - Can change to any KeyCode

☑ Verify Update() is running:
  - Add Debug.Log("UI Update"); in RuntimeUIManager.Update()
  - Check Console spam

☑ Check if UI is disabled:
  - uiStartsVisible setting
  - If false, try pressing TAB multiple times
  - UI panel might be in wrong state

☑ Alternative key:
  - Change toggleUIKey to KeyCode.Space
  - Test if that works
```

---

## Physics Simulation Issues

### Problem: Pendulum doesn't swing after changing parameters

**Quick Diagnosis:**

1. Check if rope is visible
2. Check if bucket is at end of rope
3. Check gravity value (should be negative)

**Solutions:**

```
☑ Verify initial conditions:
  - In CustomVerletPendulum Inspector:
    - segmentCount > 0
    - ropeLength > 0
    - gravity.y < 0 (should be like -9.81)

☑ Test InitializeRope():
  - After changing ropeLength, does rope reinitialize?
  - Some parameters need manual reset

☑ Check if physics is running:
  - In Editor, press Play
  - Is pendulum swinging even without UI?
  - If no, problem is in CustomVerletPendulum itself
  - If yes, problem is in UI linkage

☑ Check if air resistance is too high:
  - airResistance should be between 0 and 0.5
  - If too high (like 1.0), pendulum stops
  - Try setting to 0.02

☑ Check gravity:
  - gravity.y should be negative
  - Default: -9.81
  - If positive, pendulum flies upward
```

### Problem: Fluid particles don't emit or disappear immediately

**Quick Diagnosis:**

1. Check maxParticles setting
2. Check emissionRate
3. Check bucket is positioned correctly

**Solutions:**

```
☑ Verify bucket position:
  - Is bucket positioned at end of rope?
  - If not, particles won't emit from correct position
  - Check bucketTransform reference

☑ Check emission rate:
  - emissionRate should be > 0
  - Default: 120
  - If 0, no particles emit

☑ Check max particles:
  - maxParticles = 10000 (default)
  - Is slider set too low? (like 100)
  - Try setting to 10000

☑ Check for particle overflow:
  - If maxParticles = 10000 and emissionRate = 1000/second
  - Particles max out quickly
  - Solution: Lower emissionRate or raise maxParticles

☑ Check performance:
  - Are particles hitting GPU limit?
  - Try reducing emissionRate
  - Or lower textureResolution

☑ Test manually:
  - In CustomSPHFluid Inspector
  - Set maxParticles = 10000
  - Set emissionRate = 100
  - Press Play
  - Do particles appear?
  - If yes, problem is in UI linkage
  - If no, problem is in CustomSPHFluid
```

---

## Build/Deployment Issues

### Problem: Application crashes on startup

**Quick Diagnosis:**

1. Check Console for errors in Editor (before building)
2. Run in Editor first to verify everything works
3. Check Player Log after crash

**Solutions:**

```
☑ Verify no compile errors:
  - Open Console in Editor
  - Any red errors? Fix them first

☑ Test in Editor first:
  - Play Mode should work perfectly
  - If it crashes in Editor, it will crash in build

☑ Check for missing references:
  - Select UIManager
  - Check if Pendulum Controller is assigned
  - Check if Fluid Simulation is assigned

☑ Look at Player Log:
  - After .exe crashes, check:
  - [Game Folder]/[GameName]_Data/output_log.txt
  - Look for error messages

☑ Common crash causes:
  - NullReferenceException (missing reference)
  - OutOfMemoryException (too many particles)
  - MissingComponentException (component removed)

☑ Rebuild with debug info:
  - Build Settings → Development Build (check this)
  - Rebuild and try again
  - Check output_log.txt for more details
```

### Problem: UI doesn't work in .exe but works in Editor

**Solution:**

```
☑ Check if scene is in Build Settings:
  - File → Build Settings
  - Your scene should be listed in "Scenes in Build"
  - If not listed, add it

☑ Check scene is default scene:
  - First scene in list should be your startup scene
  - If wrong, reorder scenes

☑ Verify build includes all scripts:
  - All .cs files should be in Assets folder
  - Not in Editor folder or subfolders Editor uses

☑ Test with simple build first:
  - Build without optimization
  - Check if UI appears
  - Then optimize if needed

☑ Check Graphics API:
  - Sometimes DirectX vs OpenGL issues
  - Try other graphics API in Build Settings
```

### Problem: Camera won't work in .exe

**Solution:**

```
☑ Same as above - verify scene is in Build Settings

☑ Check Main Camera exists:
  - Must be tagged "MainCamera"
  - Select Main Camera in scene
  - Inspector → Tag dropdown → Select "MainCamera"

☑ Verify OrbitCameraController assignment:
  - Main Camera should have OrbitCameraController
  - Orbit Target should be assigned
  - Check this before building

☑ Test Input:
  - If camera won't respond to mouse
  - Check Input Manager (Edit → Project Settings → Input)
  - Verify Mouse X, Mouse Y, Mouse ScrollWheel exist
```

---

## Performance Issues

### Problem: Application runs slowly

**Quick Diagnosis:**

1. Open Stats window (Game window → Stats)
2. Check FPS
3. Check which system is bottleneck (CPU/GPU/Memory)

**Solutions:**

```
☑ If low FPS (< 30):
  - Reduce maxParticles (try 5000 instead of 10000)
  - Reduce emissionRate
  - Lower textureResolution (try 512 instead of 1024)
  - Disable collision detection in camera

☑ If GPU-bound:
  - Reduce particleVisualScale
  - Lower quality settings
  - Disable GPU instancing (useGpuInstancing = false)

☑ If CPU-bound:
  - Lower segmentCount in pendulum
  - Reduce smoothingRadius in fluid
  - Simplify UI (remove some sliders)

☑ If Memory-bound:
  - Reduce maxParticles significantly
  - Check for particle accumulation
  - Monitor particle count in real-time

☑ Optimization tips:
  - Close other applications
  - Lower screen resolution
  - Disable Discord/OBS overlays
```

### Problem: Application uses too much memory

**Solution:**

```
☑ Check particle count:
  - maxParticles * particle_size ≈ memory used
  - Each particle ≈ 100 bytes
  - 10000 particles ≈ 1 MB (plus GPU memory)

☑ Reduce maxParticles:
  - Lower from 10000 to 5000
  - Test memory usage

☑ Check for memory leaks:
  - Do particles accumulate over time?
  - Or do they respawn at same count?
  - If accumulating, there's a leak in CustomSPHFluid

☑ Disable GPU instancing:
  - useGpuInstancing = false
  - Less GPU memory used
  - May be slower though

☑ Monitor with Windows Task Manager:
  - Run .exe
  - Open Task Manager
  - Check "Memory" column
  - Should stabilize after a few seconds
  - If always increasing, memory leak
```

---

## Reference: Common Error Messages

### NullReferenceException

```
Error: NullReferenceException: Object reference not set to an instance of an object

Cause: Trying to use variable that is null/empty

Fix: Check in Inspector that all references are assigned
  - UIManager.pendulumController
  - UIManager.fluidSimulation
  - OrbitCameraController.orbitTarget
```

### MissingComponentException

```
Error: MissingComponentException: There is no 'CustomVerletPendulum'
       attached to the "Pendulum" GameObject

Cause: Script is not attached to GameObject

Fix:
  - Find the GameObject that should have script
  - In Inspector, click "Add Component"
  - Search for and add the script
```

### MissingReferenceException

```
Error: MissingReferenceException: The object of type 'Transform' has been destroyed
       but you are still trying to access it

Cause: Reference is to destroyed object

Fix:
  - Don't destroy GameObjects that scripts reference
  - Or reassign references before destroying
```

### ArrayIndexOutOfBoundsException

```
Error: IndexOutOfRangeException: Array index is out of range

Cause: Trying to access array element that doesn't exist

Common case: paintColors array is empty
Fix: Ensure paintColors has at least 1 element
```

---

## Quick Reference: Debug Techniques

### Check if Update is Running

```csharp
void Update()
{
    // Add this temporarily
    Debug.Log("Update called - Frame: " + Time.frameCount);

    // Should spam console if working
    // If no spam, Update isn't being called
}
```

### Check Variable Values

```csharp
// Add to Update/FixedUpdate
Debug.Log("Rope Length: " + pendulumController.ropeLength);
Debug.Log("Camera Position: " + transform.position);
Debug.Log("Slider Value: " + slider.value);
```

### Check if Reference Exists

```csharp
if (pendulumController == null)
    Debug.LogError("Pendulum Controller is NULL!");
else
    Debug.Log("Pendulum Controller found: " + pendulumController.name);
```

### Visual Debug (Gizmos)

```csharp
void OnDrawGizmos()
{
    if (orbitTarget != null)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(orbitTarget.position, 1f);
    }
}

// These show in Scene view as green sphere
```

---

## Still Stuck?

If you've gone through this checklist and still have issues:

1. **Check Console Carefully**
   - Every error/warning is important
   - Screenshot the error
   - Google the error message

2. **Verify Script Files Exist**
   - RuntimeUIManager.cs in Assets folder
   - OrbitCameraController.cs in Assets folder
   - No compile errors shown

3. **Reset to Defaults**
   - Delete both scripts from GameObject
   - Re-add them
   - Re-assign all references
   - Try again

4. **Minimal Reproduction**
   - Create new empty scene
   - Add only camera + pendulum + fluid
   - Add only camera controller
   - Test if that works
   - Then add UI
   - Helps isolate problem

5. **Check Documentation**
   - SETUP_GUIDE.md (detailed steps)
   - QUICK_REFERENCE.md (quick help)
   - ADVANCED_CUSTOMIZATION.md (technical details)
   - ARCHITECTURE_DIAGRAM.md (how it all connects)

---

**Remember: Start simple, then add complexity! 🚀**
