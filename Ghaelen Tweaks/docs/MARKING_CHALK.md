# Marking Chalk

Status: Preset-mark implementation added; erase, paint-face, and temporal glow support added.

This document tracks the proposed marking chalk tweak for Ghaelen Tweaks.

## Goals

- Let players place clear navigation marks while mining, caving, and exploring.
- Use a dedicated item named `marking chalk`.
- Support marks on rock faces, floors, ceilings, and tree trunks.
- Support colored marking chalk made from vanilla dyes.
- Keep the first implementation based on preset glyphs, not freehand drawing or typed text.
- Give each marking chalk item a fixed number of uses.
- Expose marking chalk use count, dyeing batch size, and temporal glow strength through Ghaelen Tweaks configuration.
- Let players paint an entire valid block face when a compact symbol is not visible enough.
- Let players spend a temporal gear to make marks that emit a small colored glow.

## Non-goals

- Freehand drawing.
- Arbitrary player-entered text.
- Freeform large paintings or decorative art sets beyond navigation marks and simple full-face paint.
- Changing the underlying marked block.

## Player Workflow

- The player holds marking chalk and right-clicks a valid block face.
- The item places a thin decor mark on the selected face.
- The placed mark uses the same color as the held marking chalk.
- The underlying block remains unchanged.
- The mark should not occupy the adjacent block space and should not interfere with torches, ladders, supports, water, or mining.
- Crouch or hold Shift while right-clicking a chalk mark to remove it and refund its use cost to the active chalk stick.
- Select the erase tool mode and right-click a chalk mark for the same erase behavior without holding Shift.
- Select paint-face mode to color the entire clicked face. This costs 4 uses, or the whole stick when the configured
  use count is below 4.
- Erasing a full-face paint mark refunds the paint mark's use cost, capped at the configured maximum.
- Craft any marking chalk with a temporal gear to create temporal marking chalk of the same color. Placed temporal
  glyphs and paint marks emit a configurable colored dynamic glow matching the chalk color.
- The item is stackable while sticks are fresh. When a player first uses a stick from a stack, that stick splits into
  its own one-item stack and tracks only that stick's remaining uses.
- If the player inventory cannot accept the untouched remainder during that split, the remainder drops near the player.

Future direction: marking chalk should eventually behave like a prospecting pick with high-level tool modes. One mode
places preset navigation marks as described in this initial spec. A second mode will support freehand drawing.

## Valid Surfaces

Initial target surfaces:

- Stone and ore blocks.
- Brick and stone-brick blocks.
- Natural tree/log faces.
- Packed dirt.
- Rammed earth.

Implemented surface filtering is intentionally narrow:

- Any solid face whose material is `Stone`, `Ore`, or `Brick`.
- Solid trunk/log-like wood blocks with paths beginning `log-`, `logsection-`, `logquad-`, or `lognarrow-`.
- Solid prepared-soil blocks with paths beginning `packeddirt`, `drypackeddirt`, or `rammed-`.

Possible later surfaces:

- Wood planks and support beams.
- Ceramic or metal blocks, if the interaction feels useful and not visually noisy.

## Colors

Support all vanilla dye colors that exist as liquid `dye-*` items in Vintage Story 1.22.3:

- `black`
- `blue`
- `gray`
- `green`
- `orange`
- `pink`
- `purple`
- `red`
- `white`
- `yellow`

Treat `dye-woad` as a blue dye source.

Vintage Story distinguishes `dye-blue` and `dye-woad` as separate liquid item variants. `dye-blue` is made from
lapis lazuli or cornflower, while `dye-woad` is made from woad flowers and is labeled "Woad blue dye". Marking chalk
should not create a separate woad chalk color unless there is a strong visual reason; `dye-woad` should output blue
marking chalk.

Using every dye is mechanically straightforward once item, decor, and texture variants exist. The non-trivial work is
asset coverage and quality control: every supported color needs readable mark textures across dark stone, light stone,
ores, and tree bark.

## Dyeing

Marking chalk can be colored in either of two ways.

