# blueprint.py: maps drawn from a blank page.
#
# The first six maps were cut out of a real-size suburb, and the audit showed why they
# played like fields: a real suburb is about 20% buildings, a tactical-shooter map is
# 40% or more solid, with 4 to 6 m paths cut through it. So the remade maps are not cut
# out of anything. Each one is a DRAWING MADE OF LETTERS (see newmaps.py): one letter is
# one 2 m square, north is at the top, and everything starts solid. You carve the paths.
#
#   ground you can walk on
#     .  grass          ,  pavement        :  asphalt (street)      ;  dirt
#     1  where the attackers start         2  where the defenders start
#     A  B  the middle of each site
#     g  floor of an OPEN garage (draw 3 wide and 4 deep; a short end that touches
#        ground you can walk on gets a door: the big door on the pavement side)
#     ^ v < >  a treehouse; the arrow is the way its ramp runs out (keep 5 squares clear)
#
#   solid things (nobody walks through, nobody sees through unless it says so)
#     H I J K  two-storey house (8.8 m). Squares with the same letter that touch are one
#              house, so give neighbours different letters.
#     h i j k  one-storey house (5.7 m)
#     G  garage with its door shut (4.2 m)      S  shed (3.2 m)
#     X  hedge, a full 2 m thick (4.2 m)        T  trees and brush (4.8 m)
#     - | +  board fence (3 m) down the middle of the square: - runs east-west,
#            | runs north-south, + is a corner or a T
#     o  crates, 1.3 m: you can shoot over them
#     V  a parked vehicle: 2 in a row is a car (you can see over it), 3 a van,
#        4 a box truck, 5 a motorhome
#     _  a closed yard: grass and a tree nobody can get to. It must be walled in.
#
# compile(bp) checks the drawing (the outside ring is solid, every square of floor can be
# reached, no gaps too thin to use) and turns it into the same pieces the game and the
# other tools already understand (props.py). export_cs.py writes them into the game.
#
#   python3 blueprint.py                 check every drawing, write drawings/new-<name>.png
#   python3 blueprint.py "Willow Court"  the same for one map, and measure it against
#                                        the audit's spec (takes about a minute)
import math, os, sys
import numpy as np

CELL = 2.0                        # meters per letter (MapBuilder.BlueprintCell)
GROUND = {'.': None, ',': 0, ':': 1, ';': 2}          # paint number, None = grass
MARKS = {'1': 'att', '2': 'dfn', 'A': 'A', 'B': 'B'}
RAMP = {'>': 0, '^': 90, '<': 180, 'v': 270}
HOUSES = {**{c: 8.8 for c in 'HIJK'}, **{c: 5.7 for c in 'hijk'}}
BOXES = {'G': 'lockup', 'S': 'shed', 'X': 'bush', 'T': 'thicket', 'o': 'crate', '_': 'yard'}
FENCE = '-|+'
VEHICLES = {2: 'car', 3: 'van', 4: 'truck', 5: 'rv'}
WALK = set(GROUND) | set(MARKS) | set(RAMP) | {'g'}
KNOWN = WALK | set(HOUSES) | set(BOXES) | set(FENCE) | {'V'}
N, E, S, W = (0, -1), (1, 0), (0, 1), (-1, 0)         # (column, row) steps; row 0 is the north edge


class Problem(Exception):
    pass


def rows_of(bp):
    rows = [ln.split()[0] for ln in bp['grid'].strip('\n').split('\n') if ln.strip()]
    if len({len(r) for r in rows}) != 1:
        raise Problem('%s: rows are not all the same length: %s' % (bp['name'], [(i, len(r)) for i, r in enumerate(rows) if len(r) != len(rows[0])]))
    return rows


def rectangles(rows, letters):
    """Join touching squares of the same letter into rectangles: (letter, col, row, wide, tall)."""
    Hn, Wn = len(rows), len(rows[0]); used = [[False] * Wn for _ in rows]; out = []
    for r in range(Hn):
        for c in range(Wn):
            ch = rows[r][c]
            if ch not in letters or used[r][c]: continue
            w = 1
            while c + w < Wn and rows[r][c + w] == ch and not used[r][c + w]: w += 1
            h = 1
            while r + h < Hn and all(rows[r + h][c + k] == ch and not used[r + h][c + k] for k in range(w)): h += 1
            for rr in range(r, r + h):
                for cc in range(c, c + w): used[rr][cc] = True
            out.append((ch, c, r, w, h))
    return out


