# Parental Controls Specification

## Goal

Add optional, recoverable consequences that discourage repeated player deaths without permanently destroying valuable
equipment. Consequences should become more onerous when deaths are frequent and should relax again after death-free
play.

## Shared design rules

- Track consequences per player rather than changing an entire world's difficulty.
- Escalate from recent deaths, not an irreversible lifetime death count.
- Give the affected player a clear explanation of the current consequence and how it recovers.
- Prefer inconvenience, maintenance, retrieval, or temporary restrictions over permanent loss.
- Avoid penalties that directly make another death much more likely.
- Store authoritative state on the server and persist it by player UID.
- Allow configuration through the normal mod config and optional Config Lib integration.
- Provide an administrative status, adjustment, and pardon path before enabling several systems together.

## Feature backlog

- [x] **Escalating respawn delay** — Add a server-enforced delay that increases after each death and decays after
  repeated death-free cooldown periods.
- [ ] **Tool insurance deductible** — Retain tools and armor but remove progressively more durability, never reducing
  an item below one durability point.
- [ ] **Tool time-out** — Move progressively more tools to secure storage for a temporary lockout instead of dropping
  or destroying them.
- [ ] **Selective item drops** — Escalate from dropping food, to loose resources, to backpack contents while preserving
  configured protected equipment categories.
- [ ] **Death debt** — Charge an escalating recoverable debt payable with configured food, fuel, metal, or temporal
  resources.
- [ ] **Recovery chores** — Require one or more safe preparation tasks such as eating a proper meal, sleeping, repairing
  armor, or restocking medical supplies.
- [ ] **Respawn sickness** — Apply configurable, temporary post-respawn inconvenience without reducing combat
  survivability enough to create a death spiral.
- [ ] **Backpack probation** — Temporarily lock an increasing number of backpack slots until the player completes
  death-free play or recovery work.
- [ ] **Safe-zone probation** — Require regrouping near a configured home or spawn area before another expedition.
- [ ] **Recovery chest** — Send protected equipment to secure home storage and require time, payment, or chores to
  unlock it.
- [ ] **Administration** — Add commands to enroll players, inspect scores and active penalties, adjust state, pardon a
  bug-related death, and opt individual systems in or out.
- [ ] **Cause weighting** — Optionally escalate repeated preventable causes such as falling or drowning differently
  from configured exceptional encounters.

## Implemented: escalating respawn delay

### Configuration

| Setting | Default | Unit | Meaning |
|---|---:|---|---|
| `pc-use-death-delay` | true | boolean | Master switch for escalating respawn delays. |
| `pc-spawn-delay-increment` | 20 | seconds | Delay added for each counted death beyond the free first death. |
| `pc-spawn-delay-cooldown` | 900 | seconds | Death-free time that removes one counted death. |
| `pc-exempt-root` | true | boolean | Exempt root, operator, and custom privileged roles. |
| `pc-exempt-by-username` | empty | comma-separated usernames | Exempt named players, ignoring letter case. |

The increment accepts `0..3600`. The cooldown accepts `1..604800`. Turning off `pc-use-death-delay` immediately releases
active delays and prevents deaths from increasing the count while it is disabled.
Exempt players do not accrue counted deaths and are released from any active delay when their exemption applies.

### Escalation and recovery

1. Before processing a death, subtract one from the counted deaths for every complete cooldown period since the previous
   death, stopping at zero.
2. Add one counted death for the current death.
3. Assign `max(0, counted deaths - 1) * increment` seconds to the current death.
4. Restart the death-free cooldown window from that death.
5. Repeat cooldown recovery until the counted deaths reach zero.

With the defaults, consecutive deaths wait 0, 20, 40, and 60 seconds. After 15 death-free minutes, one counted death is
removed. After 30 death-free minutes, two counted deaths are removed. Death-free cooldown time uses UTC wall time and
continues while the player is offline.

### Enforcement and presentation

