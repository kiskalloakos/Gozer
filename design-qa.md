# Player Home Gold Storage — Design QA

- Source visual truth: `Assets/Art/Environment/chest.png` and `Assets/Art/Environment/inventory.png`
- Implementation screenshot: `Artifacts/home-storage-gold-open.png`
- Combined comparison evidence: `Artifacts/home-storage-gold-comparison.png`
- Viewport: Unity Game view, 843 × 474 px, 16:9
- Source dimensions: chest sheet 32 × 128 px (four 32 × 32 cells); inventory canvas 110 × 100 px with a 77 × 77 visible panel
- Implementation dimensions: 843 × 474 native screenshot; popup drawn at an integer scale derived from the 320 × 180 project reference frame
- Density normalization: the comparison board enlarges the chest sheet 4× and inventory canvas 2× with nearest-neighbor scaling; the implementation remains at its native captured density
- State: Player Home, chest fully open, inventory popup and bright player inventory visible, modal backdrop active, Gold in player slot 1

## Findings

No actionable P0, P1, or P2 differences remain.

- Fonts and typography: the Gold stack is explicitly labeled `GOLD`; its count and label remain readable in both the 50 px player slots and smaller chest cells.
- Spacing and layout rhythm: the chest sits immediately beside the workbench without overlapping it. The inventory art is centered, while the existing six-slot player inventory is redrawn at its normal bottom position above the dim layer.
- Colors and visual tokens: the supplied chest and inventory pixels are used directly. The neutral black backdrop is applied at 72% opacity and preserves the warm room palette while clearly separating the modal state.
- Image quality and asset fidelity: both supplied PNGs use Point filtering, no mipmaps, no compression, and integer scaling. The four chest frames use a shared bottom-center pivot, so the opening motion does not jump.
- Copy and content: all player-facing currency terminology is now `Gold`; workbench, infirmary, extraction, pickup, and status messages no longer call it supplies.

## Interaction verification

- Entered Play Mode in the real `HomeInterior` scene.
- Opened the chest from interaction range and observed the complete forward animation.
- Verified that the inventory popup appears after the final open frame.
- Verified the room, player, and persistent HUD are all dimmed behind the popup.
- Verified the active six-slot player inventory remains bright and interactive above the dim layer.
- Dragged the full Gold stack from player inventory to a chest cell.
- Dragged Gold between two different chest cells.
- Dragged Gold back into a different player-inventory slot, then restored it to slot 1.
- Verified that moving the stack changes only its saved container and slot, not the spendable Gold balance.
- Verified clicking outside the popup closes it and plays the animation in reverse.
- Verified the scene returns to the closed chest state.
- Checked the Unity Console after the run: no errors.
- Ran `RPG/Tests/Validate Home Storage Chest`: passed.
- Ran `RPG/Tests/Validate Gold Storage`: passed, including spending Gold while its stack remains in chest slot 8 and restoring the user's saved values afterward.

## Full-view comparison evidence

`Artifacts/home-storage-gold-comparison.png` places the enlarged source assets above the native gameplay capture. The chest palette, frame sequence, inventory grid, border thickness, and pixel edges match the supplied files; the implementation evidence also shows the undimmed player inventory and labeled Gold stack.

## Focused region comparison evidence

The combined comparison board already enlarges both source assets enough to inspect individual pixels, while the implementation capture shows the popup at readable gameplay size. A separate crop was not needed.

## Comparison history

1. Initial placement at x = -4.35 left a visibly loose gap from the workbench (P2 placement issue).
2. Moved the chest to x = -5.15, retained clear silhouettes for both objects, and recaptured the open state.
3. The post-fix evidence in `Artifacts/home-storage-gold-open.png` shows the chest reading as part of the workbench area with no overlap.
4. Added the interactive player-inventory overlay and persisted Gold container/slot state; Play Mode drag testing found no remaining P0/P1/P2 issue.

## Follow-up polish

No P3 fidelity issues remain for the requested scope.

final result: passed
