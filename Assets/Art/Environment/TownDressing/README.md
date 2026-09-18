# Town dressing assets

All assets are transparent PNGs, authored at native resolution for **16 pixels = 1 Unity unit**. Use point filtering and no mipmaps.

| File | Contents |
| --- | --- |
| `town_paths.png` | 8×4 grid of 16×16 tiles. Rows: dirt centers / cardinal edges + narrow vertical paths / outer corners + broken dirt / inner corners + growing-in paths. |
| `town_ground_details.png` | 8×3 grid of 16×16 overlays: weeds, flowers, rocks, dirt, leaves, mushrooms, stump, puddle. |
| `town_trees.png` | Three 64×80 tree sprites; trunk bottom-center is the grounding point. |
| `town_bushes.png` | Three 32×32 bush sprites. |
| `town_props.png` | 6×2 grid of 32×32 props: crate, barrel, sacks, wood pile, bucket, sign, bench, table, well, cart, laundry, trough. |
| `town_fence_set_v2.png` | 7×1 grid of 32×32 samples: horizontal, vertical, four corner arrangements, and gate. The horizontal and vertical runs occupy their central 32×16 and 16×32 areas. |

Lighting is upper-left and each sprite has a restrained dark grounding shadow. Keep vegetation/props pivoted at bottom center for correct player occlusion.
