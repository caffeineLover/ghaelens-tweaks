# Marking Chalk

Status: Initial preset-mark implementation added.

This document tracks the proposed marking chalk tweak for Ghaelen Tweaks.

## Goals

- Let players place clear navigation marks while mining, caving, and exploring.
- Use a dedicated item named `marking chalk`.
- Support marks on rock faces, floors, ceilings, and tree trunks.
- Support colored marking chalk made from vanilla dyes.
- Keep the first implementation based on preset glyphs, not freehand drawing or typed text.
- Give each marking chalk item a fixed number of uses.
- Expose marking chalk use count and dyeing batch size through Ghaelen Tweaks configuration.

## Non-goals

- Freehand drawing.
- Arbitrary player-entered text.
- Large paintings or decorative art sets beyond compact navigation marks.
- Changing the underlying marked block.

## Player Workflow

- The player holds marking chalk and right-clicks a valid block face.
- The item places a thin decor mark on the selected face.
- The placed mark uses the same color as the held marking chalk.
- The underlying block remains unchanged.
- The mark should not occupy the adjacent block space and should not interfere with torches, ladders, supports, water, or mining.
- Existing decor-breaking behavior should remove marks when appropriate.
- The item is stackable. A stack tracks the currently active stick's remaining uses; when that stick is exhausted, the
  stack loses one item and the next stick starts fresh.

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

- Arrow.
- X.
- Dot.
- Ladder.
- Stairs.
- Danger.
- Exit marker.

## Orientation

Marks must support walls, floors, and ceilings.

For floor and ceiling arrows, the arrow should point in a world direction based on the player's facing direction at
placement time. This avoids ambiguous wall-local labels such as left or right.

Wall, floor, and ceiling arrows all use the same player-facing world-direction model for the initial implementation.

Non-directional symbols such as X, dot, ladder, stairs, danger, and home avoid orientation ambiguity and should work on
all valid faces.

## Technical Direction

Vintage Story already has the pieces this feature needs:

- `IBlockAccessor.SetDecor(...)` attaches decor to an existing block face.
- `BlockSelection.ToDecorIndex()` supports sub-face decor placement.
- Vanilla cave art and overlay blocks use the `Decor` behavior, `drawtype: surfacelayer`, and `renderpass: Decor`.
- Vanilla `CollectibleBehaviorArtPigment` already places decor from pigment-like items, checks claims, checks valid
  block materials, consumes pigment by chance, exposes tool modes, and plays the chalk drawing sound.

Initial implementation:

- Add a marking chalk item family with color variants.
- Add a marking decor block family with color and symbol variants.
- Default each marking chalk item to `32` uses, configurable through `marking-chalk-uses`.
- Default dyeing recipes to a batch size of `16`, configurable through `marking-chalk-dye-batch-size`.
- Use custom C# item behavior rather than vanilla `ArtPigment` so the item can choose a color-specific decor block,
  track fixed uses, restrict surfaces to the agreed list, and reserve room for a future freehand mode.
- Adjust resolved grid and barrel dye recipe quantities at runtime so the batch size config affects both dyeing routes.

The first implementation should include only the preset-mark behavior. Freehand mode is intentionally deferred.

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

- Whether marks should be removable by hand, knife, water, block breaking, or only normal decor breaking.
- Whether black marking chalk should also be craftable directly from charcoal in addition to dyeing with `dye-black`.
- Whether partially used chalk stacks need stricter split/merge behavior. The first implementation tracks uses on the
  active stick in the stack, which keeps dyed chalk stackable but means a manually split partially used stack can copy
  the active-stick use count.
