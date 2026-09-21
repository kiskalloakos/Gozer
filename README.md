# Gozer

![alt text](https://github.com/kiskalloakos/Gozer/blob/6e47a3a9c7da2f0df0129de83978270bd6c77358/rogue_final_standing.png)

## One-sentence pitch

A nostalgic top-down pixel-art survival RPG where you build a life in a warm, persistent town, then risk each night outside its lights on tense loot-and-extract expeditions.

## Visual and technical standard

Gozer uses an **orthographic three-quarter top-down view** (also called an **oblique top-down view**), similar in presentation to Stardew Valley. It is a flat 2D XY world whose sprites show roofs, front walls, and prop tops to suggest depth. It is not true isometric art and does not use a diamond grid.

The project-wide non-negotiables are **16 pixels per Unity unit**, **16 x 16 px terrain tiles**, **16 x 32 px base character frames**, a **320 x 180** reference frame, and a **5.625 orthographic camera size**. World sprites use Point filtering, no compression or mipmaps, scale `(1, 1, 1)`, and bottom-center pivots where they touch the ground. Player interpolation stays on and follow-camera pixel snapping stays off for smooth diagonal motion.

The exact camera, import, asset-size, perspective, pivot, sorting, collision, animation, UI, and drawing requirements are defined in the [Gozer pixel-art and camera standard](#gozer-pixel-art-and-camera-standard) later in this README. That section is canonical for all future art and scenes.

Why is the city on lockdown? Why does Gozer only go out alone (or coop)? How did Gozer become a hero? 

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
- Place the level-2 extraction zone at least 42 world units from the player spawn, inside a protected clearing surrounded by a loose grove with two entrances.
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

The first upgrade-driven expedition change is now implemented: purchasing the Reinforced Melee Weapon changes both player power and the next fight area.

The playable loop currently reaches:

`Leave town → fight → collect supplies → extract → return`

The loop now includes the previously missing payoff:

`Spend rewards → become permanently stronger → want another expedition`

### Reinforced melee weapon and level-2 expedition

- ✅ Purchase it from the player-home workbench for **8 Town Supplies**.
- ✅ Permanently increase melee damage from 1 to 2.
- ✅ Reduce the current enemy from three required hits to two.
- ✅ Increase upgraded expeditions from 8 enemies to 12.
- ✅ Replace the fixed test layout with a larger randomized level-2 field on the next expedition.
- ✅ Move the extraction zone, rebuild the forest and ground pattern, and redistribute enemies every time the upgraded player enters the field.
- ✅ Clearly communicate whether the upgrade was purchased, is unaffordable, or is already owned.
- ✅ Persist the upgrade between scenes and game sessions.

The infirmary remains a recovery cost rather than permanent progression. The workbench now gives successful extraction a lasting positive payoff and visibly changes the following run, completing the initial version of the core loop.

## Comprehensive development checklist

This is the project's single source of truth for completed milestones and remaining work. Checked items are playable now. Unchecked items are not implemented, even when a related prototype exists.

### Immediate priority: make expedition generation production-ready

The level-2 generator is suitable for playtesting and can generate multiple expeditions in one launch without intentionally retaining earlier worlds. Before procedural expeditions become the production core loop, complete these six steps:

- [ ] **Generate a deterministic layout from a saved seed.** Save or log the seed with each run so a reported map can be reproduced exactly for debugging, balancing, and future daily challenges.
- [ ] **Carve a guaranteed route from spawn to extraction.** Build one or more traversable corridors before adding trees and encounter dressing, rather than relying entirely on random spacing.
- [ ] **Validate connectivity before constructing the final scene.** Use a grid flood-fill or pathfinding check to prove that the player can reach extraction and required objectives; reject and regenerate invalid layouts.
- [ ] **Batch terrain updates and pool repeated scenery.** Replace thousands of individual tile writes and repeated tree creation/destruction with block tilemap updates, pooled objects, tile-based decoration, or combined renderers after profiling the target hardware.
- [ ] **Move arena rules into level and biome configuration assets.** Store dimensions, density, extraction distance, enemy budget, tiles, scenery, and progression requirements as data instead of expanding hard-coded melee-level conditionals.
- [ ] **Add automated generator stress tests.** Generate and validate at least 1,000 seeds, checking reachability, boundary containment, extraction entrances, spawn separation, overlaps, object counts, and generation time.

Known weaknesses to cover while completing this work:

- [ ] Make open-position fallbacks collision-safe instead of accepting a potentially blocked edge position after sampling fails.
- [ ] Keep extraction-grove scenery inside the arena boundary when extraction is placed near an edge.
- [ ] Replace fragile scene-name and object-name lookups with explicit references or validated configuration.
- [ ] Add loading feedback or move generation across frames if profiling shows a visible scene-entry hitch.
- [ ] Decide how seeds participate in saves, replays, daily expeditions, co-op synchronization, and bug reports.
- [ ] Add authored landmarks and layout grammar so repeated expeditions feel structurally different rather than only randomly scattered.

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

An isolated town survives at the edge of a wilderness that becomes unnatural after dark... Every direction you "go out" to from the city is different.; the wilderness, outside of town, is an overgrown industrial exclusion zone // a cursed forest // a collapsed underground world. It is full of literal montsers, town people who became part of the wilderness and animals.

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

### Required town asset canvases

All dimensions below are exact source-pixel canvas sizes at 16 PPU. Transparent padding is allowed only inside the stated canvas.

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

Every level of an upgradeable building must use the same canvas, pivot, contact point, and door position so sprite swaps do not jump. The expedition gate currently uses one closed frame only; no opening animation is part of the present design.

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

The active town art and all gameplay cameras follow this standard. Legacy character experiments and the Rogue combat test room have been removed. Any future prototype art must be re-authored or exported to this standard before becoming active game content; never treat prototype import settings as scale references.

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

---

<!-- BEGIN PIXEL ART GENERATION PROMPTS -->
## Gozer pixel-art generation prompt book

This section contains reusable prompts for regenerating the project's active town art. The player-home prompt is intentionally omitted because the replacement home has already been created.

The canonical rules are in the [Gozer pixel-art and camera standard](#gozer-pixel-art-and-camera-standard) above. If this prompt book ever conflicts with that standard, the standard wins.

### How to use this prompt book

1. Generate **one asset per run**. Do not ask an image model to create several unrelated sprites in one image.
2. For every world asset, paste the **Shared world-sprite specification** first, followed by one asset prompt.
3. Attach the approved player home as the style, palette, cluster, and projection reference.
4. Attach `Assets/Art/Characters/TownCharacterSheet.png` as the character-scale reference when useful.
5. For upgrade levels, attach the approved previous level as a locked structural reference.
6. Treat generated images as drafts until their canvas, alpha, palette, contact point, and native-size readability have been verified.
7. If a generator cannot return the exact small canvas, use its result only as a visual reference and redraw it at the required native resolution. Never smoothly downscale an illustration into the final sprite.

### Shared world-sprite specification

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

### Recommended style-and-palette reference

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

## Active town assets

### Storage building

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

### Workbench

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

### Expedition gate — closed

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

## Watchtower upgrade family

Every watchtower level uses an exact 80×96 canvas and bottom-center pivot at x=40, y=95. The four main supports, ladder x coordinate, platform position, perspective, and ground-contact line are permanent. Generate levels in order and attach the approved previous level to the next request.

### Watchtower L1

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

### Watchtower L2

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

### Watchtower L3

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

## Infirmary upgrade family

Every infirmary level uses an exact 96×80 canvas and bottom-center pivot at x=48, y=79. The centered 16-pixel-wide entrance, central facade, roof direction, view angle, scale, and ground-contact line are permanent.

### Infirmary L1

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

### Infirmary L2

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

### Infirmary L3

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

## Greenhouse upgrade family

Every greenhouse level uses an exact 96×80 canvas and bottom-center pivot at x=48, y=79. The centered 16-pixel-wide entrance, central frame, roof angle, view direction, and ground-contact line are permanent. Glass is represented with opaque colors, never actual partial transparency.

### Greenhouse L1

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

### Greenhouse L2

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

### Greenhouse L3

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

## Town dressing

### Natural path sheet

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

### Ground-detail sheet

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

### Slim town tree

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

## Character repair

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

## Native-resolution QA prompt

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

### Acceptance checklist before replacing an existing asset

- Exact canvas dimensions match this prompt book and the [Gozer pixel-art and camera standard](#gozer-pixel-art-and-camera-standard).
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
<!-- END PIXEL ART GENERATION PROMPTS -->
