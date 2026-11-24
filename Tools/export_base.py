import math, random
import numpy as np
import matplotlib; matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Polygon as MPoly, Circle
from shapely.geometry import Polygon, LineString, Point, box, MultiLineString
from shapely.ops import unary_union, split
from shapely import affinity
random.seed(5)
ROW=15; H=ROW/2; ROAD=9
X=[0,195,390,585]; Y=[0,87,174,261]
names_h={0:"Maple St",87:"Birch Ln",174:"Cedar Ln",261:"Oak St"}
names_v={0:"Elm St",195:"Hickory Rd",390:"Pine St",585:"Creek Rd"}
streets={}  # name -> (centerline, ROW polygon)
def add_street(name,line,w=ROW):
    streets[name]=(line,line.buffer(w/2,cap_style=2))
for y in Y:
    x0,x1=X[0]-H,X[-1]+H
    if y==174: x0=X[1]-H
    add_street(names_h[y],LineString([(x0,y),(x1,y)]))
for x in X: add_street(names_v[x],LineString([(x,Y[0]-H),(x,Y[-1]+H)]))
# cul-de-sac Willow Ct
bulb_c=(97.5,172); bulb_r=15
stem=LineString([(97.5,87),(97.5,172)])
ct_poly=unary_union([stem.buffer(6,cap_style=2),Point(bulb_c).buffer(bulb_r)])
streets["Willow Ct"]=(stem,ct_poly)
allroads=unary_union([p for _,p in streets.values()])
lots=[]  # (poly, street_name)
def split_x(rect,n,street):
    x0,y0,x1,y1=rect.bounds;w=(x1-x0)/n
    for i in range(n): lots.append((box(x0+i*w,y0,x0+(i+1)*w,y1),street))
def split_y(rect,n,street):
    x0,y0,x1,y1=rect.bounds;h=(y1-y0)/n
    for i in range(n): lots.append((box(x0,y0+i*h,x1,y0+(i+1)*h),street))
def normal_block(c,r):
    x0,x1=X[c]+H,X[c+1]-H; y0,y1=Y[r]+H,Y[r+1]-H; mid=(y0+y1)/2
    n=round((x1-x0)/22.5)
    split_x(box(x0,y0,x1,mid),n,names_h[Y[r]])
    split_x(box(x0,mid,x1,y1),n,names_h[Y[r+1]])
specials={(0,1),(0,2),(1,1),(2,2)}
for c in range(3):
    for r in range(3):
        if (c,r) not in specials: normal_block(c,r)
# cul-de-sac block
bx0,bx1,by0,by1=X[0]+H,X[1]-H,Y[1]+H,Y[3]-H
D=36
split_x(box(bx0,by1-D,bx1,by1),8,"Oak St")
split_y(box(bx0,by0,bx0+D,by1-D),5,"Elm St")
split_y(box(bx1-D,by0,bx1,by1-D),5,"Hickory Rd")
for piece in (box(bx0+D,by0,97.5-6,by0+D),box(97.5+6,by0,bx1-D,by0+D)): split_x(piece,2,"Birch Ln")
inner=box(bx0+D,by0+D,bx1-D,by1-D).difference(ct_poly)
cx,cy=bulb_c
cuts=[LineString([(bx0+D-1,150),(97.5,150)]),LineString([(97.5,150),(bx1-D+1,150)])]
for a in (180,135,90,45,0):
    v=(math.cos(math.radians(a)),math.sin(math.radians(a)))
    cuts.append(LineString([(cx,cy),(cx+v[0]*200,cy+v[1]*200)]))
pieces=[inner]
for cut in cuts:
    nxt=[]
    for p in pieces:
        r=split(p,cut); nxt.extend([g for g in r.geoms if g.area>5])
    pieces=nxt
for p in pieces: lots.append((p,"Willow Ct"))
# ---- house placement
def place(lot,street):
    front=lot.exterior.intersection(streets[street][1].buffer(0.4))
    if front.is_empty: return None
    segs=[front] if front.geom_type=="LineString" else list(getattr(front,"geoms",[]))
    segs=[s for s in segs if s.geom_type=="LineString"]
    if not segs: return None
    seg=max(segs,key=lambda s:s.length)
    m=np.array(seg.interpolate(0.5,normalized=True).coords[0])
    a=np.array(seg.coords[0]);b=np.array(seg.coords[-1]);t=(b-a)/np.linalg.norm(b-a)
    n=np.array([-t[1],t[0]]); c=np.array(lot.centroid.coords[0])
    if np.dot(c-m,n)<0: n=-n
    L=seg.length
    # wedge lots: use direction to centroid
    if street=="Willow Ct": n=(c-m)/np.linalg.norm(c-m); t=np.array([n[1],-n[0]])
    return m,t,n,L
