# KateVR — project notes

A seated, look-around VR scene for Meta Quest 3 (shows and festivals).
POV: Kate, in a full hazmat suit, walks through a grey, smoggy street to the door of a house while her voice-over plays. Ends on a fade as she opens the door.

## Setup (done)
- Unity 6.6 (6000.6.5f1), URP, project at `~/LocalUnityProjects/KATEVR` (outside iCloud on purpose).
- GitHub: `hhutman/KATEVR`, managed with GitHub Desktop. After each session: Commit to main → Push origin.
- Build target: Android, Build Profile "Android". XR: OpenXR + Meta Quest Support, Oculus Touch Controller Profile.
- Project Validation: all green except an SSAO warning that only applies to the PC renderer (safe to ignore).

## The grey box (done)
- Menu **KateVR → Build Greybox Street** rebuilds `Assets/Scenes/Street_Greybox.unity` from scratch (`Assets/Editor/GreyboxBuilder.cs`).
- Fog: linear, start 1.5 m, end 30 m, colour (0.47, 0.445, 0.40) — smoggier per Hope. Plus drifting ash specks around Kate (WorldKit.Ash).
- Kate Rig (`GlidePath.cs`): 3 s pause, ~1 m/s walk; the right turn is a wide arc where she slows to ~55%, body turns lazily, slight pace drift and tiny sway (set `sway` to 0 if needed). Stops ~0.65 m from the door. Walk ≈ 55 s, ending ≈ 8 s.
- `SeatedOrigin.cs`: seated tracking, recentres at start.
- Hazmat visor: head-locked quad with a dark rim, faint green tint and scratches (`Assets/Materials/Visor.png`).
- Other walkers (`Wanderer.cs`): capsules hurrying at ~1.6–1.9 m/s.

## Art direction (from Hope's storyboards, 2026-10-15)
- Berkeley as it 'used' to be — now decayed, degraded, no colour. Sparse: a few buildings left, no cars, no trees.
- Stylized (simple shapes), not photoreal.
- **Revised after headset test:** the detailed Berkeley world felt too specific. Back to the original abstract block world — 'everywhere and nowhere', even sparser. No poles/cracks/puddles. Kate's bungalow dropped: plain block house with a door. (WorldKit.cs keeps the detailed pieces if ever wanted.)
- Atmosphere: ground-hugging fog, wet cracked pavement, puddles, weeds, utility poles (Silent Hill 2-style fog as reference). Light: keep current overcast grey for now.
- People: just one for now — the figure in a long coat, hood and gas mask, hurrying toward Kate.
- Memories: point-cloud images that form, hold, then break apart like skywriting. ~No colour (faint traces at most). Appear at 10 s, 25 s, 40 s; cloud-like, varied sizes; ahead, sides, edge of view, overhead. Not tied to VO lines (yet). Placeholders: candlelight march, Power to the People fist, Free Speech Movement at Sproul Hall — need licensing before festivals.
- Ending (`EndingSequence.cs`): door is plain wall until touched. Only Kate's glove is seen — it rises from below, briefly but confidently presses flat, the outline draws in, the glove drops away, and the door swings inward (hinged on the right, opening on the left) into darkness; fade to black. No arm/sleeve.
- Headset notes 2026-10-15: fist memory more readable (maybe only because Hope knows it).
- Memories feedback (editor, 2026-10-15): not readable — look like clusters of metal sequins; Hope likes them atmospherically and wants to keep them. Judge readability in the headset before changing. Fixed: points that drifted near the head now fade out (they looked 'inside the headset').
- Visor: currently unclear what you're looking through — Hope is thinking about direction.

## Voice-over
- Scratch VO: ElevenLabs free plan (no commercial use; must credit ElevenLabs) → replace with a real performer or paid licence before ticketed showings. File: `Assets/Audio/Kate_VO_Scratch.wav` (22.8 s, 16 kHz mono).
- Head-locked inside the suit (2D AudioSource on the camera, low-pass 5 kHz). Starts at 8 s; last line 'who fucking knows?' ends ~30.5 s as Kate starts drifting right.
- Walk timing (simulated): turn starts ~26 s, drifting right ~34 s, approach ~46 s, at door ~58 s → ~27 s without words after the VO.

## Decisions
- Hold aesthetic changes until the scene has been seen in the headset.
- Walkers will become figures in full hazmat suits (art pass, later).

## Headset pipeline (done, 2026-10-15)
- Package name `com.hopehutman.katevr`, company "Hope Hutman".
- First Build And Run to Quest 3 succeeded. APK saved to `Builds/KateVR.apk` (git-ignored).
- To rebuild: plug in headset → File → Build Profiles → Run Device: Refresh, pick Quest → Build And Run.
- Hardware notes: Developer Mode is per headset (justHope Meta account). "Allow USB debugging" may need a replug while wearing the headset. Controller issues were dead batteries + firmware update.

## Build order (agreed)
1. World pass — done (reverted to sparse block world + one figure).
2. Memories — first version done: `Shaders/MemoryPoints.shader`, `MemoryCloud.cs`, `Editor/MemoryBuilder.cs`. Placeholder images cropped from Hope's storyboard into `Assets/Memories/` (swap a PNG with the same name and rebuild).
3. Hand + door — first version done (palm on door, door opens, fade).
4. Visor.

## Later
- VO + audio bed (breathing, suit squeaks, wind, footsteps), Timeline, hand + door, fade, festival loop/reset.
