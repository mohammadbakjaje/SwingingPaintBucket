# ⚡ Quick Setup Reference

## 🎯 30-Second Setup (TL;DR)

### Step 1: Attach OrbitCameraController to Main Camera

```
Select Main Camera → Add Component → Search "OrbitCameraController"
Set "Orbit Target" to your Pendulum root GameObject
```

### Step 2: Create UIManager GameObject

```
Right-click Hierarchy → Create Empty → Rename to "UIManager"
Add RuntimeUIManager component
Drag your Pendulum GameObject to "Pendulum Controller" field
Drag your Fluid GameObject to "Fluid Simulation" field
Leave UI Canvas empty (auto-created)
```

### Step 3: Build & Run

```
File → Build Settings → Build → Done!
Run the .exe and press TAB to show UI
```

---

## 📋 Checklist

- [ ] Main Camera has `OrbitCameraController` component
- [ ] OrbitCameraController has Orbit Target assigned
- [ ] UIManager GameObject exists in scene
- [ ] UIManager has `RuntimeUIManager` component
- [ ] RuntimeUIManager has Pendulum Controller assigned
- [ ] RuntimeUIManager has Fluid Simulation assigned
- [ ] Scene is saved
- [ ] Project builds without errors
- [ ] UI appears in bottom-left corner (press TAB to toggle)
- [ ] Right-mouse-button rotates camera
- [ ] Scroll wheel zooms camera
- [ ] Sliders change physics values in real-time

---

## 🎮 User Controls (For Your End Users)

Print this and include in your app or documentation:

### Camera Controls

- **Right-Click + Move**: Rotate view around pendulum
- **Scroll Wheel**: Zoom in/out
- **Middle-Click + Move**: Pan the view
- **Arrow Keys**: Rotate using keyboard
- **Numpad 8**: Top-down view
- **Numpad 2**: Side profile view
- **Numpad 7**: Isometric angle

### UI Controls

- **TAB**: Hide/Show UI panel
- **Sliders**: Drag left/right to adjust values
- **Color Buttons**: Click to change paint color
- **EXIT Button**: Quit the application

---

## 🐛 Quick Troubleshooting

| Problem              | Fix                                                      |
| -------------------- | -------------------------------------------------------- |
| UI not appearing     | Make sure UIManager is in scene, press TAB to show       |
| Camera won't move    | Verify Orbit Target is assigned in OrbitCameraController |
| Sliders don't work   | Check Pendulum/Fluid references in UIManager Inspector   |
| App crashes on start | Check Console for missing component errors               |

---

## 📁 File Reference

| File                       | Purpose                                     |
| -------------------------- | ------------------------------------------- |
| `RuntimeUIManager.cs`      | Creates and manages the UI panel at runtime |
| `OrbitCameraController.cs` | Handles all camera movement and controls    |
| `SETUP_GUIDE.md`           | Detailed setup instructions (this file)     |
| `QUICK_REFERENCE.md`       | This quick reference card                   |

---

## ✨ What You Get

✅ Professional runtime UI panel  
✅ Full control over physics parameters  
✅ Real-time paint color customization  
✅ Smooth orbital camera with zoom  
✅ Keyboard + Mouse controls  
✅ Tab to hide/show UI  
✅ Exit button for graceful shutdown

---

**That's it! You're ready to go!** 🚀
