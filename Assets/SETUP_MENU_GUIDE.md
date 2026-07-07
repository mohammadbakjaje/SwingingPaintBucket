# 🎨 Professional Setup Menu System - Complete Guide

## Overview

The RuntimeUIManager has been completely redesigned to provide a **professional, clean setup menu experience** before the simulation starts. The system is **fully in Arabic** with English translations.

---

## 🎯 Key Features

### 1. **Startup Behavior**

- Application launches with a **professional setup menu**
- Simulation is **PAUSED** (Time.timeScale = 0)
- User cannot interact with the simulation until they click "Start Simulation"

### 2. **Setup Menu Contents (Arabic)**

- **Title**: محاكاة الدهان (Paint Simulation)
- **Section 1**: أقصى عدد جزيئات (Max Particles)
- **Section 2**: اختيار ألوان الدهان (Paint Color Selection)
- **Section 3**: نوع اللوحة (Canvas Material Type)
- **Section 4**: الإعدادات الفيزيائية (Physics Settings)
  - الجاذبية (Gravity)
  - اللزوجة (Viscosity)

### 3. **During Simulation**

- UI completely **hidden** for a clean viewing experience
- Small **"الإعدادات ⚙"** (Settings) button in top-right corner
- Press **ESC key** to return to menu
- Press **Settings button** to return to menu

---

## 🚀 How to Use

### Step 1: Launch Application

1. Run your built .exe file
2. You should see the **Setup Menu Panel** (centered on screen)
3. Simulation is paused - the pendulum and fluid do NOT move yet

### Step 2: Configure Settings

Configure the following in the Setup Menu:

#### **أقصى عدد جزيئات (Max Particles)**

- Slider: 1,000 to 50,000 particles
- Higher = More visual complexity, lower performance
- Recommended: 10,000-20,000

#### **اختيار ألوان الدهان (Paint Color Selection)**

- 6 color buttons: Red, Green, Blue, Yellow, Purple, Orange
- Click any color to select paint color
- Color persists until changed

#### **نوع اللوحة (Canvas Material Type)**

- 3 buttons: قماش (Cloth), خشب (Wood), معدن (Metal)
- Changes the canvas texture/appearance
- Affects visual result

#### **الإعدادات الفيزيائية (Physics Settings)**

- **الجاذبية (Gravity)**: 1 to 20 units
  - Higher gravity = stronger downward pull
  - Recommended: 9.81 (Earth gravity)
- **اللزوجة (Viscosity)**: 0 to 1
  - Higher viscosity = thicker paint, less flow
  - Recommended: 0.2-0.5

### Step 3: Start Simulation

1. Once you've configured all settings
2. Click the orange button: **ابدأ المحاكاة (Start Simulation)**
3. Menu disappears
4. Simulation begins (Time.timeScale = 1)
5. Watch the swinging paint bucket paint the canvas!

### Step 4: Return to Menu

During simulation, you can:

- Press **ESC key** to return to menu
- Click **الإعدادات ⚙** button (top-right corner)
- This pauses the simulation and shows the menu again

---

## 🎨 UI Design Details

### Color Scheme

- **Primary Color**: Professional Blue (0.2, 0.4, 0.7)
- **Secondary Color**: Dark Background (0.15, 0.15, 0.15)
- **Accent Color**: Orange (1.0, 0.5, 0.2)
- **Text Color**: White

### Layout

- **Centered Panel**: 600x800 pixels (responsive)
- **Setup Menu**: Full screen semi-transparent overlay (5% opacity)
- **Content Panel**: Centered with blue outline
- **Settings Button**: Top-right corner, small and non-intrusive

### Professional Look

✅ Clean, modern design  
✅ Proper spacing and hierarchy  
✅ Clear section headers  
✅ Easy-to-read sliders with values  
✅ Professional color palette  
✅ No clutter or debug text

---

## 🛠️ Technical Implementation

### Main Classes & Methods

#### **Class: RuntimeUIManager**

```csharp
// Startup
private void BuildSetupMenu()  // Creates the setup menu panel
private void Start()           // Called on app launch

// Simulation Control
private void StartSimulation() // Called when user clicks "ابدأ المحاكاة"
private void ReturnToMenu()    // Called when user presses ESC or clicks settings

// UI Building
private void AddSlider()       // Creates a slider with label
private void AddColorSelector()// Creates 6 color buttons
private void AddCanvasTypeSelector() // Creates canvas type buttons
private void AddStartButton()  // Creates "Start Simulation" button
```

