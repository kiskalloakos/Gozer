# Town dressing assets

All assets are transparent PNGs, authored at native resolution for **16 pixels = 1 Unity unit**. Use point filtering and no mipmaps.

| File | Contents |
| --- | --- |
| `town_paths.png` | 8×4 grid of 16×16 tiles. Rows: dirt centers / cardinal edges + narrow vertical paths / outer corners + broken dirt / inner corners + growing-in paths. |
| `town_ground_details.png` | 8×3 grid of 16×16 overlays: weeds, flowers, rocks, dirt, leaves, mushrooms, stump, puddle. |
| `town_tree_slim.png` | One 16×32 tree, used in small perimeter clusters with a bottom-center grounding point. |

The town intentionally excludes the rejected bush, prop, fence, and large-tree sheets. Keep vegetation pivoted at bottom center for correct player occlusion.
