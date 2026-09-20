# Gozer pixel-art generation prompt book

This file contains reusable prompts for regenerating the project's active town art. The player-home prompt is intentionally omitted because the replacement home has already been created.

The canonical rules remain in [`PIXEL_ART_STANDARD.md`](PIXEL_ART_STANDARD.md). If this prompt book ever conflicts with that document, the standard wins.

## How to use this file

1. Generate **one asset per run**. Do not ask an image model to create several unrelated sprites in one image.
2. For every world asset, paste the **Shared world-sprite specification** first, followed by one asset prompt.
3. Attach the approved player home as the style, palette, cluster, and projection reference.
4. Attach `Assets/Art/Characters/TownCharacterSheet.png` as the character-scale reference when useful.
5. For upgrade levels, attach the approved previous level as a locked structural reference.
6. Treat generated images as drafts until their canvas, alpha, palette, contact point, and native-size readability have been verified.
7. If a generator cannot return the exact small canvas, use its result only as a visual reference and redraw it at the required native resolution. Never smoothly downscale an illustration into the final sprite.

## Shared world-sprite specification

Paste this block before every building, prop, or dressing prompt in this document.

```text
Use case: stylized-concept
Asset type: production-ready 2D Unity world sprite for Gozer

AUTHORITATIVE REFERENCES

- Image 1, when supplied, is the approved player home. Treat it as the authoritative reference for projection, pixel-cluster language, outline treatment, lighting, palette mood, and architectural scale. Do not copy its building design.
- Image 2, when supplied, is the approved 16×32 town character. Treat it as the authoritative scale reference.
- Any supplied previous upgrade level is a locked structural reference. Preserve its camera, permanent geometry, entrance, footprint, and contact point.

MANDATORY CAMERA AND PROJECTION

Create an elevated orthographic three-quarter top-down gameplay sprite. View the subject from above and slightly from its front/south side. Entrances face downward toward the viewer.

For buildings, the roof or upper horizontal surfaces must be visually prominent. The camera must be high enough to reveal roof planes and the top surfaces of steps, chimney caps, window ledges, barrels, crates, platforms, railings, or projecting beams where appropriate. Vertically compress front facades because they are viewed from above.

Keep vertical structural lines vertical and parallel. There are no vanishing points, perspective convergence, or distance-based shrinking.

The gameplay footprint remains aligned to a rectangular 16×16 tile grid. Do not use an isometric diamond footprint, diagonal world grid, 30-degree world axes, or isometric movement presentation.

This is a world-map sprite for an elevated farming-RPG-style scene. It is not a frontal architectural elevation, side-scrolling object, storybook illustration, dollhouse, icon, concept-art painting, or eye-level view.

Reject the result if the facade is the dominant surface, the camera appears near human eye level, the roof is only a narrow decorative cap, or the sprite resembles a front elevation.

PROJECT SCALE

- 16 source pixels per Unity world unit.
- Base terrain tile: 16×16 pixels.
- Adult character: 16×32 pixels, one tile wide and two tiles tall.
- Reference gameplay frame: 320×180 pixels.
- Judge all important shapes at native 1×, not only while enlarged.
- Doors are approximately 16 pixels wide and 24–32 pixels tall unless a wider functional entrance is explicitly requested.

PIXEL CONSTRUCTION

- Draw at the final native pixel resolution.
- Use deliberate, grid-aligned pixel clusters and hard edges.
- Alpha values may only be 0 or 255.
- No antialiasing or semitransparent outline pixels.
- No blur, feathering, soft glow, transparency gradient, or smooth shadow.
- No bilinear or bicubic resizing.
- Use stepped color ramps rather than continuous gradients.
- Use coherent 1–3 pixel clusters for small details and larger connected clusters for major forms.
- Avoid isolated single-pixel noise and evenly distributed texture noise.
- Use a restrained shared town palette and consistent selective dark outlines.
- Lighting comes from the upper-left. Highlights face upper-left; cast and contact shadows fall down-right.
- The town mood is warm, safe, handmade, rustic, and welcoming.

OUTPUT

- Produce exactly one isolated asset unless the prompt explicitly requests a sprite sheet.
- Use a genuinely transparent background.
- Do not include a checkerboard, ground plane, scenery, character, UI, label, border, presentation card, text, or watermark.
- Preserve transparent padding inside the specified canvas, but do not leave unused padding below the ground-contact point.
```

