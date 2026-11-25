# Map tools

These Python scripts draw the Maple Park neighborhood and write the game's map
data (`Assets/WaterWars/Scripts/NeighborhoodData.cs`). You only need them if you
want to change the neighborhood or add a map. They need Python 3 with
`pip install numpy matplotlib shapely`.

Run them from this folder, in this order:

1. `python3 base.py` draws the whole neighborhood (`base.png`) and saves the lots.
2. `python3 export_base.py` records every house, garage, fence and tree.
3. `python3 maps5.py` draws the six example maps into `drawings/`. The map
   definitions (which yards, where the sites are) are the `MAPS` list in this file.
4. `python3 export_cs.py` writes `NeighborhoodData.cs` for the game.

Then go back to Unity and press Play.

## Checking that every map is walkable

`python3 walkcheck/check_walkable.py` rebuilds each map's solid things (houses,
garages, fences, gates, map edges, trees, yard stuff) as shapes seen from above,
leaves a kid-sized gap around them, and checks that a kid can walk from the
attacker spawn to both sites and the defender spawn. It prints the result and
draws `walkcheck/walk_mapN.png` (green = reachable). Run it after changing the
neighborhood or a map. `export_cs.py` keeps every side yard at least
`SIDE_YARD` (3 m) wide so the lanes stay walkable.

Add `--times` to also print walking distances and times (at the game's run
speed of 5.5 m/s) between the spawns and the sites, marked against the Map
Playbook's targets: attackers 20-25 s to a site, defenders 5-10 s, defenders
10-15 s from site to site.

## Map cover (props)

Each map in `maps5.py` has a `cover=[...]` list: parked cars, vans, trucks,
portable classrooms, sheds, a snack stand, party tents, extra tall fences and
hedges (with or without a gate), and trees. `props.py` explains the numbers.
A new fence or hedge replaces any old neighborhood fence lying on the same line,
so you can turn a low picket fence into a tall one or add a gate. The game builds
the same props (`MapBuilder.BuildProps`).

## Checking sightlines

`python3 walkcheck/sightlines.py` prints the longest clear straight line on each
map; `python3 walkcheck/count.py` counts the clear lines longer than 60 m (fewer
is better: the genre keeps most fights under about 40 m). Low picket fences and
cars count as see-over. `walkcheck/debug_plot.py N` draws map N with a 5 m grid,
fences, gates and house numbers, which helps when placing new props.
