# props.py: the cover each map adds on top of the neighborhood (parked cars,
# vans, portable classrooms, sheds, snack stands, party tents, extra fences
# and hedges, trees). Each map lists its props as `cover=[...]` in maps5.py.
#
#   ("car",  x, y, angle)            car, long side pointing `angle` degrees (0 = east)
#   ("van",  x, y, angle)            van, same idea, bigger
#   ("truck", x, y, angle)           box truck, bigger still (blocks the view)
#   ("rv", x, y, angle)              motorhome (RV), 10 m long, blocks the view
#   ("portable"|"shed"|"stand"|"tent"|"townhome", x, y, angle, w, d)
#                                    townhome = a row of joined two-storey homes (solid,
#                                    9 m tall): a long block that makes narrow alleys
#                                    box w wide (east-west at angle 0) and d deep
#   ("fence"|"hedge", x1, y1, x2, y2, gap_at, gap_w)
#                                    tall fence or hedge from (x1,y1) to (x2,y2), with a
#                                    gate gap_w wide starting gap_at meters from the first
#                                    end (gap_at < 0 = no gate). It replaces any old fence
#                                    lying on that line.
#   ("tree", x, y, r)
#
# Blueprint maps (blueprint.py) are made only of props, so they add the big pieces:
#   ("house", x, y, angle, w, d, h)  solid house; its front faces `angle`, w is the side
#                                    with the front door, d runs back from it, h is its height
#   ("lockup"|"bush"|"thicket"|"crate"|"pool"|"yard", x, y, angle, w, d)
#                                    a shut garage, a hedge block, a clump of trees, a low
#                                    box you can shoot over, an above-ground pool (also
#                                    low), a closed-off yard
#   ("water", x, y, angle, w, d)     a pond or creek: nobody walks in, you see and shoot across
#   ("school", x, y, angle, w, d)    a flat-roofed block of the school, 7 m tall
#   ("bleachers", x, y, angle, w, d) two wide steps, low enough to shoot over; the low step faces `angle`
#   ("bus", x, y, angle)             a parked school bus, 10 m long, blocks the view
#   ("roadwork", x, y, angle, w, d, sides)
#                                    a plywood screen round road work, 4.2 m; a striped barricade
#                                    stands on each side in `sides` (1 south, 2 east, 4 north, 8 west)
#   ("dumpster", x, y, angle, w, d)  a dumpster, low enough to shoot over
#   ("logs", x, y, angle, length)    a fallen tree or a woodpile lying along `angle`, 1.3 m wide, shoot over
#   ("garage", x, y, angle, back)    open garage, big door facing `angle`, back door if back=1
#   ("treehouse", x, y, angle)       platform on posts, ramp running out toward `angle`
#   ("paint", x, y, w, d, what)      ground color only (0 pavement, 1 asphalt, 2 dirt, 3 sand)
#
# This file is the one place the Python tools learn a prop's size; MapBuilder.cs
# builds the same sizes in Unity (BuildProps).
import math
from shapely.geometry import Polygon, LineString, box

SIZE = {"car": (4.6, 1.9), "van": (5.2, 2.2), "truck": (9.3, 2.5), "rv": (10.0, 2.6), "bus": (10.0, 2.5)}   # truck: 7 m box + 2.2 m cab, as MapBuilder.Truck builds it
HEIGHT = {"truck": 3.4, "car": 1.75, "van": 2.4, "portable": 3.2, "shed": 3.2, "stand": 2.4, "tent": 2.7, "fence": 3.0, "hedge": 4.2, "rv": 3.5, "townhome": 9.0}
HEIGHT.update({"lockup": 4.2, "garage": 4.2, "bush": 4.2, "thicket": 6.0, "crate": 1.3, "pool": 1.2, "yard": 0.6, "treehouse": 5.4, "water": 0.0, "school": 7.0, "bleachers": 1.1, "bus": 3.0, "roadwork": 4.2, "dumpster": 1.5, "logs": 1.2})
GARAGE = (6.0, 8.0, 4.0)     # an open blueprint garage: width, depth, width of the big door
LOW = ("car", "crate", "pool", "water", "bleachers", "dumpster", "logs")   # low enough to see over (water: you see and shoot across, nobody walks in)

def height(p):
    """How tall a prop is, in meters."""
    return p[6] if p[0] == "house" else HEIGHT.get(p[0], 3.0)

def _local(x, y, a, pieces):
    """Boxes given in a piece's own frame: (sideways, back, size sideways, size back),
    where "back" points away from the way the piece faces (angle a)."""
    r = math.radians(a); bx, by = -math.cos(r), -math.sin(r)      # back
    sx, sy = by, -bx                                              # sideways
    return [_rect(x + sx*lx + bx*lz, y + sy*lx + by*lz, sx, sy, w, d) for lx, lz, w, d in pieces]

