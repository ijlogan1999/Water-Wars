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

## Maps drawn from a blank page (the remade maps)

The first six maps were cut out of the real-size neighborhood, and the audit showed
that is why they played like fields. All six have now been redrawn from a blank page
(they keep their names). Each one is a drawing made of letters in `newmaps.py`: one
letter is one 2 m square, north is at the top, everything starts solid and the paths
are carved out. The top of `blueprint.py` lists the letters (houses, shut and open
garages, hedges, trees, board fences, sheds, crates, vehicles, a treehouse, a party
tent, a pool, water you can see across, school blocks, portables, bleachers, a bus,
closed yards, the spawns and the sites).

What made the six meet the spec, so the next map can too: about 104 x 108 m with
about half of it floor, so most lanes pass through rooms (yards, clearings) of about
16 x 16 m; each site a room with three doors (one 6 m, two 4 m) kept apart, with
nothing standing within 3 squares of a door; doors on opposite sides of a room never
in line, and a shed or trees on any straight line over 40 m; walls of houses, 4.2 m
hedges or 6 m trees rather than 3 m fences.

Everything also has to sit where it would in a real neighborhood. `blueprint.py`
lists anything out of place under "out of place": a hedge, trees, a shed or crates
standing in a street, a street that just stops at a hedge (close it with `Z` road
work, a house or a vehicle), a car parked across a street (unless it is backed up to
a house or a garage, or parked by road work), a shed or a pool out in the alley.
Streets get vehicles, road work (`Z`) and dumpsters (`D`); woods and yards get logs
(`l`); crates belong at a garage sale, a loading dock or a fort. A house's front door
goes on the side along a street; with no street, on the map's edge (its street is
past the edge and the map is its back yard); otherwise on a long side. A house on
the edge that is only a square or two deep is built a whole house deep, past the edge.

`maps5.py` still holds the old cut-out maps and the Maple Park neighborhood. They
are no longer what the game plays, but export_cs.py still reads them, so leave them
until the remade maps have been played and kept.

`python3 blueprint.py "Willow Court"` checks a drawing (the outside ring is solid,
every square of floor can be reached, bots get a stepping stone everywhere, no gap
is too thin to use), draws `drawings/new-willow-court.png`, and measures it against
the audit's spec with the same ruler that measured the CS2 and Valorant maps.
`python3 maps5.py && python3 export_cs.py` then writes it into the game, where it
takes the place of the old map with the same name. The game builds such a map from
its props alone (`MapBuilder.BuildProps`): no Maple Park streets or lots, and a ring
of houses and trees past the edge so it does not look like the world ends.

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
hedges (with or without a gate), trees, townhome rows (long solid blocks that make
alleys) and RVs. `props.py` explains the numbers. Wooden fences and tall hedges are
3 m high in the game, so a jumping kid (eyes at 2.7 m) cannot peek over them.
A new fence or hedge replaces any old neighborhood fence lying on the same line,
so you can turn a low picket fence into a tall one or add a gate. The game builds
the same props (`MapBuilder.BuildProps`).

## Checking sightlines

`python3 walkcheck/sightlines.py` prints the longest clear straight line on each
map; `python3 walkcheck/count.py` counts the clear lines longer than 60 m (fewer
is better: the genre keeps most fights under about 40 m). Low picket fences and
cars count as see-over. `python3 walkcheck/spaces.py --draw` measures how much of
each map is corridor (under 8 m wide), room (8-16 m) and open (over 16 m), and
draws `space_mapN.png`. Targets: 30%+ corridor, about half room, 15% or less open,
no open area over about 400 m2. `python3 walkcheck/exposure.py --draw` measures, for
every spot, how many degrees around you an enemy within 40 m could shoot you from
(draws `expo_mapN.png`). A spot of 120 degrees or less is a corner you can hold or
wait in. A rooms-and-corridors test layout scores median about 140, 30% of spots
safe and 0% open all round; aim for that. `walkcheck/debug_plot.py N` draws map N with a 5 m grid,
fences, gates and house numbers, which helps when placing new props.