## Recommended style-and-palette reference

Generate this once if the approved home alone is not enough to keep later assets stylistically consistent.

```text
Create a compact native-resolution pixel-art style reference for Gozer's warm town. This reference will govern buildings, characters, terrain, props, and environmental details.

Use an elevated orthographic three-quarter top-down projection on a rectangular 16×16 world grid. Do not use isometric projection or an eye-level camera.

Build one restrained master palette of approximately 28–36 colors with reusable ramps for:

- Warm outline and deepest shadow.
- Timber browns.
- Cream plaster and stone.
- Terracotta roof reds.
- Warm grass greens.
- Dark foliage greens.
- Packed-earth tans.
- Cool glass blue-greens.
- Amber lamps and windows.
- Muted expedition danger colors.
- Character skin and clothing accents.

Show small native-resolution examples of:

- One 16×16 grass tile.
- One 16×16 packed-earth tile.
- One 16×16 stone tile.
- One 16×32 tree.
- One 16×32 character silhouette.
- A timber wall cluster.
- A roof-tile cluster viewed from above.
- A cream-stone cluster.
- A hard-edged glass highlight cluster.
- An amber lamp without transparent glow.
- Correct upper-left highlights and down-right shadows.
- Correct selective outlines and cluster sizes.

Use hard opaque pixels only. No antialiasing, smooth gradients, random noise, painterly rendering, photorealism, 3D rendering, or semitransparent swatches.
```

---

# Active town assets

## Storage building

Append this prompt to the shared world-sprite specification.

```text
FILENAME
storage_building.png

CANVAS AND ANCHOR

- Exact final canvas: 80×64 pixels.
- Canvas footprint: 5×4 world tiles.
- Bottom-center pivot: canvas coordinate x=40, y=63.
- Center the ground contact and entrance around x=40.
- The foundation and supports must visually reach the bottom contact row without being clipped.

PRIMARY REQUEST

Create a sturdy reinforced town storage shed or compact warehouse. It must immediately communicate storage and practical utility.

SUBJECT

- A broad centered double wooden entrance, approximately 24–28 pixels wide and 24–30 pixels tall.
- Heavy, readable timber braces.
- Cream-stone foundation supports.
- A warm brown or muted terracotta roof whose top planes are clearly visible from the elevated camera.
- One small amber lamp near the entrance, rendered with opaque clusters rather than glow.
- One restrained crate or barrel motif.
- A strong broad silhouette that is visually heavier than the player home but belongs to the same town.

COMPOSITION

Make the broad entrance the primary focal point. Keep the facade vertically compressed and make the visible roof plane a major part of the sprite. Show the top surfaces of the entrance beam, foundation, crate, or barrel where appropriate. Use only modest side-wall exposure.

PALETTE

Use approximately 22–34 colors drawn from the approved town palette.

AVOID

No eye-level warehouse facade, narrow decorative roof, fuzzy wood, hundreds of tiny planks, miniature hardware noise, semitransparent lamp glow, detached shadow, or dollhouse proportions.
```

## Workbench

Append this prompt to the shared world-sprite specification.

```text
FILENAME
workbench.png

CANVAS AND ANCHOR

- Exact final canvas: 48×32 pixels.
- Canvas footprint: 3×2 world tiles.
- Bottom-center pivot: canvas coordinate x=24, y=31.
- All legs must share a stable bottom contact line.

PRIMARY REQUEST

Create a freestanding outdoor crafting workbench that reads as interactable at native 1×.

SUBJECT

- A clearly visible top work surface viewed from above.
- Four stable legs or two heavy side supports.
- One compact vice attached to an upper corner.
- Two or three large readable tools, such as a hammer, saw, or wrench.
- One tiny lantern or candle using hard amber pixels with no transparent glow.
- A lower shelf or reinforcing beam.
- Warm timber with muted metal accents.

COMPOSITION

The elevated camera must clearly reveal the top surface. Center the bench around its bottom contact point. Separate the tools and lantern into readable clusters rather than merging them into a noisy horizontal strip.

PALETTE

Use approximately 12–22 colors from the approved town palette.

AVOID

No frontal table icon, invisible top surface, soft lantern bloom, thin subpixel legs, excessive tool clutter, fuzzy grain, or detached shadow.
```

