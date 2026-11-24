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
