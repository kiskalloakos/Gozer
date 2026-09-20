# Gozer

![alt text](https://github.com/kiskalloakos/Gozer/blob/6e47a3a9c7da2f0df0129de83978270bd6c77358/rogue_final_standing.png)

## One-sentence pitch

A nostalgic top-down pixel-art survival RPG where you build a life in a warm, persistent town, then risk each night outside its lights on tense loot-and-extract expeditions.

## Visual and technical standard

Gozer uses an **orthographic three-quarter top-down view** (also called an **oblique top-down view**), similar in presentation to Stardew Valley. It is a flat 2D XY world whose sprites show roofs, front walls, and prop tops to suggest depth. It is not true isometric art and does not use a diamond grid.

The project-wide non-negotiables are **16 pixels per Unity unit**, **16 x 16 px terrain tiles**, **16 x 32 px base character frames**, a **320 x 180** reference frame, and a **5.625 orthographic camera size**. World sprites use Point filtering, no compression or mipmaps, scale `(1, 1, 1)`, and bottom-center pivots where they touch the ground. Player interpolation stays on and follow-camera pixel snapping stays off for smooth diagonal motion.

The exact camera, import, asset-size, perspective, pivot, sorting, collision, animation, UI, and drawing requirements are defined in [PIXEL_ART_STANDARD.md](PIXEL_ART_STANDARD.md). That file is canonical for all future art and scenes.

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
- Explore a field built from three night-grass variants and the same visual language as TownHub, with dense nighttime trees and natural sight-line obstructions.
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

The first permanent upgrade at the player-home workbench is now implemented.

The playable loop currently reaches:

`Leave town → fight → collect supplies → extract → return`

The loop now includes the previously missing payoff:

`Spend rewards → become permanently stronger → want another expedition`

### Reinforced melee weapon

- ✅ Purchase it from the player-home workbench for **8 Town Supplies**.
- ✅ Permanently increase melee damage from 1 to 2.
- ✅ Reduce the current enemy from three required hits to two.
- ✅ Increase upgraded expeditions from 8 enemies to 12.
- ✅ Clearly communicate whether the upgrade was purchased, is unaffordable, or is already owned.
- ✅ Persist the upgrade between scenes and game sessions.

The infirmary remains a recovery cost rather than permanent progression. The workbench now gives successful extraction a lasting positive payoff and completes the initial version of the core loop.

## Missing systems to add

These are missing pieces of the game rather than balance tweaks or improvements to systems that already exist.

1. **An expedition objective system.** The player can currently fight, loot, and leave, but there is no mission, target, rescue, activation, delivery, or discovery that gives a run a purpose beyond collecting supplies.
2. **Meaningfully different loot.** Supplies are the only obtainable resource. The game still needs valuable items, contaminated items, and items that create real inventory or extraction decisions.
3. **Preparation and loadout choices.** Town does not yet let the player choose a weapon, consumable, tool, destination, or risk level before departing.
4. **A second enemy archetype.** Every wilderness threat currently uses the same behavior. At least one fundamentally different enemy is needed to create encounter decisions rather than larger groups of the same threat.
5. **A real noise system.** Extraction directly alerts enemies, but the game does not yet model sound radius, investigate positions, persistent alarms, or different noise levels from player actions.
6. **Field-use items and recovery decisions.** There are no healing items, escape tools, temporary buffs, deployable objects, or consumables to decide whether to use or carry home.
7. **Contamination and town consequences.** The README's signature risk—bringing home powerful items that can also change or harm the town—does not exist yet.
8. **Multiple destinations or route unlocking.** The expedition gate leads to one field, with no destination choice or permanently unlockable route.
9. **A run-result screen.** Successful and failed expeditions need a concise summary of what was secured, lost, spent, discovered, and changed.
10. **Basic game-session flow.** A player-facing title screen, save-slot flow, pause menu, settings, and quit/restart path are not implemented.

Co-op remains a later pillar, but the single-player loop should contain the missing systems above before networking work begins.

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