## Expedition gate — closed

Append this prompt to the shared world-sprite specification.

```text
FILENAME
expedition_gate_closed.png

CANVAS AND ANCHOR

- Exact final canvas: 96×48 pixels.
- Canvas footprint: 6×3 world tiles.
- Bottom-center pivot: canvas coordinate x=48, y=47.
- Keep the central gate and fence baseline stable along the bottom contact area.

PRIMARY REQUEST

Create one closed fortified expedition gate marking the boundary between the warm safe town and the dangerous outside. The current design requires one closed frame only.

SUBJECT

- A centered closed double timber gate.
- Short wooden fence wings on both sides.
- Two sturdy stone-and-timber gateposts.
- Reinforcing beams and readable hinges.
- Restrained moss or grass at the base.
- Warm town-facing timber and cream stone.
- Slightly colder, darker values in the narrow gaps and outward-facing portions.
- A continuous, unmistakably solid barrier silhouette.

COMPOSITION

View the top surfaces of the gateposts, fence rails, and reinforcing beam from the elevated camera. Keep the closed doors centered. Use nearly symmetrical structural mass with only small asymmetries in wear or vegetation.

PALETTE

Use approximately 20–32 colors from the approved town palette.

AVOID

Do not create an open frame. No eye-level palisade, black void behind the gate, soft atmospheric darkness, fuzzy moss, excessive fence-stake detail, or perspective convergence.
```

---

# Watchtower upgrade family

Every watchtower level uses an exact 80×96 canvas and bottom-center pivot at x=40, y=95. The four main supports, ladder x coordinate, platform position, perspective, and ground-contact line are permanent. Generate levels in order and attach the approved previous level to the next request.

## Watchtower L1

Append this prompt to the shared world-sprite specification.

```text
FILENAME
watchtower_l1.png

CANVAS

- Exact final canvas: 80×96 pixels.
- Canvas footprint: 5×6 world tiles.
- Bottom-center pivot: x=40, y=95.

PRIMARY REQUEST

Create the permanent Level 1 base for a timber watchtower upgrade family.

SUBJECT

- Four strong principal timber supports arranged on a rectangular ground footprint.
- A centered ladder whose x coordinate will remain fixed in every future level.
- Large readable cross-bracing.
- A small open upper lookout platform.
- Simple railing.
- A few rope bindings.
- Modest cream-stone or driven-post footings.

PROJECTION AND READABILITY

The elevated camera must clearly show the top floor of the lookout platform and the top surfaces of railing and beams. Do not turn the platform into an isometric diamond. Prefer a few thick readable supports over many thin sticks.

PALETTE

Use approximately 20–30 colors from the approved town palette.

AVOID

No roof, bell, beacon, or banner at Level 1. No tangled scaffolding, fragile one-pixel supports, floating feet, or overly narrow tower.
```

## Watchtower L2

Append this prompt to the shared world-sprite specification and attach the approved L1 as a locked structural reference.

```text
FILENAME
watchtower_l2.png

CANVAS

- Exact final canvas: 80×96 pixels.
- Bottom-center pivot: x=40, y=95.

PRIMARY REQUEST

Upgrade the supplied Level 1 watchtower into Level 2 without redesigning or moving the permanent structure.

LOCKED INVARIANTS

- Preserve all four main support positions.
- Preserve the ladder x coordinate and bottom contact.
- Preserve the lookout platform position and camera angle.
- Preserve the ground-contact line and rectangular footprint.
- Do not move, rotate, widen, narrow, raise, or lower the original tower.

LEVEL 2 ADDITIONS

- Stronger cross-bracing layered onto the existing supports.
- Reinforced lower footings.
- A small practical roof over the same platform.
- One readable bell beneath or beside the roof.
- Slightly improved timber and metal fittings.

The roof's upper planes must be clearly visible from the same elevated top-down camera used for L1.

AVOID

No beacon or banner yet. Do not replace the platform, move the ladder, change the tower's viewing angle, or generate a different tower design.
```

