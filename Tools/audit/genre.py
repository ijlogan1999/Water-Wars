# genre.py: turn published overviews of real tactical-shooter maps into walk masks,
# so ruler.py can measure them exactly like our own maps.
#
# Nothing from those games is copied into Water Wars. We only measure them. The
# map files are NOT kept in this repo: download them into a folder and pass it in.
#
#   CS2       radar images (1024x1024 PNG) plus Valve's overview .txt, which gives the
#             scale (game units per pixel) and where the spawns and bomb sites are.
#   Valorant  the navigation mesh (the floor polygons a player can walk on) as
#             published by the open-source Icarus strategy planner, in minimap
#             coordinates; the public map API gives the scale and the callouts.
#
# Where the files come from (all on raw.githubusercontent.com):
#   <folder>/de_<map>.png        2mlml/cs2-radar-images, branch master
#   <folder>/de_<map>.txt        SteamDatabase/GameTracking-CS2, branch master,
#                                game/csgo/pak01_dir/resource/overviews/
#   <folder>/val/<map>_nav.json.gz   SunkenInTime/icarus, branch main, assets/maps/world/
#
# Scale: CS players are 72 units tall and ours are 1.8 m, so 1 unit = 0.025 m puts
# their maps at kid size. Their knife run speed (250 units/s) is then 6.25 m/s and
# rifle speed (215) is 5.4 m/s; ours is 5.5. Valorant is already in meters
# (agents about 1.8 m, run speed 5.4 m/s).
import gzip, json, os, re
import numpy as np
from PIL import Image
from scipy import ndimage
from ruler import RES

UNIT_M = 0.025

CS_MAPS = ['de_dust2', 'de_mirage', 'de_inferno', 'de_ancient', 'de_anubis', 'de_overpass']   # Train's radar has a filled background, Nuke and Vertigo are two floors

# Valorant: minimap fraction per centimeter (xMultiplier from the public map API)
# and the scalars to add; callouts are (x, y) in centimeters.
VAL = {
    'ascent': dict(mult=7e-05, xs=0.813895, ys=0.573242,
                   pts={'A': (6153.585, -6626.2114), 'B': (-2344.065, -7548.511), 'att': (60, 50), 'dfn': (1995.2354, -9744.923)}),
    'bind': dict(mult=5.9e-05, xs=0.576941, ys=0.967566,
                 pts={'A': (10747.902, 2664.4436), 'B': (11108.108, -4831.4585), 'att': (161.64832, 77.51108), 'dfn': (14641.918, -1017.6743)}),
    'split': dict(mult=7.8e-05, xs=0.842188, ys=0.697578,
                  pts={'A': (6588.6597, -6761.131), 'B': (-2167.2456, -6264.7715), 'att': (1901.97, 59.588867), 'dfn': (2142.3635, -8964.969)}),
    'sunset': dict(mult=7.8e-05, xs=0.5, ys=0.515625,
                   pts={'A': (1000, 3200), 'B': (-600, -5850), 'att': (-6025, -400), 'dfn': (3805.4785, -1989.0962)}),
    'pearl': dict(mult=7.8e-05, xs=0.480469, ys=0.916016,
                  pts={'A': (6613.846, 5569.5254), 'B': (5800, -2850), 'att': (-550, -600), 'dfn': (11092.458, 378.79883)}),
    'lotus': dict(mult=7.2e-05, xs=0.454789, ys=0.917752,
                  pts={'A': (7735.5396, 5557.309), 'B': (6368.0327, 668.18317), 'C': (6676.6636, -4265.876), 'att': (1401.2915, 777.29834), 'dfn': (9686.767, 1697.8223)}),
}


def cs_mask(folder, name):
    """Walk mask + key points for a CS2 map. Returns (mask, res, points, meta)."""
    im = np.array(Image.open(os.path.join(folder, name + '.png')).convert('RGBA')).astype(float)
    r, g, b, a = im[..., 0], im[..., 1], im[..., 2], im[..., 3]
    inside = a > 128
    mx = np.maximum(np.maximum(r, g), b); mn = np.minimum(np.minimum(r, g), b)
    # crates, cars and other cover are drawn as light neutral grey on the darker, tinted floor
    cover = inside & (mx - mn < 18) & (mx > 118)
    floor = inside & ~cover
    txt = open(os.path.join(folder, name + '.txt')).read()
    val = lambda k: float(re.search(r'"%s"\s+"([-\d.]+)"' % k, txt).group(1))
    scale = val('scale'); px_m = scale * UNIT_M
    z = px_m / RES
    mask = ndimage.zoom(floor.astype(float), z, order=1) > 0.5
    # heights for the skyline measure: crates and cars about head high (72 units), every
    # other wall assumed two storeys (256 units). The radar does not record heights.
    cov = ndimage.zoom(cover.astype(float), z, order=1) > 0.5
    heights = np.where(cov, 76 * UNIT_M, 256 * UNIT_M)
    pts = {}
    for key, nm in (('TSpawn', 'att'), ('CTSpawn', 'dfn'), ('bombA', 'A'), ('bombB', 'B')):
        try: pts[nm] = (val(key + '_x') * 1024 * z, val(key + '_y') * 1024 * z)
        except AttributeError: pass
    return mask, RES, pts, dict(scale=scale, px_m=px_m, heights=heights)


def val_mask(folder, name):
    """Walk mask + key points for a Valorant map from its navigation mesh."""
    from matplotlib.path import Path
    d = json.load(gzip.open(os.path.join(folder, name + '_nav.json.gz')))
    info = VAL[name]
    span_m = 1.0 / info['mult'] / 100.0                # meters across the whole minimap
    v = np.array(d['vertices'], float).reshape(-1, 3)
    uv = v[:, :2] / d['coordinateScale']
    n = int(np.ceil(span_m / RES))
    mask = np.zeros((n, n), bool)
    comp = np.array(d['components']); main = np.bincount(comp).argmax()
    from PIL import ImageDraw
    img = Image.new('1', (n, n), 0); dr = ImageDraw.Draw(img)
    for poly, ok, c in zip(d['polygons'], d['walkable'], comp):
        if not ok or c != main: continue        # only the main floor, not crate tops and ledges
        p = [(uv[i, 0] * span_m / RES, uv[i, 1] * span_m / RES) for i in poly]
        dr.polygon(p, fill=1)
    mask = np.array(img, bool)
    # the nav mesh is pulled in from every wall by the player's radius (0.42 m): put it back
    grow = int(round(d['source']['agentRadiusCm'] / 100.0 / RES))
    mask = ndimage.binary_dilation(mask, iterations=max(1, grow))
    pts = {}
    for nm, (x, y) in info['pts'].items():
        u = y * info['mult'] + info['xs']; w = x * -info['mult'] + info['ys']
        pts[nm] = (u * span_m / RES, w * span_m / RES)
    return mask, RES, pts, dict(span_m=span_m)
