import pickle, random, numpy as np
from shapely.geometry import box, Point, LineString, Polygon, MultiPoint
from shapely.geometry.polygon import orient
from shapely.ops import unary_union, split, linemerge
D=pickle.load(open("rec.pkl","rb")); REC=D["REC"]
# --- map definitions (shared with the drawings) ---
src=open("maps5.py").read().split("# individual sheets")[0]
src=src.replace('img=plt.imread("base.png"); ','')
g={}; exec(src,g)
MAPS=g["MAPS"]; blocks=g["blocks"]; ST=g["ST"]; KINDS=g["KINDS"]
rng=random.Random(11)
F=lambda v: (("%.2f"%v).rstrip("0").rstrip(".")+"f") if abs(round(v,2)-round(v))>1e-9 else str(int(round(v)))
def arr(vals): return "new float[]{"+",".join(F(float(v)) for v in vals)+"}"
key={ (r["street"],r["num"]):i for i,r in enumerate(REC)}
# ---------- lots ----------
def extents(poly,c,t,n):
    pts=np.array(poly.exterior.coords)
    a=[np.dot(p-c,t) for p in pts]; b=[np.dot(p-c,n) for p in pts]
    return max(a)-min(a), max(b)-min(b)
THING={0.22:"Pool",0.32:"Treehouse",0.5:"Shed",0.6:"Trampoline",0.7:"Swings"}
def thingtype(k):
    for lim,nm in sorted(THING.items()):
        if k<lim: return nm
    return "None"
# ---------- make room to walk around houses ----------
# Kids have to get from the street into the backyards through the side yards,
# so every house + garage leaves at least SIDE_YARD meters of clear grass on
# both sides (Ian's playtest: the maps were barely walkable).
SIDE_YARD=3.0
GARAGE_SITES={("Willow Ct",104),("Birch Ln",154),("Birch Ln",166)}
def rect(center,t,n,w,d):
    c=np.array(center)
    return Polygon([tuple(c+t*a*w/2+n*b*d/2) for a,b in((-1,-1),(1,-1),(1,1),(-1,1))])
def lot_span(r,lo_n,hi_n):
    """The narrowest stretch of the lot (along the street) between two depths."""
    t,n,m=r["t"],r["n"],r["m"]; best=(-1e9,1e9)
    for k in np.linspace(lo_n,hi_n,7):
        ln=LineString([tuple(m+n*k-t*80),tuple(m+n*k+t*80)]).intersection(r["lot"])
        if ln.is_empty: continue
        ts=[np.dot(np.array(p)-m,t) for g_ in getattr(ln,"geoms",[ln]) for p in g_.coords]
        best=(max(best[0],min(ts)),min(best[1],max(ts)))
    return best
changed=0
for r in REC:
    t,n,m=r["t"],r["n"],r["m"]
    hc=np.array(r["house"].centroid.coords[0]); hw,hd=extents(r["house"],hc,t,n)
    hn=np.dot(hc-m,n); ht=np.dot(hc-m,t)
    gar=r["gar"]
    lo_n=hn-hd/2; hi_n=hn+hd/2
    if gar is not None:
        gc=np.array(gar.centroid.coords[0]); gw,gd=extents(gar,gc,t,n); gt=np.dot(gc-m,t); gn=np.dot(gc-m,n)
        lo_n=min(lo_n,gn-gd/2); hi_n=max(hi_n,gn+gd/2)
    lmin,lmax=lot_span(r,lo_n,hi_n)
    room=(lmax-lmin)-2*SIDE_YARD
    parts=[("h",hw)]+([("g",gw)] if gar is not None else [])
    total=hw+(gw if gar is not None else 0)
    if total<=room+1e-6 and min(ht-hw/2,(gt-gw/2) if gar is not None else 1e9)>=lmin+SIDE_YARD-1e-6 and max(ht+hw/2,(gt+gw/2) if gar is not None else -1e9)<=lmax-SIDE_YARD+1e-6:
        continue
    keep_garage = gar is not None and ((r["street"],r["num"]) in GARAGE_SITES or room-gw>=8)
    if not keep_garage: gar=None
    new_hw=min(hw, room-(gw if gar is not None else 0))
    new_hw=max(new_hw,6.0)
    # Lay the block out (house, then garage on the side it was on), centered in the lot.
    mid=(lmin+lmax)/2
    if gar is not None:
        side=1 if gt>ht else -1
        width=new_hw+gw; start=mid-width/2
        if side>0: ht2=start+new_hw/2; gt2=start+new_hw+gw/2
        else: gt2=start+gw/2; ht2=start+gw+new_hw/2
        r["gar"]=rect(m+t*gt2+n*gn,t,n,gw,gd)
        r["drive"]=rect(m+t*gt2+n*(np.dot(np.array(r["drive"].centroid.coords[0])-m,n)),t,n,4.8,extents(r["drive"],np.array(r["drive"].centroid.coords[0]),t,n)[1])
    else:
        ht2=mid; r["gar"]=None
        dc=np.array(r["drive"].centroid.coords[0]); dn=np.dot(dc-m,n); dl=extents(r["drive"],dc,t,n)[1]
        dside=1 if np.dot(dc-m,t)>ht else -1
        r["drive"]=rect(m+t*(ht2+dside*(new_hw/2+2.6))+n*dn,t,n,4.8,dl)
    r["house"]=rect(m+t*ht2+n*hn,t,n,new_hw,hd)
    changed+=1
