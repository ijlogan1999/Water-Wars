# sightlines.py: the longest clear straight line on each map (how far a sniper
# can see along the ground). Low picket fences and cars count as see-over; houses,
# tall fences, hedges, sheds, portables and vans block. Run: python3 sightlines.py
import sys, numpy as np, shapely
from shapely.geometry import Polygon, box, LineString
from shapely.prepared import prep
from shapely.ops import unary_union
from parse import fences, maps
from check_walkable import build, R
import props as PROPS

def blockers(mp):
    area = Polygon(np.array(mp['area']).reshape(-1, 2)); bx = area.bounds
    near = box(bx[0]-60, bx[1]-60, bx[2]+60, bx[3]+60)
    area, obst = build(mp, near)
    # remove see-over things: picket fences and cars (rebuild them and subtract is messy,
    # so instead keep only shapes at least 1 m^2 or long walls that are not picket)
    pick = [LineString([fences[f*5:f*5+2], fences[f*5+2:f*5+4]]).buffer(0.1) for f in mp['fences'] if int(fences[f*5+4]) == 1]
    cars = [g for p in mp.get('props', []) if p[0] in PROPS.LOW for g in PROPS.footprint(p)]
    see_over = unary_union(pick + cars).buffer(0.05) if (pick or cars) else None
    if see_over is not None: obst = obst.difference(see_over)
    return area, obst.buffer(-0.05)

def longest(mp, step=3.0, cut=40.0):
    area, ob = blockers(mp)
    free = area.difference(ob.buffer(R))
    bx = area.bounds
    X, Y = np.meshgrid(np.arange(bx[0], bx[2], step), np.arange(bx[1], bx[3], step))
    ok = shapely.contains_xy(free, X, Y)
    P = np.c_[X[ok], Y[ok]]
    pob = prep(ob)
    best = (0, None); n_long = 0
    for i in range(len(P)):
        d = np.hypot(*(P[i+1:] - P[i]).T)
        for j in np.argsort(-d):
            if d[j] < cut: break
            ln = LineString([P[i], P[i+1+j]])
            if not pob.intersects(ln) and area.contains(ln):
                if d[j] > best[0]: best = (d[j], (tuple(P[i]), tuple(P[i+1+j])))
                break
    return best

if __name__ == '__main__':
    for mp in maps:
        d, seg = longest(mp)
        print('%-18s longest clear line %3.0f m  %s' % (mp['name'], d, '' if seg is None else '(%.0f,%.0f) to (%.0f,%.0f)' % (seg[0] + seg[1])))

def all_long(mp, step=3.0, cut=70.0):
    area, ob = blockers(mp)
    free = area.difference(ob.buffer(R)); bx = area.bounds
    X, Y = np.meshgrid(np.arange(bx[0], bx[2], step), np.arange(bx[1], bx[3], step))
    ok = shapely.contains_xy(free, X, Y); P = np.c_[X[ok], Y[ok]]; pob = prep(ob); out = []
    for i in range(len(P)):
        d = np.hypot(*(P[i+1:] - P[i]).T)
        for j in np.where(d >= cut)[0]:
            ln = LineString([P[i], P[i+1+j]])
            if not pob.intersects(ln) and area.contains(ln): out.append(ln)
    return area, ob, out

def draw(i):
    import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
    mp = maps[i]; area, ob, lines = all_long(mp)
    bx = area.bounds
    fig, ax = plt.subplots(figsize=(10, 10*(bx[3]-bx[1]+10)/(bx[2]-bx[0]+10)))
    for g in (ob.geoms if hasattr(ob, 'geoms') else [ob]):
        if g.geom_type == 'Polygon': ax.fill(*g.exterior.xy, color='#444', lw=0)
    ax.plot(*area.exterior.xy, 'b-')
    for ln in lines[::max(1, len(lines)//400)]: ax.plot(*ln.xy, 'r-', lw=0.4, alpha=0.4)
    ax.set_xticks(np.arange(int(bx[0]//10*10), bx[2]+10, 10)); ax.set_yticks(np.arange(int(bx[1]//10*10), bx[3]+10, 10))
    ax.tick_params(labelsize=6); ax.grid(alpha=.3); plt.setp(ax.get_xticklabels(), rotation=90)
    ax.set_xlim(bx[0]-3, bx[2]+3); ax.set_ylim(bx[1]-3, bx[3]+3); ax.set_aspect('equal')
    ax.set_title('%s: clear lines over 70 m (%d)' % (mp['name'], len(lines)))
    fig.savefig('sight_map%d.png' % (i+1), dpi=80, bbox_inches='tight')
