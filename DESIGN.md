---
name: "光匣 / LUMEN BOX — Optical Archive"
description: "A unified optical archive language for the title and gameplay panels."
colors:
  ink: "rgb(0.2% 1% 2.2%)"
  start-focus: "rgb(100% 79% 43%)"
  exit-focus: "rgb(79% 88% 100%)"
  press-wash: "rgb(100% 88% 63% / 28%)"
components:
  artwork-target-rest:
    backgroundColor: "transparent"
  artwork-target-active:
    backgroundColor: "{colors.press-wash}"
---

# Design System: 光匣 / LUMEN BOX — Title

## Overview

**Creative North Star: "The Optical Archive"**

The optical archive now governs the isolated title and every gameplay interface surface. It pairs porcelain-silver instrument rims, midnight enamel fields, amber optical glass and calm ivory Chinese typography. The world-space puzzle, scene models, lighting and gameplay remain untouched.

Gameplay panels use three Nano Banana Pro background ratios: a 16:9 mission readout, a 3:2 guide/modal plaque, and a 21:9 action console. Generated artwork contains no lettering; all dynamic Chinese remains live in Unity with explicit safe margins and non-overlapping text boxes. The lower action console contains exactly 提示、撤回、重置.

The title is an authored image with live world-space interaction layered over it. Calm, generous composition lets the lettering and sculpture carry the scene; feedback makes the two lower-left actions visibly responsive.

**Key Characteristics:**

- Midnight blue field, porcelain-silver architecture, amber glass and light.
- Large image-authored English lettering and a single optical sculpture.
- Two transparent interaction targets aligned to the baked START and EXIT controls.

The source of truth is `Assets/UI/Generated/NanoBanana20260908/OpticalArchiveFinal/01_generated_image_url.png` (2752 × 1536), generated through Tripo `banana_pro`. Direction lineage: surface candidate 5, seed `lightbox-cover-20260908`. Runtime evidence: `VisualQA/OpticalArchive-20260908/DesktopFinal/01-title-silent-2s.png` and `01-hover-start.png`. Final visual review disposition: ship; physical headset comfort remains unverified.

## Colors

### Primary

Amber glass, transmitted light, and the baked golden START surface connect the optical sculpture to the main action. `start-focus` supplies its live underline and translucent hover tint.

### Neutral

Midnight blue surrounds porcelain-silver architecture and pale lettering. `ink` is the actual Unity camera/cover background; the authored image contains its own continuous palette rather than flat reusable color tokens. `exit-focus` is the cooler live feedback accent for EXIT. `press-wash` is shared by both actions.

**The Material Color Rule.** Preserve the image’s glass, metal, and illumination relationships; do not replace its gradients with invented flat palette tokens.

## Typography

The oversized two-line LUMEN / BOX display, smaller SPATIAL PROJECTION PUZZLE descriptor, and START / EXIT labels are baked into the source image. The display has fine, high-contrast strokes and a pale sculpted finish; the descriptor and action lettering remain subordinate. There are no live title text elements and no verified font family, point-size scale, or fallback stack to canonize.

**The Authored Lettering Rule.** Typography changes require a new reviewed artwork and corresponding target realignment, rather than duplicate Unity text.

## Layout

### Current reference opening and practice gate — 2026-09-10 13:01 reference

The user's newer reference explicitly requests a closer downward-looking opening, superseding the level-gaze candidate below. The designed eye is (-4.971, 5.840, -4.716), with horizontal distance 6.85 m, lateral offset -0.18 m, pitch 15° / yaw 45°, and desktop FOV 80°. Scene geometry, table and display orientations remain unchanged. The XR root stays upright: a separate eye-centred view pivot applies the look angle to the head/hand relative pose while preserving horizontal physical head translation. This is a ray-interaction view remap, not a rigid direct-touch tracking space; physical-headset comfort and MRC recording remain unverified.

The teaching card captures the centre of the current view once when shown, after the XR starting pose is ready, and then remains fixed in world space. Its existing artwork, type scale and padding are preserved. Practice is gated until the user presses 开始练习 and releases held inputs; the closing frame cannot place/grab a cube or activate gameplay shortcuts. Reopening teaching cancels a drag and restores the original cube. Controller disconnect/reconnect must not manufacture a fresh press or a release.