print("houses moved/narrowed for side yards:",changed)

lot_lines=[]; SPOTS=[]
for r in REC:
    t,n,m=r["t"],r["n"],r["m"]
    hc=np.array(r["house"].centroid.coords[0]); hw,hd=extents(r["house"],hc,t,n)
    if r["gar"] is not None: gc=np.array(r["gar"].centroid.coords[0]); garage=[gc[0],gc[1]]
    else: garage=[]
    dc=np.array(r["drive"].centroid.coords[0]); dw,dl=extents(r["drive"],dc,t,n)
    tt="None"; tc=[0,0]
    if r["thing"] is not None:
        tt=thingtype(r["k"]); tc=list(r["thing"][0].centroid.coords[0])
    depth=max(np.dot(np.array(p)-m,n) for p in r["lot"].exterior.coords)
    back=m+n*(7.5+10+(depth-17.5)*0.55)
    height=rng.choice([4.2,6.5]); color=rng.randrange(8)
    blockers=unary_union([r["house"]]+([r["gar"]] if r["gar"] is not None else []))
    spot=[]
    for k in np.arange(depth-4.5,15,-0.5):
        for s_ in (0,-2,2,-4,4,-6,6):
            c=m+n*k+t*s_
            fp=Polygon([tuple(c+t*a*2.6+n*b) for a,b in((-1,-10.9),(1,-10.9),(1,4.3),(-1,4.3))])
            if r["lot"].buffer(-0.3).contains(fp) and not fp.intersects(blockers.buffer(0.8)): spot=list(c); break
        if spot: break
    if tt=="Treehouse" and not spot: tt="None"
    trees=[v for p in r["trees"] for v in p]; SPOTS.append(spot)
    lot_lines.append('            new LotRec("%s",%d,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)'%(
        r["street"],r["num"],arr(np.array(orient(r["lot"],1).exterior.coords[:-1]).ravel()),
        arr(m),arr(t),arr(n),F(r["L"]),arr(hc),F(hw),F(hd),F(height),str(color),arr(garage),arr(dc),F(dl),
        "Yard."+tt,arr(tc),arr(back),arr(trees),F(depth),arr(spot)))
# ---------- fences ----------
merged=linemerge(unary_union(FF:=[f for f in D["FENCES"] if not f.is_empty]))
verts=MultiPoint([p for r in REC for p in r["lot"].exterior.coords])
segs=[]
for ln in getattr(merged,"geoms",[merged]):
    for piece in split(ln,verts.buffer(0.01)).geoms if True else []:
        c=list(piece.coords)
        for a,b in zip(c,c[1:]):
            if np.hypot(b[0]-a[0],b[1]-a[1])>0.3: segs.append((np.array(a),np.array(b)))
