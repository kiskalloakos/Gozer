# Town hub pixel-art brief

The current Unity scene uses colored placeholder shapes. Replace them with the sprites below without changing the gameplay code.

## Shared rules

- View: top-down three-quarter view, matching the current character.
- Grid: 32 x 32 px tiles; import environment art at 32 pixels per Unity unit.
- Palette: warm greens, timber browns, cream stone, amber lamps; town must feel safe and inviting.
- Files: transparent PNG, no smoothing, no shadows cut off at the canvas edge.
- Pivot for buildings and props: bottom-center.
- Light direction: upper-left.
- Keep doors at least 32 px wide and all important silhouettes readable at 1x scale.

## First delivery — required to replace every placeholder

1. **Ground tile sheet** — 256 x 256 px
   - 32 px tiles for grass (3 variations), packed dirt (3), stone town-square paving (3), and path edges/corners.
2. **Player home** — 192 x 160 px
   - Warm cottage, front door facing down, roof, chimney, two lit windows.
3. **Storage building** — 160 x 128 px
   - Reinforced shed/warehouse with a broad door, stacked crate motif, and a small lamp.
4. **Workbench** — 96 x 64 px
   - Separate freestanding bench with tools, vice, and a warm lantern glow.
5. **Watchtower, levels 1–3** — three 160 x 192 px transparent PNGs
   - L1: rough timber lookout; L2: roof, bell, stronger supports; L3: tall beacon and orange town banner.
6. **Infirmary, levels 1–3** — three 192 x 160 px transparent PNGs
   - L1: modest clinic; L2: extension and herb rack; L3: lit treatment wing and recognizable medical sign (no modern red-cross trademark).
7. **Greenhouse, levels 1–3** — three 192 x 160 px transparent PNGs
   - L1: small glass/cloth growing shelter; L2: second bay and water barrel; L3: reinforced glowing greenhouse with healthy crops.
8. **Town fence set** — 32 px tiles
   - Horizontal, vertical, four corners, end caps, damaged variant, and fence shadow.
9. **Expedition gate** — 192 x 96 px
   - Closed and open frames; make the outside-facing side darker and less welcoming.
10. **No-armor starter look**
11. **Walking animation all around 360**

Next:

12. Props around the town, and visually and functionally finishing the town.

## Second delivery — atmosphere and readability

- 32 x 32 px props: crate, barrel, sack, wood pile, signpost, well, bench, flower box, weeds, three rocks.
- Lamp post: 32 x 64 px, unlit and lit versions.
- Chimney-smoke animation: 6 frames, each 32 x 48 px.
- Window-glow animation: 4 frames, subtle flicker.
- Interaction icon: 16 x 16 px, hand or small speech marker.
- Upgrade sparkle: 6–8 frames on a 64 x 64 px canvas.

## Naming

Use lowercase snake case, for example `town_home.png`, `watchtower_l2.png`, and `ground_tiles.png`. Keep each upgrade level on the same canvas size so Unity can swap sprites without the building jumping.