#### **Time Control**

```csharp
// On startup - Paused
Time.timeScale = 0f;

// On "Start Simulation" - Running
Time.timeScale = 1f;

// On "Return to Menu" - Paused again
Time.timeScale = 0f;
```

#### **UI Hierarchy**

```
Canvas (ScreenSpaceOverlay)
├── SetupMenuPanel (Full Screen Overlay)
│   └── ContentPanel (Centered, 600x800)
│       ├── Title: "محاكاة الدهان"
│       ├── MaxParticles Slider
│       ├── ColorSelector (6 buttons in grid)
│       ├── CanvasTypeSelector (3 buttons)
│       ├── GravitySlider
│       ├── ViscositySlider
│       └── StartSimulationButton
│
└── RuntimeSettingsButton (Top-Right, visible during simulation)
```

---

## 📝 Arabic Labels Reference

| English                | Arabic               | Context        |
| ---------------------- | -------------------- | -------------- |
| Paint Simulation       | محاكاة الدهان        | Main title     |
| Max Particles          | أقصى عدد جزيئات      | Section header |
| Number of Particles    | عدد الجزيئات         | Slider label   |
| Paint Color Selection  | اختيار ألوان الدهان  | Section header |
| Canvas Material        | نوع اللوحة           | Section header |
| Cloth                  | قماش                 | Canvas type    |
| Wood                   | خشب                  | Canvas type    |
| Metal                  | معدن                 | Canvas type    |
| Physics Settings       | الإعدادات الفيزيائية | Section header |
| Gravity                | الجاذبية             | Slider label   |
| Viscosity              | اللزوجة              | Slider label   |
| Start Simulation       | ابدأ المحاكاة        | Button label   |
| Settings               | الإعدادات            | Button label   |
| Returned to Setup Menu | العودة إلى القائمة   | Debug message  |

---

## ⚙️ Configuration

### Default Values

You can customize the default values in the RuntimeUIManager:

```csharp
[Header("Color Theme")]
public Color primaryColor = new Color(0.2f, 0.4f, 0.7f, 1f);      // Change theme color
public Color secondaryColor = new Color(0.15f, 0.15f, 0.15f, 1f); // Change background
public Color accentColor = new Color(1f, 0.5f, 0.2f, 1f);        // Change accent
public Color textColor = Color.white;                              // Change text color
```

### Slider Ranges

In the `BuildSetupMenu()` method, you can adjust these ranges:

```csharp
// Max Particles: 1000 to 50000
AddSlider(contentPanel, "عدد الجزيئات", fluidSimulation.maxParticles,
    1000f, 50000f, ...);

// Gravity: 1 to 20
AddSlider(contentPanel, "الجاذبية (Gravity)", Mathf.Abs(pendulumController.gravity.y),
    1f, 20f, ...);

// Viscosity: 0 to 1
AddSlider(contentPanel, "اللزوجة (Viscosity)", fluidSimulation.viscosity,
    0f, 1f, ...);
```

---

## 🐛 Troubleshooting

### Issue: Setup menu doesn't appear

**Solution:**

- Check that UIManager GameObject exists in scene
- Verify RuntimeUIManager script is attached to UIManager
- Check Console for errors
- Time.timeScale should be 0 at startup

### Issue: Simulation doesn't pause on startup

**Solution:**

- Add this to Start() method if missing:

```csharp
Time.timeScale = 0f;
```

### Issue: Settings button doesn't appear during simulation

**Solution:**

- Verify CreateRuntimeSettingsButton() is called in StartSimulation()
- Check that runtimeSettingsButton is created correctly
- The button should be visible in top-right corner

### Issue: Returning to menu doesn't work

**Solution:**

- Press ESC key should work (check Input.GetKeyDown(KeyCode.Escape))
- Click settings button should work
- If neither works, verify ReturnToMenu() method exists

### Issue: Arabic text displays as boxes

**Solution:**

- Your system may need Arabic language support
- Or the font doesn't support Arabic characters
- Try using a Unicode font that supports Arabic
- Fallback: The script includes English labels too

---

