# exposure.py: how many directions can you be shot from at each spot?
# From every walkable spot we cast 360 rays (one per degree) at standing eye height.
# A ray "is open" when an enemy could stand 8+ m away along it, inside rifle range
# (40 m) and inside the map. Houses, tall fences, hedges, sheds, vans and trucks
# block; picket fences and cars do not (you can see and shoot over them).
#   exposed angle   degrees of open rays. A spot tucked into a corner of a room is
#                   about 90; the middle of a lawn is 360.
#   safe spot       exposed angle 120 or less: somewhere to hold an angle or wait.
#   lanes           how many separate open wedges (gaps of 10+ degrees between them):
#                   1-2 is a corner you can hold, 4+ means enemies come from all over.
# Spots within 15 m of a spawn are left out of the numbers.
# Run: python3 exposure.py [--draw]   (draws expo_mapN.png)
import sys, numpy as np, shapely
from shapely.geometry import Polygon
from sightlines import blockers
from parse import maps

CELL = 0.5; RANGE = 40.0; NEAR = 8.0; SAMPLE = 2.0; R_KID = 0.4

def grids(mp):
    area, ob = blockers(mp)
    bx = area.bounds
    xs = np.arange(bx[0], bx[2], CELL); ys = np.arange(bx[1], bx[3], CELL)
    X, Y = np.meshgrid(xs, ys)
    clear = shapely.contains_xy(area, X, Y) & ~shapely.contains_xy(ob, X, Y)
    stand = clear & ~shapely.contains_xy(ob.buffer(R_KID), X, Y)
    from scipy import ndimage                                    # only ground you can actually reach
    lab, k = ndimage.label(stand)
    if k > 1: stand = lab == np.argmax(np.bincount(lab.ravel())[1:]) + 1
    return area, bx, clear, stand

def expose(mp):
    area, bx, clear, stand = grids(mp)
    H, W = clear.shape
    sx = np.arange(bx[0] + 1, bx[2], SAMPLE); sy = np.arange(bx[1] + 1, bx[3], SAMPLE)
    SX, SY = np.meshgrid(sx, sy)
    ci = ((SY - bx[1]) / CELL).astype(int); cj = ((SX - bx[0]) / CELL).astype(int)
    ok = stand[ci.clip(0, H-1), cj.clip(0, W-1)]
    P = np.c_[SX[ok], SY[ok]]
    ang = np.radians(np.arange(360)); steps = np.arange(CELL, RANGE + CELL, CELL / 2)
    dx = np.cos(ang)[:, None] * steps; dy = np.sin(ang)[:, None] * steps
    deg = np.zeros(len(P)); lanes = np.zeros(len(P), int)
    for k0 in range(0, len(P), 200):
        p = P[k0:k0+200]
        gx = ((p[:, 0, None, None] + dx - bx[0]) / CELL).astype(int)
        gy = ((p[:, 1, None, None] + dy - bx[1]) / CELL).astype(int)
        inside = (gx >= 0) & (gx < W) & (gy >= 0) & (gy < H)
        c = np.zeros(gx.shape, bool); s = np.zeros(gx.shape, bool)
        c[inside] = clear[gy[inside], gx[inside]]; s[inside] = stand[gy[inside], gx[inside]]
        reach = np.cumprod(c, axis=2).astype(bool)                 # still unblocked
        far = steps[None, None, :] >= NEAR
        open_ray = (reach & s & far).any(axis=2)                   # someone could stand there and see you
        deg[k0:k0+200] = open_ray.sum(axis=1)
        for i, o in enumerate(open_ray):
            if o.all(): lanes[k0+i] = 9; continue
            if not o.any(): continue
            r = np.roll(o, -int(np.argmin(o)))                     # start in a closed ray
            runs = np.diff(np.r_[0, r.astype(int), 0]); st = np.where(runs == 1)[0]; en = np.where(runs == -1)[0]
            # merge wedges separated by gaps under 10 degrees
            n = 1
            for a in range(1, len(st)):
                if st[a] - en[a-1] >= 10: n += 1
            lanes[k0+i] = n
    return P, deg, lanes

def summary(mp, skip_spawns=True):
    P, deg, lanes = expose(mp)
    if skip_spawns:   # spawn rooms are meant to be safe from the enemy anyway
        keep = np.ones(len(P), bool)
        for sp in (mp['att'], mp['dfn']): keep &= np.hypot(P[:, 0]-sp[0], P[:, 1]-sp[1]) > 15
        P, deg, lanes = P[keep], deg[keep], lanes[keep]
    return dict(n=len(P), median=float(np.median(deg)), safe=float((deg <= 120).mean()),
                wide=float((deg >= 270).mean()), holdable=float(((lanes <= 2) & (deg <= 180)).mean()),
                lanes=float(np.median(lanes)))

def draw(i):
    import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
    mp = maps[i]; area, ob = blockers(mp); P, deg, lanes = expose(mp); bx = area.bounds
    fig, ax = plt.subplots(figsize=(8, 8*(bx[3]-bx[1]+5)/(bx[2]-bx[0]+5)))
    for g in (ob.geoms if hasattr(ob, 'geoms') else [ob]):
        if g.geom_type == 'Polygon': ax.fill(*g.exterior.xy, color='#333', lw=0)
    sc = ax.scatter(P[:, 0], P[:, 1], c=deg, cmap='RdYlGn_r', vmin=60, vmax=360, s=9, marker='s')
    ax.plot(*area.exterior.xy, color='#888', lw=0.8)
    for s in mp['sites']: ax.add_patch(plt.Circle(s[3], s[4], fill=False, color='b', lw=1.5))
    ax.plot(*mp['att'], 'r*', ms=12); ax.plot(*mp['dfn'], 'c*', ms=12)
    ax.set_xlim(bx[0]-2, bx[2]+2); ax.set_ylim(bx[1]-2, bx[3]+2); ax.set_aspect('equal'); ax.set_xticks([]); ax.set_yticks([])
    s = summary(mp)
    ax.set_title('%s: degrees you can be shot from\ngreen = corner to hold (<=120)  red = open on all sides.  safe %.0f%%, median %.0f deg'
                 % (mp['name'], 100*s['safe'], s['median']), fontsize=9)
    fig.colorbar(sc, ax=ax, shrink=0.6)
    fig.savefig('expo_map%d.png' % (i+1), dpi=90, bbox_inches='tight'); plt.close(fig)

if __name__ == '__main__':
    for i, mp in enumerate(maps):
        s = summary(mp)
        print('%-18s spots %4d  median %3.0f deg  safe(<=120) %3.0f%%  open-all-round(>=270) %3.0f%%  holdable corners %3.0f%%  median lanes %.0f'
              % (mp['name'], s['n'], s['median'], 100*s['safe'], 100*s['wide'], 100*s['holdable'], s['lanes']))
        if '--draw' in sys.argv: draw(i)