## Watchtower L3

Append this prompt to the shared world-sprite specification and attach the approved L2 as a locked structural reference.

```text
FILENAME
watchtower_l3.png

CANVAS

- Exact final canvas: 80×96 pixels.
- Bottom-center pivot: x=40, y=95.

PRIMARY REQUEST

Upgrade the supplied Level 2 watchtower into the final Level 3 landmark while preserving every permanent structural anchor.

LOCKED INVARIANTS

- Preserve supports, ladder, platform, roof, bell, footprint, contact line, scale, and camera angle.
- Add to the existing tower; do not replace it.

LEVEL 3 ADDITIONS

- A tall but compact beacon mounted above the existing roof.
- Stronger visible supports and a few restrained metal reinforcements.
- One simple orange town banner attached near the upper platform.
- A slightly more finished and defensible appearance.

Render beacon light with a small hard opaque color ramp. The banner may extend sideways but must not visually shift the tower's center or contact point.

AVOID

No semitransparent flame, bloom, wind effects, enormous flag, new ladder, changed roof direction, changed foundation, or unrelated tower silhouette.
```

---

# Infirmary upgrade family

Every infirmary level uses an exact 96×80 canvas and bottom-center pivot at x=48, y=79. The centered 16-pixel-wide entrance, central facade, roof direction, view angle, scale, and ground-contact line are permanent.

## Infirmary L1

Append this prompt to the shared world-sprite specification.

```text
FILENAME
infirmary_l1.png

CANVAS

- Exact final canvas: 96×80 pixels.
- Canvas footprint: 6×5 world tiles.
- Bottom-center pivot: x=48, y=79.

PRIMARY REQUEST

Create the permanent Level 1 base for a modest timber-and-cream-stone town clinic.

SUBJECT

- One centered wooden entrance, exactly 16 pixels wide and approximately 24–28 pixels tall.
- Two warm readable front windows.
- A roof with clearly visible upper planes and one permanent ridge direction.
- Restrained timber framing and cream-stone lower walls.
- One small herb box.
- A simple original healing emblem that does not resemble a protected red-cross mark.
- A reassuring, clean, welcoming appearance.

COMPOSITION

Keep the door centered at x=48. Make the elevated roof a major visual mass and compress the facade vertically. Reserve one side of the canvas as a logical future treatment-wing expansion area.

PALETTE

Use approximately 24–34 colors from the approved town palette.

AVOID

No red cross, modern hospital symbol, large extension, mansion proportions, dense vines, or miniature shelves at L1.
```

## Infirmary L2

Append this prompt to the shared world-sprite specification and attach the approved L1 as a locked structural reference.

```text
FILENAME
infirmary_l2.png

CANVAS

- Exact final canvas: 96×80 pixels.
- Bottom-center pivot: x=48, y=79.

PRIMARY REQUEST

Upgrade the supplied Level 1 infirmary into Level 2 without replacing its central building.

LOCKED INVARIANTS

- Preserve the centered door's x coordinate, width, height, and baseline.
- Preserve the central facade, roof direction, main ridge, camera angle, contact line, and scale.
- Preserve the original healing emblem and visual identity.

LEVEL 2 ADDITIONS

- One compact side treatment-room extension placed in the reserved expansion area.
- A larger organized herb rack.
- One additional warm window.
- Slightly improved roofing and structural reinforcement.
- A few purposeful medicinal-plant clusters.

The addition must look constructed onto the L1 clinic. It must not rotate or visually recenter the original building.

AVOID

No moved door, completely new roof, second entrance, giant sign, dense foliage noise, perspective change, or unrelated cottage design.
```

## Infirmary L3

Append this prompt to the shared world-sprite specification and attach the approved L2 as a locked structural reference.