## 🎮 User Experience Flow

```
┌─────────────────────────────────┐
│  Application Launches           │
│  Time.timeScale = 0 (PAUSED)    │
│  Setup Menu appears (centered)  │
└──────────────┬──────────────────┘
               │
    ┌──────────▼──────────┐
    │ User Configures:    │
    │ - Particles         │
    │ - Color             │
    │ - Canvas Type       │
    │ - Gravity           │
    │ - Viscosity         │
    └──────────┬──────────┘
               │
    ┌──────────▼───────────────────┐
    │ User Clicks                   │
    │ "ابدأ المحاكاة"                │
    │ (Start Simulation)            │
    └──────────┬───────────────────┘
               │
    ┌──────────▼──────────────────────┐
    │ Setup Menu Disappears           │
    │ Time.timeScale = 1 (RUNNING)    │
    │ Settings ⚙ Button appears      │
    │ SIMULATION STARTS               │
    └──────────┬───────────────────────┘
               │
    ┌──────────▼──────────────┐
    │ During Simulation:       │
    │ - Watch paint bucket     │
    │ - Clean interface        │
    │ - Only settings button   │
    └──────────┬───────────────┘
               │
       ┌───────┴────────┐
       │                │
    ┌──▼──┐          ┌──▼──────────┐
    │ ESC │          │ Settings ⚙  │
    │ Key │          │   Button    │
    └──┬──┘          └──┬──────────┘
       │                │
       └────────┬───────┘
                │
        ┌───────▼────────────────┐
        │ Return to Menu         │
        │ Setup Menu Reappears   │
        │ Time.timeScale = 0     │
        │ (PAUSED AGAIN)         │
        └────────────────────────┘
```

---

## 📊 Performance Considerations

- **Setup Menu Performance**: Minimal overhead, displayed only at startup
- **Runtime Button**: Single small button, negligible performance impact
- **Slider Updates**: Only update when slider is moved
- **Frame Rate**: Should maintain 60+ FPS once simulation starts

---

## 🎬 Example Workflow

1. **User launches app**
   - Sees centered setup menu
   - Reads Arabic labels (or English translations)

2. **User configures settings**
   - Adjusts particle count: 15,000
   - Selects red color
   - Chooses wooden canvas
   - Sets gravity to 9.81
   - Sets viscosity to 0.3

3. **User clicks "ابدأ المحاكاة"**
   - Menu fades away
   - Settings button appears in corner
   - Simulation starts running
   - Pendulum swings with red paint
   - Particles paint the wooden canvas

4. **User watches simulation**
   - Beautiful, uncluttered view
   - Can interact with camera (arrow keys, right-click)
   - Can pause/adjust settings anytime

5. **User presses ESC or clicks settings**
   - Simulation pauses
   - Settings menu reappears
   - Can adjust parameters again

---

## ✨ Professional Features

✅ **Clean Interface**: No debug clutter  
✅ **Responsive Design**: Centered panel scales to content  
✅ **Professional Colors**: Blue/orange theme  
✅ **Arabic-First**: All labels in Arabic with English sub-text  
✅ **Smooth Transitions**: Menu appears/disappears cleanly  
✅ **Intuitive Controls**: Obvious what to do  
✅ **Non-Intrusive**: Settings button is small and unobtrusive  
✅ **Production-Ready**: Looks like a finished product

---

## 🚀 Next Steps

1. **Test the Setup Menu**
   - Launch app in Editor (Play Mode)
   - Verify menu appears and is centered
   - Check that Time.timeScale is 0

2. **Test Parameter Adjustment**
   - Try adjusting sliders
   - Verify values update in real-time
   - Click different color buttons

3. **Start Simulation**
   - Click "ابدأ المحاكاة"
   - Verify simulation runs (pendulum swings)
   - Verify settings button appears

4. **Return to Menu**
   - Press ESC or click settings button
   - Verify menu reappears
   - Verify simulation is paused

5. **Build and Deploy**
   - Build to .exe
   - Test on standalone application
   - Enjoy your professional UI!

---

## 📞 Support

If you have issues:

1. Check the Troubleshooting section above
2. Verify UIManager GameObject is in scene
3. Check Console for error messages
4. Ensure Time.timeScale is being controlled correctly

**Congratulations on creating a professional simulation interface! 🎉**