def compile(bp, origin=(0.0, 0.0)):
    """Turn a drawing into a map the other tools understand (the same dict parse.py makes)."""
    rows = rows_of(bp); Hn, Wn = len(rows), len(rows[0]); name = bp['name']
    ox, oy = origin
    at = lambda c, r: rows[r][c] if 0 <= r < Hn and 0 <= c < Wn else None
    walk = lambda c, r: at(c, r) in WALK
    X = lambda c: ox + c * CELL                      # west edge of column c
    Y = lambda r: oy + (Hn - r) * CELL               # north edge of row r
    mid = lambda c, r: (X(c) + CELL / 2, Y(r) - CELL / 2)
    box = lambda c, r, w, h: (X(c) + w * CELL / 2, Y(r) - h * CELL / 2, w * CELL, h * CELL)   # center x, y, size x, y
    bad = []

    # ---- the drawing has to make sense -------------------------------------------
    for r in range(Hn):
        for c in range(Wn):
            ch = rows[r][c]
            if ch not in KNOWN: bad.append('row %d col %d: no such letter %r' % (r, c, ch))
            if (r in (0, Hn - 1) or c in (0, Wn - 1)) and ch in WALK: bad.append('row %d col %d: the outside ring must be solid' % (r, c))
    if bad: raise Problem(name + ':\n  ' + '\n  '.join(bad[:30]))
    where = {}
    for r in range(Hn):
        for c in range(Wn):
            if rows[r][c] in MARKS: where.setdefault(rows[r][c], []).append((c, r))
    for m in MARKS:
        if len(where.get(m, [])) != 1: bad.append('needs exactly one %r, found %d' % (m, len(where.get(m, []))))
    for r in range(Hn):
        for c in range(Wn):
            ch = rows[r][c]
            if ch in WALK:
                # two squares of floor that only touch at a corner: nobody fits, but a view would leak
                for dc, dr in ((1, 1), (-1, 1)):
                    if walk(c + dc, r + dr) and not walk(c + dc, r) and not walk(c, r + dr):
                        bad.append('row %d col %d: floor touches floor only at a corner (toward row %d col %d)' % (r, c, r + dr, c + dc))
            if ch == '_':
                for dc in (-1, 0, 1):
                    for dr in (-1, 0, 1):
                        if walk(c + dc, r + dr): bad.append('row %d col %d: closed yard is open to floor at row %d col %d' % (r, c, r + dr, c + dc))
            if ch in '-|':
                # a fence runs down the middle of its square; with a wall beside it there would be a 0.9 m slot
                for d in ((N, S) if ch == '-' else (E, W)):
                    nb = at(c + d[0], r + d[1])
                    if nb is not None and nb not in WALK and nb not in FENCE and nb != '_':
                        bad.append('row %d col %d: fence %r runs right beside a solid %r: leave floor or yard on both sides' % (r, c, ch, nb))
    # every square of floor can be reached from the attackers' start
    if not bad:
        seen = {where['1'][0]}; todo = [where['1'][0]]
        while todo:
            c, r = todo.pop()
            for dc, dr in (N, E, S, W):
                if walk(c + dc, r + dr) and (c + dc, r + dr) not in seen: seen.add((c + dc, r + dr)); todo.append((c + dc, r + dr))
        lost = [(r, c) for r in range(Hn) for c in range(Wn) if walk(c, r) and (c, r) not in seen]
        if lost: bad.append('%d squares of floor cannot be reached, e.g. (row, col) %s' % (len(lost), lost[:8]))
    if bad: raise Problem(name + ':\n  ' + '\n  '.join(bad[:40]))

    props = []
    # ---- ground colors -------------------------------------------------------------
    def ground(c, r):
        ch = rows[r][c]
        if ch in GROUND: return GROUND[ch]
        if ch == 'g': return 0
        if ch in MARKS or ch in RAMP:      # a marker takes the color of the floor next to it
            near = [GROUND[at(c + dc, r + dr)] for dc, dr in (W, E, N, S) if at(c + dc, r + dr) in GROUND]
            return max(set(near), key=near.count) if near else None
        return 'solid'
    gl = [''.join({None: ' ', 0: 'a', 1: 'b', 2: 'c', 'solid': ' '}[ground(c, r)] for c in range(Wn)) for r in range(Hn)]
    for ch, c, r, w, h in rectangles(gl, 'abc'):
        x, y, sx, sy = box(c, r, w, h); props.append(('paint', x, y, sx, sy, 'abc'.index(ch)))

    # ---- which way a block should face: the side with the most floor along it -----------
    def floor_along(c, r, w, h):
        return {0: sum(walk(c + w, rr) for rr in range(r, r + h)), 90: sum(walk(cc, r - 1) for cc in range(c, c + w)),
                180: sum(walk(c - 1, rr) for rr in range(r, r + h)), 270: sum(walk(cc, r + h) for cc in range(c, c + w))}

    # ---- houses ------------------------------------------------------------------------
    for ch, c, r, w, h in rectangles(rows, set(HOUSES)):
        x, y, sx, sy = box(c, r, w, h); f = floor_along(c, r, w, h)
        # the roof's ridge runs the long way, so the front door goes on a long side
        sides = (90, 270) if w >= h else (0, 180)
        a = max(sides, key=lambda k: (f[k], k == 270 or k == 0))
        props.append(('house', x, y, a, max(sx, sy), min(sx, sy), HOUSES[ch]) if w != h else ('house', x, y, a, sx, sy, HOUSES[ch]))
    # ---- plain boxes -------------------------------------------------------------------
    for ch, c, r, w, h in rectangles(rows, set(BOXES)):
        x, y, sx, sy = box(c, r, w, h)
        if ch == 'G':      # the shut door faces the floor; a box prop's front is south at angle 0
            f = floor_along(c, r, w, h); a = max(f, key=lambda k: f[k])
            props.append(('lockup', x, y, {270: 0, 0: 90, 90: 180, 180: 270}[a], sx if a in (90, 270) else sy, sy if a in (90, 270) else sx))
        else: props.append((BOXES[ch], x, y, 0, sx, sy))
    # ---- parked vehicles ---------------------------------------------------------------
    for ch, c, r, w, h in rectangles(rows, 'V'):
        n = max(w, h)
        if min(w, h) != 1 or n not in VEHICLES: raise Problem('%s: row %d col %d: a vehicle is 2 to 5 squares in one line, this is %d x %d' % (name, r, c, w, h))
        x, y, _, _ = box(c, r, w, h); props.append((VEHICLES[n], x, y, 0 if w > 1 else 90))
    # ---- open garages --------------------------------------------------------------------
    for ch, c, r, w, h in rectangles(rows, 'g'):
        if (w, h) not in ((3, 4), (4, 3)): raise Problem('%s: row %d col %d: an open garage is 3 x 4 squares, this is %d x %d' % (name, r, c, w, h))
        ends = [(90, (c + 1, r - 1)), (270, (c + 1, r + h))] if h == 4 else [(180, (c - 1, r + 1)), (0, (c + w, r + 1))]
        opens = [(a, cr) for a, cr in ends if walk(*cr)]
        if not opens: raise Problem('%s: row %d col %d: the open garage has no floor at either end' % (name, r, c))
        opens.sort(key=lambda o: ground(*o[1]) is None)       # the big door goes on the paved (driveway) side
        x, y, _, _ = box(c, r, w, h); props.append(('garage', x, y, opens[0][0], 1 if len(opens) > 1 else 0))
    # ---- treehouses ----------------------------------------------------------------------
    for r in range(Hn):
        for c in range(Wn):
            if rows[r][c] in RAMP: props.append(('treehouse',) + mid(c, r) + (RAMP[rows[r][c]],))
    # ---- fences: lines from square middle to square middle --------------------------------
    wants = {'-': (E, W), '|': (N, S), '+': (N, E, S, W)}
    horiz, vert = {}, {}
    for r in range(Hn):
        for c in range(Wn):
            ch = rows[r][c]
            if ch not in FENCE: continue
            cx, cy = mid(c, r)
            for d in wants[ch]:
                nb = at(c + d[0], r + d[1])
                opp = (-d[0], -d[1])
                if nb in WALK and ch == '+': continue                 # a corner post does not reach out over floor
                # to the square's edge; or all the way to the next fence when that one runs across
                reach = CELL if (nb is not None and nb in FENCE and opp not in wants[nb]) else CELL / 2
                if d[1] == 0: horiz.setdefault(round(cy, 3), []).append(tuple(sorted((cx, cx + d[0] * reach))))
                else: vert.setdefault(round(cx, 3), []).append(tuple(sorted((cy, cy - d[1] * reach))))
    def joined(spans):
        out = []
        for a, b in sorted(spans):
            if out and a <= out[-1][1] + 1e-6: out[-1][1] = max(out[-1][1], b)
            else: out.append([a, b])
        return out
    for y, spans in sorted(horiz.items()):
        for a, b in joined(spans): props.append(('fence', a, y, b, y, -1, 0))
    for x, spans in sorted(vert.items()):
        for a, b in joined(spans): props.append(('fence', x, a, x, b, -1, 0))
    # ---- anything placed by hand, in meters from the drawing's south-west corner ---------
    for p in bp.get('extra', []):
        k = p[0]
        if k in ('fence', 'hedge'): props.append((k, p[1] + ox, p[2] + oy, p[3] + ox, p[4] + oy) + tuple(p[5:]))
        else: props.append((k, p[1] + ox, p[2] + oy) + tuple(p[3:]))

    x0, y0, x1, y1 = ox, oy, ox + Wn * CELL, oy + Hn * CELL
    ring = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]                         # counterclockwise
    pt = lambda m: list(mid(*where[m][0]))
    return dict(name=name, note=bp.get('note', ''), start=bp.get('start', ''), blueprint=True,
                area=[v for p in ring for v in p], att=pt('1'), dfn=pt('2'),
                sites=[(bp['A'], 'Spot', -1, pt('A'), float(bp.get('radius', 8))), (bp['B'], 'Spot', -1, pt('B'), float(bp.get('radius', 8)))],
                rbs=[], edges=[(list(a), list(b), 'Wall') for a, b in zip(ring, ring[1:] + ring[:1])],
                props=props, lots=[], fences=[], rows=rows)


