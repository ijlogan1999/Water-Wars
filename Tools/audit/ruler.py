# ruler.py: one ruler for every map, ours or anybody's.
#
# Everything here works on a "walk mask": a top-down grid where True = ground a
# player can stand on and False = anything solid (walls, buildings, crates, fences,
# out of bounds). Give it a mask and how many meters one cell is, and it measures:
#
#   solid share     how much of the map's outline is solid stuff, not floor
#   width shares    how much of the floor is in spaces under 3 m wide, 3-5, 5-8,
#                   8-16 and over 16 m (the width of the biggest circle of clear
#                   floor that covers a spot)
#   path widths     how wide the routes are, measured along the center line of
#                   every space (the "skeleton"), by length
#   exposure        from how many degrees around a spot an enemy 8-40 m away could
#                   see you; and how far you can see on average
#   run distances   shortest walking distance between named points
#
# The same code measures the CS2 and Valorant maps (see genre.py) and our own
# (see ours.py), so the numbers can sit side by side.
import numpy as np
from scipy import ndimage

RES = 0.25          # meters per cell used for all measuring


def largest_part(mask):
    lab, k = ndimage.label(mask)
    if k <= 1: return mask
    sizes = np.bincount(lab.ravel()); sizes[0] = 0
    return lab == np.argmax(sizes)


def hull_mask(mask):
    """Filled convex hull of the walkable floor (the map's outline)."""
    from scipy.spatial import ConvexHull
    from matplotlib.path import Path
    ys, xs = np.nonzero(mask)
    pts = np.c_[xs, ys]
    if len(pts) > 20000: pts = pts[np.random.default_rng(0).choice(len(pts), 20000, replace=False)]
    h = ConvexHull(pts)
    poly = Path(pts[h.vertices])
    Y, X = np.mgrid[0:mask.shape[0], 0:mask.shape[1]]
    return poly.contains_points(np.c_[X.ravel(), Y.ravel()]).reshape(mask.shape) | mask


def width_map(mask, res=RES, top=40.0):
    """Width (m) of the biggest clear circle covering each floor cell."""
    edt = ndimage.distance_transform_edt(mask) * res
    width = np.zeros_like(edt)
    r = res
    while r <= top / 2:
        centers = edt >= r
        if not centers.any(): break
        covered = ndimage.distance_transform_edt(~centers) * res <= r
        width[covered & mask] = 2 * r
        r += res
    return width


BINS = [(0, 3), (3, 5), (5, 8), (8, 16), (16, 1e9)]
BIN_NAMES = ['under 3 m', '3-5 m', '5-8 m', '8-16 m', 'over 16 m']


def width_shares(mask, res=RES, keep=None):
    w = width_map(mask, res)
    sel = mask if keep is None else (mask & keep)
    n = sel.sum()
    return [float(((w >= a) & (w < b) & sel).sum() / n) for a, b in BINS], w


def path_widths(mask, res=RES, keep=None):
    """Widths along the center lines of every space, weighted by length."""
    from skimage.morphology import medial_axis
    skel, dist = medial_axis(mask, return_distance=True)
    if keep is not None: skel = skel & keep
    w = dist[skel] * 2 * res
    return w


def _rays(P, see, res, far):
    """Cast 360 rays from each point through `see`. Returns cell coords, the clear flag
    along each ray, and how many steps each ray stays clear."""
    H, W = see.shape
    ang = np.radians(np.arange(360))
    steps = np.arange(res, far + res, res / 2) / res        # in cells
    dx = np.cos(ang)[:, None] * steps; dy = np.sin(ang)[:, None] * steps
    gx = np.rint(P[:, 0, None, None] + dx).astype(int)
    gy = np.rint(P[:, 1, None, None] + dy).astype(int)
    inside = (gx >= 0) & (gx < W) & (gy >= 0) & (gy < H)
    c = np.zeros(gx.shape, bool); c[inside] = see[gy[inside], gx[inside]]
    reach = np.cumprod(c, axis=2).astype(bool)
    return gx, gy, inside, reach, steps * res


