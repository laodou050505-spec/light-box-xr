# LUMEN BOX EN — Text Table

All user-visible strings for LumenBox-EN. Update this file whenever text changes.
Last updated: 2026-10-22

## MissionReadout (Left panel, worldScale 0.00145, d=2.039m)

| Element    | Font            | Size (pt) | Text                                                   |
|------------|-----------------|-----------|--------------------------------------------------------|
| Level      | Cinzel SemiBold | 42 (auto) | Archive 01 / 12  ·  First Calibration                 |
| Rule       | Jost Regular    | 38 (auto) | Place two ground blocks to light both projections      |
| Status     | Jost Medium     | 42 (auto) | Front 0/2 · Side 0/2                                  |
| CubeCount  | Jost Regular    | 36        | Blocks  0 / 2                                         |

> Rule text shortened from "Place two ground blocks to illuminate both projections at once" (2026-10-22 revision).

## GuidePanel / Controls (Right panel, worldScale 0.00145, d=2.039m)

| Element | Font         | Size (pt) | Text                                                          |
|---------|--------------|-----------|---------------------------------------------------------------|
| Title   | Cinzel SemiBold | 38     | Controls                                                      |
| Body    | Jost Regular | 38 (auto) | 1  Pull trigger to grab a block                               |
|         |              |           | 2  Drag to an orange cell, release to snap                    |
|         |              |           | 3  Make both projections teal                                 |
|         |              |           |                                                               |
|         |              |           | Tap Next when done                                            |

> Body text shortened from 3 verbose lines to 3 concise lines (2026-10-22 revision).
> Old lines: "1  Pull trigger to grab a block from the left" / "2  Drag to an orange cell; release to snap" / "3  Turn both projections teal"

## ActionDock (Bottom bar, worldScale 0.00145, d=2.041m)

| Element | Font         | Size (pt) | Text  |
|---------|--------------|-----------|-------|
| Hint    | Jost Medium  | 35        | Hint  |
| Undo    | Jost Medium  | 35        | Undo  |
| Reset   | Jost Medium  | 35        | Reset |

## Tutorial Card (Center overlay, worldScale 0.0013, d=1.720m)

| Element      | Font            | Size (pt) | Text                                                                             |
|--------------|-----------------|-----------|----------------------------------------------------------------------------------|
| Title        | Cinzel SemiBold | 52        | LUMEN BOX — Tutorial                                                             |
| Body         | Jost Regular    | 32 (auto) | The tutorial requires only 2 ground blocks.                                      |
|              |                 |           | 1  Aim at the block source on the left; pull trigger to grab                     |
|              |                 |           | 2  Drag to an orange highlighted cell; release to snap                           |
|              |                 |           | 3  Turn both the front and side projections teal                                 |
| TutorialNote | Jost Regular    | 32        | Tap Next when done to continue.                                                  |
| Start Practice (button) | Jost Medium | 35 | Start Practice                                                          |

> TutorialNote renamed from "NextArchiveNote" to prevent Cinzel (all-caps) mis-assignment via IsTitle() (2026-10-22 revision).
> Text shortened from "Tap Next when done to continue through the archives" to "Tap Next when done to continue."

## Archive Complete Card (Center overlay, worldScale 0.0013, d=1.720m)

| Element             | Font            | Size (pt) | Text                                              |
|---------------------|-----------------|-----------|---------------------------------------------------|
| CompletionTitle     | Jost Regular    | 52        | Archive Complete                                  |
| CompletionDetail    | Jost Regular    | 32 (auto) | Archive 01 passed dual-projection verification    |
| CompletionProgress  | Jost Regular    | 30 (auto) | Ready for Archive 02                              |
| Next (button)       | Jost Medium     | 35        | Next                                              |

## Title Screen (StructureBuildTitle.unity)

Title scene has no TMP text — all lettering is baked into the cover image asset.
