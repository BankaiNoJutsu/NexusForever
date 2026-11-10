# Primal Matrix: allocation rules read from the client and the tables (2026-11-10)

Status: **Mapped only. Not implemented.** The save request (`ClientPrimalMatrixSave`) is still decoded and dropped, and no snapshot is sent. This note removes the blocker recorded on
that handler ("authoritative allocation rules are required") for everything except the items listed in section 5. No retail parity claim.

Evidence tier per section: native bodies (2, 3), raw tables (1, 4), launch-era press coverage as an independent description (4).

## 1. Layout (tables)
`PrimalMatrixNode` has 136 rows. `positionX` and `positionY` are signed (-9 to 9) axial coordinates on a hexagonal lattice: a node's six neighbours are at
`(+1, 0)`, `(-1, 0)`, `(0, +1)`, `(0, -1)`, `(+1, -1)` and `(-1, +1)`.

The other diagonal pair was tried and rejected by the table itself: with the pair above every one of the 16 nodes of type 1 has exactly six neighbours, all of type 0 (the "cluster of
six around a centre" the launch coverage describes), and all 136 nodes are reachable from the start node; with `(+1, +1)` / `(-1, -1)` the type 1 nodes have four neighbours and only 80
nodes are reachable.

| Node type | Rows | Max allocations | Cost per allocation |
|---|---:|---|---|
| 0 (colour node) | 96 | 5 (72 rows), 3 (18), 1 (6) | one of red, blue or green: 10000, 7500 or 5000 |
| 1 (cluster centre) | 16 | 1 | none |
| 2 (bridge) | 24 | 1 | purple: 10000, 7500 or 5000 |

Row 1 at `(0, 0)` is the only row with `flags` bit 0.

## 2. When a node can be allocated (native)
`1406e4fc0` computes the state the UI gives a node, in this order:

1. allocations at or above `maxAllocations` (row `+0x14`): state 3, full;
2. any allocation: state 2;
3. row `flags` bit 0 (row `+0x10`): state 1, available. This is the start node;
4. otherwise state 1 if **any of the six neighbours is in state 2 or 3**, else state 0, locked.

So a node is open when it is the start node or a neighbour holds at least one allocation. The neighbour does not have to be full. `PrimalMatrix_HandleNodeAllocationInput`
(`1406e73e0`) only acts on a node in state 1 or 2 and refreshes the six neighbours after a change.

## 3. Cost (native)
`1406e4e00` allows one more allocation when, for each of the four cost columns that is non-zero, the pending total plus the node's cost does not exceed the player's balance. The columns
sit at row `+0x18`, `+0x1c`, `+0x20` and `+0x24` (red, blue, green, purple in table order) and are compared with four 64-bit balances at game `+0x1648`, `+0x1650`, `+0x1658` and
`+0x1660`. The cost is per allocation: the handler adds `cost * (new count - old count)` to the pending totals.

The request (`140523a60`, opcode `0x72`) carries node ids and the increments made since the last save. The snapshot consumer (`1404d6af0`) first clears every node's saved count, then
sets the count of each listed node, so a snapshot must always list every allocated node. Its last word is stored at game `+0x6e68` and raises `CharacterEldanAugmentationsUpdated` when it
changes: it is the AMP power the matrix contributes.

## 4. Rewards (tables)
Each node names one `PrimalMatrixReward` row per class. A reward row has up to four typed slots:

| Type | Rows (slot 0) | Data | Reading |
|---:|---:|---|---|
| 5 | 36 | `objectId` = a Property id, `value` 10 to 30 | adds to that property. Slot 1 of nearly every row is type 5, property 153 (Heroism), value 25; the launch coverage says unlocking nodes raises a Heroism rating |
| 4 | 36 | `objectId` = a Spell4 id (`87947` to `88219`) | grants that spell |
| 2 | 1 | `value` 1 | AMP power or an ability point; the coverage says bridge nodes grant "AMP Points, Ability Points" |
| 3 | 1 | `value` 1 | the other of the two |

By node type (Warrior column; the other classes have the same shape): the 96 colour nodes all carry a type 5 reward; the 24 bridge nodes carry type 2 (9 nodes), type 3 (9) or
type 4 (6); the 16 cluster centres carry no reward row at all.

## 5. Still unknown
* Which of reward types 2 and 3 is AMP power and which is the ability point.
* Whether a type 5 value is granted once per allocation or once per node (nodes with five allocations carry the same 30 as nodes with one).
* Whether a cluster centre (type 1, free) needs all six surrounding nodes full; the UI rule in section 2 only asks for one allocated neighbour, and the coverage describes completing a
  cluster before moving on.
* What the retired server validated. The rules in sections 2 and 3 are the client's own; a server would be expected to enforce the same, but that is an assumption.
* Refund and reset behaviour (the UI has a `NodeDeallocationPrevented` event; no reset packet was looked for).

## 6. What an implementation needs
Per-character storage of node id and count (a new table in both the MySQL and the SQLite migration sets), the validation in sections 1 to 3 against the account's four essence balances,
a snapshot on login and after each save, and the rewards of section 4. The prerequisite check for type 292 already compares a count and only needs the stored value.