# ---------------------------------------------------------------------------------------
# A picture of the drawing
# ---------------------------------------------------------------------------------------
COLORS = {'.': '#9fce7a', ',': '#d9d5cc', ':': '#77777c', ';': '#c9a777', 'g': '#f1ede4', '_': '#b9dc9a',
          'G': '#8d8d8a', 'S': '#6f8266', 'X': '#2f6b2a', 'T': '#1f4f1c', 'o': '#b08556', 'V': '#3b5f9c',
          '-': '#7a5230', '|': '#7a5230', '+': '#7a5230'}
COLORS.update({c: '#b5654b' for c in 'HIJK'}); COLORS.update({c: '#d9a28c' for c in 'hijk'})


def draw(bp, path, mp=None):
    import matplotlib; matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    from matplotlib.patches import Rectangle, Polygon as MPoly, Circle
    import props as PROPS
    mp = mp or compile(bp); rows = mp['rows']; Hn, Wn = len(rows), len(rows[0])
    fig, ax = plt.subplots(figsize=(Wn * 0.19 + 1, Hn * 0.19 + 1.2))
    ax.add_patch(Rectangle((0, 0), Wn * CELL, Hn * CELL, fc='#9fce7a', ec='none'))
    for p in mp['props']:
        if p[0] == 'paint': ax.add_patch(Rectangle((p[1] - p[3] / 2, p[2] - p[4] / 2), p[3], p[4], fc=[COLORS[','], COLORS[':'], COLORS[';']][int(p[5])], ec='none'))
    shade = {'house': None, 'lockup': COLORS['G'], 'shed': COLORS['S'], 'bush': COLORS['X'], 'thicket': COLORS['T'], 'crate': COLORS['o'],
             'yard': COLORS['_'], 'fence': COLORS['-'], 'hedge': COLORS['X'], 'garage': '#55555a', 'treehouse': '#7a5230',
             'car': '#6f8fd0', 'van': COLORS['V'], 'truck': COLORS['V'], 'rv': COLORS['V'], 'stand': '#e67e22', 'tent': '#ff6fa8',
             'portable': '#d9b26f', 'townhome': '#b5838d', 'tree': '#2e7d32'}
    for p in mp['props']:
        if p[0] == 'paint': continue
        col = shade.get(p[0]) or ('#b5654b' if p[6] > 7 else '#d9a28c')
        for g in PROPS.footprint(p):
            ax.add_patch(MPoly(list(g.exterior.coords), fc=col, ec='#222' if p[0] not in ('yard',) else 'none', lw=0.4))
        if p[0] == 'treehouse':
            r = math.radians(p[3]); ax.add_patch(Rectangle((p[1] - 2.5, p[2] - 2.5), 5, 5, fc='#c49a6c', ec='#222', lw=0.5, alpha=.8))
            ax.plot([p[1] + math.cos(r) * 2.5, p[1] + math.cos(r) * 9.8], [p[2] + math.sin(r) * 2.5, p[2] + math.sin(r) * 9.8], color='#c49a6c', lw=4)
    for k, col, lab in (('att', '#ff8a1f', 'ATTACKERS'), ('dfn', '#18b8b8', 'DEFENDERS')):
        ax.add_patch(Circle(mp[k], 2.2, fc=col, ec='k', zorder=5)); ax.text(mp[k][0], mp[k][1] - 4, lab, ha='center', va='center', fontsize=7, weight='bold', zorder=6)
    for s, nm in zip(mp['sites'], 'AB'):
        ax.add_patch(Circle(s[3], s[4], fc='#3d8cff', ec='#0d3d99', lw=1.5, alpha=.35, zorder=4))
        ax.text(s[3][0], s[3][1], nm, fontsize=14, weight='bold', color='#0d3d99', ha='center', va='center', zorder=6)
    ax.set_xlim(0, Wn * CELL); ax.set_ylim(0, Hn * CELL); ax.set_aspect('equal')
    ax.set_xticks(np.arange(0, Wn * CELL + 1, 10)); ax.set_yticks(np.arange(0, Hn * CELL + 1, 10)); ax.tick_params(labelsize=6)
    ax.grid(color='k', alpha=.12, lw=0.4)
    ax.set_title(mp['name'] + '   (1 square of the grid = 10 m, north is up)', fontsize=10, loc='left')
    from matplotlib.patches import Patch
    key = [('two-storey house', '#b5654b'), ('one-storey house', '#d9a28c'), ('shut garage', COLORS['G']), ('hedge', COLORS['X']),
           ('trees', COLORS['T']), ('board fence', COLORS['-']), ('shed', COLORS['S']), ('crates (shoot over)', COLORS['o']),
           ('truck or van', COLORS['V']), ('car (see over)', '#6f8fd0'), ('closed yard', COLORS['_']), ('street', COLORS[':']),
           ('pavement', COLORS[',']), ('dirt', COLORS[';'])]
    ax.legend(handles=[Patch(fc=c, ec='#444', lw=0.4, label=t) for t, c in key], loc='upper center', bbox_to_anchor=(0.5, -0.03),
              ncol=7, fontsize=6.5, frameon=False, handlelength=1.2, columnspacing=1.0)
    fig.tight_layout(); fig.savefig(path, dpi=110); plt.close(fig)


