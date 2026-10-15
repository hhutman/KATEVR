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
- Fog: linear, start 3 m, end 45 m, colour (0.50, 0.48, 0.44).
- Kate Rig (`GlidePath.cs`): 3 s pause, then ~1 m/s glide with ease-in/out, gentle right turn up the walkway, stops at the door. ~50 s total.
- `SeatedOrigin.cs`: seated tracking, recentres at start.
- Hazmat visor: head-locked quad with a dark rim, faint green tint and scratches (`Assets/Materials/Visor.png`).
- Other walkers (`Wanderer.cs`): capsules hurrying at ~1.6–1.9 m/s.

## Decisions
- Hold aesthetic changes until the scene has been seen in the headset.
- Walkers will become figures in full hazmat suits (art pass, later).

## Headset pipeline (done, 2026-10-15)
- Package name `com.hopehutman.katevr`, company "Hope Hutman".
- First Build And Run to Quest 3 succeeded. APK saved to `Builds/KateVR.apk` (git-ignored).
- To rebuild: plug in headset → File → Build Profiles → Run Device: Refresh, pick Quest → Build And Run.
- Hardware notes: Developer Mode is per headset (justHope Meta account). "Allow USB debugging" may need a replug while wearing the headset. Controller issues were dead batteries + firmware update.

## Next steps
1. Headset review: comfort of glide/turn, visor, walker pace, scale/height, fog.
2. Then: VO + audio bed (breathing, suit squeaks, wind, footsteps), Timeline, hand + door, fade, festival loop/reset.