# join tiny buffer remnants: keep segments > 0.3
fence_lines=[];gates=[]
for a,b in segs:
    mp=Point((a+b)/2); owners=[r for r in REC if r["lot"].exterior.distance(mp)<0.15]
    L=np.linalg.norm(b-a); d=(b-a)/L
    isback=len(owners)==2 and np.dot(owners[0]["n"],owners[1]["n"])<-0.5 and abs(np.dot(d,owners[0]["n"]))<0.3
    if isback: kind=0
    else:
        x=rng.random(); kind=1 if x<0.25 else 2 if x<0.4 else 0   # 0 wood, 1 picket, 2 hedge
    gate = L>=6 and (isback or rng.random()<0.5)
    # Side fences of corner lots (the ones facing a street, park or school) always
    # get a gate in the middle, so you can get into those backyards from the side.
    side_gate = not gate and L>=6 and len(owners)==1
    if side_gate:
        g0=L/2-1.25
        pieces=[(a,a+d*g0),(a+d*(g0+2.5),b)]
        gc=a+d*(g0+1.25); gates.append((gc,np.array([-d[1],d[0]])))
    elif gate:
        g0=rng.uniform(1.2,L-1.2-2.5)
        pieces=[(a,a+d*g0),(a+d*(g0+2.5),b)]
        gc=a+d*(g0+1.25); gates.append((gc,np.array([-d[1],d[0]])))
    else: pieces=[(a,b)]
    for p,q in pieces:
        if np.linalg.norm(q-p)>0.2: fence_lines.append((p,q,kind))
fence_geom=unary_union([LineString([p,q]) for p,q,_ in fence_lines]+[LineString([gc-np.array([-n[1],n[0]])*1.25,gc+np.array([-n[1],n[0]])*1.25]) for gc,n in gates])
print("fences",len(fence_lines),"gates",len(gates))
# ---------- maps ----------
SCHOOL=box(397.5,181.5,577.5,253.5); PARK=box(202.5,94.5,382.5,166.5)
roads=unary_union([ST[k].buffer(7.5,cap_style=2) for k in ST if k!="Willow Ct"]+[ST["Willow Ct"].buffer(6,cap_style=2),Point(97.5,172).buffer(15)])
def classify(p):
    P=Point(p)
    if roads.contains(P): return "Street"
    if PARK.contains(P): return "Park"
    if SCHOOL.contains(P): return "School"
    if p[0]>592.5 or p[1]<-7.5 or p[1]>268.5: return "Woods"
    for r in REC:
        if r["lot"].contains(P):
            # front yard (near the street) -> curb
            return "Curb" if np.dot(np.array(p)-r["m"],r["n"])<7 else "Lot"
    return "Verge"