# ---------------------------------------------------------------------------------------
# Measure a drawing against the audit's spec (Tools/audit)
# ---------------------------------------------------------------------------------------
SPEC = [  # (what, how to read it from the ruler's numbers, lowest, highest)
    ('floor, m2', lambda m: m['floor_m2'], 5000, 6500),
    ('solid, %', lambda m: 100 * m['solid_share'], 40, 100),
    ('open ground (over 16 m wide), %', lambda m: 100 * m['width_shares'][4], 0, 5),
    ('path width (median), m', lambda m: m['path_median'], 4, 6),
    ('corners (hard to be seen), %', lambda m: 100 * m['expo_safe'], 45, 100),
    ('seen from all round, %', lambda m: 100 * m['expo_allround'], 0, 3),
    ('mean view, m', lambda m: m['view_mean'], 0, 9),
    ('spots with a 40 m view, %', lambda m: 100 * m['long40'], 0, 20),
    ('angles to hold, %', lambda m: 100 * m['hold_share'], 25, 100),
    ('wall rise, degrees', lambda m: m['rise_mean'], 30, 90),
    ('low directions, %', lambda m: 100 * m['low_share'], 0, 20),
    ('A: openings', lambda m: m['entrances']['A']['ring_count'], 3, 4),
    ('A: openings, m in all', lambda m: m['entrances']['A']['ring_width'], 12, 16),
    ('B: openings', lambda m: m['entrances']['B']['ring_count'], 3, 4),
    ('B: openings, m in all', lambda m: m['entrances']['B']['ring_width'], 12, 16),
    ('attackers to A, m', lambda m: m['runs'][('att', 'A')], 95, 125),
    ('attackers to B, m', lambda m: m['runs'][('att', 'B')], 95, 125),
    ('defenders to A, m', lambda m: m['runs'][('dfn', 'A')], 40, 65),
    ('defenders to B, m', lambda m: m['runs'][('dfn', 'B')], 40, 65),
    ('A to B, m', lambda m: m['runs'][('A', 'B')], 80, 110),
]


