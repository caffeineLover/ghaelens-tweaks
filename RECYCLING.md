# Recycling Tweaks

This document tracks recycle, reuse, and repurpose changes for Ghaelen Tweaks.

## Goals

- Give underused or awkward leftover blocks a practical recovery path.
- Keep recovery values understandable from the original material cost and the tool used.
- Prefer cleaner recovery with saws and rougher recovery with axes when the distinction matters.

## Items

### Palisades

Status: Implemented.

Palisades can be broken down into fuel with an axe or recovered as boards with a saw.

| Block family | Tool | Returned item |
| --- | --- | ---: |
| `game:palisadewall-three-*` | Axe | 3 firewood |
| `game:palisadewall-three-*` | Saw | 3 oak boards |
| `game:palisadewall-four-*` | Axe | 4 firewood |
| `game:palisadewall-four-*` | Saw | 4 oak boards |
| `game:palisadestakes-*` | Axe | 1 firewood |
| `game:palisadestakes-*` | Saw | 1 oak board |

Breaking palisades with other tools uses normal drop behavior.

Implementation note: the current drop logic is in `Ghaelen Tweaks/BlockBehaviorPalisadeFirewoodDrops.cs`, applied by `Ghaelen Tweaks/assets/survival/patches/palisades.json`. Vanilla palisades are generic wood blocks, so saw recovery uses `game:plank-oak`.

### Barricades

Status: Implemented.

All looted/cluttered barricade variants should be recyclable into aged recovered wood.

| Block family | Tool | Returned item |
| --- | --- | ---: |
| `game:clutter-barricade*` | Axe | 4 aged firewood |
| `game:clutter-barricade*` | Saw | 4 aged oak boards |

Design note: the saw gives clean board recovery, while the axe gives rough partial fuel recovery. Because these barricades are old salvaged clutter, use `game:agedfirewood` and `game:plank-aged` rather than fresh wood outputs.

Implementation note: the current drop logic is in `Ghaelen Tweaks/BlockBehaviorBarricadeRecyclingDrops.cs`, applied by `Ghaelen Tweaks/assets/survival/patches/clutter-barricades.json`.
