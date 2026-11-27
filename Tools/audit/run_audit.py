# run_audit.py: measure the genre maps and ours with the same ruler and save the
# numbers (audit_numbers.json) and the grids the pictures are drawn from (audit_out/).
#   python3 run_audit.py <folder> all     every map (about 15 minutes)
#   python3 run_audit.py <folder> ours    just our six maps (no downloads needed)
#   python3 run_audit.py <folder> genre   just the CS2 and Valorant maps
# <folder> holds the downloaded genre files (see genre.py for where they come from).
# audit_baseline_2026-11-26.json is the audit's snapshot, kept for comparison: every map as
# it stood on the morning of 26 Nov 2026, before the wall heights and Willow Court changed.
# (Our six were re-measured that day after ours.py stopped seeing through thin fences.)
import json, os, sys
import numpy as np
from scipy import ndimage
import ruler as R, genre as G, ours as O

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'audit_out')
os.makedirs(OUT, exist_ok=True)


def keep_mask(mask, pts, res, r=15.0):
    """Everything except the ground within r meters of a spawn."""
    H, W = mask.shape; Y, X = np.mgrid[0:H, 0:W]
    keep = np.ones(mask.shape, bool)
    for k in ('att', 'dfn'):
        if k in pts: keep &= np.hypot(X - pts[k][0], Y - pts[k][1]) * res > r
    return keep


def one(label, family, mask, res, pts, see=None, heights=6.4):
    mask = R.largest_part(mask)
    m = R.measure(label, mask, res, pts, keep_mask(mask, pts, res), see=see, heights=heights)
    m['family'] = family
    np.savez_compressed(os.path.join(OUT, label.replace(' ', '_') + '.npz'), mask=m['_mask'], w=m['_w'], P=m['_P'], deg=m['_deg'],
                        pts=np.array([[k, v[0], v[1]] for k, v in pts.items()], dtype=object), res=res)
    runs = m.pop('runs', {})
    m['runs'] = {'%s>%s' % k: v for k, v in runs.items()}
    for k in ('_w', '_mask', '_P', '_deg'): m.pop(k)
    print('%-22s floor %5.0f m2  solid %2.0f%%  widths %s  path median %4.1f m  exposure %3.0f deg  safe %2.0f%%  allround %2.0f%%  view %4.1f m  to cover %.1f m  wall rise %4.1f deg  low %2.0f%%' % (
        label, m['floor_m2'], 100 * m['solid_share'], ' '.join('%2.0f' % (100 * s) for s in m['width_shares']), m['path_median'],
        m['expo_median'], 100 * m['expo_safe'], 100 * m['expo_allround'], m['view_mean'], m['to_cover'], m['rise_mean'], 100 * m['low_share']), flush=True)
    print('   rise if walls were 3 m %4.1f  longest look median %4.1f m  spots with a 40 m look %2.0f%%  angles to hold %2.0f%%  tucked %2.0f%%' % (
        m['rise3_mean'], m['long_median'], 100 * m['long40'], 100 * m['hold_share'], 100 * m['tucked_share']), flush=True)
    for nm, e in m.get('entrances', {}).items():
        print('   site %s: ways in %4.1f m across %d openings (within 25 m); from attackers %4.1f m across %d' % (
            nm, e['ring_width'], e['ring_count'], e.get('att_width', float('nan')), e.get('att_count', 0)), flush=True)
    if m['runs']:
        r = m['runs']; g = lambda a, b: r.get('%s>%s' % (a, b), float('nan'))
        print('   runs: att>A %.0f  att>B %.0f  dfn>A %.0f  dfn>B %.0f  A>B %.0f  att>dfn %.0f m' % (g('att', 'A'), g('att', 'B'), g('dfn', 'A'), g('dfn', 'B'), g('A', 'B'), g('att', 'dfn')), flush=True)
    return m


if __name__ == '__main__':
    folder = sys.argv[1] if len(sys.argv) > 1 else None
    only = sys.argv[2] if len(sys.argv) > 2 else 'all'
    res = []
    if folder and only in ('all', 'genre'):
        for name in G.CS_MAPS:
            mask, r, pts, meta = G.cs_mask(folder, name)
            res.append(one('CS2 ' + name[3:].title(), 'CS2', mask, r, pts, heights=meta['heights']))
        for name in G.VAL:
            mask, r, pts, meta = G.val_mask(os.path.join(folder, 'val'), name)
            res.append(one('Valorant ' + name.title(), 'Valorant', mask, r, pts))
    if only in ('all', 'ours'):
        for i in range(len(O.maps)):
            mask, r, pts, meta = O.our_mask(i)
            res.append(one(meta['name'].split('. ')[-1], 'Water Wars', mask, r, pts, see=meta['see'], heights=meta['heights']))
    # one file of numbers; a partial run replaces only the maps it measured
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'audit_numbers.json')
    old = json.load(open(path)) if os.path.exists(path) else []
    fresh = {m['name'] for m in res}
    json.dump([m for m in old if m['name'] not in fresh] + res, open(path, 'w'), indent=1)