The three-action dock remains at the authored world point (-3.691, 3.709, -3.436) and follows only view rotation. Its opening-relative authoring offset is (0, -1.59, 2.30), leaving room for both the full tabletop and a bottom safe margin without changing the button layout. At 1372×1374 the bottom inset is 5.05% / 69.3 px and the conservative full-tabletop gap is 1.84% / 25.2 px. World-space margins are verified at the reference opening, not promised under arbitrary head movement. The 3.45 m feathered cover, screen-mounted top cards and immediate post-level-3 guide expiry are unchanged. Current evidence is in `VisualQA/TutorialView-20260910/`; previous layout sections below are historical.

`evidence-confirm.log` passes tutorial centring, interaction lock/release/reopen, controller reconnection, actual XR bootstrap/pivot and planar head motion, non-overlap, all three dock buttons, completion and level-3 guide lifecycle. The standalone desktop player passes actual Input System mouse title hover/START and gameplay startup. Desktop output: `Builds/StructureBuild-TutorialQA.app`. Both formal APK aliases are 63,669,631 bytes, SHA256 `2ba6c28835783a58b2590f4dcc40b029b3b5d2f1554616de1dde4eabb215abf7`; v2 signing, 16 KB alignment, ZIP integrity, ARM64/IL2CPP/PICO Multiview/Activity/Input System-only checks pass. The emulator was idle at delivery, then received a data-preserving replacement installation; title startup is visually confirmed. Android logs additionally confirm gameplay root (0,45,0), camera (15,45,0), the new eye point and trackingFallback=False. Further visual interaction stopped when the user changed the emulator back to its desktop, so no fresh gameplay emulator screenshot or physical controller gate walkthrough is claimed. Physical headset comfort remains untested.

### Spatial gameplay update — 2026-09-09

Desktop and PICO now share world-space UI. Mission and guide sit above the actual left/right projection-screen rims, aligned with each screen plane, with 0.13 m vertical clearance and 0.13 m toward the player. Their shared world scale is 0.0046; the guide remains narrower. The lower three-action dock is fixed at approximately (-4.301, 4.920, -4.301), and only its orientation follows the view. The tutorial is fixed in the room; completion samples a world pose once on display. Desktop opening view is 18° pitch / 45° yaw / 80° FOV at (-5.197, 6.520, -5.197). XR retains actual head tracking.

The upright cover is centred at (0, 1.58, 0), now 3.45 m ahead of the authored eye (updated 2026-09-10 from 4 m). Scale 0.0043 preserves the artwork proportions; only the feather fringe can leave the near-square reference view. All lettering and both button targets remain visible. A persisted UI shader feathers the outer 6.5% into the dark background. Existing Tripo Nano Banana artwork is reused.

Evidence: `VisualQA/SpatialUI-20260909/` and its `Desktop/` directory. Final editor checks passed for actual startup, natural spatial routing, screen alignment/clearance, fixed positions under head motion, and pointer-driven tutorial → hint → undo → reset → completion → next-level flow. PICO emulator confirmed title, tutorial and screen-mounted panels; physical headset comfort and the bottom dock under a lowered emulator gaze remain unverified.

### Neutral XR opening and guide expiry — 2026-09-10

The initial 18° tracking-root proposal was rejected by the user: only the player position should change, never the scene orientation or the physical tracking frame. The approved final layout uses yaw-only XR tracking and a level desktop preview (0° pitch / 45° yaw): distance 8.15 m / eye height 5.00 m, eye (-5.763, 5.00, -5.763). Physical forward/backward movement remains horizontal. The scene geometry, display orientations and teaching-card point remain unchanged. With user approval, only the dock's world Y was lowered from 4.920 to 3.590, retaining X/Z (-4.301, -4.301); it still stays fixed in space and only rotates with the view.

The right-hand quick guide is visible through levels 1–3 and its Canvas plus raycaster hide immediately when level 3 completes. In-memory campaign progress prevents reset or a revisit from restoring it; a new campaign restores onboarding. Mission information and the three-action dock remain available. No new save-game rule or new artwork is introduced.