Plain marking chalk is crafted from vanilla `game:stone-chalk`; the initial recipe turns one chalk stone into four
plain marking chalk sticks.

### Barrel Dyeing

Use a barrel recipe:

- Input: `1 L` of `dye-{color}` plus plain marking chalk.
- Output: marking chalk of the matching color.
- `dye-woad` outputs blue marking chalk.

Default batch size: `16` plain marking chalk plus `1 L` dye producing `16` colored marking chalk. The batch size is
configurable through `marking-chalk-dye-batch-size`.

### Bowl Crafting

Use a grid recipe with a fired bowl containing dye:

- Input: a fired bowl containing at least `1 L` of `dye-{color}` plus plain marking chalk.
- Output: marking chalk of the matching color.
- The recipe consumes `1 L` of dye and leaves the bowl.
- `dye-woad` outputs blue marking chalk.
- Use the configured batch size for the number of plain marking chalk pieces consumed and colored marking chalk pieces
  produced.

Vintage Story grid recipes can require liquid content through `liquidContainerProps.requiresContent` and
`liquidContainerProps.requiresLitres`. The implemented bowl route uses explicit recipes per dye color because the dye
color is stored as bowl liquid content rather than as a wildcard grid ingredient.

## Symbols

Initial glyph set:

- Arrow up.
- Arrow right.
- Arrow down.
- Arrow left.
- X.
- Dot.
- Ladder.
- Stairs.
- Danger.
- Exit marker.
- Full-face paint.

The tool-mode selector also includes an erase mode. Erase mode and Shift/crouch right-click both remove nearby marking
chalk decor from the clicked face without affecting vanilla cave art or other decor. Erasing refunds the removed mark's
use cost to the active chalk stick, capped at the configured maximum.

## Orientation

Marks must support walls, floors, and ceilings.

Arrow selection is explicit. The tool-mode selector exposes arrow up, arrow right, arrow down, and arrow left as
separate marks.

Wall arrow marks use the selected direction directly on the wall face. Floor and ceiling arrow marks use the selected
direction relative to the player's facing at placement time: arrow up means forward, arrow right means the player's
right, arrow down means backward, and arrow left means the player's left.

Non-directional symbols such as X, dot, ladder, stairs, danger, and exit avoid orientation ambiguity and should work on
all valid faces. Full-face paint uses the clicked face directly and has no orientation.

## Technical Direction

Vintage Story already has the pieces this feature needs:

- `IBlockAccessor.SetDecor(...)` attaches decor to an existing block face.
- `BlockSelection.ToDecorIndex()` supports sub-face decor placement.
- Vanilla cave art and overlay blocks use the `Decor` behavior, `drawtype: surfacelayer`, and `renderpass: Decor`.
- Vanilla `CollectibleBehaviorArtPigment` already places decor from pigment-like items, checks claims, checks valid
  block materials, consumes pigment by chance, exposes tool modes, and plays the chalk drawing sound.

Initial implementation:

- Add a marking chalk item family with color variants.
- Add a marking decor block family with color, column, and row variants matching vanilla cave-art surfacelayer cells.
- Add a separate full-face paint decor family without column and row variants. It uses `new DecorBits(blockSel.Face)`
  instead of `BlockSelection.ToDecorIndex()` so it fills the clicked face as decor without replacing the block.
- Add a temporal marking chalk item family. Temporal glyph and paint decor use the same visible art plus glow vertex
  flags, while a hidden `markingchalklight` entity supplies persistent configurable colored dynamic light because
  `SetDecor(...)` marks chunk decor dirty but does not reliably run the normal block-light placement path.
- The temporal chalk glow is for visibility, not spawn control. Lore creature spawn checks read
  `BlockAccessor.GetLightLevel(...)`; they compare against `MaxLightLevel` inclusively, so a real light level of 2
  would still allow a spawn rule with `maxLightLevel: 2`.