def rect(center,t,n,w,d):
    return Polygon([tuple(center+t*sx*w/2+n*sy*d/2) for sx,sy in((-1,-1),(1,-1),(1,1),(-1,1))])
fig,ax=plt.subplots(figsize=(21,10.5),dpi=100); ax.set_facecolor("#7db05a")
def draw(poly,fc,ec="none",lw=0.5,z=2,alpha=1):
    if poly.is_empty: return
    for g in (poly.geoms if hasattr(poly,"geoms") else [poly]):
        if g.geom_type=="Polygon": ax.add_patch(MPoly(list(g.exterior.coords),fc=fc,ec=ec,lw=lw,zorder=z,alpha=alpha))
def line(geom,color,lw,z=4):
    if geom.is_empty: return
    for g in (geom.geoms if hasattr(geom,"geoms") else [geom]):
        if g.geom_type=="LineString": ax.plot(*g.xy,color=color,lw=lw,zorder=z,solid_capstyle="butt")
pal=["#f2d7d0","#d9e4f2","#efe3bf","#d6e8cf","#e7d9ef","#f3efe6","#e8cfae","#f0e0c8"]
# roads: ROW grass verge, sidewalks, asphalt
for name,(cl,rowp) in streets.items():
    w=ROAD if name!="Willow Ct" else 8
    draw(cl.buffer(w/2+3.2,cap_style=2 if name!="Willow Ct" else 1),"#d9d5cc",z=1)
    draw(cl.buffer(w/2+1.6,cap_style=2 if name!="Willow Ct" else 1),"#86b863",z=1)
draw(Point(bulb_c).buffer(bulb_r+0.5),"#d9d5cc",z=1); draw(Point(bulb_c).buffer(bulb_r-1.5),"#86b863",z=1)
for name,(cl,rowp) in streets.items():
    w=ROAD if name!="Willow Ct" else 8
    draw(cl.buffer(w/2,cap_style=2 if name!="Willow Ct" else 1),"#5f5f66",z=2)
