# Gozer

![alt text](https://github.com/kiskalloakos/Gozer/blob/270f5d7642847d01ee2bc0d02d7e8c92f360e398/TownCharacterSheet.png)

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
- See the persistent five-heart health display and six-slot inventory bar in every scene.
- See all secured Town Supplies as one numbered stack in the first inventory slot instead of as a separate upper-left counter.
- Begin a new save with 18 Town Supplies, enough to test the current workbench and treatment economy.
- Click the player home to enter its separate interior scene.
- Click the front door inside the player home to return to TownHub.
- Click the workbench inside the player home to purchase the one-time Reinforced Melee Weapon upgrade for 8 Town Supplies.
- Permanently increase melee damage from 1 to 2 after purchasing the workbench upgrade, reducing the current enemy from three required hits to two.
- Increase the expedition population from 8 enemies to 12 after reaching melee level 2.
- Receive clear workbench feedback when the upgrade is purchased, unaffordable, or already owned.
- Click the infirmary for treatment when health is missing or the player is injured.
- Spend 3 Town Supplies at the infirmary to clear the injury and restore all five hearts.
- Receive clear text feedback when already healthy, successfully treated, unable to afford treatment, or too injured to leave town.
- Keep health, injury state, secured supplies, and town resources between scene changes and game sessions.
- Use the expedition gate to leave town, provided the player has more than zero health.
- Only the player home and infirmary remain as active town buildings; the earlier storage, watchtower, and greenhouse prototypes are retired.

#### Expedition field

- Travel through the town expedition gate into a larger nighttime field.
- Enter the original fixed 64 x 48 test field while using the base melee weapon at melee level 1.
- After purchasing the Reinforced Melee Weapon, generate a fresh 92 x 68 level-2 field every time the expedition scene is entered, including multiple expeditions during the same game launch.
- Randomize the level-2 ground pattern, forest placement, enemy positions, and extraction location from a new runtime seed for each expedition.
- Save and log every procedural expedition seed, show it during loading, and reproduce an exact layout from a requested seed through code or `-expedition-seed=<number>`.
- Build a deterministic multi-segment route before placing scenery, reserve 1.65 world units of clearance around it, validate connectivity on a 0.5-unit flood-fill grid, and retry invalid candidates deterministically up to 20 times.
- Enter every expedition through a minimum-five-second black loading/reveal sequence. The player, combat, enemies, and physics remain paused while a smoothly decelerating overhead camera zooms to the character and the full screen fades from 0% to 100% brightness. There is no camera blur.
- Place the level-2 extraction zone at least 42 world units from the player spawn, tucked inside a tight grove with route-facing and opposite entrances. Its `4.3 x 2.7` world-unit orange marker uses 48% opacity and sorting order `-9999`, so it behaves as a readable ground decal beneath characters, enemies, and trees.
- Keep enemy and extraction-reinforcement placement inside the active arena bounds and away from solid obstacles whenever a valid sampled position is available.
- Explore fields built from three night-grass variants and the same visual language as TownHub, with dense nighttime trees and natural sight-line obstructions.
- Move and aim freely while the camera follows the player.
- Attack toward the mouse cursor with a short-range melee strike by pressing the left mouse button.
- Knock enemies backward with successful hits.
- Interrupt an enemy's attack windup by landing a melee hit.
- See a brief hit flash and placeholder swing effect when combat connects.
- Fight enemies that wander until they detect the player.
- Read an enemy's alert, pursuit, attack windup, and recovery states through color, movement, and warning indicators.
- Take half a heart of damage from a successful enemy attack.
- Receive brief invulnerability and knockback after taking damage, preventing instant repeated hits.
- Kill an enemy with three normal melee hits.
- Make a defeated enemy drop one glowing expedition-supply pickup.
- Walk over dropped loot to add it to the existing supply stack in the first inventory slot.
- See secured supplies and newly carried expedition supplies combined into one visible stack count; defeat still removes only the unsecured portion.
- Find the physical extraction point hidden among the trees.
- Begin a ten-second extraction countdown by entering the extraction zone.
- Alert all surviving enemies when extraction begins, causing the activation to create danger.
- Spawn one fresh off-camera reinforcement when extraction begins at melee level 1, or two at melee level 2.
- Spawn extraction reinforcements only once per expedition, even if extraction is cancelled and restarted.
- Cancel extraction by leaving the zone.
- Cancel extraction by attacking; after attacking, the player must leave and re-enter the zone to try again.
- Successfully extract to secure all carried loot, convert it into Town Supplies, and return to TownHub.
- Return to town with the exact amount of health remaining after a successful expedition.
- See how many supplies were secured after returning to town.

#### Defeat and recovery

- Lose health in half-heart increments until reaching zero.
- Lose every carried expedition pickup on defeat; unsecured loot never becomes Town Supplies.
- Automatically retreat to TownHub after being defeated.
- Return marked as injured with zero health.
- Remain unable to begin another expedition at zero health.
- Recover by paying for treatment at the infirmary, which clears the injury and restores five full hearts.

#### Interface and interaction

- Use the same health and inventory HUD in TownHub, the player home, the expedition field, and future scenes automatically.
- Receive lightweight, background-free notifications at the top center of the screen.
- Interact with buildings and doors by clicking their artwork without persistent “Click to...” instructions.
- See a dedicated hand cursor only while hovering over interactive artwork such as the player home, infirmary, workbench, or doors.
- See a brief pressed-cursor response when a click interaction is accepted.

</details>

## Latest completed functional milestone

The procedural level-2 expedition is now deterministic, reproducible, connectivity-validated, visually inspectable, and protected by automated stress tests.

### Procedural-expedition reliability and inspection

- ✅ Generate the `92 x 68` level-2 arena from a saved seed with exactly 170 regular trees.
- ✅ Preserve a clear multi-segment route from player spawn to extraction before adding forest and grove scenery.
- ✅ Reject unreachable candidates with a conservative four-direction flood-fill and regenerate deterministically when necessary.
- ✅ Keep extraction, trees, and grove scenery inside arena boundaries while maintaining spawn and extraction clearances.
- ✅ Reproduce the same extraction and scenery positions when the same seed is generated again.
- ✅ Validate seeds 1–500 in five batches of 100 without loading 500 Unity scenes.
- ✅ Preview the real production expedition immediately through **RPG → Tests → Preview Random Expedition**, without walking through TownHub.
- ✅ Begin each expedition behind a black loading screen, then reveal the generated arena with a five-second brightness fade and smoothly decelerating overhead-to-player zoom.
- ✅ Keep the enlarged extraction marker on the ground layer at 48% opacity while the tighter grove partially conceals it.

The playable progression loop remains:

`Leave town → fight → collect supplies → extract → return → spend rewards → become permanently stronger → attempt a changed expedition`

The Reinforced Melee Weapon remains the first permanent reward: it costs 8 Town Supplies, raises melee damage from 1 to 2, increases the expedition from 8 to 12 enemies, unlocks the larger procedural field, and persists between sessions. The infirmary remains a recovery cost rather than permanent progression.

## Comprehensive development checklist

This is the project's single source of truth for completed milestones and remaining work. Checked items are playable now. Unchecked items are not implemented, even when a related prototype exists.

### Immediate priority: make expedition generation production-ready

The level-2 generator is suitable for playtesting and can generate multiple expeditions in one launch without intentionally retaining earlier worlds. Before procedural expeditions become the production core loop, complete these six steps:

For rapid visual inspection in Unity, use **RPG → Tests → Preview Random Expedition**. It opens the production expedition scene with a fresh procedural seed and enters Play Mode directly, without requiring the level-2 unlock or a trip through TownHub.

- [x] **Generate a deterministic layout from a saved seed.** The last seed is saved and every run logs its seed; requested and command-line seeds reproduce the same layout.
- [x] **Carve a guaranteed route from spawn to extraction.** A deterministic multi-segment corridor is reserved before trees and extraction-grove scenery are placed.
- [x] **Validate connectivity before constructing the final scene.** A grid flood-fill rejects invalid candidates and retries deterministically before scene objects are built.
- [ ] **Batch terrain updates and pool repeated scenery.** Replace thousands of individual tile writes and repeated tree creation/destruction with block tilemap updates, pooled objects, tile-based decoration, or combined renderers after profiling the target hardware.
- [ ] **Move arena rules into level and biome configuration assets.** Store dimensions, density, extraction distance, enemy budget, tiles, scenery, and progression requirements as data instead of expanding hard-coded melee-level conditionals.
- [x] **Add automated generator stress tests.** The lightweight editor test has validated 500 unique seeds in five batches of 100 without loading gameplay scenes.

Known weaknesses to cover while completing this work:

- [x] Make open-position fallbacks collision-safe instead of accepting a potentially blocked edge position after sampling fails.
- [x] Keep extraction-grove scenery inside the arena boundary when extraction is placed near an edge.
- [ ] Replace fragile scene-name and object-name lookups with explicit references or validated configuration.
- [x] Add a minimum-five-second black expedition loading/reveal sequence with a loading tab, a 0% to 100% brightness fade, and a smoothly decelerating overhead camera zoom. No blur effect is used.
- [ ] Decide how seeds participate in future daily expeditions and co-op synchronization. Saves, exact-seed reproduction, command-line reproduction, and bug-report logging now use the saved expedition seed.
- [ ] Add authored landmarks and layout grammar so repeated expeditions feel structurally different rather than only randomly scattered.

### Generator validation evidence

The current generator passed seeds **1–500** in five separate 100-seed batches. Each seed is generated twice and fails the batch immediately if any assertion fails. The automated checks cover:

- deterministic replay of the layout seed, retry attempt, extraction position, all regular-tree positions, and all grove-tree positions;
- successful generation within 20 deterministic attempts;
- four-direction reachability from player spawn `(0, -18)` to extraction on a 0.5-unit validation grid;
- extraction inside the arena and at least 42 world units from spawn;
- exactly 170 regular trees and between 8 and 14 extraction-grove trees;
- no regular tree within the seven-unit spawn clearing;
- arena containment for extraction, regular trees, and grove trees; and
- at least 0.75 world units between every pair of scenery positions.

These are fast in-memory layout tests, not 500 instantiated scenes. They prove determinism, reachability, containment, counts, and spacing. They do not replace visual Play Mode inspection of sprite sorting, perceived extraction visibility, enemy behavior, loading animation, or real scene-generation performance; use **Preview Random Expedition** for those checks.

### Core single-player systems still missing

- [ ] **Expedition objectives.** Add missions such as hunting a target, rescuing someone, activating machinery, delivering an item, or discovering a location so a run has purpose beyond collecting supplies.
- [ ] **Meaningfully different loot.** Add common, valuable, and contaminated resources plus items that create inventory and extraction decisions.
- [ ] **Preparation and loadouts.** Let the player choose weapons, consumables, tools, destination, and risk level before leaving town.
- [ ] **More enemy archetypes.** Add at least one fundamentally different regular enemy and one escalation enemy instead of only increasing the number of the current stalker.
- [ ] **A real noise system.** Model sound radius, investigate positions, persistent alarms, and different noise levels for melee attacks, firearms, broken objects, and extraction machinery.
- [ ] **Field-use items.** Add healing, escape tools, temporary buffs, deployable objects, and consumables that compete with loot for inventory space.
- [ ] **Contamination and town consequences.** Allow powerful finds to unlock benefits while also causing visible town problems, events, or story branches.
- [ ] **Multiple destinations and route unlocking.** Expand the expedition gate beyond one field and support permanently unlocked routes.
- [ ] **A run-result screen.** Summarize secured loot, losses, health, treatment needs, spending, discoveries, and world changes after success or defeat.
- [ ] **Basic game-session flow.** Add a title screen, save slots, pause menu, settings, and clear quit/restart paths.

Co-op remains a later pillar. The single-player loop above should be proven before networking work begins.

### Original prototype milestones

- [x] **Create the initial town hub.** The current active town contains the player home, home workbench, infirmary, and expedition gate. Earlier storage, watchtower, and greenhouse prototypes were retired rather than kept as active upgradeable buildings.
- [x] **Create one compact, replayable expedition zone.** Melee level 1 uses the fixed test field; melee level 2 rebuilds it as a larger randomized field on every entry.
- [ ] **Complete the intended starter weapon set.** Player movement, mouse aiming, and melee combat work; the planned revolver and shotgun do not exist yet.
- [ ] **Implement three resource tiers.** Common expedition supplies work; valuable and contaminated resource tiers do not.
- [ ] **Implement two regular enemies and one escalation enemy.** One wandering/pursuing melee enemy exists; distinct additional archetypes do not.
- [x] **Implement one physical extraction point.** Entering it begins a dangerous ten-second countdown, alerts enemies, and spawns reinforcements.
- [x] **Implement one permanent reward.** The Reinforced Melee Weapon permanently increases damage and changes the next expedition.

The proof-of-fun question remains: **after a successful run, does the player immediately want to go back out for one more?**

### Near-term design decisions

- [x] **Define the first extraction method and why it is dangerous.** It is a physical zone with a ten-second activation, cancelled by leaving or attacking, that alerts enemies and calls reinforcements.
- [ ] **Write the first five town upgrades and define their visible effects.** Reinforced melee is the first permanent upgrade; four more upgrades and the broader town-upgrade plan remain undefined.
- [ ] **Define the production procedural-expedition rules.** Decide biome structure, route grammar, landmarks, objectives, difficulty scaling, seed persistence, and how much authored content each run contains.
- [ ] **Implement co-op multiplayer later.** Begin only after the single-player core loop and procedural generation are reliable.

### Scope guardrails

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

## Remaining pixel-art backlog

The repository already contains the active town character sheet, workbench, infirmary exports, and closed/open expedition-gate artwork. Do not recreate retired storage, watchtower, or greenhouse art unless those buildings are intentionally returned to the design.

- Add more town props and functional dressing after the active town layout is settled.
- Create NPC character art for the player home, infirmary, workbench, and future active services as their gameplay roles are defined.
- Replace placeholder expedition enemies and combat effects with production mob, monster, hit, and attack artwork while preserving the established scale and projection.