Current evidence: `VisualQA/PositionOnly-20260910/evidence-confirm.log`, `03-spatial-hud.png`, `03a-runtime-xr-neutral-pose.png` and `10-level4-guide-hidden.png`. At the 1372×1374 reference viewport, the dock's bottom inset is 5.18% / 71.2 px, side insets 36.04%, and the full tabletop-to-dock gap is at least 2.65% / 36.4 px. QA verifies the exact 16 rendered WorkbenchTop modules and both actual 12-tile projection grids, not small snap markers or the distant WorkbenchBase scenery. All are visible without UI overlap. The real XR placement method preserves world-up and horizontal tracked motion; title/guide lifecycle and pointer interactions also passed.

The previous `GuideCover-20260910` package and emulator evidence used the rejected 18° tracking frame and are superseded, not proof of this upright version. Keep the original running desktop QA app intact; the current desktop build uses `StructureBuild-PositionQA.app`. Opening-view screen insets are not head-locked guarantees: as requested, the dock is a fixed world-space object. Physical-headset comfort still requires hardware testing.

Final delivery: desktop and Android builds succeeded, and the standalone desktop input-system pointer test reached gameplay. The upright APK is 63,660,899 bytes, SHA256 `30f2ed0689d093d9788b4ac2cbd67fcc69f03b1c407569bc3e3cec1b26955ddd`, with both formal APK aliases updated. Android package/signature/alignment checks passed. On the user's subsequent request, the package was installed over the prior version on emulator-5554 at 2026-09-10 12:14 with data preserved and the installed APK hash verified. It was not launched for a new visual test.

The title occupies the left, the aperture sculpture the right, with START above EXIT at lower left. This is the title’s composition, not a universal grid. The world-space cover is centered on the authored gaze line and oriented to the initial eye rotation. Artwork width is 1380 canvas units; height derives from the source aspect ratio. There is no responsive rearrangement: preserve the full image and its proportions.

Normalized target rectangles use the artwork’s top-left origin: START `(0.079, 0.678, 0.366, 0.067)` and EXIT `(0.080, 0.777, 0.121, 0.066)` as `(x, y, width, height)`. Collider dimensions add 14 canvas units to the visual target width and height, with depth 16. These are alignment measurements for this asset, not reusable spacing tokens.

## Elevation & Depth

The authored sculpture supplies depth through nested silver arches, translucent amber blocks, lit projection panels, and a reflective floor. The interaction layer adds tint and an underline; it adds no independent drop shadow. Cover luminance breathes at 0.10 cycles per second between the installer’s cool-white tint and white, remaining opaque with zero scale motion.

## Shapes

Rectilinear cubes and projection grids sit inside a monumental circular aperture. The baked START control is a wide, gently rounded gold bar; EXIT is a smaller outlined rectangle. Their exact corner geometry belongs to the raster artwork, so no invented radius scale is defined. Live target overlays remain rectangular.

## Components

### Artwork actions

Resting overlays are transparent, preserving the baked labels and surfaces. Mouse or tracked-controller hover applies a 12% accent tint and reveals a matching 3-unit underline positioned 5 units below the target. The underline expands from 16% to full width using exponential smoothing at rate 14 per second. Focus persists while any pointer remains on the target.

Pressing either action produces the shared warm flash for 0.18 seconds. START then loads the gameplay scene in Single mode; EXIT requests application quit. Repeated activation is guarded during transition. Both tracked-controller rays have aim assistance. Physical headset interaction still requires device verification.

**The Artwork Alignment Rule.** Keep targets parented to the artwork so visual controls and live feedback remain aligned.

## Do's and Don'ts

### Do:

- Do preserve the artwork aspect ratio, readable front face, and alignment between lettering and hit targets.
- Do keep amber emphasis on START and cooler emphasis on EXIT.
- Do preserve visible hover and press feedback before leaving the title scene.

### Don't:

- Don’t overlay duplicate text on the generated lettering or invent a font token for it.
- Don’t stretch, mirror, or crop the cover to fill a different viewport.
- Don’t apply this title-only composition or palette to existing gameplay without a separate design decision.
