# Gozer pixel-art and camera standard

This document is the canonical visual and technical specification for the project. If another document, Unity scene, importer, or script disagrees with this file, this file wins. Do not change an individual asset or camera in isolation; update this standard and the shared `PixelArtStandard` constants first.

## View and projection

The game uses an **orthographic three-quarter top-down view** (also called an **oblique top-down view**), similar in presentation to Stardew Valley.

- The game is fully 2D and gameplay movement happens on Unity's flat XY plane.
- The camera is orthographic and looks straight at that plane. There is no perspective convergence, vanishing point, or distance-based shrinking.
- Sprites create the illusion of depth by showing roofs, front walls, and the tops of props while their gameplay footprint remains on the ground plane.
- This is **not true isometric or dimetric art**: we do not use a diamond grid, 30-degree world axes, or isometric movement.
- “Down” means toward the viewer and shows a character's front. “Up” means away from the viewer and shows the character's back.

All new scenes, characters, buildings, props, effects, and UI must preserve this projection.

## Pixel grid and world scale

These values are fixed project-wide:

| Rule | Standard |
| --- | --- |
| Asset pixels per unit | **16 PPU** |
| Base terrain tile | **16 x 16 px** |
| Tile size in Unity | **1 x 1 world unit** |
| Base character frame | **16 x 32 px** |
| Base character size in Unity | **1 x 2 world units** |
| Default transform scale | **(1, 1, 1)** |
| Reference render resolution | **320 x 180 px (16:9)** |
| Walk animation speed | **8 frames per second** |

One source pixel is therefore 1/16 of a Unity unit. Never compensate for a wrong import PPU by scaling a Transform. Import it at 16 PPU and keep the Transform at scale 1.

The 16 x 16 tile is a construction grid, not a rule that every sprite must fit inside one tile. Buildings deliberately span several tiles and characters deliberately occupy two tiles vertically.

## Camera setup

Every gameplay camera uses:

- Projection: **Orthographic**.
- Assets PPU: **16**.
- Reference resolution: **320 x 180**.
- Orthographic Size: **5.625**, calculated as `180 / (2 x 16)`.
- Rigidbody2D interpolation on the player: **Interpolate**.
- Camera follow: in `LateUpdate`, following the interpolated player position.
- Camera pixel-grid snapping: **off** while following. Snapping both axes caused uneven diagonal motion and visible camera shake.
- Perspective camera, rotation, dynamic zoom, and non-uniform camera scale: not used for normal gameplay.

At 5.625 orthographic size, the camera shows 180 source pixels vertically. At a 16:9 window it shows 320 source pixels horizontally. Display the reference image at an integer multiple whenever possible: 640 x 360 (2x), 960 x 540 (3x), 1280 x 720 (4x), 1600 x 900 (5x), or 1920 x 1080 (6x). When the device aspect ratio differs, letterbox or pillarbox instead of stretching pixels.

Sub-pixel camera movement is intentional for smooth motion. Point filtering keeps the artwork crisp; the camera does not have to jump a whole source pixel per frame.

## Unity texture import settings

World pixel art must use:

- Texture Type: **Sprite (2D and UI)**.
- Sprite Mode: Single, or Multiple for a sheet/atlas.
- Pixels Per Unit: **16**.
- Filter Mode: **Point (no filter)**.
- Compression: **None / Uncompressed**.
- Generate Mip Maps: **Off**.
- Wrap Mode: **Clamp**.
- Alpha Is Transparency: **On** for transparent sprites.
- Mesh Type: **Full Rect** where available, so frames and pivots remain stable.

Do not resize pixel art using bilinear/bicubic filtering. Redraw or nearest-neighbor scale it, and keep every animation frame and upgrade level on an identical canvas.

## Pivots, anchors, sorting, and collision

- Terrain tiles: center pivot `(0.5, 0.5)`.
- Characters, buildings, gates, and freestanding props: bottom-center pivot `(0.5, 0)`.
- Treat the bottom-center pivot as the object's contact point with the ground.
- Y-sort world entities from the contact point using `Mathf.RoundToInt(-worldY * 100)`.
- Ground renders below all entities; roofs and upper walls are visual height, not gameplay depth.
- Character collision covers only the feet. The current 16 x 32 player uses an approximately `0.5 x 0.38` world-unit capsule at offset `(0, 0.19)`.
- Building collision covers only the walk-blocking footprint along the bottom of the building. Never wrap a collider around the roof or full transparent canvas.
- Interaction triggers are separate from solid footprint colliders and may be slightly larger for comfort.

## Character sheets and movement