def _rect(cx, cy, ux, uy, length, width):
    # rectangle `length` along (ux,uy) and `width` across it
    vx, vy = -uy, ux
    return Polygon([(cx + ux*a*length/2 + vx*b*width/2, cy + uy*a*length/2 + vy*b*width/2)
                    for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1))])

def wall_pieces(p):
    """For fence/hedge props: the solid pieces (a, b) with the gate left out."""
    k, x1, y1, x2, y2, gap_at, gap_w = p
    L = math.hypot(x2-x1, y2-y1); ux, uy = (x2-x1)/L, (y2-y1)/L
    if gap_at < 0: return [((x1, y1), (x2, y2))]
    out = []
    if gap_at > 0.05: out.append(((x1, y1), (x1+ux*gap_at, y1+uy*gap_at)))
    e = gap_at + gap_w
    if e < L - 0.05: out.append(((x1+ux*e, y1+uy*e), (x2, y2)))
    return out

def gate_center(p):
    k, x1, y1, x2, y2, gap_at, gap_w = p
    if gap_at < 0: return None
    L = math.hypot(x2-x1, y2-y1); ux, uy = (x2-x1)/L, (y2-y1)/L
    m = gap_at + gap_w/2
    return (x1+ux*m, y1+uy*m), (-uy, ux)

def footprint(p):
    """Solid shapes a kid bumps into, seen from above."""
    k = p[0]
    if k in ("car", "van", "truck", "rv", "bus"):
        _, x, y, a = p; l, w = SIZE[k]; r = math.radians(a)
        return [_rect(x, y, math.cos(r), math.sin(r), l, w)]
    if k == "house":
        _, x, y, a, w, d, h = p; r = math.radians(a)
        return [_rect(x, y, math.cos(r), math.sin(r), d, w)]
    if k == "garage":      # the same walls MapBuilder.BuildGarage puts up, plus the workbench and boxes
        _, x, y, a, back = p; w, d, door = GARAGE; t = 0.25; stub = (w - door) / 2
        pieces = [(-w/2, 0, t, d), (w/2, 0, t, d), (-w/2 + stub/2, -d/2, stub, t), (w/2 - stub/2, -d/2, stub, t),
                  (-w/2 + 0.8, 1.6, 1.2, 2.4), (w/2 - 1, 0.6, 1.3, 1.3)]
        if back > 0.5:
            pc = (w - 2.2) / 2
            pieces += [(-w/2 + pc/2, d/2, pc, t), (w/2 - pc/2, d/2, pc, t)]
        else: pieces.append((0, d/2, w, t))
        return _local(x, y, a, pieces)
    if k == "treehouse":   # four posts and the trunk; you can walk under the platform
        _, x, y, a = p
        return _local(x, y, a, [(sx*2.3, sz*2.3, 0.3, 0.3) for sx in (-1, 1) for sz in (-1, 1)] + [(0, 3.4, 1.4, 1.4)])
    if k == "paint": return []
    if k in ("portable", "shed", "stand", "townhome", "lockup", "bush", "thicket", "crate", "pool", "yard", "water", "school", "bleachers", "dumpster", "roadwork"):
        _, x, y, a, w, d = p[:6]; r = math.radians(a)
        return [_rect(x, y, math.cos(r), math.sin(r), w, d)]
    if k == "logs":
        _, x, y, a, length = p; r = math.radians(a)
        return [_rect(x, y, math.cos(r), math.sin(r), length, 1.3)]
    if k == "tent":   # only the four poles block you; the roof is overhead
        _, x, y, a, w, d = p; r = math.radians(a); ux, uy = math.cos(r), math.sin(r); vx, vy = -uy, ux
        return [_rect(x + ux*sx*(w/2-0.2) + vx*sz*(d/2-0.2), y + uy*sx*(w/2-0.2) + vy*sz*(d/2-0.2), 1, 0, 0.2, 0.2)
                for sx in (-1, 1) for sz in (-1, 1)]
    if k in ("fence", "hedge"):
        th = 0.2 if k == "fence" else 0.9
        return [LineString([a, b]).buffer(th/2, cap_style=2) for a, b in wall_pieces(p)]
    if k == "tree":
        _, x, y, r = p
        return [box(x-0.3, y-0.3, x+0.3, y+0.3)]
    raise ValueError(k)

def replaces_fence(p, a, b, within=0.5):
    """True when an old neighborhood fence from a to b lies on this fence/hedge prop's line."""
    if p[0] not in ("fence", "hedge"): return False
    line = LineString([(p[1], p[2]), (p[3], p[4])])
    from shapely.geometry import Point
    return line.distance(Point(a)) < within and line.distance(Point(b)) < within

def blocks_sight(p):
    """Shapes that block a standing kid's view (for the sightline check)."""
    if p[0] in ("car",):  return footprint(p)   # crouch-high cover; counts as blocking for long lines
    return footprint(p) if p[0] != "tent" else []
