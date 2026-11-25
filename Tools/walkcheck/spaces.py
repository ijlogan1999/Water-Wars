# spaces.py: how much of each map is corridor and how much is open space.
# For every walkable spot we find the biggest circle of open ground that covers it
# (nothing solid inside: houses, fences, hedges, cars, map edge...). Its width says
# what kind of space the spot is in:
#   corridor   narrower than 8 m   (side yards, gates, alleys, gaps between cars)
#   room       8 to 16 m           (a backyard with stuff in it, a site with cover)
#   open       wider than 16 m     (a lawn, a field, a street crossing with nothing in it)
# Run: python3 spaces.py   (draws space_mapN.png too)
import sys, numpy as np, shapely
from shapely.geometry import Polygon, box
from scipy import ndimage
from parse import maps
from check_walkable import build
STEP = 0.5
def classify(mp):
    area = Polygon(np.array(mp['area']).reshape(-1, 2)); bx = area.bounds
    near = box(bx[0]-60, bx[1]-60, bx[2]+60, bx[3]+60)
    area, obst = build(mp, near)
    free = area.difference(obst)
    xs = np.arange(bx[0], bx[2], STEP); ys = np.arange(bx[1], bx[3], STEP)
    X, Y = np.meshgrid(xs, ys); ok = shapely.contains_xy(free, X, Y)
    edt = ndimage.distance_transform_edt(ok) * STEP          # distance to nearest solid thing
    width = np.zeros_like(edt)
    for r in np.arange(0.5, 30, 0.5):
        centers = edt >= r
        if not centers.any(): break
        covered = ndimage.distance_transform_edt(~centers) * STEP <= r
        width[covered & ok] = 2 * r
    return X, Y, ok, width
def summary(mp):
    X, Y, ok, w = classify(mp); n = ok.sum()
    c = ((w < 8) & ok).sum(); m = ((w >= 8) & (w < 16) & ok).sum(); o = ((w >= 16) & ok).sum()
    # open blobs bigger than 150 m^2
    lab, k = ndimage.label((w >= 16) & ok)
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, k+1)) * STEP * STEP
    big = sorted([s for s in sizes if s > 150], reverse=True)
    return dict(walk=n*STEP*STEP, corridor=c/n, room=m/n, open=o/n, open_areas=len(big), biggest=big[0] if big else 0)
def draw(i):
    import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
    from matplotlib.colors import ListedColormap
    mp = maps[i]; X, Y, ok, w = classify(mp)
    cls = np.where(~ok, 0, np.where(w < 8, 1, np.where(w < 16, 2, 3)))
    fig, ax = plt.subplots(figsize=(8, 8*(Y.max()-Y.min()+5)/(X.max()-X.min()+5)))
    ax.pcolormesh(X, Y, cls, cmap=ListedColormap(['#3a3a3a', '#2e86de', '#f6c344', '#e74c3c']), vmin=0, vmax=3, shading='auto')
    ax.set_aspect('equal'); ax.set_xticks([]); ax.set_yticks([])
    s = summary(mp)
    ax.set_title('%s\nblue corridor %.0f%%  yellow room %.0f%%  red open %.0f%%' % (mp['name'], 100*s['corridor'], 100*s['room'], 100*s['open']), fontsize=10)
    fig.savefig('space_map%d.png' % (i+1), dpi=90, bbox_inches='tight'); plt.close(fig)
if __name__ == '__main__':
    for i, mp in enumerate(maps):
        s = summary(mp)
        print('%-18s walkable %5.0f m2  corridor %3.0f%%  room %3.0f%%  open %3.0f%%  open areas %d (biggest %.0f m2)' % (
            mp['name'], s['walk'], 100*s['corridor'], 100*s['room'], 100*s['open'], s['open_areas'], s['biggest']))
        if '--draw' in sys.argv: draw(i)
