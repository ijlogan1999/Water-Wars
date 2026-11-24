# check_walkable.py: can a kid actually walk from the attacker spawn to both sites
# and the defender spawn on every map? Rebuilds each map's solid things (houses,
# garages, fences, gates, map edges, trees, yard stuff) as shapes seen from above,
# keeps a kid-sized gap around them, and draws walk_mapN.png (green = reachable).
# Run: python3 check_walkable.py   (needs numpy, shapely, matplotlib)
# If MapBuilder.cs gets new solid things, add them here too.
import math, sys
import numpy as np
from shapely.geometry import Polygon, Point, LineString, box
from shapely.ops import unary_union
from shapely import affinity
from parse import lots, fences, gates, parktrees, maps
R=0.45  # CharacterController radius + skin
def rect(cx,cy,ax,ay,hx,hz):
    # ax,ay = local +Z (forward) direction; local +X = (ay,-ax)
    fx,fy=ax,ay; rx,ry=ay,-ax
    pts=[]
    for sx,sz in((-1,-1),(1,-1),(1,1),(-1,1)):
        pts.append((cx+rx*sx*hx+fx*sz*hz, cy+ry*sx*hx+fy*sz*hz))
    return Polygon(pts)
def local(cx,cy,ax,ay,lx,lz):
    rx,ry=ay,-ax
    return cx+rx*lx+ax*lz, cy+ry*lx+ay*lz
def piece(obs,cx,cy,ax,ay,lx,lz,sx,sz):
    px,py=local(cx,cy,ax,ay,lx,lz); obs.append(rect(px,py,ax,ay,sx/2,sz/2))
def wall(obs,a,b,thick):
    a=np.array(a,float); b=np.array(b,float); d=b-a; L=np.linalg.norm(d)
    if L<0.05: return
    d/=L; m=(a+b)/2; obs.append(rect(m[0],m[1],d[0],d[1],thick/2,L/2))
def norm(v):
    v=np.array(v,float); return v/np.linalg.norm(v)
