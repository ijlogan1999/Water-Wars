# figures.py: pictures for the audit, drawn from what run_audit.py saved in audit_out/.
#   python3 figures.py <folder to write PNGs into>
# Every panel is drawn at the same scale, so sizes compare directly.
# Two more pictures are made by hand from Python (see the functions at the bottom):
#   long_looks(png, map)              one of our maps: how far each spot can see, and the ways into its sites
#   looks_data(npz, map) then before_after(png, old_npz, new_npz)
#                                     the same map before and after a change, side by side
import os, sys
import numpy as np
import matplotlib; matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.colors import ListedColormap
from matplotlib.patches import Patch

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'audit_out')
INK, QUIET = '#191919', '#666666'
LANE, ROOM, OPEN = '#8a8a8a', '#cfcfcf', '#257de0'      # under 8 m, 8-16 m, over 16 m wide
SPAN = 180.0                                             # meters shown in every panel


def panel(ax, label, title, note):
    d = np.load(os.path.join(OUT, label.replace(' ', '_') + '.npz'), allow_pickle=True)
    mask, w, res = d['mask'], d['w'], float(d['res'])
    cls = np.zeros(mask.shape, int)                      # 0 = solid or outside
    cls[mask & (w < 8)] = 1; cls[mask & (w >= 8) & (w < 16)] = 2; cls[mask & (w >= 16)] = 3
    ys, xs = np.nonzero(mask)
    cx, cy = (xs.min() + xs.max()) / 2 * res, (ys.min() + ys.max()) / 2 * res
    H, W = mask.shape
    ax.imshow(cls, cmap=ListedColormap(['#ffffff', LANE, ROOM, OPEN]), vmin=0, vmax=3,
              extent=(0, W * res, H * res, 0), interpolation='nearest')
    ax.set_xlim(cx - SPAN / 2, cx + SPAN / 2); ax.set_ylim(cy + SPAN / 2, cy - SPAN / 2)
    ax.set_aspect('equal'); ax.set_xticks([]); ax.set_yticks([])
    for s in ax.spines.values(): s.set_color('#e3e3e3')
    ax.set_title(title, fontsize=11, color=INK, loc='left', pad=14)
    ax.text(0, 1.012, note, transform=ax.transAxes, fontsize=8.5, color=QUIET, va='bottom')


def same_scale(path, numbers):
    by = {m['name']: m for m in numbers}
    rows = [['CS2 Ancient', 'CS2 Mirage', 'Valorant Ascent', 'CS2 Dust2'],
            ['Willow Court', 'Pine St Backyards', 'Park Run', 'Schoolyard']]
    fig, axes = plt.subplots(2, 4, figsize=(15, 8.4))
    for r, row in enumerate(rows):
        for c, name in enumerate(row):
            m = by[name]
            note = '%d%% open ground  ·  %.0f m² of floor' % (round(100 * m['width_shares'][4]), m['floor_m2'])
            panel(axes[r, c], name, name + ('' if r == 0 else '  (ours)'), note)
    ax = axes[0, 0]
    ax.plot([ax.get_xlim()[0] + 8, ax.get_xlim()[0] + 58], [ax.get_ylim()[0] - 8] * 2, color=INK, lw=2, solid_capstyle='butt')
    ax.text(ax.get_xlim()[0] + 8, ax.get_ylim()[0] - 12, '50 m', fontsize=8.5, color=INK, va='bottom')
    fig.legend(handles=[Patch(color=LANE, label='Lanes: under 8 m wide'), Patch(color=ROOM, label='Rooms: 8 to 16 m wide'),
                        Patch(color=OPEN, label='Open ground: over 16 m wide')],
               loc='lower center', ncol=3, frameon=False, fontsize=10, bbox_to_anchor=(0.5, 0.005))
    fig.suptitle('Their maps are lanes and rooms. Four of ours are mostly open ground.', fontsize=15, color=INK, x=0.012, ha='left', y=0.985)
    fig.text(0.012, 0.935, 'Walkable floor only, all eight maps at the same scale. White is solid or out of bounds.', fontsize=10, color=QUIET)
    fig.subplots_adjust(left=0.012, right=0.988, top=0.865, bottom=0.07, wspace=0.04, hspace=0.2)
    fig.savefig(path, dpi=140, facecolor='white'); plt.close(fig)


if __name__ == '__main__':
    import json
    dest = sys.argv[1]; os.makedirs(dest, exist_ok=True)
    numbers = json.load(open(os.path.join(HERE, 'audit_numbers.json')))
    same_scale(os.path.join(dest, 'same-scale.png'), numbers)