def measure(mp):
    """The audit ruler's numbers for a compiled drawing."""
    here = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, os.path.join(here, 'audit'))
    import ruler as R, ours as O
    from run_audit import keep_mask
    mask, res, pts, meta = O.our_mask(mp)
    mask = R.largest_part(mask)
    return R.measure(mp['name'], mask, res, pts, keep_mask(mask, pts, res), see=meta['see'], heights=meta['heights'])


def stepping_stones(mp):
    """What the bots will get in the game: one stepping stone in the middle of every square
    a kid fits on, linked to the stones around it when a kid can walk straight across
    (the same test NavGraph.cs makes). Returns how many stones there are, how many
    cannot be reached from the attackers' start, and the map's four key points checked."""
    import shapely
    from shapely.geometry import Point, LineString
    from shapely.ops import unary_union
    from shapely.prepared import prep
    import props as PROPS
    rows = mp['rows']; Hn, Wn = len(rows), len(rows[0]); x0, y0 = mp['area'][0], mp['area'][1]
    solid = prep(unary_union([g for p in mp['props'] for g in PROPS.footprint(p)]))
    at = lambda c, r: (x0 + (c + 0.5) * CELL, y0 + (Hn - r - 0.5) * CELL)
    free = {(c, r) for r in range(Hn) for c in range(Wn) if not solid.intersects(Point(at(c, r)).buffer(0.45))}
    links = {n: [] for n in free}
    for c, r in free:
        for dc, dr in ((1, 0), (0, 1), (1, 1), (1, -1)):
            n = (c + dc, r + dr)
            if n in free and not solid.intersects(LineString([at(c, r), at(*n)]).buffer(0.405)):
                links[(c, r)].append(n); links[n].append((c, r))
    key = {}
    for nm, pt in (('attackers', mp['att']), ('defenders', mp['dfn']), ('A', mp['sites'][0][3]), ('B', mp['sites'][1][3])):
        key[nm] = (int((pt[0] - x0) // CELL), Hn - 1 - int((pt[1] - y0) // CELL))
    seen = {key['attackers']} if key['attackers'] in free else set(); todo = list(seen)
    while todo:
        n = todo.pop()
        for k in links[n]:
            if k not in seen: seen.add(k); todo.append(k)
    walk = {(c, r) for r in range(Hn) for c in range(Wn) if rows[r][c] in WALK}
    cut_off = sorted((r, c) for c, r in (free & walk) - seen)
    return dict(stones=len(free & walk), cut_off=cut_off, key={nm: k in seen for nm, k in key.items()},
                stray=len(free - walk))


def report(m):
    for what, f, lo, hi in SPEC:
        v = f(m); print('  %-34s %7.1f   want %s   %s' % (what, v, ('%g to %g' % (lo, hi)) if hi < 1000 and lo > 0 else ('under %g' % hi if lo == 0 else 'over %g' % lo),
                                                         'ok' if lo <= v <= hi else '<-- off'))
    print('  (also: 30 m views median %.1f m, rise if all walls were 3 m %.1f, attackers to defenders %.0f m, from attackers into A %d openings, B %d)' % (
        m['long_median'], m['rise3_mean'], m['runs'][('att', 'dfn')], m['entrances']['A'].get('att_count', 0), m['entrances']['B'].get('att_count', 0)))


if __name__ == '__main__':
    import newmaps
    here = os.path.dirname(os.path.abspath(__file__)); os.makedirs(os.path.join(here, 'drawings'), exist_ok=True)
    want = sys.argv[1] if len(sys.argv) > 1 else None
    for bp in newmaps.MAPS:
        if want and bp['name'] != want: continue
        mp = compile(bp)
        png = os.path.join(here, 'drawings', 'new-' + bp['name'].lower().replace(' ', '-') + '.png')
        draw(bp, png, mp)
        floor = sum(ch in WALK for row in mp['rows'] for ch in row)
        print('%s: %d x %d m, %d pieces, about %d m2 of floor. Picture: %s' % (bp['name'], len(mp['rows'][0]) * CELL, len(mp['rows']) * CELL, len(mp['props']), floor * CELL * CELL, png))
        st = stepping_stones(mp)
        print('   bots: %d stepping stones, %d cut off from the attackers\' start %s; start, defenders, A, B reachable: %s%s' % (
            st['stones'], len(st['cut_off']), st['cut_off'][:6] if st['cut_off'] else '', all(st['key'].values()),
            '; %d stones outside the floor (should be 0)' % st['stray'] if st['stray'] else ''))
        if want: report(measure(mp))
