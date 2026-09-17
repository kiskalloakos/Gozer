# Nightshift — Game Direction

## One-sentence pitch

A nostalgic top-down pixel-art survival RPG where you build a life in a warm, persistent town, then risk each night outside its lights on tense loot-and-extract expeditions—alone or with friends.

## The player fantasy

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

## Setting and tone

An isolated town survives at the edge of a wilderness that becomes unnatural after dark. The wilderness could be an overgrown industrial exclusion zone, a cursed forest, or a collapsed underground world; choose one early and commit to it.

The important tonal rule: town life is genuinely comforting, not sarcastically cozy. The darkness outside earns its tension because players care about returning.

## Single-player implementation

The full core loop is designed for one player from the beginning.

- **No required AI party:** do not create AI companions for the first version. A solid lone-explorer loop is more valuable than unreliable helpers.
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
| Loot | All loot belongs to the player | Prefer individually-instanced basic loot; share major discoveries |
| Objectives | One player can complete every objective | Optional tasks reward splitting up or coordinating |
| Downed state | Injury, escape item, or costly retreat | Teammates can revive or carry a downed player |
| Extraction | Player chooses when to leave | Each player can leave with their haul, or the group chooses a shared extraction rule |
| Town | Personal persistent town | Join the host's town; visitors contribute resources and retain personal character progression |

Avoid forced betrayal roles. They are interesting only in multiplayer and undermine the solo experience.

## Shared tension system: contamination

The wilderness contains valuable **cursed / contaminated** items. Bringing them back may unlock powerful upgrades, unusual NPC events, or new areas—but also causes a town-level problem.

- In solo, the player chooses whether the reward is worth managing the consequence.
- In co-op, the same choice becomes a real group discussion.
- The result is visible: sick crops, power failures, strange visitors, altered town defenses, or new story branches.

This is the game's signature rule: **every expedition can change home.**

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
- Enemies should be identified by behavior as much as appearance: stalker, swarmer, noise-reactive hunter, armored guardian.
- Sound is gameplay: gunshots, broken glass, alarms, and extraction machinery can attract danger.

## Vertical slice: build this first

Do not start with an open world, full multiplayer, farming, or a large RPG story.

Build one playable 10–15 minute loop:

1. A small town hub with a workbench, storage, and three upgradeable buildings.
2. One compact, replayable expedition zone.
3. One player character, movement, aiming, and two satisfying weapons: revolver and shotgun.
4. Three resource tiers: common, valuable, and contaminated.
5. Two regular enemies plus one escalation enemy.
6. One physical extraction point with a short, dangerous activation window.
7. One permanent reward: a building upgrade, tool, or new route.

The proof-of-fun question is simple: **after a successful run, does the player immediately want to go back out for one more?**

## Scope guardrails

- Start 2D pixel art, not realistic 3D or an open world.
- Make solo feel excellent before networking.
- One biome and one town beat five shallow biomes.
- Prefer emergent stories from systems over hundreds of scripted quests.
- Ship a small demo around the central loop before adding broad crafting, romance, vehicles, or competitive modes.

## Near-term design decisions

1. Choose the wilderness theme: cursed forest, industrial exclusion zone, or underground ruins.
2. Define the extraction method and why it is dangerous.
3. Write the first five town upgrades and what each visibly changes.
4. Build movement, aiming, one enemy, one gun, and extraction before making pixel-art content at scale.
5. Playtest the vertical slice solo. Add co-op only when the solo loop is enjoyable without it.