The current standard character frame is 16 x 32 px. A four-direction, four-frame character sheet uses a 64 x 128 px active grid, excluding any explicitly documented transparent margin.

Current row order from top to bottom:

1. Down / front-facing
2. Right / right-facing
3. Up / back-facing
4. Left / left-facing

Each row contains four frames and plays at 8 FPS. Idle uses frame 0 of the last facing direction. Diagonal movement is normalized so it is not faster than cardinal movement. On diagonals, the horizontal input owns the displayed animation: W+D moves northeast while showing the right-facing walk; W+A moves northwest while showing the left-facing walk.

Keep the feet on the same baseline in every frame. Motion belongs in limbs and body bounce, not in a drifting pivot.

## Required town asset canvases

All dimensions below are exact source-pixel canvas sizes at 16 PPU. Transparent padding is allowed only inside the stated canvas.

| Asset | Pixel canvas | Tile/world footprint of canvas | Ratio |
| --- | ---: | ---: | ---: |
| Ground tile | 16 x 16 | 1 x 1 | 1:1 |
| Player home | 96 x 80 | 6 x 5 | 6:5 |
| Storage | 80 x 64 | 5 x 4 | 5:4 |
| Workbench | 48 x 32 | 3 x 2 | 3:2 |
| Watchtower L1-L3 | 80 x 96 each | 5 x 6 | 5:6 |
| Infirmary L1-L3 | 96 x 80 each | 6 x 5 | 6:5 |
| Greenhouse L1-L3 | 96 x 80 each | 6 x 5 | 6:5 |
| Expedition gate | 96 x 48 | 6 x 3 | 2:1 |

Every level of an upgradeable building must use the same canvas, pivot, contact point, and door position so sprite swaps do not jump. The expedition gate currently uses one closed frame only; no opening animation is part of the present design.

## Proportions and drawing rules

- A normal adult character is 16 x 32 px: approximately 1 tile wide and 2 tiles tall.
- Exterior doors should be about 16 px wide and 24-32 px tall, aligned to the building's bottom/front facade.
- Common small props normally occupy 16 x 16 px; tall props may use 16 x 32 px; large props may use clean tile multiples.
- Use deliberate pixel clusters, hard edges, and no anti-aliasing or semi-transparent outline pixels.
- Use one consistent light direction: upper-left. Highlights face upper-left and cast/contact shadows fall down-right.
- Roofs and front facades may overlap characters visually, but their colliders remain at ground level.
- Important silhouettes, doors, interactable objects, and hazards must remain readable at native 320 x 180 resolution.
- Keep the town palette warm and welcoming; expedition spaces may be colder, darker, and higher contrast without changing scale or projection.

## UI standard

- Design gameplay UI against the 320 x 180 reference frame.
- Use a pixel font and whole-pixel placement at the reference resolution.
- Use integer scaling for panels, icons, and text. Do not smoothly scale individual UI sprites.
- Use 16 x 16 px as the normal icon unit; larger UI art should be clean multiples of it.
- Keep critical text and prompts inside a safe inset of at least 8 reference pixels.

## File and source rules

- Use transparent PNG for sprites and lowercase snake_case names, for example `town_home.png` and `watchtower_l2.png`.
- Keep editable source files separate from exported game-ready PNGs.
- Do not overwrite an approved sprite with a differently sized canvas under the same filename.
- Document any intentional sheet margin in the importer code.
- `Assets/Scripts/PixelArtStandard.cs` is the code equivalent of the fixed numerical values in this document.

## Legacy prototype assets

The active town art and both gameplay cameras follow this standard. Older duplicate character experiments, the old 32 px ground sheet, the removed fence sheet, and the Rogue test room's temporary 128 px combat artwork remain in the repository only as prototype/source material. They are not scale references and must not be reused for new content. Re-author or export them to this standard before promoting them to approved game art; do not imitate their existing `.meta` PPU values.

## Acceptance checklist

Before accepting new art or a new gameplay scene, verify:

1. It uses the orthographic three-quarter top-down projection and flat XY movement.
2. World art imports at 16 PPU with Point filtering, no compression, and no mipmaps.
3. Source dimensions match the approved canvas or a documented 16-pixel tile multiple.
4. Transform scale is `(1, 1, 1)`.
5. The bottom-center ground contact point does not move between frames or levels.
6. Colliders describe feet or footprints, not the full visible sprite.
7. The camera resolves to 5.625 orthographic size from the shared constants.
8. Motion is smooth with player interpolation on and camera snapping off.
9. The result is readable at native 320 x 180 and crisp at integer display scales.
