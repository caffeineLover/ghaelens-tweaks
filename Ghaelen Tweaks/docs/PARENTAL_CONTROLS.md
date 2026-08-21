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
| `pc-death-delay` | 15 | seconds | Base delay included in every death's respawn wait. |
| `pc-death-delay-increase` | 15 | seconds | Amount added for every currently accumulated death increase. |
| `pc-death-delay-cooldown` | 900 | seconds | Death-free time that removes one accumulated increase. |

The base and increase accept `0..3600`. The cooldown accepts `1..604800`. Turning off `pc-use-death-delay` immediately
releases active delays and prevents deaths from escalating the system while it is disabled.

### Escalation and recovery

1. Before processing a death, remove one accumulated increase for every complete cooldown period since the previous
   death.
2. Add one new increase for the current death.
3. Assign `base + (accumulated increases * increase)` seconds to the current death.
4. Restart the death-free cooldown window from that death.
5. Repeat cooldown recovery until the accumulated increases reach zero, leaving only the configured base for a future
   death.

With the defaults, consecutive deaths wait 30, 45, 60, and 75 seconds. After 15 death-free minutes, one increase is
removed. After 30 death-free minutes, two increases are removed. Death-free cooldown time uses UTC wall time and
continues while the player is offline.

### Enforcement and presentation

- The server persists the accumulated increase count, last-death time, and active respawn deadline in permanent player
  mod data.
- The current death uses the newly increased delay.
- A server Harmony prefix rejects respawn requests before vanilla consumes a temporal return-point use or begins
  teleporting.
- The server sends the remaining duration to the affected client over a one-way mod channel.
- The client reuses the vanilla death dialog's countdown line, disables the respawn button until the delay expires, and
  restores the vanilla revival text afterward.
- Reconnecting while dead resynchronizes the remaining deadline and does not bypass the wait.
- Config changes affect future death calculations. A delay already assigned to a death retains its original deadline.

### Acceptance criteria

- [x] The first death with default settings has a 30-second delay.
- [x] Each consecutive death adds another 15 seconds.
- [x] Every complete 900-second death-free period removes one accumulated 15-second increase.
- [x] Recovery repeats until no accumulated increases remain.
- [x] The server rejects early respawn packets even if the client UI is bypassed.
- [x] Active delay and escalation state survive reconnects and server restarts.
- [x] Config Lib exposes the master switch and all three numeric settings with their units, defaults, and ranges.

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
