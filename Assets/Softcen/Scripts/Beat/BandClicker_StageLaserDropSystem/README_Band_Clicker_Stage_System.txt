Band Clicker Stage / Laser / Drop System

Files:
1. SongColorPalette.cs
   - ScriptableObject with per-song colors.

2. BeatStageColorSync.cs
   - Scene singleton that exposes current song colors to beams/lasers/drop effects.

3. BeatLaserSweep.cs
   - Beat-reactive LineRenderer laser with sweeping motion, jitter, alpha/width pulse, and song colors.

4. BeatDropDetector.cs
   - Runtime detector that watches precomputed JSON beat events from BeatPlay.
   - It does NOT analyze audio.
   - It detects dense beat moments and emits DropDetected.

5. StageDropExplosion.cs
   - Plays particle bursts, flash renderers, real light flashes, and optional camera shake on drop.

Recommended setup:
- Add BeatStageColorSync to your stage root.
- Create SongColorPalette assets:
  Assets/Create/Band Clicker/Stage/Song Color Palette
- Assign palette to BeatStageColorSync.
- Add BeatLaserSweep to laser GameObjects.
- Assign BeatPlay and target Transform.
- Add BeatDropDetector to a stage controller object and assign BeatPlay.
- Add StageDropExplosion and assign BeatDropDetector.

Recommended BeatLaserSweep settings:
HiHat/Cymbal laser:
- baseAlpha 0
- maxAlpha 0.9
- baseWidth 0.005
- maxWidth 0.05
- releaseSpeed 22
- sweepAngle 55
- sweepSpeed 1.7
- beatJitterAngle 5

Mid/Snare laser:
- reactTo Snare, Tom, Mid
- maxAlpha 0.65
- maxWidth 0.045
- releaseSpeed 12
- sweepAngle 35
- sweepSpeed 0.9

Energy laser:
- reactTo Energy, Kick
- maxAlpha 1
- maxWidth 0.07
- releaseSpeed 8
- sweepAngle 25
- sweepSpeed 0.6

Recommended BeatDropDetector settings:
- windowSeconds 1.25
- minimumBeatsInWindow 5
- minimumIntensitySum 4.0
- cooldownSeconds 6.0

For mobile:
- Use Unlit Transparent/Additive materials.
- Disable shadows.
- Use MaterialPropertyBlock.
- Keep laser count around 4-10.
- Use particles sparingly for drop bursts.
