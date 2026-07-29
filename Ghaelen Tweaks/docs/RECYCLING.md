# Recycling Tweaks

This document tracks reuse and repurpose changes for Ghaelen Tweaks.

## Goals

- Give underused or awkward leftover objects a practical recovery path.
- Keep recovery values understandable from the original material cost and the tool used.
- Prefer cleaner recovery with saws and rougher recovery with axes when the distinction matters.

## Items

### Palisades

Status: Implemented.

Palisades can be dismantled into fuel with an axe or recovered as boards with a saw.

| Block family | Tool | Returned item |
| --- | --- | ---: |
| `game:palisadewall-three-*` | Axe | 3 firewood |
| `game:palisadewall-three-*` | Saw | 3 oak boards |
| `game:palisadewall-four-*` | Axe | 4 firewood |
| `game:palisadewall-four-*` | Saw | 4 oak boards |
| `game:palisadestakes-*` | Axe | 1 firewood |
| `game:palisadestakes-*` | Saw | 1 oak board |

Breaking palisades with other tools uses normal drop behavior.

Implementation note: the current drop logic is in `Ghaelen Tweaks/src/BlockBehaviors/BlockBehaviorPalisadeFirewoodDrops.cs`, applied by `Ghaelen Tweaks/assets/survival/patches/palisades.json`. Vanilla palisades are generic wood blocks, so saw recovery uses `game:plank-oak`.

### Wooden Clutter

Status: Implemented.

Selected wooden clutter variants are recyclable into aged recovered wood and
burn directly as firepit fuel.

| Clutter type | Axe output | Saw output | Firepit fuel |
| --- | ---: | ---: | ---: |
| `game:clutter` with `type=barricade1..6` | 4 aged firewood | 4 aged boards | 700 C for 96 seconds |
| `game:clutter` with `type=rubble-wood1..4` | 2 aged firewood | 2 aged boards | 700 C for 48 seconds |
| `game:clutter` with `type=table-ruined1..6` | 4 aged firewood | 4 aged boards | 700 C for 96 seconds |
| `game:clutter` with `type=crate/crate-small-stacked` | 6 aged firewood | 6 aged boards | 700 C for 144 seconds |
| `game:clutter` with `type=crate/crate-large-rot` | 6 aged firewood plus 32 rot | 6 aged boards plus 32 rot | 700 C for 144 seconds |
| `game:clutter` with `type=chestrubble` | 3 aged firewood | 3 aged boards | 700 C for 72 seconds |

Design note: the saw gives clean board recovery, while the axe gives rough fuel
recovery. Because these are old salvaged clutter, use `game:agedfirewood` and
`game:plank-aged` rather than fresh wood outputs.

Recipe note: the current recipes are in `Ghaelen Tweaks/assets/ghaelentweaks/recipes/grid/clutter-barricade-recycling.json`
and `Ghaelen Tweaks/assets/ghaelentweaks/recipes/grid/clutter-wood-recycling.json`.
The axe and saw ingredients set `toolDurabilityCost` to `0`, so the tools are
kept and do not lose durability.

Returned-stack note: `crate/crate-large-rot` returns rot through the consumed
clutter ingredient's `returnedStack`. The aged wood remains the visible crafting
output, while the 32 rot is placed into the player's inventory or dropped nearby
if inventory space is unavailable.

Fuel note: direct firepit fuel support is in
`Ghaelen Tweaks/src/Patches/ClutterFuelPatches.cs`. Burn temperature matches
aged firewood, and burn duration scales from the number of aged firewood the
same clutter item recovers with an axe.
