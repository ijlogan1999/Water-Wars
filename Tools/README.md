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