```text
FILENAME
infirmary_l3.png

CANVAS

- Exact final canvas: 96×80 pixels.
- Bottom-center pivot: x=48, y=79.

PRIMARY REQUEST

Upgrade the supplied Level 2 infirmary into a polished Level 3 clinic while retaining all permanent architecture.

LOCKED INVARIANTS

- Preserve the original entrance, facade, treatment wing, roof direction, footprint, contact line, scale, and camera angle.
- Preserve the building as the same clinic; add improvements rather than rebuilding it.

LEVEL 3 ADDITIONS

- Finish and reinforce the treatment wing.
- Add one improved chimney or compact ventilation feature whose top is visible from above.
- Add organized medicinal plants in a few large readable clusters.
- Improve window framing and warm interior readability.
- Enlarge or clarify the original non-trademark healing emblem without moving it arbitrarily.
- Add a restrained sense of prosperity and competence.

AVOID

No mansion transformation, changed entrance, extra storeys, ornate fantasy hospital, soft window bloom, foliage blanket, new camera angle, or changed footprint.
```

---

# Greenhouse upgrade family

Every greenhouse level uses an exact 96×80 canvas and bottom-center pivot at x=48, y=79. The centered 16-pixel-wide entrance, central frame, roof angle, view direction, and ground-contact line are permanent. Glass is represented with opaque colors, never actual partial transparency.

## Greenhouse L1

Append this prompt to the shared world-sprite specification.

```text
FILENAME
greenhouse_l1.png

CANVAS

- Exact final canvas: 96×80 pixels.
- Canvas footprint: 6×5 world tiles.
- Bottom-center pivot: x=48, y=79.

PRIMARY REQUEST

Create the permanent Level 1 base for a compact timber greenhouse.

SUBJECT

- One centered entrance exactly 16 pixels wide and approximately 24–28 pixels tall.
- A simple single-bay timber structural frame.
- Pale blue-green glass represented with fully opaque colors.
- Clearly visible upper roof-glass planes from the elevated camera.
- A few restrained cloth repairs.
- Several readable crop clusters behind lower panels.
- A planned expansion area that will allow a second bay without moving the entrance.

COMPOSITION

Keep the door centered at x=48. Use purposeful 1–3 pixel stepped glass highlights. Make crop masses readable but subordinate to the frame and entrance.

PALETTE

Use approximately 22–32 colors from the approved town palette.

AVOID

No transparent glass pixels, smooth reflections, glowing interior, excessive plants, domed conservatory, or side-positioned entrance.
```

## Greenhouse L2

Append this prompt to the shared world-sprite specification and attach the approved L1 as a locked structural reference.

```text
FILENAME
greenhouse_l2.png

CANVAS

- Exact final canvas: 96×80 pixels.
- Bottom-center pivot: x=48, y=79.

PRIMARY REQUEST

Upgrade the supplied Level 1 greenhouse into Level 2 without changing its permanent camera, entrance, or central frame.

LOCKED INVARIANTS

- Preserve the entrance at x=48 with the same dimensions and baseline.
- Preserve the original bay, central frame, roof angle, view direction, footprint, and contact line.
- Do not shift the building sideways or turn it into a long perspective structure.

LEVEL 2 ADDITIONS

- Add a second growing bay within the reserved expansion area.
- Add one readable water barrel with a visible top surface.
- Reinforce the lower timber framing.
- Add several additional crop clusters.
- Repair some of the L1 cloth patches with better glass or framing.

The second bay must look attached to the L1 structure while the original centered entrance remains the visual anchor.

AVOID

No relocated door, side-facing entrance, dramatic perspective recession, completely new roof geometry, smooth glass, or plant noise.
```

## Greenhouse L3

Append this prompt to the shared world-sprite specification and attach the approved L2 as a locked structural reference.

```text
FILENAME
greenhouse_l3.png

CANVAS

- Exact final canvas: 96×80 pixels.
- Bottom-center pivot: x=48, y=79.

PRIMARY REQUEST

Upgrade the supplied Level 2 greenhouse into the final Level 3 structure while preserving both previous stages.

LOCKED INVARIANTS

- Preserve both bays, centered entrance, central frame, roof direction, footprint, contact line, scale, and camera angle.
- Add improvements to the same building rather than replacing it with a conservatory.

LEVEL 3 ADDITIONS

- Reinforced timber and a few restrained metal joints.
- Improved opaque glass panels with clean stepped highlights.
- Denser but organized healthy crop clusters.
- Simple readable irrigation pipes or channels.
- One small cultivation lamp rendered with hard opaque amber pixels.
- A prosperous but practical final appearance.

AVOID

No domes, cathedral conservatory, moved entrance, altered bay layout, soft light bloom, transparent glass, dense leaf noise, or changed perspective.
```