STREETX=[0,195,390,585]; STREETY=[0,87,174,261]; ROW=15
def tree(obs,p): obs.append(box(p[0]-0.3,p[1]-0.3,p[0]+0.3,p[1]+0.3))
def build(mp, near, tweak=None):
    obs=[]
    siteLots={s[2]:s for s in mp['sites'] if s[2]>=0}
    for i in mp['lots']:
        L=lots[i]; ax,ay=L['inw']; hx,hy=L['house']
        hw,hd,hh=L['hw'],L['hd'],L['hh']
        if tweak: hw,hd,hh=tweak(L)
        piece(obs,hx,hy,ax,ay,0,0,hw,hd)
        r=hd*0.72; low=hh-r/math.sqrt(2)
        if low<1.8:
            zz=min(hd/2,1.8-low)
            piece(obs,hx,hy,ax,ay,0,0,hw+0.6,2*zz)
        site=siteLots.get(i)
        if len(L['garage'])==2:
            gx,gy=L['garage']; w,d=6.6,8.4
            back = site is not None and site[1]=='Garage'
            if back:
                pc=(w-2.2)/2
                piece(obs,gx,gy,ax,ay,-w/2+pc/2,d/2,pc,0.25); piece(obs,gx,gy,ax,ay,w/2-pc/2,d/2,pc,0.25)
            else: piece(obs,gx,gy,ax,ay,0,d/2,w,0.25)
            piece(obs,gx,gy,ax,ay,-w/2,0,0.25,d); piece(obs,gx,gy,ax,ay,w/2,0,0.25,d)
            piece(obs,gx,gy,ax,ay,-w/2+0.8,1.6,1.2,2.4); piece(obs,gx,gy,ax,ay,w/2-1,0.6,1.3,1.3)
        t=L['trees']
        for k in range(0,len(t),2): tree(obs,(t[k],t[k+1]))
        want = len(L['treespot'])==2 and (L['thing']=='Treehouse' or (site and site[1]=='Treehouse'))
        if want:
            cx,cy=L['treespot']; half=2.5
            for sx in(-1,1):
                for sz in(-1,1): piece(obs,cx,cy,ax,ay,sx*(half-0.2),sz*(half-0.2),0.3,0.3)
            piece(obs,cx,cy,ax,ay,0,half+0.9,1.4,1.4)
            if site:
                p=np.array(L['treespot'])-np.array(L['inw'])*4+np.array(L['along'])*4; obs.append(box(p[0]-.7,p[1]-.7,p[0]+.7,p[1]+.7))
        elif not (site and site[1]=='Yard'):
            th=L['thing']; tx,ty=L['thingAt'] if L['thingAt'] else (0,0)
            if th=='Pool': piece(obs,tx,ty,ax,ay,0,0,6,3.6)
            elif th=='Shed': piece(obs,tx,ty,ax,ay,0,0,3,3)
            elif th=='Trampoline': piece(obs,tx,ty,ax,ay,0,0,4.2,4.2)
            elif th=='Swings':
                for s in(-1,1): piece(obs,tx,ty,ax,ay,s*2,0,0.15,1.4)
        if site and site[1]!='Spot':
            b=np.array(L['backyard']); al=np.array(L['along']); iw=np.array(L['inw'])
            p=b-al*4.5; piece(obs,p[0],p[1],ax,ay,0,0,2.2,1.6)
            p=b+al*5+iw*2; piece(obs,p[0],p[1],ax,ay,0,0,1,0.8)
    for fid in mp['fences']:
        f=fences[fid*5:fid*5+5]; k=int(f[4])
        wall(obs,f[0:2],f[2:4],{0:0.2,1:0.15,2:0.9}[k])
    area=Polygon(np.array(mp['area']).reshape(-1,2))
    def onedge(p,w):
        return area.exterior.distance(Point(p))<w
    for g in range(0,len(gates),4):
        c=np.array(gates[g:g+2]); wv=np.array(gates[g+2:g+4]); al=np.array([-wv[1],wv[0]])
        if not near.contains(Point(c)) or not onedge(c,0.8): continue
        wall(obs,c-al*1.3,c+al*1.3,0.2)
    # street trees
    x0=STREETX[0]-ROW/2; x1=STREETX[3]+ROW/2; y0=STREETY[0]-ROW/2; y1=STREETY[3]+ROW/2
    def onh(p): return any(abs(p[1]-y)<ROW/2 for y in STREETY)
    for i in range(4):
        d=x0+10
        while d<x1:
            for s in(-1,1):
                p=(d,STREETY[i]+s*6.4); skip= i==2 and d<STREETX[1]
                if not skip and near.contains(Point(p)) and not any(abs(p[0]-x)<ROW/2 and onh(p) for x in STREETX): tree(obs,p)
            d+=24
        d=y0+10
        while d<y1:
            for s in(-1,1):
                p=(STREETX[i]+s*6.4,d)
                if near.contains(Point(p)) and not any(abs(p[0]-x)<ROW/2 and onh(p) for x in STREETX): tree(obs,p)
            d+=24
    # edges invisible walls
    for a,b,k in mp['edges']:
        a=np.array(a);b=np.array(b);d=b-a;L=np.linalg.norm(d)
        if L<0.05: continue
        d/=L; out=np.array([d[1],-d[0]])
        wall(obs,a+out*0.35,b+out*0.35,0.4)
    # park (Park Run)
    if near.intersects(box(202.5,94.5,382.5,166.5)):
        for x in(301.5,326.5): obs.append(box(x-.125,108-.125,x+.125,108+.125))
        wall(obs,(306,116.8),(322,116.8),0.4); obs.append(box(310,97.9,318,99.3))
        for sx in(-1,1):
            for sz in(-1,1): obs.append(box(343+sx*1.35-.125,129+sz*1.35-.125,343+sx*1.35+.125,129+sz*1.35+.125))
        for s in(-1,1): obs.append(box(355+s*3-.075,142-.9,355+s*3+.075,142+.9))
        obs.append(box(363-1.75,128-1.75,363+1.75,128+1.75)); obs.append(box(352-1.25,121-.35,352+1.25,121+.35))
        for sx in(-1,0,1):
            for sz in(-1,1): obs.append(box(309+sx*8.5-.2,142.5+sz*7-.2,309+sx*8.5+.2,142.5+sz*7+.2))
        for i in(-1,0,1):
            cx=309+i*5.2; cz=142.5+(2 if i==0 else -1.5); obs.append(box(cx-1.2,cz-.85,cx+1.2,cz+.85))
        obs.append(box(309-7.5-.45,142.5-6-.45,309-7.5+.45,142.5-6+.45))
        wall(obs,(245,97.5),(255,97.5),0.1)
        for k in range(0,len(parktrees),3): tree(obs,parktrees[k:k+2])
    if near.intersects(box(397.5,181.5,577.5,253.5)):
        obs.append(box(400,215,470,248)); obs.append(box(400,190,425,215))
        obs.append(box(460.2,211.5,463.8,213.5)); obs.append(box(431,212.7,435,213.3))
        for p in((436,192),(450,192),(472,203)): obs.append(box(p[0]-.95,p[1]-2.3,p[0]+.95,p[1]+2.3))
        obs.append(box(453,197.75,463,200.25))
        for x in(496.5,573.5):
            obs.append(box(x-.1,216.2,x+.1,216.6)); obs.append(box(x-.1,223.4,x+.1,223.8))
        obs.append(box(534.5,224.45,545.5,225.55))  # first bleacher step only (others climbable)
        obs.append(box(525.5,213.8,528.5,216.2))
        x0_,x1_,y0_,y1_=398.5,576.5,182.5,252.5
        for a,b in [((x0_,y0_),(462,y0_)),((513,y0_),(x1_,y0_)),((x0_,y1_),(x1_,y1_)),((x0_,y0_),(x0_,y1_)),((x1_,y0_),(x1_,y1_))]: wall(obs,a,b,0.08)
    return area, unary_union(obs)

