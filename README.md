# VR CPR Emergency Response Training Simulator

A Unity-based educational prototype that guides learners through a simulated emergency response in a virtual training room. Learners assess a virtual casualty, request assistance, practise CPR interactions, and complete an AED scenario with step-by-step instructions, narration, and feedback.

> This project simulates a scripted training scenario. It does not measure physical compression depth or force, and is not a substitute for practical CPR training or certification.

## Features

- Explorable training room with XR locomotion, snap turning, and collision handling.
- Spatial **Start Training** button and an introductory guide character.
- Instruction panels, narrated guidance, environmental audio, and a compression metronome.
- Virtual hand-contact checks for assessment, airway positioning, and CPR hand placement.
- Compression timing feedback and recorded attempts.
- AED grabbing, pad sockets, simulated analysis, and hands-clear checks before shock delivery.
- Textured male casualty model posed for the training scenario.
- Scripted recovery and ambulance handover, followed by completion narration, a **two-second blackout**, and a return to the Start Training screen.

## Technology

| Component | Project version |
| --- | --- |
| Unity Editor | **2020.3.49f1 LTS** |
| XR Interaction Toolkit | 2.3.2 |
| OpenXR Plugin | 1.7.0 |
| TextMesh Pro | 3.0.6 |
| Development language | C# |
| Desktop XR testing | XR Device Simulator |

Package dependencies are defined in [Packages/manifest.json](Packages/manifest.json). Use the recorded Unity version to avoid unintended project upgrades.

## Getting started

1. Install Unity Hub and **Unity 2020.3.49f1** with build support for your intended target.
2. Clone the repository:

   ```bash
   git clone https://github.com/pasanmayura/VR-CPR-Emergency-Response-Training-Simulator.git
   ```

3. In Unity Hub, add the cloned project folder and open it with the matching Editor version.
4. Allow Unity to import assets and restore packages.
5. Open [MainScene.unity](Assets/_Project/Scenes/MainScene.unity).
6. Enter **Play Mode**, focus the Game view, and select **Start Training** or press **Enter**.
7. Follow the guide and the instructions displayed during each stage.

For headset testing, configure the intended device's OpenXR runtime and XR Plug-in Management settings for the build target. Several scenario actions currently use keyboard input; a complete controller-only workflow should not be assumed.

## Training flow

| Stage | Simulated interaction |
| --- | --- |
| Orientation | Start the session and listen to the guide. |
| Responsiveness | Approach the casualty, ask for a response, and touch the shoulder. |
| Request assistance | Touch the phone to trigger a simulated emergency call. |
| Airway | Position the virtual hands at the forehead and chin and complete the hold timer. |
| Breathing | Start the observation timer and record the simulated absence of normal breathing. |
| Hand placement | Position both virtual hands in the required chest zones. |
| Compressions | Complete the configured compression practice with timing and placement feedback. |
| AED | Retrieve and release the AED to activate it, attach the pads, and follow analysis and shock prompts. |
| Recovery and handover | Follow the scripted recovery and ambulance handover sequence. |
| Completion | Wait for the final narration and blackout; the scene resets to Start Training. |

These describe the application's scripted flow, rather than a standalone clinical procedure. The recovery outcome is scripted and is not a prediction of a real patient's response.

## Desktop controls

Focus the **Game view** before using keyboard controls. Actions are available only during their relevant training stages.

| Input | Action |
| --- | --- |
| Enter | Start training; during the AED shock prompt, request the simulated shock. |
| T | Ask the casualty whether they are responsive. |
| 1 / 2 | Select the left / right virtual hand. |
| Shift + left-click | Place the selected virtual hand on a target surface. |
| R | Reset both virtual hands to their resting positions. |
| Escape | Clear the virtual hand selection. |
| B | Begin breathing observation. |
| N | Record the simulated absence of normal breathing. |
| Space | Attempt a compression. |

XR Device Simulator movement and grab controls are configured separately through its input actions. Follow the in-scene prompts for AED handling and pad placement.

## Feedback and assessment

The current scene configures compression practice for **30 compressions**, with at least **25 rhythm measurements** in the configured **100–120 per minute** range. The first compression establishes the timing baseline. A **110 BPM** metronome provides a rhythm cue.

The compression system records attempt information, input intervals, calculated rates, and position errors, and provides live feedback and an attempt result. These are software interaction measurements, not measurements of physical CPR quality.

Recorded compression attempts are held in memory during the session. Reloading the scene resets them; persistent result export and a consolidated end-of-session report are not currently provided.

## Building