SITES={
 0:[("pavilion","Spot",None,(309,142.5),9),("playground","Spot",None,(352,135),9)],
 1:[("104 Willow Ct garage","Garage",("Willow Ct",104),None,8),("110 Willow Ct treehouse","Treehouse",("Willow Ct",110),None,8.5)],
 2:[("school back doors","Spot",None,(447,207),9),("soccer bleachers","Spot",None,(540,222),9.5)],
 3:[("footbridge","Spot",None,(617,131),8),("130 Cedar Ln backyard","Yard",("Cedar Ln",130),(566,140),8)],
 4:[("122 Cedar Ln backyard","Yard",("Cedar Ln",122),(476,150),8),("154 Birch Ln garage","Garage",("Birch Ln",154),None,8)],
 5:[("166 Birch Ln garage","Garage",("Birch Ln",166),None,8),("100 Hickory Rd backyard","Yard",("Hickory Rd",100),(170,110),8)],
}
STARTS={0:"Maple St",1:"Birch Ln",2:"Birch Ln",3:"Maple St",4:"Pine St",5:"Maple St"}
map_lines=[]
k0=0
for i,m in enumerate(MAPS):
    area=orient(m["area"].simplify(0.05),1)
    ring=np.array(area.exterior.coords[:-1])
    near=area.buffer(45)
    lots=[j for j,r in enumerate(REC) if r["lot"].intersects(near)]
    fen=[j for j,(p,q,_) in enumerate(fence_lines) if LineString([p,q]).intersects(near)]
    # boundary pieces
    pieces=[]
    ext=list(area.exterior.coords)
    for side,(a,b) in enumerate(zip(ext,ext[1:])):
        a=np.array(a);b=np.array(b);L=np.linalg.norm(b-a)
        if L<0.05: continue
        d=(b-a)/L; out=np.array([d[1],-d[0]])   # ccw ring: outside is to the right
        cur=None;start=0
        steps=max(1,int(L/0.5))
        for s_ in range(steps+1):
            u=min(L,s_*L/steps); p=a+d*u
            c=classify(p+out*0.8)
            covered=fence_geom.distance(Point(p))<0.35
            lab=c+("|F" if covered and c in("Lot","Curb") else "")
            if cur is None: cur=lab;start=u
            elif lab!=cur or s_==steps:
                pieces.append((a+d*start,a+d*u,cur,side)); cur=lab;start=u
    # merge tiny pieces
    pcs=[]
    # (only merge pieces on the same straight side: merging across a corner
    #  would make an edge that cuts diagonally through the map)
    for p,q,c,side in pieces:
        if np.linalg.norm(q-p)<0.01: continue
        if pcs and pcs[-1][3]==side and (np.linalg.norm(q-p)<1.6 and c.startswith("Verge") or c==pcs[-1][2]) and np.linalg.norm(pcs[-1][1]-p)<0.01:
            pcs[-1]=(pcs[-1][0],q,pcs[-1][2],side); continue
        pcs.append((p,q,c,side))
    blk=blocks(m["area"])
    rbs=[]
    for j,(p,dd) in enumerate(blk): rbs.append((p,dd,KINDS[(k0+j)%len(KINDS)].title().replace(" ","")))
    k0+=len(blk)
    sites=[]
    for (label,kind,lk,c,rad) in SITES[i]:
        li=key[lk] if lk else -1
        r=REC[li] if lk else None
        if kind=="Garage": gc=np.array(r["gar"].centroid.coords[0]); c=gc+r["n"]*3.5
        if kind=="Yard":   # the open middle of the backyard, never on the house
            dep=max(np.dot(np.array(p)-r["m"],r["n"]) for p in r["lot"].exterior.coords)
            c=r["m"]+r["n"]*(7.5+10+(dep-17.5)*0.55)
        if kind=="Treehouse":
            c=np.array(SPOTS[li]); assert len(c)==2, "no treehouse spot"
        sites.append('new SiteDef("%s",SiteKind.%s,%d,%s,%s)'%(label,kind,li,arr(c),F(rad)))
    att=np.array(m["att"]);dfn=np.array(m["dfn"])
    name=m["name"].split(". ",1)[1].replace(" (the one you approved)","")
    map_lines.append('''            new MapDef("%s", "%s", "%s",
                %s,
                %s, %s,
                new[]{ %s },
                new[]{ %s },
                new[]{ %s },
                new int[]{%s},
                new int[]{%s})'''%(name,m["note"].replace('"',"'"),STARTS[i],arr(ring.ravel()),arr(att),arr(dfn),
        ", ".join(sites),
        ", ".join('new Roadblock(Roadblock.Kind.%s,%s,%s)'%(kd,arr(p),arr(dd)) for p,dd,kd in rbs),
        ", ".join('new Edge(%s,%s,Edge.Kind.%s)'%(arr(p),arr(q),c.replace("|F","Fenced")) for p,q,c,_ in pcs),
        ",".join(map(str,lots)),",".join(map(str,fen))))
    print(name,len(lots),"lots",len(fen),"fences",len(pcs),"edges",len(rbs),"blocks",[c for _,_,c,_ in pcs])
park="".join("%s,%s,%s,"%(F(x),F(y),F(r)) for x,y,r in D["PARKTREES"])
out='''// NeighborhoodData.cs   (GENERATED: made by a script from the Maple Park drawings, don't edit by hand)
// ---------------------------------------------------------------------------
// The layout of the whole Maple Park neighborhood and its six example maps,
// written out as plain numbers so the game builds exactly what the drawings
// show. Positions are in meters: x goes east, y goes north (in Unity, the
// drawing's y becomes z).
// ---------------------------------------------------------------------------

namespace WaterWars
{
    public static partial class NeighborhoodData
    {
        public static readonly LotRec[] Lots =
        {
%s
        };

        // Fences: x1, y1, x2, y2, kind (0 wood, 1 white picket, 2 hedge). Gaps for gates are already left out.
        public static readonly float[] Fences =
        {
            %s
        };

        // Gates: x, y, then the direction you walk through it.
        public static readonly float[] Gates =
        {
            %s
        };

        // Trees in Maple Park: x, y, radius.
        public static readonly float[] ParkTrees = { %s };

        public static readonly MapDef[] Maps =
        {
%s
        };
    }
}
'''%(",\n".join(lot_lines),
    ",\n            ".join("%s,%s,%s,%s,%d"%(F(p[0]),F(p[1]),F(q[0]),F(q[1]),k) for p,q,k in fence_lines),
    ",\n            ".join("%s,%s,%s,%s"%(F(c[0]),F(c[1]),F(n[0]),F(n[1])) for c,n in gates),
    park.rstrip(","),",\n".join(map_lines))
open("../Assets/WaterWars/Scripts/NeighborhoodData.cs","w").write(out)
print(len(out))


print("treehouse spots",sum(1 for x in SPOTS if x),"of",len(SPOTS), "willow110", SPOTS[key[("Willow Ct",110)]])
