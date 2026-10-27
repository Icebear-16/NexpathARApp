# NEXPATH AR APP — SQLITE INTEGRATION SUMMARY

## What was done
Replaced the hardcoded `DuplexGraph.asset` graph source with a live SQLite database so the search UI (SearchBar + Offices/Classrooms/Laboratories filters) actually queries real destinations and routes to them.

## Files added / changed

### New C# scripts (Assets/ARNav_Phase1/Scripts/Core/)
- **SqliteNative.cs** — Minimal P/Invoke wrapper over the sqlite3 C API (open, exec, prepare_v2, step, bind_text, column readers, finalize, close, errmsg, free). No `Mono.Data.Sqlite` / `System.Data` assembly — avoids the .NET Framework vs .NET Standard IL mismatch and IL2CPP stripping issues.
- **DatabaseManager.cs** — `[DefaultExecutionOrder(-100)]` singleton. Creates `nexpath.db` in `Application.persistentDataPath` (Android APK StreamingAssets is read-only, so persistent path is required). Schema creation, `CreateAndSeed()`, `OpenConn()` returning a `NativeConn : IDisposable` wrapper, `Exec()`, `Count()`.
- **DbSeedData.cs** — Hardcoded arrays exported from `DuplexGraph.asset`: 36 node names, positions (x/y/z), types, floors, authoritative areas, is_transition flags.
- **DbSeeder.cs** — Seeds the DB: 36 nodes, 46 directed edges (bidirectional pairs), 15 `Location` rows (Room 101→L1_R101, Room 106→L1_R106 lab, etc.). Edge list includes stair portals `STAIR_L1_BOT→STAIR_L2_MID→STAIR_L2_TOP` for cross-floor routing.
- **DbQueries.cs** — `LoadAllNodes(db)`, `LoadAllEdges(db)`, `Search(db, query, category)` using native prepared statements with parameter binding (`LIKE @like`, category filter).

### Modified scripts
- **GraphDatabase.cs** — Added `LoadFromDatabase()` which replaces `nodes`/`edges` lists from SQLite via `DbQueries`.
- **NavigationManager.cs** — Changed `Start()` to an `IEnumerator` that yields one frame (so `DatabaseManager` at order -100 finishes seeding first), then calls `graph.LoadFromDatabase()`. `[DefaultExecutionOrder(10)]`.
- **AppUIController.cs** — `Start()` now hooks `searchBar.onValueChanged → RefreshResults()`. `ToggleFilter()` calls `RefreshResults()`. `RefreshResults()` calls `DbQueries.Search()` and auto-selects the first hit. `StartNavigationMode()` sets `NavigationManager.I.DestinationNodeId` to the selected location's `nodeName`, calls `Recalculate()`, and shows `Navigating to <name>` on the distance bar instead of mocked `45 meters`.

### Native plugin
- **Assets/Plugins/x86_64/sqlite3.dll** — Windows Editor native SQLite library (staged).
- **Android:** uses system `libsqlite.so` (present on every Android device) — no `.so` to add.

### Removed
- `Assets/Plugins/Mono.Data.Sqlite.dll` and `System.Data.dll` (invalid IL for this Unity profile).
- `Assets/link.xml` (no longer needed with native P/Invoke).
- Hand-crafted `.meta` files for deleted DLLs (Unity regenerates them).

## Database wireframe (your Tables 3.1–3.3 + AR extensions)

**Location** (your Table 3.1)
- location_id INT PK Auto, name VARCHAR(255), category VARCHAR(50), description TEXT, node_id INT FK

**Navigation_Nodes** (your Table 3.2)
- node_id INT PK Auto, node_name VARCHAR(100), x_coordinate DECIMAL(9,6), y_coordinate DECIMAL(9,6), z_coordinate DECIMAL(9,6), node_type VARCHAR(50)
- **AR extensions:** floor INT, authoritative_area VARCHAR(16), is_transition INT

**Navigation_Edges** (your Table 3.3)
- edge_id INT PK Auto, start_node_id INT FK, end_node_id INT FK, distance DECIMAL(9,6), is_walkable BOOLEAN
- **AR extensions:** is_stairs INT, is_elevator INT

## Seed data counts
- Nodes: 36 (exported from DuplexGraph.asset)
- Edges: 46 directed (bidirectional pairs, includes stair portals)
- Locations: 15 (Rooms 100-109 on L1, Rooms 200,202,205-208 on L2; categories Classroom/Office/Laboratory)

## Runtime flow (what you should see on Play)
1. `[App] Managers ready: Loc > Nav > Visual`
2. `[DB] Seeded OK` then `[DB] Ready nodes=36 edges=46 locs=15 path=<persistentDataPath>/nexpath.db`
3. Vuforia `NO_POSE -- NOT_OBSERVED` for ground floor / stairs targets (normal until camera sees them)
4. `[GraphDB] Loaded from SQLite: 36 nodes / 46 edges`
5. Open search panel → type `Room` → Console: `[Search] Room 101 (Classroom) -> L1_R101`
6. Click **Start Nav** → cyan `PathRenderer` line draws from nearest node to `L1_R101`, distance bar shows `Navigating to Room 101`

## Manual setup in Unity (30 seconds)
1. Open `AR_Navigation` scene.
2. Hierarchy → `Managers` GameObject → Add Component → `DatabaseManager` (leave `reseedOnStart` unchecked).
3. Select `NavigationManager` → confirm `graph` field references `DuplexGraph.asset` and `loadFromDatabase` is checked.
4. Press Play and watch Console for the `[DB]` / `[GraphDB]` / `[Search]` lines.

## How to test it works
- First Play: Console must show `[DB] Seeded OK` + `[DB] Ready nodes=36 ... locs=15`.
- Search: type `Room`, `Office`, or `Laboratory` — Console should log matching `[Search]` rows with node names.
- Start Nav: after selecting a location, clicking Start Nav should draw the cyan path and update the distance bar text.
- Cross-floor: search for an upper-floor room (e.g. `Room 200`), Start Nav — path should route through stair portal `STAIR_L1_BOT→MID→TOP`.

## Known caveats
- `sqlite3.dll` is Editor/Windows-only. Android builds use the OS library automatically.
- If `DllNotFoundException: sqlite3` appears in Editor, confirm `Assets/Plugins/x86_64/sqlite3.dll` is present and its `.meta` was regenerated by Unity (reimport if needed).
- `DuplexGraph.asset` is now only a fallback; live data comes from SQLite. Edit the DB (or set `reseedOnStart=true` temporarily) to change graph data.

Database wireframe — your Tables 3.1–3.3 exactly, plus 3 AR columns each on nodes (floor, authoritative_area, is_transition) and edges (is_stairs, is_elevator) so your existing Dijkstra + cyan-path visuals keep working unchanged.
Full rundown — every file added/modified/removed, the runtime flow (what Console should show), the 30-second manual Unity setup, and a step-by-step test checklist you can hand back to me later with results.
The key technical switch: I moved from Mono.Data.Sqlite (which threw InvalidProgramException because Unity's build targets .NET Framework 4.x, not your .NET Standard 2.1) to a native P/Invoke wrapper over sqlite3.dll/libsqlite.so — zero managed SQLite dependencies, works in Editor and on Android IL2CPP.
