# VGRoid flat-spawn forensics

The VGRoid sometimes generates with no rock at all, just floor tiles. Two reported occurrences, both confirmed flat from their replays, with the affected grid identified by name:

| round     | server | reported  | VGRoid grid     | `IronRock` |
| --------- | ------ | --------- | --------------- | ---------: |
| **15201** | beta   | first     | `Buccinum-90-E` |          0 |
| **15646** | alpha  | follow-up | `Echinus-53-Z`  |          0 |

Names are decoded out of each replay's recorded server state (the client OOMs loading the 155-player round). The decode was validated end to end on local rounds where the name was read off the mass scanner in-game first.

## The reports

> **[FG] Vgroid spawned in completly flat.**
> No structure or rock just tiles. Its just a barren flatland
> Station: Bagel Station

|            |                                            |
| ---------- | ------------------------------------------ |
| server     | beta                                       |
| round      | 15201                                      |
| players    | 42                                         |
| build      | `b51b35f8cc5819c322b1c245c0ff90dd55c615f0` |
| engine     | 277.0.0                                    |
| round time | 00:14:56                                   |
| round type | Secret                                     |
| map        | Bagel Station                              |
| posted     | 07/06/2026 00:21                           |

[report](https://discord.com/channels/1272545509562777621/1512853913206915265/1512853913206915265)

> **[FG] new VGRoid glitch**
> it just deicded to not spawn with ANY rocks....

|            |                                            |
| ---------- | ------------------------------------------ |
| server     | alpha                                      |
| round      | 15646                                      |
| players    | 155                                        |
| build      | `d09f18644af606d153e38e2282ee1bc1a9a86822` |
| engine     | 277.0.0                                    |
| round time | 00:22:35                                   |
| round type | Secret                                     |
| map        | Cog                                        |
| posted     | 08/06/2026 01:08                           |

[report](https://discord.com/channels/1272545509562777621/1513228274837225656/1513228274837225656)

## How the name is derived

The VGRoid is named via `GetFTLName(NamesBorer, seed)`, which produces the shape `<BorerName>-<10..99>-<A..Z>`:

```csharp
// Content.Shared/Salvage/SharedSalvageSystem.cs
public string GetFTLName(LocalizedDatasetPrototype dataset, int seed)
{
    var random = new System.Random(seed);
    return $"{Loc.GetString(dataset.Values[random.Next(dataset.Values.Count)])}-{random.Next(10, 100)}-{(char) (65 + random.Next(26))}";
}
```

These strings are runtime-built, so they are not in the mapped-string pool. They land in state inline (unmapped), which makes a literal scan for that shape reliable.

`NamesBorer` has three consumers at round start: gateway gen, expeditions, vgroid. Expeditions store a seed and render the name on demand, contributing zero name strings to state. The vgroid is named once, on the grid's MetaData:

```csharp
// Content.Server/Shuttles/Systems/ShuttleSystem.GridFill.cs
if (_protoManager.Resolve(group.Value.NameDataset, out var dataset))
    _metadata.SetEntityName(spawned, _salvage.GetFTLName(dataset, _random.Next()));
```

The gateway generator spawns exactly 3, and writes each name **twice**, once on the destination map's MetaData and once on the gateway's `DestinationName` markup:

```csharp
// Content.Server/Gateway/Systems/GatewayGeneratorSystem.cs
private static readonly ProtoId<LocalizedDatasetPrototype> PlanetNames = "NamesBorer";
...
for (var i = 0; i < 3; i++)
    GenerateDestination(uid, generator);
...
var gatewayName = _salvage.GetFTLName(_protoManager.Index(PlanetNames), seed);
_metadata.SetEntityName(mapUid, gatewayName);                                   // MetaData
...
_gateway.SetDestinationName(gatewayUid,
    FormattedMessage.FromMarkupOrThrow($"[color=#D381C996]{gatewayName}[/color]"), gatewayComp);  // gateway ref
```

So in state: 3 gateway names with one MetaData record + extra gateway references, and 1 vgroid name with a single lone MetaData record. **The VGRoid is the borer name referenced exactly once.**

## Evidence: round 15646

Four borer-format names, all base words exclusive to `borer.ftl`:

| name             | refs | role       |
| ---------------- | ---: | ---------- |
| `Sepia-85-J`     |    3 | gateway    |
| `Spondylus-25-J` |    3 | gateway    |
| `Ascaris-51-G`   |    3 | gateway    |
| `Echinus-53-Z`   |    1 | **VGRoid** |

8 bytes before, 16 after each hit. Inline framing is `... 15 01 <len+1> <len>` then UTF-8 (`01` = unmapped marker):

```
Sepia-85-J
  @0x82253c  pre=05c50100 15010b0a  post=0000 8e15a80ab501 00 0000000000000000
  @0x824e6f  pre=00000000 15010b0a  post=3b3c3d 010115 a2b408 0000000000 da9f
  @0x826560  pre=00000000 15010b0a  post=3b3c3d 010115 a2b408 0000000000 d49f
Spondylus-25-J
  @0x823bd9  pre=00000000 15010f0e  post=3b3c3d 010115 a2b408 0000000000 0080
  @0x824eb6  pre=00000000 15010f0e  post=3b3c3d 010115 a2b408 0000000000 0080
  @0x824f08  pre=05c50100 15010f0e  post=0000 8e15a80ab501 00 0000000000000000
Ascaris-51-G
  @0x823b90  pre=00000000 15010d0c  post=3b3c3d 010115 a2b408 0000000000 da9f
  @0x823c2b  pre=05c50100 15010d0c  post=0000 8e15a80ab501 00 0000000000000000
  @0x8265a7  pre=00000000 15010d0c  post=3b3c3d 010115 a2b408 0000000000 0080
Echinus-53-Z
  @0x9f95ed  pre=05c50100 15010d0c  post=0000 8e15a80ab501 00 fc2c3ac42cf98a
```

Two record shapes:

- MetaData: `pre=05c50100...`, `post=0000...` (null Description + null PrototypeId). One per name.
- Gateway `DestinationName` ref: `pre=00000000...`, `post=3b3c3d 010115...`. Only the three gateway names, twice each.

The three gateway MetaData trailers are zeroed (`...b501 00 00000000`, empty fresh maps); `Echinus-53-Z` trails into live component data (`...b501 00 fc2c3ac42cf98a`, a populated grid).

## Evidence: round 15201

The server crashed and truncated the replay's zip central directory, but `zip -FF` rebuilt the archive from the surviving local headers and `data_0` came out intact, the state we need sits at the front of the file, far from the corruption. Same structure as 15646:

| name             | refs | role       |
| ---------------- | ---: | ---------- |
| `Alcyonium-24-H` |    3 | gateway    |
| `Argonauta-35-U` |    3 | gateway    |
| `Ostrea-71-G`    |    3 | gateway    |
| `Buccinum-90-E`  |    1 | **VGRoid** |

```
Buccinum-90-E
  @0xa1f09e  pre=05c50100 15010e0d  post=0000 d41fa80ab501 00 588dacc2def2fb   <- single MetaData, live trailer
```

## Flatness (both rounds)

The VGRoid body is `IronRock` (`VGRoidFill` is a `FillGridDunGen` with `entity: IronRock`), so the `IronRock` entity count is the asteroid's mass. In both bug rounds it is 0, while everything else in the round generated at normal scale. Against the normal round 15691:

| prototype           | 15201 (bug) | 15646 (bug) | 15691 (normal) | what it is     |
| ------------------- | ----------: | ----------: | -------------: | -------------- |
| `IronRock`          |       **0** |       **0** |     **22,198** | VGRoid body    |
| `AsteroidRock`      |       1,618 |         511 |            434 | mini-asteroids |
| `WallRock`          |         206 |         205 |            213 | mini-asteroids |
| `Grille`            |       2,693 |       2,760 |          2,887 | station        |
| `CableApcExtension` |      11,140 |      11,125 |         11,364 | station        |

Same station scale, same mini-asteroid fields. The failure is isolated to the VGRoid fill.

The one alternative to rule out is timing: VGRoid generation is async (~15s in), so a snapshot taken before it finishes would also read 0. That is not the case for either bug round. `data_0` is keyed to the recording's start time, both are snapshotted at 00:02:30, and their mini-asteroids (`AsteroidRock`, generated in the same pass) are already present. A snapshot taken at tick 1, before generation, reads 0 across the board:

| round               | snapshot time | `AsteroidRock` | `IronRock` |
| ------------------- | ------------- | -------------: | ---------: |
| local r46 (pre-gen) | 00:00:00      |              0 |          0 |
| **15201 (bug)**     | **00:02:30**  |      **1,618** |      **0** |
| **15646 (bug)**     | **00:02:30**  |        **511** |      **0** |
| 15691 (normal)      | 03:32:35      |            434 |     22,198 |

Both bug rounds are post-generation (mini-asteroids present) but their VGRoid `IronRock` fill produced nothing. The named grids are generated-but-empty, matching the reports.

## Validation (locally recorded, name confirmed in-game)

To ground-truth the scan: recorded fresh rounds on a local dev server, read the VGRoid name off the mass scanner in-game, ended the round, then ran the same scan on the resulting replay. The decoded name matched the in-game name every time.

| local round | name read in-game | name decoded     |
| ----------- | ----------------- | ---------------- |
| 44          | `Cypraea-32-M`    | `Cypraea-32-M`   |
| 45          | `Spondylus-21-B`  | `Spondylus-21-B` |
| 46          | `Isis-85-Q`       | `Isis-85-Q`      |

Solo local server, so no gateway destinations spawn, which means exactly one borer-format name in state, the VGRoid. Round 46:

```
Isis-85-Q
  @0x6e3166  pre=05c50100 15010a09  post=0000 01a90ab501 00 42f15cc4174c42c4
```

Same MetaData signature as the bug-round grids. The middle `post` varint differs per round (component/timing values); the record shape is identical.

## Getting the name from a replay (step by step)

For a normal, uncorrupted replay zip. Needs `unzip`, `zstd`, and `python3`. Download `vgroid_name.py` (below).

**1. Extract and decompress the round-start state.** `_replay/data_0.dat` is a 4-byte little-endian length followed by a single zstd frame, so strip the 4 bytes and decompress:

```bash
unzip -p your_replay.zip _replay/data_0.dat | tail -c +5 | zstd -dc > data_0.bin
```

**2. Run `vgroid_name.py` on it:**

```bash
python3 vgroid_name.py data_0.bin
```

**3. The line tagged `VGRoid` is the asteroid.** Example, round 15646:

```
Ascaris-51-G       refs=3 gateway-refs=2  -> gateway destination
Echinus-53-Z       refs=1 gateway-refs=0  -> VGRoid
Sepia-85-J         refs=3 gateway-refs=2  -> gateway destination
Spondylus-25-J     refs=3 gateway-refs=2  -> gateway destination
```

On a solo/local server with no gateways there is just one line, and it is the VGRoid.

For a crash-truncated zip, run `zip -FF broken.zip --out fixed.zip` first and use `fixed.zip`.

## Method

1. `_replay/data_0.dat` = 4-byte LE uncompressed length + single zstd frame. Strip 4 bytes, decompress, get the start-time full state (~17 MB). This is the blob searched. (If the zip is truncated from a crash, `zip -FF in.zip --out fixed.zip` rebuilds it first.)
2. Name: literal-search the blob for the `<BorerName>-NN-X` shape. The surrounding bytes give the record shape that tells gateway names from the grid; the grid is the borer name with one MetaData record and no gateway reference.
3. Prototype counts (flat check): `_replay/strings.dat` = `zstd(varint count, then [varint len][utf8] per entry)`, the mapped-string pool in `_mappedStrings` order. A mapped string at pool index `i` is encoded in-state as a LEB128 varint of `i + 2`. Encode `poolIndex + 2` for a prototype id (e.g. `IronRock`) and count its occurrences in the blob. Validated against `CableApcExtension` (11,125) and `Grille` (2,760), both realistic.