---

# Town dressing

## Natural path sheet

Use the shared world-sprite specification, but the output is a sprite sheet rather than one isolated sprite.

```text
FILENAME
town_paths.png

CANVAS AND GRID

- Exact final sheet: 128×64 pixels.
- Grid: 8 columns × 4 rows.
- Every cell: exactly 16×16 pixels.
- Center pivot for every tile.
- Transparent background outside the path shapes.

PRIMARY REQUEST

Create a seamless natural packed-earth path-overlay sheet for the warm town.

CONTENTS

- Several subtle center variations.
- Vertical and horizontal segments.
- Ends and caps.
- All necessary corners.
- T-junctions and crossings.
- Natural irregular edges that connect seamlessly across neighboring cells.
- Sparse embedded stones and small grass intrusions.

STYLE

Match the approved home's palette, cluster language, upper-left light, and detail density. Use larger connected clusters and quiet interior areas. Make path edges organic without obscuring tile connectivity.

AVOID

No obvious rectangular cell borders, evenly scattered dots, random noise, large color fields with no texture hierarchy, labels, grid lines, mockup scene, or shadows crossing tile boundaries incorrectly.
```

## Ground-detail sheet

Use the shared world-sprite specification, but the output is a sprite sheet.

```text
FILENAME
town_ground_details.png

CANVAS AND GRID

- Exact final sheet: 128×48 pixels.
- Grid: 8 columns × 3 rows.
- Every cell: exactly 16×16 pixels.
- Transparent background.
- Use bottom-center contact for freestanding details.

PRIMARY REQUEST

Create a coherent sheet of small town-environment details matching the approved home and 16 PPU character.

CONTENTS

- Several grass tufts.
- Small weeds.
- Tiny flower clusters.
- Two or three rock shapes.
- Short fallen sticks.
- Small packed-earth patches.
- One plank.
- One sack.
- One small crate.
- One mushroom pair.
- Other restrained rustic details where space remains.

Each item needs a strong native-resolution silhouette. Vary visual weight: include quiet subtle details as well as a few recognizable props. Keep every item within its own cell.

AVOID

No equal-density clutter in every cell, fuzzy foliage, random single-pixel texture, soft shadows, labels, dividers, or presentation background.
```

## Slim town tree

Append this prompt to the shared world-sprite specification.

```text
FILENAME
town_tree_slim.png

CANVAS AND ANCHOR

- Exact final canvas: 16×32 pixels.
- Bottom-center pivot: x=8, y=31.
- The trunk's ground contact must reach the bottom-center row.

PRIMARY REQUEST

Create one slim but appealing town tree that matches the approved warm-town pixel style and reads correctly beside a 16×32 adult character.

SUBJECT

- One clearly readable brown trunk.
- Two or three connected foliage masses.
- A naturally asymmetric silhouette.
- Selective dark edging.
- Upper-left leaf highlights and down-right shadow clusters.
- Enough visible trunk to establish the ground contact.

PALETTE

Use approximately 8–14 approved colors.

AVOID

No flat triangle, perfect symmetry, smooth circular foliage, gradient leaves, isolated one-pixel leaf noise, black empty holes, soft shadow, or oversized canopy that clips the canvas.
```

---

# Character repair

Attach `Assets/Art/Characters/TownCharacterSheet.png` as the edit target.

