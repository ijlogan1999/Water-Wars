# ours.py: walk masks for the six Water Wars maps, built from the same solid
# shapes check_walkable.py uses (houses, garages, fences, hedges, cars, props...).
import os, sys
import numpy as np, shapely
from shapely.geometry import Polygon, box
HERE = os.path.dirname(os.path.abspath(__file__))
WC = os.path.join(HERE, '..', 'walkcheck')
sys.path.insert(0, WC)
_cwd = os.getcwd(); os.chdir(WC)
from parse import maps            # noqa: E402
from check_walkable import build  # noqa: E402
os.chdir(_cwd)
from ruler import RES             # noqa: E402


from sightlines import blockers  # noqa: E402
from check_walkable import piece, wall  # noqa: E402
from parse import lots, fences       # noqa: E402
import props as PROPS                # noqa: E402
from shapely.ops import unary_union  # noqa: E402

# Heights in meters, kept in step with MapBuilder.cs ("How tall things are").
# The map's edge, by what MapBuilder puts there. 0 would mean you can see over or through it.
EDGE_H = {'Street': 3.2, 'Curb': 4.2, 'CurbFenced': 4.2, 'LotFenced': 4.2, 'School': 3.2, 'Lot': 3.0, 'Park': 4.8, 'Verge': 4.8, 'Woods': 4.8}
LOT_FENCE_H = {0: 3.0, 1: 1.1, 2: 4.2}     # wood, picket, hedge (MapBuilder.BuildFences)
HOUSE_TALL = 1.35                          # MapBuilder.HouseTall
GARAGE_H = 4.2                             # MapBuilder.GarageHeight
# A wooden fence is 0.2 m thick, thinner than one 0.25 m grid cell, so a fence that is
# tilted or sits between grid points would be drawn with holes in it and the ruler would
# see (and walk) straight through. Every solid shape is fattened by this much first,
# which makes the thinnest wall 0.36 m: always at least one whole cell, at any angle.
FATTEN = 0.08


def our_mask(i):
    """Walk mask, key points, and what blocks the view (with heights) for map i."""
    mp = maps[i]
    area = Polygon(np.array(mp['area']).reshape(-1, 2)); bx = area.bounds
    near = box(bx[0] - 60, bx[1] - 60, bx[2] + 60, bx[3] + 60)
    area, obst = build(mp, near)
    free = area.difference(obst.buffer(FATTEN))
    xs = np.arange(bx[0] - 1, bx[2] + 1, RES); ys = np.arange(bx[1] - 1, bx[3] + 1, RES)
    X, Y = np.meshgrid(xs, ys)
    flip = lambda a: a[::-1]                                   # so north is up
    mask = flip(shapely.contains_xy(free, X, Y))
    top = ys[-1]
    cell = lambda p: ((p[0] - xs[0]) / RES, (top - p[1]) / RES)
    pts = {'att': cell(mp['att']), 'dfn': cell(mp['dfn'])}
    for s, nm in zip(mp['sites'], 'AB'): pts[nm] = cell(s[3])

    # ---- what you can see across: floor plus picket fences and cars -----------
    _, ob = blockers(mp)
    inside = shapely.contains_xy(area, X, Y)
    see = flip(inside & ~shapely.contains_xy(ob.buffer(0.05 + FATTEN), X, Y))   # blockers() shrank them by 0.05

    # ---- how tall each thing that blocks the view is ---------------------------
    Hr = np.zeros(X.shape)
    blocked = inside & ~flip(see)
    Hr[blocked] = 3.0                                          # anything not listed below
    def paint(geoms, h):
        if not geoms: return
        g = unary_union(geoms)
        Hr[shapely.contains_xy(g, X, Y) & blocked] = h
    by_h = {}
    for li in mp['lots']:
        L = lots[li]; o = []
        piece(o, L['house'][0], L['house'][1], L['inw'][0], L['inw'][1], 0, 0, L['hw'], L['hd'])
        by_h.setdefault(round(L['hh'] * HOUSE_TALL, 2), []).extend(o)
        if len(L['garage']) == 2:
            g = []; piece(g, L['garage'][0], L['garage'][1], L['inw'][0], L['inw'][1], 0, 0, 6.9, 8.7)
            by_h.setdefault('garage', []).extend(g)
    paint(by_h.pop('garage', []), GARAGE_H)
    for h, gs in by_h.items(): paint(gs, h)
    fk = {}
    for fid in mp['fences']:
        f = fences[fid * 5:fid * 5 + 5]; o = []
        wall(o, f[0:2], f[2:4], 1.2)
        fk.setdefault(int(f[4]), []).extend(o)
    for k, gs in fk.items(): paint(gs, LOT_FENCE_H[k])
    pk = {}
    for p in mp.get('props', []):
        pk.setdefault(p[0], []).extend(g.buffer(0.3) for g in PROPS.footprint(p))
    for k, gs in pk.items(): paint(gs, PROPS.HEIGHT.get(k, 3.0))
    # the map's edge: a band just outside the area, as tall as what stands there
    for a, b, k in mp['edges']:
        o = []; a = np.array(a, float); b = np.array(b, float); d = b - a; n = np.linalg.norm(d)
        if n < 0.05: continue
        d /= n; out = np.array([d[1], -d[0]])
        wall(o, a + out * 0.9 - d * 0.5, b + out * 0.9 + d * 0.5, 2.0)
        Hr[shapely.contains_xy(o[0], X, Y) & ~inside] = EDGE_H.get(k, 0)
    return mask, RES, pts, dict(name=mp['name'], see=see, heights=flip(Hr))
