# VR CPR Training Simulator: 5-Person Parallel Development Plan

Since this is your first Unity project, the most important thing is not to let all 5 members start creating random things independently. You need a shared Unity structure first, then divide the work into modules that can be developed in parallel.

## 1. Overall development structure

Your project should be divided into these major systems:

```
                    VR CPR TRAINING SIMULATOR
                              │
          ┌───────────────────┼───────────────────┐
          │                   │                   │
      Environment          VR Systems          Gameplay
          │                   │                   │
   Room / Props       XR / Locomotion       CPR Scenario
   Lighting           Interaction           AED
   Audio              UI                    Assessment
          │                   │                   │
          └───────────────────┼───────────────────┘
                              │
                         Integration
                              │
                    Testing + Presentation

```

The key is that everyone works on separate areas, but everything eventually connects to one common project.

## 2. First: Do NOT divide the work immediately

Before the five members start parallel development, have one setup session together. This should be your **Day 1 / Phase 0**.

Everyone should be present to create:

* Unity project

* Git repository

* Unity version

* OpenXR

* XR Interaction Toolkit

* Input System

* XR Device Simulator

* Basic scene

* Folder structure

* Basic VR player

* Git workflow

Then make sure everyone can clone/open the project and run the same basic VR scene.

> **Extremely Important:** Otherwise, you can end up with Pasan on Unity version X, Pavithra with different packages, Enuka using a different input system, Kavisha with a different scene, and Nuzha with a different project structure, making integration painful.

## 3. Recommended roles for your 5 members

These are primary responsibilities, not isolated ones. Everyone should understand the complete application.

| Team Member | Main Responsibility | 
| ----- | ----- | 
| **Pasan** | VR/XR Systems + Integration | 
| **Pavithra** | Environment + 3D Assets + Lighting | 
| **Enuka** | CPR Gameplay System | 
| **Kavisha** | AED + Interactive Objects + Physics | 
| **Nuzha** | UI + Assessment + Audio | 

## 4. Pasan — VR Systems + Integration

### Main tasks

Set up:

* Unity project

* OpenXR

* XR Interaction Toolkit

* XR Origin

* XR Device Simulator

* Input Actions

* Locomotion & interaction system

* Grab system

* Ray/direct interaction

* Tracking origin & comfort settings

They should create the basic VR player:

```
XR Origin
│
├── Main Camera
├── Left Controller
├── Right Controller
│
└── Locomotion System

```

They also manage:

* Git

* Scene integration

* Package conflicts

* Final builds

* Project structure

* Merging everyone's work

*Note: Pasan should not spend the whole project making features. Their most important job is making sure everyone's work eventually works together.*

## 5. Pavithra — Environment

This person creates the actual world.

### Create:

* Training room, floor, walls, ceiling, doors, table, chairs

* Training equipment area, casualty area, AED table

*The environment should remain relatively small. Don't build an entire hospital.*

```
       ┌──────────────────────────┐
       │                          │
       │      Training Area       │
       │                          │
       │        MANNEQUIN         │
       │           ↓              │
       │                          │
       │                    AED   │
       │                    TABLE │
       │                          │
       │   PHONE                  │
       │                          │
       └──────────────────────────┘

```

### Also responsible for:

* Lighting

* Materials

* Environmental props

* Visual consistency

* Basic environment audio locations

## 6. Enuka — CPR Gameplay

This is one of the most important roles. They build the actual CPR sequence.

### System flow

Approach casualty $\rightarrow$ Check responsiveness $\rightarrow$ Call emergency assistance $\rightarrow$ Position hands $\rightarrow$ Start CPR $\rightarrow$ Count compressions $\rightarrow$ Measure rhythm $\rightarrow$ Complete CPR stage

### Implement:

1. **Responsiveness interaction:** The user interacts with the casualty (Unity detects player interacted with shoulder $\rightarrow$ responsiveness checked $\rightarrow$ continue scenario).

2. **CPR zone:** Create an interaction zone around the chest to determine whether virtual hands are correctly positioned.

3. **Compression system:** Track compression sequence, count, timing, position, and completion.

*Enuka should work closely with Nuzha because the assessment system needs the CPR data.*

## 7. Kavisha — AED + Interactive Objects

This person handles the more physical object interactions.

### AED Flow

Retrieve AED $\rightarrow$ Carry AED $\rightarrow$ Place AED $\rightarrow$ Open AED $\rightarrow$ Take pads $\rightarrow$ Place pads $\rightarrow$ Activate $\rightarrow$ Complete AED sequence

### Other interactive objects

* Phone

* Doors

* Buttons

* Training equipment & other physical objects

*This gives your project good evidence for the requirement: believable hands-on interaction.*

## 8. Nuzha — UI + Assessment + Audio

### World-space UI

Create start screen, tutorial instructions, emergency instructions, CPR instructions, AED instructions, feedback, final results, and restart/exit options.

