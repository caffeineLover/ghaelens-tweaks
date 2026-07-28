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

### Barricades

Status: Implemented.

All looted/cluttered barricade variants are recyclable into aged recovered wood.

| Clutter type | Tool | Returned item |
| --- | --- | ---: |
| `game:clutter` with `type=barricade1..6` | Axe | 4 aged firewood |
| `game:clutter` with `type=barricade1..6` | Saw | 4 aged boards |

Design note: the saw gives clean board recovery, while the axe gives rough fuel recovery. Because these barricades are old salvaged clutter, use `game:agedfirewood` and `game:plank-aged` rather than fresh wood outputs.

Recipe note: the current recipes are in `Ghaelen Tweaks/assets/ghaelentweaks/recipes/grid/clutter-barricade-recycling.json`. The axe and saw ingredients set `toolDurabilityCost` to `0`, so the tools are kept and do not lose durability.

Fuel note: direct firepit fuel support is in `Ghaelen Tweaks/src/Patches/ClutterFuelPatches.cs`. Barricade clutter burns at 700 C for 24 seconds, matching aged firewood.