- The server persists the counted deaths, last-death time, and active respawn deadline in permanent player
  mod data.
- The first counted death is free; the current death otherwise uses the newly increased count.
- A server Harmony prefix rejects respawn requests before vanilla consumes a temporal return-point use or begins
  teleporting.
- A client Harmony prefix prevents an early click from latching vanilla's `respawning` state, and the countdown tick
  clears that stale state after any server rejection so the button reliably unlocks when the delay expires.
- The server sends the remaining duration and next scheduled delay to the affected client over a one-way mod channel.
- The client reuses the vanilla death dialog's countdown line, disables the respawn button until the delay expires, and
  shows the scheduled delay for one more death beneath the current countdown or vanilla revival text.
- Reconnecting while dead resynchronizes the remaining deadline and does not bypass the wait.
- Config changes affect future death calculations. A delay already assigned to a death retains its original deadline.

### Acceptance criteria

- [x] The first counted death has no respawn delay.
- [x] Each consecutive death adds another 20 seconds with default settings.
- [x] Every complete 900-second death-free period removes one counted death.
- [x] Recovery repeats until the counted deaths reach zero.
- [x] The server rejects early respawn packets even if the client UI is bypassed.
- [x] An early rejected respawn attempt does not leave the death dialog permanently disabled.
- [x] The death dialog shows the delay one more death would receive at the player's current count.
- [x] Active delay and escalation state survive reconnects and server restarts.
- [x] Config Lib exposes the master switch and both numeric settings with their units, defaults, and ranges.

## Draft: escalating respawn sickness

### Proposed movement-speed model

Respawn sickness should use its own per-player stack count rather than sharing the death-delay count. On each death,
expired sickness stacks are removed and one new stack is added. While at least one stack remains, the movement penalty
is:

`base debuff + (current stacks * debuff increase)`, capped by the configured maximum debuff.

Each completed sickness cooldown removes one stack. Removing the final stack ends sickness and removes the stat
modifier completely. With the recommended defaults below, consecutive deaths produce 10%, 15%, 20%, and 25% movement
penalties. One stack recovers after each 15-minute death-free cooldown.

Vintage Story already blends player movement through the `walkspeed` entity stat. The implementation can add one
named negative modifier through that API, allowing it to combine normally with armor, class, and other mod modifiers
without patching movement physics.

### Proposed configuration

| Setting | Recommended default | Unit | Meaning |
|---|---:|---|---|
| `pc-use-respawn-sickness` | false | boolean | Master switch; already exposed in Config Lib. |
| `pc-respawn-sickness-movement-debuff` | 5 | percent | Base movement penalty while sickness is active. |
| `pc-respawn-sickness-movement-debuff-increase` | 5 | percent | Additional penalty added by every current stack. |
| `pc-respawn-sickness-movement-debuff-maximum` | 40 | percent | Safety cap on the penalty contributed by this mod. |
| `pc-respawn-sickness-cooldown` | 900 | seconds | Death-free time that removes one sickness stack. |

The three movement values should use whole percentages in configuration and convert to the negative `walkspeed` stat
value internally. A maximum debuff is preferable to a minimum final speed because it limits only this mod's penalty
without overriding armor, classes, or another mod's movement effects.

### Additional effects considered

- Mining-speed reduction fits the recovery theme and is less likely than combat penalties to cause another death.
- Faster hunger drain is noticeable but can turn resource scarcity into a death spiral.
- Reduced healing or maximum health directly increases the chance of another death and is not recommended.
- Disabling sprint at a high stack count is clear but feels abrupt compared with the smooth movement progression.
- Visual effects, stagger, or camera distortion communicate sickness but may create accessibility problems.

### Decision still required

- Decide whether sickness cooldown counts real wall time, including offline time like death delay, or only time actively
  connected and alive. Active-alive time better rewards survival; wall time is simpler and lets a player recover by
  taking a break.