- Default each marking chalk item to `32` uses, configurable through `marking-chalk-uses`.
- Default dyeing recipes to a batch size of `16`, configurable through `marking-chalk-dye-batch-size`.
- Default temporal marking chalk dynamic glow to light level `3`, configurable from `0` to `32` through
  `temporal-marking-chalk-light-level`.
- Use custom C# item behavior rather than vanilla `ArtPigment` so the item can choose a color-specific decor block,
  track fixed uses, restrict surfaces to the agreed list, and reserve room for a future freehand mode.
- Keep fresh marking chalk stackable, but split a partially used active stick away from the fresh remainder so the
  remaining-use attribute never appears to apply to every stick in the original stack.
- Adjust resolved grid and barrel dye recipe quantities at runtime so the batch size config affects both dyeing routes.
- Store mark art as one 96x96 spritesheet per color and resolve tool modes to `col`/`row` decor variants. Vintage
  Story's `surfacelayer` decor path expects this cell-based format; standalone per-symbol textures rendered as filled
  squares during the first in-game test.
- Map right and left arrow modes to the opposite-looking source cells. In-game surfacelayer rendering mirrored
  horizontal arrows on tested wall faces, so the swapped mapping makes the placed mark match the selected tool icon.
- For floor and ceiling arrow marks, use the up-arrow spritesheet cell and set `DecorBits.Rotation` from the player's
  horizontal eye-to-hit vector. Vintage Story's top and bottom face UV axes differ, so the rotation mapping is
  face-specific. Fall back to the opposite of `BlockFacing.HorizontalFromYaw(...)` only for near-vertical clicks whose
  horizontal camera-ray projection is too small to classify.
- Vintage Story Reference `1.22.3` shows `BlockFacing.HorizontalAngleIndex` uses east `0`, north `1`, west `2`, and
  south `3`; `DecorBits.Rotation` is passed to `SurfaceLayerTesselator` through `vars.decorRotationData`. For top and
  bottom surfacelayer arrows, convert desired world direction with `GameMath.Mod(1 - HorizontalAngleIndex, 4)`. The
  rejected `HorizontalAngleIndex + 1` formula is identical for east and west but swaps north and south, which is why
  right/left could test correctly while up/down remained reversed.
- When placing a rotated floor or ceiling arrow, remove older Ghaelen Tweaks chalk decor in the same face subcell with a
  different rotation so redraws replace the old mark instead of stacking on top of it.
- Use the same exact sub-face decor index path for erasing that placement uses for drawing. Erasing searches nearby
  subcells on the clicked face, removes only `ghaelentweaks:markingchalk-*` decor so players do not have to hit the
  original placement cell perfectly, and refunds the removed mark's use cost capped at the configured maximum. If no
  nearby sub-face glyph is found, erasing falls back to full-face paint decor on the clicked face.

The implemented baseline remains preset-mark and decor based. Freehand mode is intentionally deferred.

## Freehand Drawing Assessment

Freehand drawing has two possible interpretations.

### Grid-stamped freehand

This would sample the player's held right-click movement across a block face and place small dot or stroke decor marks
at decor subpositions. Vintage Story already exposes `BlockSelection.ToDecorIndex()`, which maps a hit position to a
16x16 sub-face decor grid. This makes rough freehand marks technically possible without custom mesh storage.

Expected difficulty: moderate to high.

Main risks:

- Many small decors per drawing can increase chunk decor storage, network updates, and render work.
- Stroke sampling must avoid placing duplicate decor cells every interaction tick.
- Dragging across block boundaries, edges, floors, ceilings, and wall faces needs careful coordinate handling.
- Multiplayer prediction and server authority need to feel responsive without letting the client spam block updates.
- Undo/erasing becomes more important because freehand mistakes are common.

### True smooth freehand

This would store vector strokes or bitmap data, synchronize them to clients, and render custom geometry or dynamic
textures on block faces.

Expected difficulty: high.

This is a different feature from decor-based preset marks and should not be part of the first marking chalk version.

## Open Questions

- Whether black marking chalk should also be craftable directly from charcoal in addition to dyeing with `dye-black`.