def long_looks(path, index=1):
    """One of our maps: how far you can see from each spot, and the ways into each site."""
    import ruler as R, ours as O
    from run_audit import keep_mask
    mask, res, pts, meta = O.our_mask(index)
    mask = R.largest_part(mask)
    P, deg, view, rise, low, longest, rise3 = R.exposure(mask, res, keep=keep_mask(mask, pts, res), see=meta['see'], heights=meta['heights'])
    H, W = mask.shape
    fig, ax = plt.subplots(figsize=(8.6, 10.4))
    ax.imshow(np.where(mask, 1, 0), cmap=ListedColormap(['#ffffff', '#ececec']), extent=(0, W * res, H * res, 0), interpolation='nearest')
    steps = [(0, 20, 0.16), (20, 30, 0.38), (30, 40, 0.66), (40, 1e9, 1.0)]
    names = ['under 20 m', '20 to 30 m', '30 to 40 m', '40 m or more']
    for (a, b, alpha), nm in zip(steps, names):
        k = (longest >= a) & (longest < b)
        ax.scatter(P[k, 0] * res, P[k, 1] * res, s=20, marker='s', color=OPEN, alpha=alpha, lw=0, label=nm)
    for nm in [k for k in pts if k not in ('att', 'dfn')]:
        e, cut = R.site_entrances(mask, pts, nm, res); wdt, n = e['ring_width'], e['ring_count']
        ys, xs = np.nonzero(cut)
        ax.scatter(xs * res, ys * res, s=7, marker='s', color='#f47230', lw=0, label='Ways into a site' if nm == 'A' else None, zorder=3)
        ax.text(pts[nm][0] * res, pts[nm][1] * res, nm, fontsize=15, color=INK, ha='center', va='center', weight='bold')
        ax.text(pts[nm][0] * res, pts[nm][1] * res + 5.5, '%d opening%s, %.0f m' % (n, '' if n == 1 else 's', wdt), fontsize=8.5, color=INK, ha='center', va='center')
    for k, word in (('att', 'Attackers'), ('dfn', 'Defenders')):
        ax.text(pts[k][0] * res, pts[k][1] * res, word, fontsize=9, color=QUIET, ha='center', va='center')
    ys, xs = np.nonzero(mask)
    ax.set_xlim(xs.min() * res - 3, xs.max() * res + 3); ax.set_ylim(ys.max() * res + 3, ys.min() * res - 3)
    ax.set_aspect('equal'); ax.set_xticks([]); ax.set_yticks([])
    for s in ax.spines.values(): s.set_visible(False)
    ax.legend(loc='upper center', bbox_to_anchor=(0.5, -0.01), ncol=5, frameon=False, fontsize=9, title='Longest clear view from each spot', title_fontsize=9, handletextpad=0.2, columnspacing=1.2)
    name = meta['name'].split('. ')[-1]
    fig.suptitle('%s: %d%% of spots can see 40 m or more' % (name, round(100 * (longest >= 40).mean())), fontsize=14, color=INK, x=0.03, ha='left', y=0.985)
    fig.text(0.03, 0.948, 'On the real CS2 and Valorant maps it is 7 to 26% (Dust2: 55%). Orange marks the ways into each site.', fontsize=8.6, color=QUIET)
    fig.subplots_adjust(left=0.03, right=0.97, top=0.93, bottom=0.075)
    fig.savefig(path, dpi=140, facecolor='white'); plt.close(fig)


def looks_data(path, index=1):
    """Save what before_after() draws for one of our maps: the floor, and how far each spot can see."""
    import ruler as R, ours as O
    from run_audit import keep_mask
    mask, res, pts, meta = O.our_mask(index)
    mask = R.largest_part(mask)
    P, deg, view, rise, low, longest, rise3 = R.exposure(mask, res, keep=keep_mask(mask, pts, res), see=meta['see'], heights=meta['heights'])
    np.savez_compressed(path, mask=mask, res=res, P=P, longest=longest, name=meta['name'].split('. ')[-1],
                        pts=np.array([[k, v[0], v[1]] for k, v in pts.items()], dtype=object))


def before_after(path, before, after, labels=('This morning', 'Now')):
    """Two saved looks_data() files of the same map side by side, same scale and same colors."""
    A = [np.load(f, allow_pickle=True) for f in (before, after)]
    fig, axes = plt.subplots(1, 2, figsize=(13.2, 9.6))
    steps = [(0, 20, 0.16), (20, 30, 0.38), (30, 40, 0.66), (40, 1e9, 1.0)]
    names = ['under 20 m', '20 to 30 m', '30 to 40 m', '40 m or more']
    for ax, d, lab in zip(axes, A, labels):
        mask, res, P, longest = d['mask'], float(d['res']), d['P'], d['longest']
        H, W = mask.shape
        ax.imshow(np.where(mask, 1, 0), cmap=ListedColormap(['#ffffff', '#ececec']), extent=(0, W * res, H * res, 0), interpolation='nearest')
        for (a, b, alpha), nm in zip(steps, names):
            k = (longest >= a) & (longest < b)
            ax.scatter(P[k, 0] * res, P[k, 1] * res, s=17, marker='s', color=OPEN, alpha=alpha, lw=0, label=nm)
        words = {'att': 'Attackers', 'dfn': 'Defenders'}
        for k, x, y in d['pts']:
            big = k not in words
            ax.text(float(x) * res, float(y) * res, words.get(k, k), fontsize=15 if big else 9, color=INK if big else QUIET, ha='center', va='center', weight='bold' if big else 'normal')
        ys, xs = np.nonzero(A[0]['mask'] | (A[1]['mask'] if A[1]['mask'].shape == A[0]['mask'].shape else False))
        ax.set_xlim(xs.min() * res - 3, xs.max() * res + 3); ax.set_ylim(ys.max() * res + 3, ys.min() * res - 3)
        ax.set_aspect('equal'); ax.set_xticks([]); ax.set_yticks([])
        for s in ax.spines.values(): s.set_visible(False)
        ax.set_title('%s: %d%% of spots can see 40 m or more' % (lab, round(100 * (longest >= 40).mean())), fontsize=11.5, color=INK, loc='left', pad=6)
    axes[0].legend(loc='upper center', bbox_to_anchor=(1.03, -0.01), ncol=4, frameon=False, fontsize=9, title='Longest clear view from each spot', title_fontsize=9, handletextpad=0.2, columnspacing=1.2)
    fig.suptitle('%s, before and after' % str(A[1]['name']), fontsize=14, color=INK, x=0.03, ha='left', y=0.985)
    fig.text(0.03, 0.945, 'On the real CS2 and Valorant maps 7 to 26% of spots can see 40 m or more (Dust2: 55%).', fontsize=8.8, color=QUIET)
    fig.subplots_adjust(left=0.03, right=0.97, top=0.9, bottom=0.075, wspace=0.04)
    fig.savefig(path, dpi=140, facecolor='white'); plt.close(fig)
