# Town hub pixel-art brief

This brief applies the project-wide [pixel-art and camera standard](PIXEL_ART_STANDARD.md) to the town. The canonical rules are: orthographic three-quarter top-down view, 16 PPU, 16 x 16 px tiles, Point filtering, bottom-center world pivots, upper-left lighting, and default Transform scale `(1, 1, 1)`.

## Current required town assets

| Asset | Exact canvas | Content |
| --- | ---: | --- |
| Ground tile sheet | 128 x 128 px | 8 x 8 sheet of 16 px tiles: grass variations, packed dirt variations, stone paving, and path edges/corners. |
| Player home | 96 x 80 px | Warm cottage, down-facing front door, roof, chimney, and two lit windows. |
| Storage | 80 x 64 px | Reinforced shed/warehouse, broad door, crate motif, and small lamp. |
| Workbench | 48 x 32 px | Freestanding bench with tools, vice, and warm lantern glow. |
| Watchtower L1-L3 | 80 x 96 px each | L1 rough lookout; L2 roof, bell, and stronger supports; L3 beacon and orange town banner. |
| Infirmary L1-L3 | 96 x 80 px each | L1 modest clinic; L2 extension and herb rack; L3 lit treatment wing and original medical sign. |
| Greenhouse L1-L3 | 96 x 80 px each | L1 small shelter; L2 second bay and barrel; L3 reinforced glowing greenhouse and healthy crops. |
| Expedition gate | 96 x 48 px | One closed frame. The outside-facing side is darker and less welcoming. No opening animation. |
| Player walk cycle | 16 x 32 px per frame | Four directions, four frames each, ordered down/right/up/left from top to bottom. |

Upgradeable building levels must retain the same canvas, bottom-center pivot, baseline, and entrance position. Doors should be approximately 16 px wide and 24-32 px tall. Building colliders cover only their lower footprint, allowing the player to walk visually close to the facade.

The town fence is currently removed from scope. Do not re-add or import a fence sheet until its placement and tile set receive a new brief.

## Next atmosphere delivery

- Small props: normally 16 x 16 px; use 16 x 32 px for tall props and clean 16 px multiples for larger objects.
- Suggested set: crate, barrel, sack, wood pile, signpost, well, bench, flower box, weeds, and three rocks.
- Lamp post: 16 x 32 px, with unlit and lit versions on matching canvases.
- Chimney smoke: six 16 x 24 px frames on identical canvases.
- Window glow: four subtle frames, matching the window's exact canvas.
- Interaction icon: 16 x 16 px.
- Upgrade sparkle: six to eight frames on a 32 x 32 px canvas.

## Delivery rules

- Export transparent PNGs with lowercase snake_case names such as `town_home.png` and `watchtower_l2.png`.
- Draw without anti-aliasing and inspect every sprite at native 1x size.
- Keep shadows inside the canvas and use the common upper-left light direction.
- Do not enlarge art to make it appear bigger in Unity; choose the approved source canvas and import at 16 PPU.
- Check every delivery against the acceptance checklist in [PIXEL_ART_STANDARD.md](PIXEL_ART_STANDARD.md).