def sample_points(mask, res=RES, sample=2.0, keep=None):
    H, W = mask.shape
    step = max(1, int(round(sample / res)))
    ys, xs = np.mgrid[step // 2:H:step, step // 2:W:step]
    stand = ndimage.binary_erosion(mask, iterations=max(1, int(round(0.4 / res))))
    ok = stand[ys, xs]
    if keep is not None: ok &= keep[ys, xs]
    return np.c_[xs[ok], ys[ok]].astype(float), stand


def exposure(mask, res=RES, sample=2.0, rng=40.0, near=8.0, keep=None, see=None, heights=6.4, eye=1.6):
    """For sample spots:
      deg    degrees (0-360) from which an enemy standing near..rng m away could see you
      view   mean clear view distance (capped at 60 m)
      rise   how high the first wall in each direction rises above your eye line, in
             degrees, averaged over all directions (0 where nothing taller than you is there)
      low    share of directions where that rise is under 10 degrees (sky and far stuff)
      longest  the longest clear look from the spot (capped at 60 m), counting only views
               at least 3 degrees wide
      rise3  the rise again, pretending every wall is 3 m tall (so only closeness counts)
    `see` = cells you can see across (floor plus anything low enough to see over); it
    defaults to the walk mask. `heights` = wall heights in meters: one number for every
    wall, or a grid with a height for each solid cell."""
    if see is None: see = mask
    P, stand = sample_points(mask, res, sample, keep)
    H, W = mask.shape
    deg = np.zeros(len(P)); view = np.zeros(len(P)); rise = np.zeros(len(P)); low = np.zeros(len(P))
    longest = np.zeros(len(P)); rise3 = np.zeros(len(P))
    for k0 in range(0, len(P), 150):
        p = P[k0:k0 + 150]
        gx, gy, inside, reach, dist_m = _rays(p, see, res, 60.0)
        s = np.zeros(gx.shape, bool); s[inside] = stand[gy[inside], gx[inside]]
        band = (dist_m[None, None, :] >= near) & (dist_m[None, None, :] <= rng)
        deg[k0:k0 + 150] = (reach & s & band).any(axis=2).sum(axis=1)
        n_clear = reach.sum(axis=2)                                   # steps before the first wall
        view[k0:k0 + 150] = (n_clear * (res / 2)).mean(axis=1)
        # a "look" has to be a wedge at least 3 degrees wide, so a hairline gap does not count
        wedge = np.minimum(np.minimum(np.roll(n_clear, 1, axis=1), n_clear), np.roll(n_clear, -1, axis=1))
        longest[k0:k0 + 150] = (wedge * (res / 2)).max(axis=1)
        hit = np.minimum(n_clear, gx.shape[2] - 1)
        hx = np.take_along_axis(gx, hit[..., None], axis=2)[..., 0]
        hy = np.take_along_axis(gy, hit[..., None], axis=2)[..., 0]
        ok = (hx >= 0) & (hx < W) & (hy >= 0) & (hy < H) & (n_clear < gx.shape[2])
        if np.isscalar(heights): h = np.where(ok, heights, 0.0)
        else:
            h = np.zeros(hx.shape); h[ok] = heights[hy[ok], hx[ok]]
        d = np.maximum(dist_m[hit], res)
        a = np.degrees(np.arctan2(np.maximum(h - eye, 0.0), d))
        rise[k0:k0 + 150] = a.mean(axis=1); low[k0:k0 + 150] = (a < 10).mean(axis=1)
        # the same thing if every wall were exactly 3 m tall: shows how close the walls are
        rise3[k0:k0 + 150] = np.degrees(np.arctan2(np.where(ok, 3.0 - eye, 0.0), d)).mean(axis=1)
    return P, deg, view, rise, low, longest, rise3


def run_distances(mask, points, res=RES):
    """Shortest walking distance (m) from each named point to every other.
    points = {name: (col, row)} in mask cells."""
    import heapq
    H, W = mask.shape
    walk = ndimage.binary_erosion(mask, iterations=1)
    out = {}
    names = list(points)
    def snap(c, r):
        c, r = int(round(c)), int(round(r))
        if 0 <= r < H and 0 <= c < W and walk[r, c]: return r, c
        d = ndimage.distance_transform_edt(~walk, return_indices=True)[1]
        r = min(max(r, 0), H - 1); c = min(max(c, 0), W - 1)
        return int(d[0][r, c]), int(d[1][r, c])
    cells = {n: snap(*points[n]) for n in names}
    nb = [(-1, 0, 1), (1, 0, 1), (0, -1, 1), (0, 1, 1), (-1, -1, 1.4142), (-1, 1, 1.4142), (1, -1, 1.4142), (1, 1, 1.4142)]
    for a in names:
        dist = np.full((H, W), np.inf); r0, c0 = cells[a]; dist[r0, c0] = 0
        pq = [(0.0, r0, c0)]
        while pq:
            d, r, c = heapq.heappop(pq)
            if d > dist[r, c]: continue
            for dr, dc, w in nb:
                rr, cc = r + dr, c + dc
                if 0 <= rr < H and 0 <= cc < W and walk[rr, cc]:
                    nd = d + w
                    if nd < dist[rr, cc]: dist[rr, cc] = nd; heapq.heappush(pq, (nd, rr, cc))
        for b in names:
            if b != a: out[(a, b)] = float(dist[cells[b]] * res)
    return out


def entrances(mask, src, dst, res=RES):
    """How much floor would you have to wall off to cut `src` from `dst`? (Both are
    True/False grids.) Returns (total width in meters, number of separate openings,
    the cut cells). This is the "doorways" number: a site with three 3 m doors gives
    about 9 m and 3. Uses a maximum-flow search on the grid, moving in 4 directions."""
    from scipy.sparse import csr_matrix
    from scipy.sparse.csgraph import maximum_flow, breadth_first_order
    H, W = mask.shape
    idx = -np.ones(mask.shape, int); n = int(mask.sum()); idx[mask] = np.arange(n)
    BIG = 1 << 20
    S, T = 2 * n, 2 * n + 1
    rows = [np.arange(n)]; cols = [np.arange(n) + n]; caps = [np.ones(n, int)]      # in -> out, one each
    inner = mask & ~src & ~dst
    c = np.ones(n, int); c[idx[src & mask]] = BIG; c[idx[dst & mask]] = BIG; caps[0] = c
    for dy, dx in ((0, 1), (1, 0)):
        a = mask[:H - dy, :W - dx] & mask[dy:, dx:]
        u = idx[:H - dy, :W - dx][a]; v = idx[dy:, dx:][a]
        rows += [u + n, v + n]; cols += [v, u]; caps += [np.full(len(u), BIG), np.full(len(u), BIG)]
    su = idx[src & mask]; du = idx[dst & mask]
    rows += [np.full(len(su), S), du + n]; cols += [su, np.full(len(du), T)]
    caps += [np.full(len(su), BIG), np.full(len(du), BIG)]
    G = csr_matrix((np.concatenate(caps).astype(np.int32), (np.concatenate(rows), np.concatenate(cols))), shape=(2 * n + 2, 2 * n + 2))
    r = maximum_flow(G, S, T, method='dinic')
    resid = (G - r.flow); resid.data = np.maximum(resid.data, 0); resid.eliminate_zeros()
    seen = np.zeros(2 * n + 2, bool); seen[breadth_first_order(resid, S, return_predecessors=False)] = True
    cut_nodes = np.nonzero(seen[:n] & ~seen[n:2 * n])[0]
    cut = np.zeros(mask.shape, bool); ys, xs = np.nonzero(mask); cut[ys[cut_nodes], xs[cut_nodes]] = True
    _, k = ndimage.label(ndimage.binary_dilation(cut, iterations=2))
    return float(r.flow_value * res), int(k), cut


KID = 0.5      # meters: half the gap a kid needs to get through (with a little room)


def site_entrances(mask, points, nm, res=RES):
    """The ways into site `nm`: total doorway width and number of openings, (a) on the
    tightest ring between 6 m and 25 m from the site's center and (b) between the
    attackers' spawn and the site. Measured where a kid's center can go, so gaps too
    small to squeeze through do not count, then the kid's width is added back."""
    stand = largest_part(ndimage.binary_erosion(mask, iterations=int(round(KID / res))))
    w, n, cut = entrances(stand, disc(mask, points[nm], 6.0, res), ~disc(mask, points[nm], 25.0, res), res)
    out = dict(ring_width=w + 2 * KID * n, ring_count=n)
    if 'att' in points:
        w2, n2, _ = entrances(stand, disc(mask, points['att'], 6.0, res), disc(mask, points[nm], 6.0, res), res)
        out.update(att_width=w2 + 2 * KID * n2, att_count=n2)
    return out, cut


def disc(mask, c, r, res=RES):
    H, W = mask.shape; Y, X = np.mgrid[0:H, 0:W]
    return np.hypot(X - c[0], Y - c[1]) * res <= r


def measure(name, mask, res=RES, points=None, keep=None, do_runs=True, see=None, heights=6.4):
    """All the numbers for one map. `keep` = cells to count (to leave out spawns)."""
    mask = largest_part(mask)
    hull = hull_mask(mask)
    shares, w = width_shares(mask, res, keep)
    pw = path_widths(mask, res, keep)
    P, deg, view, rise, low, longest, rise3 = exposure(mask, res, keep=keep, see=see, heights=heights)
    sel = mask if keep is None else mask & keep
    edt = ndimage.distance_transform_edt(mask) * res
    ys, xs = np.nonzero(mask)
    out = dict(name=name,
               floor_m2=float(mask.sum() * res * res),
               box_m=(float((xs.max() - xs.min() + 1) * res), float((ys.max() - ys.min() + 1) * res)),
               solid_share=float(1 - mask.sum() / hull.sum()),
               width_shares=shares,
               under8=float(sum(shares[:3])),
               median_width=float(np.median(w[sel])),
               to_cover=float(edt[sel].mean()),
               path_median=float(np.median(pw)), path_p25=float(np.percentile(pw, 25)), path_p75=float(np.percentile(pw, 75)),
               path_under5=float((pw < 5).mean()), path_under8=float((pw < 8).mean()),
               expo_median=float(np.median(deg)), expo_safe=float((deg <= 120).mean()), expo_allround=float((deg >= 270).mean()),
               view_mean=float(view.mean()), rise_mean=float(rise.mean()), low_share=float(low.mean()),
               rise3_mean=float(rise3.mean()),
               long_median=float(np.median(longest)), long40=float((longest >= 40).mean()),
               # an angle to hold: hard to see (90 degrees or less) but looking down a lane 15 m or longer
               hold_share=float(((deg <= 90) & (longest >= 15)).mean()),
               # a place to wait: seen from 45 degrees or less
               tucked_share=float((deg <= 45).mean()),
               floor_per_player=float(mask.sum() * res * res / 10))
    if points:
        out['entrances'] = {nm: site_entrances(mask, points, nm, res)[0] for nm in points if nm not in ('att', 'dfn')}
    if points and do_runs: out['runs'] = run_distances(mask, points, res)
    out['_w'] = w; out['_mask'] = mask; out['_P'] = P; out['_deg'] = deg
    return out
