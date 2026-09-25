# Gozer

![alt text](https://github.com/kiskalloakos/Gozer/blob/6f5c3006f497f0f4629626ef2c59adf2834f907f/Screenshot%202026-09-21%20at%2016.47.11.png)

## One-sentence pitch

A nostalgic top-down pixel-art survival RPG where you build a life in a warm, persistent town, then risk each night outside its lights on tense loot-and-extract expeditions.

## Visual and technical standard

Gozer uses an **orthographic three-quarter top-down view** (also called an **oblique top-down view**), similar in presentation to Stardew Valley. It is a flat 2D XY world whose sprites show roofs, front walls, and prop tops to suggest depth. It is not true isometric art and does not use a diamond grid.

The project-wide non-negotiables are **16 pixels per Unity unit**, **16 x 16 px terrain tiles**, **16 x 32 px base character frames**, a **320 x 180** reference frame, and a **5.625 orthographic camera size**. World sprites use Point filtering, no compression or mipmaps, scale `(1, 1, 1)`, and bottom-center pivots where they touch the ground. Player interpolation stays on and follow-camera pixel snapping stays off for smooth diagonal motion.

The exact camera, import, asset-size, perspective, pivot, sorting, collision, animation, UI, and drawing requirements are defined in the [Gozer pixel-art and camera standard](#gozer-pixel-art-and-camera-standard) later in this README. That section is canonical for all future art and scenes.

Why is the city on lockdown? Why does Gozer only go out alone or in co-op? How did Gozer become a hero?

## The player fantasy

You are the "town Gozer". Which locally simply means a kind of hero. Everyone knows you in town, you are looked up to and feared. Also frequently asked for favors.

Return home carrying a rare, hard-won find after a frightening run. Use it to make the town safer, stranger, and more alive; then decide how much further to push your luck tomorrow night.

The feeling is deliberately built on contrast:

- **Day / town:** warm, personal, calm, social, and full of visible progress.
- **Night / wilderness:** dark, loud, unpredictable, and increasingly hostile.

You bring valuable resources IN the town, so that "one day we can escape" but that day never comes (?)...

## Creative pillars

1. **Nostalgia with a modern hook** — readable pixel art, tactile inventory and crafting, memorable sound effects, and compact places that invite exploration.
2. **A home worth returning to** — the town is not a menu. It visibly changes from resources, choices, and consequences.
3. **Meaningful extraction tension** — each expedition is a choice between leaving safely and risking another room, objective, or rare resource.
4. **Great alone; better together** — solo is a complete first-class experience. Co-op amplifies stories rather than making the game playable.
5. **Short, replayable sessions** — a good run takes roughly 10–20 minutes, with long-term RPG and town progression across many runs.

## Core loop

```text
Prepare in town
  → choose gear, supplies, and a destination
  → enter a dangerous expedition zone
  → explore, fight, loot, and complete objectives
  → decide when to extract
  → return with resources, injuries, discoveries, or problems
  → upgrade town, unlock options, and prepare again
```

## Current build

<details open>
<summary><strong>Current functionality</strong></summary>

### Current functionality in plain English

The current local build now contains the first playable version of the town-to-expedition loop. The player can leave home, enter danger, fight for supplies, choose whether to extract, and return to town with either rewards or an injury.

#### Town and player home

- Walk around TownHub with smooth four-direction movement and directional character animation.
- Move behind tall town scenery and have foreground objects fade instead of hiding the player completely.
- See the persistent five-heart health display and four-slot inventory bar in every scene.
- See secured Gold as numbered, persistent stacks in player-inventory or home-chest slots instead of as a separate upper-left counter.
- Begin a new save with 0 Gold and four wooden tools in quickbar slots 1–4: shovel, axe, pickaxe, and sword.
- Click the player home to enter its separate interior scene.
- Click the front door inside the player home to return to TownHub.
- Click the workbench inside the player home to craft a wooden axe, pickaxe, sword, or shovel from 5 Wood.
- Open the animated storage chest beside the Player Home workbench to view its pixel-art inventory panel over a dimmed room; left-click to pick up or place a full stack, right-click to take half or place one, and right-drag to distribute items across slots. The village clock continues while storage is open.
- Begin the fixed tutorial expedition with eight enemies; after a successful extraction, seeded expeditions begin at a higher population and continue scaling with threat.
- Click the infirmary for treatment when health is missing or the player is injured.
- Spend 3 Gold at the infirmary to clear the injury and restore all five hearts.
- Receive clear text feedback when already healthy, successfully treated, unable to afford treatment, or too injured to leave town.
- Keep health, injury state, total Gold, and every split Gold stack's inventory or chest slot between scene changes and game sessions.
- Use the expedition gate to leave town, provided the player has more than zero health.
- Only the player home and infirmary remain as active town buildings; the earlier storage, watchtower, and greenhouse prototypes are retired.

#### Expedition field

- Travel through the town expedition gate into a larger nighttime field.
- Enter the original fixed 64 x 48 field for the first expedition, regardless of weapon level.
- After successfully extracting from the tutorial, generate a fresh 120 x 90 field every time the expedition scene is entered, including multiple expeditions during the same game launch.
- Randomize the level-2 player spawn, ground pattern, forest placement, enemy positions, and extraction location from a new runtime seed for each expedition.
- Save and log every procedural expedition seed, show it during loading, and reproduce an exact layout from a requested seed through code or `-expedition-seed=<number>`.
- Build a deterministic multi-segment route before placing scenery, reserve 1.65 world units of clearance around it, validate connectivity on a 0.5-unit flood-fill grid, and retry invalid candidates deterministically up to 20 times.
- Enter every expedition through a minimum-five-second black loading/reveal sequence. The player, combat, enemies, and physics remain paused while a smoothly decelerating overhead camera zooms to the character and the full screen fades from 0% to 100% brightness. There is no camera blur.
- Limit level-2 nighttime visibility to a large, uniformly clear, soft-edged field of view around the player while keeping health, inventory, warnings, and extraction UI fully readable. During loading, the overhead view begins unrestricted so the whole arena is visible, then the darkness eases inward to the current gameplay radius alongside the camera and brightness reveal. That gameplay radius begins at 6.5 units and gradually tightens after successful runs to a 4.75-unit floor.
- Keep the fog centered on the seeded player position during the overhead loading camera movement, preventing the visibility mask from appearing late or revealing the full arena first.
- Place the level-2 extraction zone at least 56 world units from the seeded player spawn, tucked inside a tight grove with route-facing and opposite entrances. Its `4.3 x 2.7` world-unit orange marker uses 48% opacity and sorting order `-9999`, so it behaves as a readable ground decal beneath characters, enemies, and trees.
- Keep enemy and extraction-reinforcement placement inside the active arena bounds and away from solid obstacles whenever a valid sampled position is available.
- Keep the playable arena collider-enclosed while extending seeded ground beyond it to cover the complete loading and gameplay camera footprint; an unreachable perimeter forest hides the boundary and prevents the camera from revealing the empty background.
- Explore fields built from three night-grass variants and the same visual language as TownHub, with dense nighttime trees and natural sight-line obstructions.
- Move and aim freely while the camera follows the player.
- Attack toward the mouse cursor with a short-range melee strike by pressing the left mouse button. A hit or chop requires enough energy before it can affect the target; empty swings remain free.
- Knock enemies backward with successful hits.
- Interrupt an enemy's attack windup by landing a melee hit.
- See a brief hit flash and placeholder swing effect when combat connects.
- Fight enemies that wander until they detect the player.
- Read an enemy's alert, pursuit, attack windup, and recovery states through color, movement, and warning indicators.
- Take half a heart of damage from a successful enemy attack.
- Receive brief invulnerability and knockback after taking damage, preventing instant repeated hits.
- Kill a baseline enemy with three wooden-sword hits or six hits from another starter tool; enemy maximum health gains one point after every successful run.
- Make a defeated enemy drop one glowing expedition-supply pickup.
- Walk over dropped Gold or Wood to place each pickup in the first free player-inventory slot, scanning all 16 slots from the quickbar onward. A pickup stays on the ground when the bag is full.
- Store expedition Gold as ordinary Gold inventory stacks with an unsecured quantity. Successful extraction clears that risk marker; defeat removes all player-inventory items and Gold, while items already in the home chest remain safe.
- Find the physical extraction point hidden among the trees.
- Begin a ten-second extraction countdown by entering the extraction zone.
- Alert all surviving enemies when extraction begins, causing the activation to create danger.
- Spawn one fresh off-camera reinforcement during the tutorial. Seeded expeditions begin with two and add another after every second successful run, up to six additional reinforcements.
- Spawn extraction reinforcements only once per expedition, even if extraction is cancelled and restarted.
- Cancel extraction by leaving the zone.
- Cancel extraction by attacking; after attacking, the player must leave and re-enter the zone to try again.
- Successfully extract to secure all carried loot, convert it into Gold, and return to TownHub.
- Increase the persistent threat level after every successful expedition, including the tutorial. The first 24 successes each add two regular enemies, every second success adds an extraction reinforcement up to six additional reinforcements, enemies continuously become faster and more perceptive with diminishing growth, enemy health increases by one point per success, and visibility continuously tightens toward its minimum radius.
- Keep the current threat unchanged after defeat, so only successful runs advance the difficulty.
- Show the procedural seed and current threat level during loading, then report the newly unlocked threat level after returning successfully to town.
- Return to town with the exact amount of health remaining after a successful expedition.
- See how much Gold was secured after returning to town.

#### Defeat and recovery

- Lose health in half-heart increments until reaching zero.
- Lose all items and Gold in the player inventory on defeat. Items stored in the home chest remain safe.
- Automatically retreat to TownHub after being defeated.
- Return marked as injured with zero health.
- Remain unable to begin another expedition at zero health.
- Recover by paying for treatment at the infirmary, which clears the injury and restores five full hearts.

#### Interface and interaction

- Use the same health and inventory HUD in TownHub, the player home, the expedition field, and future scenes automatically.
- Receive lightweight, background-free notifications at the top center of the screen.
- Interact with buildings and doors by clicking their artwork without persistent “Click to...” instructions.
- See a dedicated hand cursor while hovering over interactive artwork such as the player home, infirmary, workbench, or doors, regardless of the player's distance.
- Interact with town objects and doors only while standing within two tiles of their collision boundary.
- See a brief pressed-cursor response when a click interaction is accepted.

#### Completed inventory and player-home storage update

The entries below supersede earlier descriptions of the inventory bar in this README. Earlier text is retained for project history.

- [x] Rename the former Town Supplies resource to **Gold** everywhere the player sees it, while preserving the existing saved-balance key so established saves keep their currency.
- [x] Replace the old six-slot inventory bar with the supplied four-slot `bottom_inventory.png` quickbar.
- [x] Render the quickbar with point filtering, no compression, no mipmaps, and the source artwork's measured per-column centers so Gold and its count stay aligned in every one of its four cells.
- [x] Open the full player inventory with **E** from any game scene; it is a 4 × 4 grid using `inventory.png` over a dimmed world. Expeditions continue while inventory is open.
- [x] Open the animated Player Home chest beside the workbench into paired, labeled 4 × 4 **INVENTORY** and **CHEST** grids over a dimmed room. Village time continues while the chest is open.
- [x] Split Gold into persistent stacks across player-inventory and chest cells using left-click, right-click, and right-drag distribution. Infirmary treatment can spend Gold from either container.
- [x] Draw Gold as a centered yellow token with a small in-cell top-right count; the open player and chest grids show a **GOLD** tooltip on hover, while the bottom quickbar does not.
- [x] Restore the hover cursor for interactable world objects and draggable Gold, while retaining the pressed-cursor click feedback.

</details>

## Latest completed functional milestone

The procedural level-2 expedition is now larger, deterministic, reproducible, connectivity-validated, visibility-limited, persistently escalating across successful runs, visually inspectable, construction-profiled, and protected by automated tests.

### Procedural-expedition reliability and inspection

- ✅ Generate the `120 x 90` level-2 arena from a saved seed with exactly 294 regular trees.
- ✅ Generate a deterministic safe player spawn and place extraction at least 56 world units from it.
- ✅ Apply a large, soft-edged nighttime field of view throughout the loading reveal and gameplay without obscuring gameplay UI.
- ✅ Persistently raise the next expedition's threat after each successful run while leaving difficulty unchanged after defeat.
- ✅ Preserve a clear multi-segment route from player spawn to extraction before adding forest and grove scenery.
- ✅ Reject unreachable candidates with a conservative four-direction flood-fill and regenerate deterministically when necessary.
- ✅ Keep extraction, trees, and grove scenery inside arena boundaries while maintaining spawn and extraction clearances.
- ✅ Reproduce the same extraction and scenery positions when the same seed is generated again.
- ✅ Validate seeds 1–500 in five batches of 100 without loading 500 Unity scenes.
- ✅ Preview the real production expedition immediately through **RPG → Tests → Preview Random Expedition**, without walking through TownHub.
- ✅ Begin each expedition behind a black loading screen, then reveal the generated arena with a five-second brightness fade and smoothly decelerating overhead-to-player zoom.
- ✅ Display both the expedition seed and persistent threat level during the procedural loading sequence.
- ✅ Keep the enlarged extraction marker on the ground layer at 48% opacity while the tighter grove partially conceals it.
- ✅ Submit the full `120 x 90` ground through one block tilemap update, reuse existing tree objects through a grow-only scene pool, and log construction timing plus pool reuse for every generated expedition.
- ✅ Separate playable and visual bounds: retain the `120 x 90` collision arena while generating camera-sized ground overscan and a deterministic unreachable boundary forest so neither loading nor edge-adjacent gameplay exposes the map void.

The playable progression loop remains:

`Prepare tools in town → fight and collect Gold → extract → return with rewards → craft or recover → attempt a harder expedition`

New saves start with no Gold and the wooden shovel, axe, pickaxe, and sword in quickbar slots 1–4. Completing the fixed tutorial unlocks the larger procedural field and raises the threat for the next expedition. The infirmary costs 3 Gold when treatment is needed; additional ways to earn Gold in town are planned.

## Persistent threat progression

Let `r` be the number of successfully completed expeditions stored in the save. The fixed tutorial is **Threat 1** with `r = 0`; completing it records `r = 1`, and the next expedition is seeded **Threat 2**. Defeated runs do not increase this value.

| System | Current rule for a seeded expedition |
| --- | --- |
| Threat shown to the player | `r + 1` |
| Regular enemies | `24 + 2 × min(r, 24)` |
| Extraction reinforcements | `2 + min(floor(r / 2), 6)` |
| Enemy movement-speed multiplier | `1 + 0.5 × (1 - e^(-0.08r))` |
| Enemy detection-radius bonus | `4 × (1 - e^(-0.08r))` world units |
| Enemy attack-windup multiplier | `0.6 + 0.4 × e^(-0.06r)` |
| Enemy maximum health | Baseline health plus `r` |
| Clear visibility radius | `4.75 + 1.75 × e^(-0.12r)` world units |

The continuous speed, detection, attack-timing, and visibility curves use diminishing growth. Enemy-count caps protect runtime performance. Enemy health currently grows by one point per successful run without a cap. This health rule is provisional until tool damage and the wider combat progression are decided.

The completed-run count and pending town notification persist in the active save. A successful extraction records the increase immediately before returning to TownHub. TownHub consumes the pending notification once and reports the next threat level alongside secured supplies. **RPG → Reset Town Progress Now** clears both threat keys along with the rest of the save progression.

### Implementation map

- `ExpeditionLayoutPlanner` owns the deterministic `120 x 90` layout, randomized safe spawn, route, extraction-distance rule, regular forest, and extraction grove.
- `ExpeditionArenaGenerator` builds that plan in the active scene, batches the complete ground into one tilemap update, reuses a grow-only scene tree pool, logs construction timings, moves the player to the seeded spawn, and supplies collision-safe positions for enemies and reinforcements.
- `ExpeditionFieldOfView` creates the uniform transparent center, soft fog edge, loading-time player tracking, and threat-scaled visibility radius.
- `ExpeditionRunProgression` owns the two persistent save values, current threat, regular-enemy bonus, and extraction-reinforcement bonus.
- `ExpeditionDifficultyDirector` applies run-based enemy counts and stats, installs level-2 visibility, and creates extraction reinforcements.
- `ExtractionZone` advances threat after a successful extraction; `ExpeditionLoadingSequence` displays seed and threat; `TownHubController` consumes and displays the one-time next-threat notification.
- `ExpeditionGenerationStressTest` validates deterministic layouts and connectivity; `ExpeditionRunProgressionTest` validates persistent threat behavior while preserving the user's current save values.

## Generator validation evidence

The current generator passed seeds **1–500** in five separate 100-seed batches. Each seed is generated twice and fails the batch immediately if any assertion fails. The automated checks cover:

- deterministic replay of the layout seed, retry attempt, player spawn, extraction position, all regular-tree positions, and all grove-tree positions;
- successful generation within 20 deterministic attempts;
- four-direction reachability from the seeded player spawn to extraction on a 0.5-unit validation grid;
- player spawn and extraction inside the arena, with extraction at least 56 world units from spawn;
- exactly 294 regular trees and between 8 and 14 extraction-grove trees;
- no regular tree within the seven-unit spawn clearing;
- arena containment for extraction, regular trees, and grove trees; and
- deterministic reproduction of the unreachable boundary forest, with every boundary tree outside the playable arena;
- at least 0.75 world units between every pair of scenery positions.

These are fast in-memory layout tests, not 500 instantiated scenes. They prove determinism, reachability, containment, counts, and spacing. They do not replace visual Play Mode inspection of sprite sorting, perceived extraction visibility, enemy behavior, loading animation, or real scene-generation performance; use **Preview Random Expedition** for those checks.

## Expedition construction profiling evidence

The production preview now logs total construction time, layout time, the batched-ground stage, the pooled-forest stage, and tree reuse/creation counts. With camera-sized ground overscan and the unreachable perimeter forest enabled, a Unity Editor preview on the development Mac constructed a `120 x 90` playable expedition in **26 ms** total: **8 ms** for the extended ground batch and **10 ms** for the forest pool. It reused all **116** tree objects already serialized in the scene, created **376** objects to reach that layout's 492-tree high-water mark, and destroyed none. The post-change deterministic test revalidated seeds **1–100** successfully in **543 ms**, including overlap checks for the complete interior and boundary forest. Player-build profiling on minimum-spec Windows, macOS, and Linux hardware remains necessary before final performance budgets are locked.

## Runtime progression validation evidence

The editor command **RPG → Tests → Validate Run Progression** verifies the persistent escalation path without permanently changing the active save. It temporarily starts from zero completed procedural runs and checks that:

- a new save begins at Threat 1 with no run-based enemy bonus;
- the first successful tutorial run unlocks the seeded Threat 2 field and adds one regular enemy to the next run;
- the pending town notification reports Threat 2 exactly once;
- the second success adds a second regular enemy and the first additional extraction reinforcement; and
- the previous completed-run and pending-notification values are restored after the test, including when an assertion fails.

Unity Play Mode inspection additionally verified the enlarged, uniform visibility circle and readable HUD after the loading sequence. The visibility component is created before the reveal begins and tracks the randomized player position while the overhead camera moves, so the loading fade reveals the fog-limited arena rather than briefly showing an unrestricted view.

## Scope guardrails

- Keep the game 2D pixel art rather than expanding into realistic 3D or an open world.
- Follow the [Gozer pixel-art and camera standard](#gozer-pixel-art-and-camera-standard) in this README for every scene and asset: orthographic three-quarter top-down, 16 PPU, 16 x 16 tiles, 16 x 32 base characters, and a 320 x 180 reference frame.
- Make solo feel excellent before networking.
- Prefer one deep biome and one meaningful town over five shallow biomes.
- Prefer emergent stories from systems over hundreds of scripted quests.
- Ship a small demo around the central loop before adding broad crafting, romance, vehicles, or competitive modes.

## Setting and tone

An isolated town survives at the edge of a wilderness that becomes unnatural after dark. Every route out of the city can lead somewhere different: an overgrown industrial exclusion zone, a cursed forest, or a collapsed underground world. It is populated by literal monsters, townspeople who became part of the wilderness, and altered animals.

The important tonal rule: town life is genuinely comforting and cozy. Outside the city is dark and scary.

## Single-player implementation

- **Solo-scaled encounters:** reduce enemy count and simultaneous threats, but retain dangerous enemy behaviors, sound cues, and decision-making.
- **Risk comes from pressure, not numbers:** limited healing, limited inventory, darkness, distance from extraction, and escalating night danger make a solo run tense.
- **Active extraction:** extraction is a physical destination such as a lift, gate, train platform, or radio beacon. Triggering it may take time or make noise.
- **Recovery instead of pure punishment:** death or retreat should cost the current haul and create an injury/problem, while permanent town upgrades, key discoveries, and story progress remain.
- **Optional helpers later:** earned tools can assist without replacing the player: a scout drone, tracking dog, portable workbench, or an NPC guard at extraction.

## Co-op implementation

Co-op is optional drop-in play using the same places, systems, and rules as solo.

| System | Single-player | Co-op |
| --- | --- | --- |
| Combat | Fewer concurrent threats; enemies remain dangerous | More mixed threats and chances to divide attention |
| Loot | All loot belongs to the player | Trading is available between co-op players. |
| Objectives | One player can complete every objective | Optional tasks reward splitting up or coordinating |
| Downed state | Injury, escape item, or costly retreat | Teammates can revive or carry a downed player |
| Extraction | Player chooses when to leave | Each player can leave with their haul, or the group chooses a shared extraction rule |
| Town | Personal persistent town | Join the host's town; visitors contribute resources and retain personal character progression |


## Shared tension system: contamination

The wilderness contains valuable **cursed / contaminated** items. Bringing them back may unlock powerful upgrades, unusual NPC events, or new areas—but also causes a town-level problem.

- In solo, the player chooses whether the reward is worth managing the consequence.
- In co-op, the same choice becomes a real group discussion.
- The result is visible: sick crops, power failures, strange visitors, altered town defenses, or new story branches.

This is the game's signature rule: **every expedition can change home.** Consequences are real.

## Reference palette

Use references as signals for feeling and structure, not features to copy.

- **Minecraft / Terraria:** discovery, progression through materials, unexpected stories, a world that feels worth poking at.
- **Stardew Valley:** a place that feels like home and develops emotional value.
- **Among Us / Mimesis:** suspense caused by uncertainty and social decisions, without requiring player betrayal.
- **Counter-Strike:** clear weapon identity, precise feedback, recognizable sound, and high-stakes moments.

## Combat direction

Combat should be simple to learn and rich in feedback.

- Top-down, direct aiming.
- Small, distinct weapon roster: for example revolver, shotgun, rifle, improvised melee tool.
- Every weapon needs a readable rhythm, recoil, sound profile, and ideal range.
- Enemies should be identified by behavior as much as appearance.
- Sound is a mechanic you need to look out for: gunshots, broken glass, alarms, and extraction machinery can attract danger.

The ones outside want to get inside. Kill you. You "stayed inside", you are "one of them". You should be outside with them, free and wild (they think).

---

<!-- BEGIN PIXEL ART STANDARD -->
## Gozer pixel-art and camera standard

This section is the canonical visual and technical specification for the project. If another document, Unity scene, importer, or script disagrees with this README, this README wins. Do not change an individual asset or camera in isolation; update this standard and the shared `PixelArtStandard` constants first.

### View and projection

The game uses an **orthographic three-quarter top-down view** (also called an **oblique top-down view**), similar in presentation to Stardew Valley.

- The game is fully 2D and gameplay movement happens on Unity's flat XY plane.
- The camera is orthographic and looks straight at that plane. There is no perspective convergence, vanishing point, or distance-based shrinking.
- Sprites create the illusion of depth by showing roofs, front walls, and the tops of props while their gameplay footprint remains on the ground plane.
- This is **not true isometric or dimetric art**: we do not use a diamond grid, 30-degree world axes, or isometric movement.
- “Down” means toward the viewer and shows a character's front. “Up” means away from the viewer and shows the character's back.

All new scenes, characters, buildings, props, effects, and UI must preserve this projection.

### Pixel grid and world scale

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

### Camera setup

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

### Unity texture import settings

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

### Pivots, anchors, sorting, and collision

- Terrain tiles: center pivot `(0.5, 0.5)`.
- Characters, buildings, gates, and freestanding props: bottom-center pivot `(0.5, 0)`.
- Treat the bottom-center pivot as the object's contact point with the ground.
- Y-sort world entities from the contact point using `Mathf.RoundToInt(-worldY * 100)`.
- Ground renders below all entities; roofs and upper walls are visual height, not gameplay depth.
- Character collision covers only the feet. The current 16 x 32 player uses an approximately `0.5 x 0.38` world-unit capsule at offset `(0, 0.19)`.
- Building collision covers only the walk-blocking footprint along the bottom of the building. Never wrap a collider around the roof or full transparent canvas.
- Interaction triggers are separate from solid footprint colliders and may be slightly larger for comfort.

### Character sheets and movement

The current standard character frame is 16 x 32 px. A four-direction, four-frame character sheet uses a 64 x 128 px active grid, excluding any explicitly documented transparent margin.

Current row order from top to bottom:

1. Down / front-facing
2. Right / right-facing
3. Up / back-facing
4. Left / left-facing

Each row contains four frames and plays at 8 FPS. Idle uses frame 0 of the last facing direction. Diagonal movement is normalized so it is not faster than cardinal movement. On diagonals, the horizontal input owns the displayed animation: W+D moves northeast while showing the right-facing walk; W+A moves northwest while showing the left-facing walk.

Keep the feet on the same baseline in every frame. Motion belongs in limbs and body bounce, not in a drifting pivot.

### Approved town asset canvases

All dimensions below are exact source-pixel canvas sizes at 16 PPU. Transparent padding is allowed only inside the stated canvas. Storage, watchtower, and greenhouse are retired prototypes rather than active town requirements; their dimensions remain documented only in case they are deliberately reintroduced.

| Asset | Pixel canvas | Tile/world footprint of canvas | Ratio |
| --- | ---: | ---: | ---: |
| Ground tile | 16 x 16 | 1 x 1 | 1:1 |
| Player home | 128 x 112 | 8 x 7 | 8:7 |
| Storage | 80 x 64 | 5 x 4 | 5:4 |
| Workbench | 48 x 32 | 3 x 2 | 3:2 |
| Watchtower L1-L3 | 80 x 96 each | 5 x 6 | 5:6 |
| Infirmary L1-L3 | 96 x 80 each | 6 x 5 | 6:5 |
| Greenhouse L1-L3 | 96 x 80 each | 6 x 5 | 6:5 |
| Expedition gate | 96 x 48 | 6 x 3 | 2:1 |

Every level of an upgradeable building must use the same canvas, pivot, contact point, and door position so sprite swaps do not jump. Closed and open expedition-gate assets exist, but the active gate currently displays the closed state only; no opening animation is implemented.

### Proportions and drawing rules

- A normal adult character is 16 x 32 px: approximately 1 tile wide and 2 tiles tall.
- Exterior doors should be about 16 px wide and 24-32 px tall, aligned to the building's bottom/front facade.
- Common small props normally occupy 16 x 16 px; tall props may use 16 x 32 px; large props may use clean tile multiples.
- Use deliberate pixel clusters, hard edges, and no anti-aliasing or semi-transparent outline pixels.
- Use one consistent light direction: upper-left. Highlights face upper-left and cast/contact shadows fall down-right.
- Roofs and front facades may overlap characters visually, but their colliders remain at ground level.
- Important silhouettes, doors, interactable objects, and hazards must remain readable at native 320 x 180 resolution.
- Keep the town palette warm and welcoming; expedition spaces may be colder, darker, and higher contrast without changing scale or projection.

### UI standard

- Design gameplay UI against the 320 x 180 reference frame.
- Use a pixel font and whole-pixel placement at the reference resolution.
- Use integer scaling for panels, icons, and text. Do not smoothly scale individual UI sprites.
- Use 16 x 16 px as the normal icon unit; larger UI art should be clean multiples of it.
- Keep critical text and prompts inside a safe inset of at least 8 reference pixels.

### File and source rules

- Use transparent PNG for sprites and lowercase snake_case names, for example `town_home.png` and `watchtower_l2.png`.
- Keep editable source files separate from exported game-ready PNGs.
- Do not overwrite an approved sprite with a differently sized canvas under the same filename.
- Document any intentional sheet margin in the importer code.
- `Assets/Scripts/PixelArtStandard.cs` is the code equivalent of the fixed numerical values in this document.

### Legacy prototype assets

The active town art and both gameplay cameras follow this standard. Older duplicate character experiments, the old 32 px ground sheet, the removed fence sheet, and the Rogue test room's temporary 128 px combat artwork remain in the repository only as prototype/source material. They are not scale references and must not be reused for new content. Re-author or export them to this standard before promoting them to approved game art; do not imitate their existing `.meta` PPU values.

### Acceptance checklist

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
<!-- END PIXEL ART STANDARD -->




# FUNCTIONAL TO-DO AND FIXES



## DONE


- [x] **Create the initial town hub.** The current active town contains the player home, home workbench, infirmary, and expedition gate. Earlier storage, watchtower, and greenhouse prototypes were retired rather than kept as active upgradeable buildings.
- [x] **Create one compact, replayable expedition zone.** The first run uses the fixed test field; later runs rebuild a larger randomized field on every entry.
- [x] **Generate a deterministic layout from a saved seed.** The last seed is saved and every run logs its seed; requested and command-line seeds reproduce the same layout.
- [x] **Carve a guaranteed route from spawn to extraction.** A deterministic multi-segment corridor is reserved before trees and extraction-grove scenery are placed.
- [x] **Validate connectivity before constructing the final scene.** A grid flood-fill rejects invalid candidates and retries deterministically before scene objects are built.
- [x] **Batch terrain updates and pool repeated scenery.** The level-2 ground is submitted through one `SetTilesBlock` call instead of 10,800 individual writes. Forest construction reuses every existing child under the Trees root, grows the pool only when required, and deactivates surplus objects instead of destroying them. Each production preview logs stage timings and reuse counts.
- [x] **Add automated generator stress tests.** The lightweight editor test has validated 500 unique seeds in five batches of 100 without loading gameplay scenes.
- [x] Make open-position fallbacks collision-safe instead of accepting a potentially blocked edge position after sampling fails.
- [x] Keep extraction-grove scenery inside the arena boundary when extraction is placed near an edge.
- [x] Keep the visible ground beyond the unreachable collider boundary for both the loading overview and edge-adjacent gameplay, with a deterministic perimeter forest masking the transition.
- [x] Add a minimum-five-second black expedition loading/reveal sequence with a loading tab, a 0% to 100% brightness fade, and a smoothly decelerating overhead camera zoom. No blur effect is used.
- [x] **Enlarge the procedural combat space.** Level 2 now generates a `120 x 90` arena with 294 regular trees while level 1 remains the fixed `64 x 48` introductory field.
- [x] **Seed the player spawn and extraction together.** Each procedural seed reproduces its safe player spawn, route, extraction point, forest, grove, and ground pattern; extraction remains at least 56 world units from spawn.
- [x] **Add level-2 nighttime visibility.** A uniform 6.5-unit clear circle with a soft edge follows the player, appears during the loading reveal, leaves gameplay UI readable, and tightens gradually toward 4.75 units as threat rises.
- [x] **Escalate successful runs.** Every success persists the next threat level and increases later enemy pressure, perception, speed, attack timing, health, and visibility pressure; defeat leaves threat unchanged.
- [x] **Communicate and reset threat.** Loading displays the current threat, TownHub reports the next threat after success, and the town-progress reset clears the escalation state.
- [x] **Validate persistent progression safely.** The editor progression test verifies the first two threat increases and restores the existing save values afterward.
- [x] **Implement one physical extraction point.** Entering it begins a dangerous ten-second countdown, alerts enemies, and spawns reinforcements.
- [x] **Define the first extraction method and why it is dangerous.** It is a physical zone with a ten-second activation, cancelled by leaving or attacking, that alerts enemies and calls reinforcements.
- [x] I shouldnt be able to strike backwards when walking in the other direction
- [x] Striking should happen only close to me, a meelee attack is a meelee attack.
- [x] **Complete the intended starter weapon set.** Player movement, mouse aiming, and melee combat work; the planned revolver and shotgun do not exist yet.
- [x] **A run-result screen.** Summarize secured loot, losses, health, treatment needs, spending, discoveries, and world changes after success or defeat.
- [x] **Basic game-session flow.** Add a title screen, save slots, pause menu, settings, and clear quit/restart paths.
- [x] Replace fragile scene-name and object-name lookups with explicit references or validated configuration.
- [x] Decide how seeds participate in future daily expeditions and co-op synchronization. Saves, exact-seed reproduction, command-line reproduction, and bug-report logging now use the saved expedition seed.
- [x] ENEMIES: Damage + HP
- [x] **Preparation and loadouts.** Let the player choose weapons, consumables, tools, destination, and risk level before leaving town.
- [x] **trees should also regrow with time**


## TOWN


- [ ] trees should take more than 1 day to grow
- [ ] hitbox of entering houses should only be the DOOR
- [ ] gold could be the main resource (for simplicity) - it already works globally; meaning, even if gold is in chest, you can heal in infirmary.
- [ ] **FARMING SYSTEM, CROPS**
- [ ] Gradual Unlocks: You start with access to your farm and the immediate town, but many regions remain blocked initially. Progression-Based: You open these locked areas by completing side quests, repairing infrastructure, or upgrading your tools. Open-Ended Freedom: While the map expands linearly through gameplay milestones, you have total freedom in how you spend your daily time, choose your skills, and interact with villagers.
- [ ] **Infirmary full setup with NPC, clear upgrades, prices, etc**
- [ ] **Other town supplies like market for trading specifically so you can trade Gold, etc**
- [ ] **Farmerama-like elements? Zoomumba-like elements?**
- [ ] **ANIMALS as mobs** (How about this as a meme? https://www.tiktok.com/@hyraxhub/video/7687660914950130975?_r=1&_t=ZN-99wClKYnuI2 and then also crocodiles, afking around ponds), also cats (https://toffeecraft.itch.io/cat-pack)



## EXPEDITION


- [ ] meelee (hand) attacks are gone. I should be able to attack with hands too.
- [ ] if you walk out of enemy view, they should stop following
- [ ] there are way too many enemies in threat 2 - either make upgrades available before going in threat 2, or make threat 2 easier (and rewrite the whole hardening part of the game)
- [ ] Decide the enemy-health curve alongside tool damage, future weapons, and the rest of threat progression. The current game adds one maximum-health point per successful run; keep this behavior until a clearer overall combat rule is chosen.
- [ ] **Meaningfully different loot.** Add common, valuable, and contaminated resources plus items that create inventory and extraction decisions.
- [ ] **Implement three resource tiers.** Common expedition supplies work; valuable and contaminated resource tiers do not.
- [ ] **Implement two regular enemies and one escalation enemy.** One wandering/pursuing melee enemy exists; distinct additional archetypes do not. **Demons could pretend that they are npcs**
- [ ] **Write the first five town upgrades and define their visible effects.** The broader town-upgrade plan remains undefined.
- [ ] **Expedition objectives.** Add missions such as hunting a target, rescuing someone, activating machinery, delivering an item, or discovering a location so a run has purpose beyond collecting supplies.
- [ ] **More enemy archetypes.** Add at least one fundamentally different regular enemy and one escalation enemy instead of only increasing the number of the current stalker.
- [ ] **A real noise system.** Model sound radius, investigate positions, persistent alarms, and different noise levels for melee attacks, firearms, broken objects, and extraction machinery.
- [ ] **Field-use items.** Add healing, escape tools, temporary buffs, deployable objects, and consumables that compete with loot for inventory space.
- [ ] **Contamination and town consequences.** Allow powerful finds to unlock benefits while also causing visible town problems, events, or story branches.
- [ ] **“Hidden” field of view** Already in, but it should be upgradable from the town supplies.
- [ ] When clicking expedition gate, there’s a moon animation with a wolf sound effect and it zooms in from top down to the character and for a slight mini second you can see where the extraction zone is. This is a Easter egg. Also, dnb-like music starts to get you in the mood.
- [ ] Mircovolts-like elements?
- [ ] Dash ? Sprint with fatigue?


## GENERAL


- [ ] Co-op implementation.



# VISUAL AND AUDITORY TO-DO AND FIXES



## VISUAL


## EXPEDITION


- [x] swoosh effect for melee attacks '/Users/kiskalloakos/Documents/Pixel Art Assets/Thrust'
- [x] first enemy looks '/Users/kiskalloakos/Documents/Pixel Art Assets/Tiny RPG Character Asset Pack 02 -Free Demon_A&Blood Monster_A/Characters(100x100 split)/Demon_A/Demon_A'
- [x] inventory (either one) is not centered
- [x] we need numbers 1-4 on the bot navbar, and also display those numbers in inventory, and be able to switch between those numbers with scroll-wheel and also numbers on keyboard
- [x] attacking MEELEE animation for Player
- [x] swords
- [x] simplify walking animations for main character
- [x] redo attack animations (both meelee and with tool in hand) - OR MAKE IT INTO **ONE**
- [x] draw wooden tool movement in all 4 directions
- [ ] next-level weapons and guns


## TOWN


- [ ] clock should be analog
- [ ] we could give it, gradually, a modern-civilization vibe with SOLAR PANELS UPGRADES, WINDMILLS, etc - it's like "I bring valuable resources IN the town, so that "one day we can escape" but that day never comes (?)
- [ ] Add more town props and functional dressing after the active town layout is settled.
- [ ] Create NPC character art for the player home, infirmary, workbench, and future active services as their gameplay roles are defined.



## AUDITORY


- [ ] walking
- [ ] opening chest
- [ ] upgrading infintrary
- [ ] upgrading workbench
- [ ] hitting swoosh DSGNImpt_MELEE-Magic Kick_HY_PC-006
- [ ] hitting enemy DSGNMisc_HIT-Hit Noise_HY_PC-005
- [ ] taking damage
