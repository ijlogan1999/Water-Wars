import sys, numpy as np
import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
from shapely.geometry import Polygon, box
sys.path.insert(0, '.')
from parse import lots, fences, gates, maps
from check_walkable import build
i = int(sys.argv[1]); mp = maps[i]
area = Polygon(np.array(mp['area']).reshape(-1, 2)); bx = area.bounds
near = box(bx[0]-60, bx[1]-60, bx[2]+60, bx[3]+60)
_, obst = build(mp, near)
fig, ax = plt.subplots(figsize=(14, 14*(bx[3]-bx[1]+10)/(bx[2]-bx[0]+10)))
for g in (obst.geoms if hasattr(obst, 'geoms') else [obst]):
    ax.fill(*g.exterior.xy, color='#555', lw=0)
ax.plot(*area.exterior.xy, 'b-', lw=1)
col = {0: '#8a5a2b', 1: '#bbbbbb', 2: '#1d7a1d'}
for fid in mp['fences']:
    f = fences[fid*5:fid*5+5]
    ax.plot([f[0], f[2]], [f[1], f[3]], color=col[int(f[4])], lw=3)
for g in range(0, len(gates), 4):
    c = gates[g:g+2]
    if bx[0]-5 < c[0] < bx[2]+5 and bx[1]-5 < c[1] < bx[3]+5: ax.plot(*c, 'mo', ms=6)
for L in lots:
    hx, hy = L['house']
    if bx[0] < hx < bx[2] and bx[1] < hy < bx[3]:
        ax.text(hx, hy, '%d %s' % (L['num'], L['street'].split()[0]), fontsize=7, ha='center', color='w')
ax.plot(*mp['att'], 'r*', ms=16); ax.plot(*mp['dfn'], 'c*', ms=16)
for s in mp['sites']:
    ax.add_patch(plt.Circle(s[3], s[4], fill=False, color='orange', lw=2)); ax.text(*s[3], s[0][:12], color='orange', fontsize=8)
ax.set_xticks(np.arange(int(bx[0]//5*5), bx[2]+5, 5)); ax.set_yticks(np.arange(int(bx[1]//5*5), bx[3]+5, 5))
ax.tick_params(labelsize=6); ax.grid(alpha=.35)
plt.setp(ax.get_xticklabels(), rotation=90)
ax.set_xlim(bx[0]-3, bx[2]+3); ax.set_ylim(bx[1]-3, bx[3]+3); ax.set_aspect('equal')
fig.savefig('dbg_%d.png' % (i+1), dpi=80, bbox_inches='tight')