draw(Point(bulb_c).buffer(bulb_r-3),"#5f5f66",z=2); draw(Point(bulb_c).buffer(4),"#86b863",z=3)
num={}
REC=[]; FENCES=[]; PARKTREES=[]
HOUSES={}
LOTPOLY={}
for lot,street in lots:
    pl=place(lot,street)
    if pl is None: continue
    m,t,n,L=pl
    draw(lot,"#7db05a",ec="none",z=1)
    hd=10; hw=min(13,L-6); garage = L>=21 and random.random()<0.8
    if street=="Willow Ct": hw=min(12,max(9,L-4)); garage=random.random()<0.6
    side=random.choice([-1,1]); setback=7.5
    shift = -side*3.4 if garage else 0
    house=rect(m+n*(setback+hd/2)+t*shift,t,n,hw,hd)
    gar=rect(m+n*(setback+4.2)+t*(shift+side*(hw/2+3.3)),t,n,6.6,8.4) if garage else None
    if not lot.buffer(-0.8).contains(house) or (gar and not lot.buffer(-0.5).contains(gar)):
        gar=None; house=rect(m+n*(setback+hd/2),t,n,min(hw,10),hd)
        if not lot.buffer(-0.5).contains(house): continue
    # fence: property line except the front yard band
    depth_band=setback+hd*0.6
    band=LineString([tuple(m-t*L),tuple(m+t*L)]).buffer(depth_band,cap_style=2)
    if street=="Willow Ct": band=Point(bulb_c).buffer(bulb_r+setback+hd*0.6).union(streets["Willow Ct"][0].buffer(6+depth_band,cap_style=2))
    fence=lot.exterior.difference(band)
    line(fence,"#8a5a33",1.0,z=4); FENCES.append(fence)
    # driveway
    dw_from=(gar.centroid if gar else house.centroid); dc=np.array(dw_from.coords[0])
    dlen=np.dot(dc-m,n)-4.2 if gar else np.dot(dc-m,n)-5
    lateral=np.dot(dc-m,t)
    if not gar: lateral += random.choice([-1,1])*(min(hw,10)/2+2)
    drive=rect(m+t*lateral+n*(dlen/2-1.6),t,n,4.8,dlen+3.2)
    draw(drive,"#cbc6bb",z=3)
    draw(house,random.choice(pal),ec="#3a3a3a",lw=0.6,z=5)
    if gar: draw(gar,"#c4c4be",ec="#3a3a3a",lw=0.6,z=5)
    # backyard thing
    depth=max(np.dot(np.array(p)-m,n) for p in lot.exterior.coords)
    k=random.random(); bp=m+n*(setback+hd+ (depth-setback-hd)*0.55)+t*random.uniform(-L*0.2,L*0.2)
    thing=None
    if k<.22: thing=(rect(bp,t,n,6,3.6),"#6cc0f0","#3a7fb0")
    elif k<.32: thing=(rect(bp,t,n,4.5,4.5),"#b98a55","#5a3a1a")   # treehouse
    elif k<.5: thing=(rect(bp+t*L*0.25,t,n,3,3),"#8c9a80","#444")    # shed
    elif k<.6: thing=(Point(tuple(bp)).buffer(2.2),"#26262e","none") # trampoline
    elif k<.7: thing=(rect(bp,t,n,4,1.6),"#a33","none")              # swing set
    if thing and lot.buffer(-0.5).contains(thing[0]): draw(thing[0],thing[1],ec=thing[2],z=5)
    else: thing=None
    rec=dict(lot=lot,street=street,m=m,t=t,n=n,L=L,house=house,gar=gar,drive=drive,thing=thing,k=k,trees=[])
    if random.random()<.65:
        tp=m+n*random.uniform(2.5,5)+t*random.uniform(-L*0.35,L*0.35)
        ax.add_patch(Circle(tuple(tp),2.4,fc="#3e7a32",alpha=.9,zorder=6)); rec['trees'].append(tp)
    if random.random()<.5:
        tp=m+n*(depth-4)+t*random.uniform(-L*0.3,L*0.3)
        if lot.contains(Point(tp)): ax.add_patch(Circle(tuple(tp),3.2,fc="#3e7a32",alpha=.85,zorder=6)); rec['trees'].append(tp)
    num[street]=num.get(street,98)+2
    rec['num']=num[street]; REC.append(rec)
    hc=np.array(house.centroid.coords[0]); HOUSES[(street,num[street])]=(hc,m,n); LOTPOLY[(street,num[street])]=lot; ax.text(hc[0],hc[1],str(num[street]),ha="center",va="center",fontsize=5,zorder=7)
# ---- park
px0,px1,py0,py1=X[1]+H,X[2]-H,Y[1]+H,Y[2]-H
draw(box(px0,py0,px1,py1),"#93c96e",ec="#5d8a40",lw=1,z=1)
dia=Polygon([(250,100),(272,122),(250,144),(228,122)]); draw(dia,"#c9a06e",ec="w",z=2)
draw(box(225,97,276,148).difference(box(225,97,276,148)),"none")
ax.plot([250,250],[100,96],color="#888",lw=1,zorder=2)
draw(box(300,100,328,116),"#9a9a9a",ec="w",z=2); ax.text(314,108,"courts",ha="center",fontsize=6,zorder=7)
draw(box(335,120,370,150),"#e3cf8f",z=2); [ax.add_patch(Circle(p,1.5,fc="#d24",zorder=3)) for p in [(343,128),(355,140),(362,128)]]
ax.text(352,152,"playground",ha="center",fontsize=6,zorder=7)
draw(box(300,135,318,150),"#a0703f",z=3); ax.text(309,131,"pavilion",ha="center",fontsize=6,zorder=7)
path=LineString([(px0,130),(230,150),(300,155),(px1,140)]); line(path.buffer(1.2),"#e0d6c0",0,z=2); draw(path.buffer(1.2),"#e3d9c3",z=2)
for _ in range(28):
    p=(random.uniform(px0+3,px1-3),random.uniform(py0+3,py1-3))
    if not any(g.buffer(4).contains(Point(p)) for g in (dia,box(300,100,328,116),box(335,120,370,150),box(300,135,318,150))):
        rr=random.uniform(3,4.5); ax.add_patch(Circle(p,rr,fc="#3e7a32",alpha=.85,zorder=4)); PARKTREES.append((p[0],p[1],rr))