```text
Use case: precise-object-edit
Asset type: production-ready four-direction pixel-art character animation sheet

INPUT

Image 1 is the current town-character sheet and the edit target. Preserve the character's identity, yellow shirt, hair, clothing, proportions, palette, and recognizable appearance.

OUTPUT

- Filename: TownCharacterSheet.png
- Exact sheet: 64×128 pixels.
- Four columns × four rows.
- Every frame: 16×32 pixels.
- Row 1: down/front.
- Row 2: right.
- Row 3: up/back.
- Row 4: left.
- Four frames per direction.
- Designed for playback at 8 FPS.
- Transparent background.
- Bottom-center pivot for every frame.

PRIMARY REQUEST

Clean and stabilize the existing walk animation without redesigning the character.

REQUIRED CORRECTIONS

- Put the feet on the exact same baseline in all 16 frames.
- No foot, outline, or shadow pixel may extend lower in one frame than another.
- Keep the head and torso centered on a stable body axis.
- Remove side-to-side torso and head wobble.
- Derive motion primarily from alternating legs and arms.
- Allow at most a subtle deliberate one-pixel body bounce.
- Maintain consistent head width, shoulder width, torso length, and hair mass.
- Make left- and right-facing frames structurally consistent.
- Preserve clear front, side, and back silhouettes.
- Frame 0 in each row is a stable neutral idle pose.
- Keep every pixel safely inside its own 16×32 cell.

PIXEL RULES

- Hard opaque pixels only.
- Alpha only 0 or 255.
- No antialiasing, blur, or resampling.
- Preserve the current limited palette unless a replacement color is required for clarity.

INVARIANTS

Do not add weapons, armor, accessories, ground shadows, effects, extra frames, labels, borders, or a checkerboard. Change only alignment, animation consistency, and unclear clusters.
```

---

# Native-resolution QA prompt

Use this after importing each completed asset. Supply a gameplay screenshot captured at exactly 320×180 plus the source sprites visible in it.

```text
You are performing strict visual QA on Gozer, a 2D pixel-art RPG.

Image 1 is a gameplay screenshot captured at exactly 320×180 source pixels. Additional images are source sprites used in that screenshot.

AUDIT AGAINST THIS STANDARD

- Elevated orthographic three-quarter top-down presentation.
- No eye-level facade, side-scrolling view, isometric/dimetric projection, or perspective convergence.
- Rectangular 16×16 world grid.
- 16 pixels per Unity world unit.
- Adult characters are 16×32 pixels.
- World sprites use hard grid-aligned clusters.
- Alpha is only 0 or 255.
- No antialiasing, semitransparent outlines, blur, compression damage, or smooth scaling.
- Lighting consistently comes from upper-left; shadows fall down-right.
- Buildings, props, terrain, and characters share a palette and comparable detail density.
- Standard doors are approximately 16 pixels wide and 24–32 pixels tall.
- Entrances, interactables, hazards, and silhouettes remain readable at native 1×.
- Upgrade levels retain identical canvases, contact points, door positions, permanent geometry, footprint alignment, and viewing angle.
- Character feet remain on one baseline throughout animation.
- The town feels warm, safe, welcoming, and visually calm enough for gameplay.

INSPECT

1. Pixel integrity.
2. Scale consistency.
3. Camera and projection consistency.
4. Roof-to-facade balance.
5. Lighting consistency.
6. Palette cohesion.
7. Outline consistency.
8. Detail-density consistency.
9. Door and character proportions.
10. Ground-to-path transitions.
11. Silhouette readability.
12. Visual hierarchy at 320×180.
13. Upgrade-swap stability.
14. Animation baseline stability.

FOR EVERY PROBLEM, REPORT

- Severity: blocker, major, moderate, or minor.
- Exact asset or screen region.
- Violated rule.
- What is visually wrong at native size.
- A concrete pixel-level correction.
- Whether the asset should be edited or completely redrawn.

Do not judge primarily from a zoomed view. Native 320×180 readability is authoritative. Finish with a pass/fail result for each asset and an ordered list of the five most important corrections.
```

## Acceptance checklist before replacing an existing asset

- Exact canvas dimensions match this document and `PIXEL_ART_STANDARD.md`.
- Native art is crisp at 1×.
- Only alpha 0 and 255 are present.
- There are no thousands of accidental colors from smooth resampling.
- Bottom-center contact is correct.
- Door and character scale agree.
- Projection is visibly elevated top-down rather than frontal.
- Roof and upper surfaces are readable.
- Upper-left lighting is consistent.
- Upgrade anchors match the previous level.
- The sprite remains readable inside an actual 320×180 gameplay screenshot.
- Unity import uses Sprite, 16 PPU, Point filtering, no compression, no mipmaps, Clamp, alpha transparency, and Full Rect.