def check(mp, tweak=None, verbose=True, draw=None):
    area=Polygon(np.array(mp['area']).reshape(-1,2))
    bx=area.bounds; near=box(bx[0]-60,bx[1]-60,bx[2]+60,bx[3]+60)
    area, obst = build(mp, near, tweak)
    free = area.difference(obst.buffer(R, quad_segs=4))
    comps = list(free.geoms) if free.geom_type=='MultiPolygon' else [free]
    att=Point(mp['att']); 
    def comp_of(p):
        best=None;bd=1e9
        for i,c in enumerate(comps):
            d=c.distance(p)
            if d<bd: bd=d;best=i
        return best,bd
    ca,da=comp_of(att)
    res={'att_dist':round(da,2)}
    for name,p in [('defenders',Point(mp['dfn']))]+[('site '+s[0],Point(s[3])) for s in mp['sites']]:
        c,d=comp_of(p); res[name]=(c==ca, round(d,2))
    res['main_area']=round(comps[ca].area); res['total_free']=round(free.area); res['ncomp']=len(comps)
    if draw:
        import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
        fig,ax=plt.subplots(figsize=(10,10*(bx[3]-bx[1]+10)/(bx[2]-bx[0]+10)))
        def fill(g,c,a=1):
            for gg in (g.geoms if hasattr(g,'geoms') else [g]):
                if gg.geom_type=='Polygon':
                    ax.fill(*gg.exterior.xy,color=c,alpha=a,lw=0)
                    for h in gg.interiors: ax.fill(*h.xy,color='white',lw=0)
        fill(free,'#ffcccc'); fill(comps[ca],'#9be39b'); fill(obst.intersection(near),'#333333')
        ax.plot(*area.exterior.xy,'b-',lw=0.8)
        ax.plot(*att.xy,'r*',ms=14); ax.plot(*Point(mp['dfn']).xy,'b*',ms=14)
        for s in mp['sites']: ax.add_patch(plt.Circle(s[3],s[4],fill=False,color='orange',lw=2))
        ax.set_xlim(bx[0]-3,bx[2]+3); ax.set_ylim(bx[1]-3,bx[3]+3); ax.set_aspect('equal'); ax.set_title(mp['name']+'  green = reachable from attacker spawn')
        fig.savefig(draw,dpi=110,bbox_inches='tight'); plt.close(fig)
    return res
if __name__=='__main__':
    for i,mp in enumerate(maps):
        print(mp['name'], check(mp, draw='walk_map%d.png'%(i+1)))