1. Open **File → Build Settings**.
2. Confirm `Assets/_Project/Scenes/MainScene.unity` is enabled in **Scenes In Build**. The completion flow reloads this scene.
3. Select the intended target platform and check its Player and XR settings.
4. Build into a separate output folder, such as `Builds/Windows`.
5. Test the complete sequence in the built application, including audio completion, blackout, and restart.

## Project structure

```text
Assets/
├── _Project/
│   ├── Scenes/          # Main training scene
│   ├── Scripts/         # Training flow, CPR, AED, UI, and audio logic
│   │   └── Editor/      # Editor-only setup tools
│   ├── Prefabs/         # Reusable environment and training objects
│   ├── Audio/           # Narration and sound assets
│   └── Resources/       # Runtime-loaded resources, including the guide
├── Art/Medical/Male/    # Imported patient model, textures, and posed mesh
└── XR/                  # XR configuration assets
Packages/               # Package manifest and dependency lock file
ProjectSettings/        # Unity project settings
```

Key implementation files:

| Script | Responsibility |
| --- | --- |
| [GameFlowManager.cs](Assets/_Project/Scripts/GameFlowManager.cs) | Start screen, guide introduction, blackout, and scene restart. |
| [CompressionSystem.cs](Assets/_Project/Scripts/CompressionSystem.cs) | Compression attempts, timing, validation, and feedback. |
| [AEDController.cs](Assets/_Project/Scripts/AEDController.cs) | AED activation, pad placement, analysis, and shock checks. |
| [AEDHandoverController.cs](Assets/_Project/Scripts/AEDHandoverController.cs) | Recovery, ambulance handover, and completion narration. |
| [CPRInteraction.cs](Assets/_Project/Scripts/CPRInteraction.cs) | Shared contact checks and instruction display updates. |

## Current limitations

- The scenario combines XR interactions with desktop keyboard and mouse controls.
- Headset comfort, text readability, reach, and floor-level interaction require device testing.
- Some desktop helper interfaces use `OnGUI`; they should not be treated as headset-ready UI.
- The simulation does not validate physical compression depth, force, or real patient outcomes.
- The current completion flow resets the session rather than presenting a persistent results dashboard.

## Team

| Member | Student ID | Main contribution areas |
| --- | --- | --- |
| Pavithra | IM/2021/002 | Training environment, interactions, comfort features, and audio integration. |
| Pasan | IM/2021/020 | XR setup, locomotion, scene integration, guide character, narration, and testing. |
| Enuka | IM/2021/033 | CPR interactions, compression validation, AED logic, and handover. |
| Kavisha | IM/2021/049 | AED model preparation, integration support, and collision improvements. |
| Nuzha | IM/2021/116 | Model research and sourcing, UI, and presentation support. |

## Credits

### 3D assets

| Asset | Creator | Source | License information |
| --- | --- | --- | --- |
| AED Defibrilator | avredu | [Sketchfab model](https://skfb.ly/pCNJp) | [Creative Commons Attribution–NonCommercial 4.0 International (CC BY-NC 4.0)](https://creativecommons.org/licenses/by-nc/4.0/) |
| Office Phone | R3indeer | [Sketchfab model](https://skfb.ly/oHNtv) | [Creative Commons Attribution–NonCommercial 4.0 International (CC BY-NC 4.0)](https://creativecommons.org/licenses/by-nc/4.0/) |
| Mei Posed 001 | Renderpeople | [Free3D model](https://free3d.com/3d-model/mei-posed-001-963144.html) | Personal Use License, as listed on Free3D. |
| Male character free 3D model | Vawek | [Creator profile on CGTrader](https://www.cgtrader.com/designers/vawek) | License details were not supplied; refer to the original model listing and download terms. |

The male model was posed and baked into a static training mesh, with texture maps prepared for Unity. See the [model preparation notes](Assets/Art/Medical/Male/README.md).

### Audio and tools

- Audio sources acknowledged by the project: [Freesound](https://freesound.org/) and [Pixabay](https://pixabay.com/). Individual track credits and license details still need to be documented.
- Tools acknowledged in the project presentation: Cursor, ChatGPT, and LuvVoice.

Third-party assets retain their respective license terms. The attributions above do not grant a separate license to redistribute or commercially use those assets.

## Development notes

- Commit Unity `.meta` files alongside their assets to preserve references.
- Keep editor-only scripts in an `Editor` folder.
- Avoid committing generated `Library`, `Temp`, `Logs`, and build-output folders.
- See [Project_Guidelines.md](Project_Guidelines.md) for collaboration guidance and [project_plan.md](project_plan.md) for the original development plan.