* *Important:* Do not create a normal flat-screen Canvas HUD. The UI should exist inside the VR world.

### Assessment system

Nuzha receives data from the gameplay systems:

```
CPR System ──(Hand position ✓, Compression count ✓, Rhythm ✗)──> Assessment Manager ──> Final Result

```

### Audio

Manage emergency sound, instructor voice, AED voice, interaction sounds, button sounds, environmental sounds, and spatial audio. *Do not leave audio until the final day; add it during integration.*

## 9. Folder structure

Before parallel work begins, create a clean folder structure:

```
Assets/
│
├── _Project/
│   │
│   ├── Scenes/
│   ├── Scripts/
│   ├── Prefabs/
│   ├── Materials/
│   ├── UI/
│   ├── Audio/
│   ├── Animations/
│   ├── Settings/
│   └── ScriptableObjects/
│
├── Art/
│   ├── Environment/
│   ├── Characters/
│   ├── Medical/
│   └── Props/
│
├── ThirdParty/
│
└── XR/

```

*Very important:* Do not have everyone dumping assets into `Assets/` with random filenames.

## 10. Scene structure

Keep the project simple. Initially:

```
Scenes/
│
├── MainMenu.unity
├── TrainingRoom.unity
└── TestScene.unity

```

*The main experience happens in `TrainingRoom.unity`. Don't create Scene1 through Scene5 unless you actually need separate scenes. Your assignment specifically wants one coherent experience.*

## 11. How everyone works in parallel

Imagine the team starts Monday.

### Day 1 — Everyone together

Do: Unity setup $\rightarrow$ Git $\rightarrow$ packages $\rightarrow$ XR $\rightarrow$ test.
Everyone must be able to open project $\rightarrow$ press Play $\rightarrow$ enter VR simulation $\rightarrow$ move $\rightarrow$ grab object.

Only after that:

```
                COMMON BASE
                     │
       ┌─────────────┼─────────────┐
       ↓             ↓             ↓
 Environment       CPR           AED
       │             │             │
       └─────────────┼─────────────┘
                     ↓
                  UI/Audio
                     ↓
                 Integration

```

### Use prefabs to avoid conflicts

Instead of everyone editing the same scene, members should develop prefabs/components separately:

* Enuka creates `CPRMannequin.prefab`

* Kavisha creates `AED.prefab`

* Nuzha creates `TrainingResultPanel.prefab`

* Pavithra creates `TrainingRoomEnvironment.prefab`

* Pasan integrates them into the main scene.

## 12. Git workflow

Strongly recommended using GitHub with individual feature branches:

```
main
 │
 ├── pasan-xr
 ├── pavithra-environment
 ├── enuka-cpr
 ├── kavisha-aed
 └── nuzha-ui

```

*Do not let everyone directly modify main.*

### Important Unity Git rule

Your `.gitignore` should include standard Unity-generated folders:

* `Library/`

* `Temp/`

* `Logs/`

* `Obj/`

* `Build/`

* `Builds/`

* `UserSettings/`

Commit important project files such as `Assets/`, `Packages/`, and `ProjectSettings/`.

## 13. Development phases

* **Phase 0 — Project setup:** Everyone installs Unity, sets up GitHub, configures XR packages, OpenXR, XR Interaction Toolkit, XR Device Simulator, Input System, and folder structure.

* **Phase 1 — Core VR Prototype:** Basic room blockout, basic mannequin interaction, basic grab objects + AED prototype, and basic world-space UI.

* **Phase 2 — Environment:** Pavithra develops environment properly while others build their respective gameplay loops.

* **Phase 3 — Gameplay Integration:** Connect start, tutorial, emergency, assessment, phone, CPR, AED, assessment, and end.

* **Phase 4 — Polish:** Improve lighting, materials, sound, animations, UI, interaction feedback, object physics, transitions, and instructions.

* **Phase 5 — Testing:** Peer testing where one person who didn't build the feature tests it.

* **Phase 6 — Exported Build:** Generate Windows build with keyboard/mouse fallback.

* **Phase 7 — Final Presentation:** Demonstrate concept, D.I.C.E., tracking origin, locomotion, comfort, presence, interaction, physics, UI, advanced features, testing, limitations, exported build, and credits.

## 14. What you should NOT do

* ❌ **Pasan builds the whole game first:** Creates a bottleneck.

* ❌ **Everyone edits `TrainingRoom.unity` directly:** Causes heavy Git conflicts.

* ❌ **Everyone creates their own XR setup:** Leads to incompatible systems.

* ❌ **Build the final environment first:** You can waste days making a beautiful room before knowing whether gameplay works.

* ❌ **Start with realistic CPR physics:** Get the interaction working first.

* ❌ **Download hundreds of assets:** Keep a small, consistent environment.

## 15. Your first milestone

Before writing any CPR code, achieve **Milestone 1**:
*"All 5 members can run the same basic VR project with working movement and grab mechanics."*

Once this works for everyone, start building the actual CPR simulator.