ax.text(292,160,"MAPLE PARK",ha="center",fontsize=11,weight="bold",color="#2d4a1e",zorder=8)
# ---- school
sx0,sx1,sy0,sy1=X[2]+H,X[3]-H,Y[2]+H,Y[3]-H
draw(box(sx0,sy0,sx1,sy1),"#8fbf6a",ec="#5d8a40",z=1)
school=unary_union([box(400,215,470,248),box(400,190,425,215)]); draw(school,"#d8b48a",ec="#333",lw=0.8,z=5)
ax.text(440,232,"Maple Elementary",ha="center",fontsize=8,weight="bold",zorder=7)
draw(box(430,186,480,210),"#9a9a9a",z=2); ax.text(455,198,"parking",ha="center",fontsize=6,zorder=7)
draw(box(495,190,575,250),"#93c96e",ec="w",lw=1.2,z=2); ax.text(535,220,"soccer field",ha="center",fontsize=7,zorder=7)
draw(box(440,240,480,252),"#e3cf8f",z=2)
line(box(sx0+1,sy0+1,sx1-1,sy1-1).exterior,"#aaaaaa",1.2,z=3); ax.text(560,184,"chain-link fence",fontsize=5.5,zorder=7)
# ---- creek & woods
yy=np.linspace(-25,285,300); xx=625+10*np.sin(yy/27)+4*np.sin(yy/9)
ax.fill_betweenx(yy,xx-3.5,xx+3.5,color="#5aa6d6",zorder=2)
for _ in range(220):
    y=random.uniform(-25,285); x=random.uniform(596,668)
    cxk=625+10*np.sin(y/27)+4*np.sin(y/9)
    if abs(x-cxk)>6: ax.add_patch(Circle((x,y),random.uniform(3,5.5),fc=random.choice(["#3e7a32","#356b2b","#47853a"]),alpha=.9,zorder=3))
yb=130; xb=625+10*np.sin(yb/27)+4*np.sin(yb/9)
draw(box(xb-7,yb-1.6,xb+7,yb+1.6),"#a0703f",z=5); ax.text(xb+9,yb+2,"footbridge",fontsize=6.5,zorder=7)
draw(LineString([(592.5,130),(xb-7,130)]).buffer(1),"#d8ccb0",z=4)
ax.text(xx[200]+6,yy[200],"Turtle Creek",rotation=90,fontsize=9,color="#1e4f74",zorder=8)
# woods south & north edges
for _ in range(90):
    p=(random.uniform(-25,595),random.choice([random.uniform(-25,-10),random.uniform(271,285)]))
    ax.add_patch(Circle(p,random.uniform(3,5),fc="#3e7a32",alpha=.85,zorder=3))
# street trees
for name,(cl,_) in streets.items():
    if name=="Willow Ct": continue
    for d in np.arange(10,cl.length,24):
        p=np.array(cl.interpolate(d).coords[0]); a=np.array(cl.coords[0]);b=np.array(cl.coords[-1]);tt=(b-a)/np.linalg.norm(b-a);nn=np.array([-tt[1],tt[0]])
        for s in(-1,1):
            q=p+nn*s*6.4
            if not any(streets[o][1].contains(Point(q)) for o in streets if o!=name): ax.add_patch(Circle(tuple(q),1.8,fc="#4d8f3d",zorder=4))
# labels
for y,nm in names_h.items():
    x=60 if y!=174 else 290
    ax.text(x+40 if y in (87,) else x,y,nm,color="w",fontsize=8,weight="bold",va="center",zorder=9,bbox=dict(fc="#444",ec="none",alpha=.75,pad=1))
for x,nm in names_v.items():
    ax.text(x,40,nm,color="w",fontsize=8,weight="bold",ha="center",rotation=90,zorder=9,bbox=dict(fc="#444",ec="none",alpha=.75,pad=1))
ax.text(97.5,115,"Willow Ct",color="w",fontsize=7,weight="bold",rotation=90,ha="center",zorder=9,bbox=dict(fc="#444",ec="none",alpha=.75,pad=1))
ax.plot([20,70],[-18,-18],color="k",lw=3,zorder=9); ax.text(45,-15,"50 m",ha="center",fontsize=8,zorder=9)




import pickle
pickle.dump(dict(REC=REC,FENCES=FENCES,PARKTREES=PARKTREES),open('rec.pkl','wb'))
print(len(REC),len(FENCES))
