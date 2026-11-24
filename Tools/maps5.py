import pickle, numpy as np
import matplotlib; matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.path import Path as MPath
from matplotlib.patches import PathPatch, Circle, Polygon as MPoly
from shapely.geometry import box, Point, LineString, Polygon
from shapely.geometry.polygon import orient
from shapely.ops import unary_union
from shapely import wkt
plt.rcParams["font.family"]="DejaVu Sans"
img=plt.imread("base.png"); EXT=(-25,668,-25,285)
LOT={k:wkt.loads(v) for k,v in pickle.load(open("lots.pkl","rb")).items()}
ROW=15;H=7.5;X=[0,195,390,585];Y=[0,87,174,261]
ST={"Maple St":LineString([(-7.5,0),(592.5,0)]),"Birch Ln":LineString([(-7.5,87),(592.5,87)]),
    "Cedar Ln":LineString([(187.5,174),(592.5,174)]),"Oak St":LineString([(-7.5,261),(592.5,261)]),
    "Elm St":LineString([(0,-7.5),(0,268.5)]),"Hickory Rd":LineString([(195,-7.5),(195,268.5)]),
    "Pine St":LineString([(390,-7.5),(390,268.5)]),"Creek Rd":LineString([(585,-7.5),(585,268.5)]),"Willow Ct":LineString([(97.5,87),(97.5,172)])}
def road(n,a,b):  # piece of a street's right-of-way between a and b along it
    c=ST[n]; (x0,y0),(x1,y1)=c.coords
    return box(a,y0-H,b,y0+H) if y0==y1 else box(x0-H,a,x0+H,b)
def U(*g): return unary_union(g).buffer(0.05,join_style=2).buffer(-0.05,join_style=2)
L=lambda s,n: LOT[(s,n)]
bulb=Point(97.5,172).buffer(15)
MAPS=[
 dict(name="1. Park Run (the one you approved)",
  area=U(L("Maple St",126),L("Maple St",128),L("Birch Ln",126),L("Birch Ln",128),road("Maple St",315,360),road("Birch Ln",315,360),
         Polygon([(315,94.5),(360,94.5),(372,106.5),(372,166.5),(295,166.5),(295,98.5)])),
  att=(337.5,0),dfn=(333,160),A=(309,142.5,"pavilion"),B=(352,135,"playground"),mid=(314,108,"courts"),
  note="Yards of 126 & 128 Maple and 126 & 128 Birch, opening into the park."),
 dict(name="2. Willow Court",
  area=U(box(67.5,79.5,127.5,150),L("Willow Ct",102),L("Willow Ct",104),L("Willow Ct",106),L("Willow Ct",108),L("Willow Ct",110),L("Willow Ct",112),bulb),
  att=(97.5,87),dfn=(97.5,208),A=(62,195,"104 Willow Ct garage"),B=(132,195,"110 Willow Ct treehouse"),mid=(97.5,172,"cul-de-sac"),
  note="Up the side yards of 166 & 168 Birch or straight up the Willow Ct stem, into the cul-de-sac."),
 dict(name="3. Schoolyard",
  area=U(L("Birch Ln",154),L("Birch Ln",156),L("Cedar Ln",122),L("Cedar Ln",124),road("Birch Ln",465,510),road("Cedar Ln",465,510),
         Polygon([(465,181.5),(510,181.5),(560,190),(577.5,200),(577.5,253.5),(420,253.5),(420,200),(440,190)])),
  att=(487.5,87),dfn=(500,248),A=(445,205,"school back doors"),B=(540,222,"soccer goal & bleachers"),mid=(455,186,"parking lot"),
  note="Through 154 & 156 Birch and 122 & 124 Cedar, across Cedar Ln, into Maple Elementary's yard."),
 dict(name="4. Creekside",
  area=U(L("Maple St",144),L("Maple St",146),L("Birch Ln",144),L("Birch Ln",146),road("Maple St",532.5,577.5),road("Birch Ln",532.5,577.5),
         L("Birch Ln",160),L("Birch Ln",162),L("Cedar Ln",128),L("Cedar Ln",130),box(577.5,94.5,655,166.5)),
  att=(555,0),dfn=(648,152),A=(617,131,"footbridge"),B=(566,140,"130 Cedar backyard"),mid=(585,120,"Creek Rd"),
  note="Through 144 & 146 Maple and 144 & 146 Birch, then along Creek Rd down to the creek."),
 dict(name="5. Pine St Backyards",
  area=U(road("Pine St",112.5,148.5),L("Cedar Ln",116),L("Birch Ln",148),
         L("Cedar Ln",118),L("Birch Ln",150),L("Cedar Ln",120),L("Birch Ln",152),L("Cedar Ln",122),L("Birch Ln",154),L("Cedar Ln",124),L("Birch Ln",156)),
  att=(390,130.5),dfn=(503,130.5),A=(476,150,"122 Cedar backyard"),B=(470,110,"154 Birch garage"),mid=(431,130.5,"back fence line"),
  note="Sideways map: in from Pine St over the back fences of five Cedar and five Birch houses. Front yards walled by parked cars."),
 dict(name="6. Hickory Corner",
  area=U(L("Maple St",108),L("Maple St",110),L("Birch Ln",108),L("Birch Ln",110),road("Maple St",97.5,142.5),road("Birch Ln",55,187.5),
         L("Birch Ln",166),box(91.5,94.5,103.5,150),L("Birch Ln",168),L("Birch Ln",170),L("Hickory Rd",100),L("Willow Ct",114)),
  att=(120,0),dfn=(127,143),A=(79,117,"166 Birch garage"),B=(170,110,"100 Hickory backyard"),mid=(115,112,"168 Birch yard"),
  note="Through 108 & 110 Maple and 108 & 110 Birch. Turn left for A (166 Birch), right for B (100 Hickory), or run along Birch Ln to hit either site from the side. Defenders hold the yard behind."),
]
KINDS=["CAR CRASH","MOVING TRUCK","ROAD WORK","GARBAGE TRUCK","POLICE TAPE","ICE CREAM TRUCK","FIRE TRUCK","DELIVERY VAN"]
def holepatch(ax,outer,hole,**kw):
    outer=orient(outer,1); hole=orient(hole,-1)
    v=[];c=[]
    for ring in (outer.exterior,hole.exterior):
        pts=list(ring.coords); v+=pts; c+=[MPath.MOVETO]+[MPath.LINETO]*(len(pts)-2)+[MPath.CLOSEPOLY]
    ax.add_patch(PathPatch(MPath(v,c),**kw))
def blocks(area):
    out=[]
    for n,cl in ST.items():
        inter=cl.intersection(area.boundary)
        for g in getattr(inter,"geoms",[inter]):
            if g.is_empty or g.geom_type!="Point": continue
            p=np.array(g.coords[0]); a=np.array(cl.coords[0]); b=np.array(cl.coords[1]); t=(b-a)/np.linalg.norm(b-a)
            for s in (1,-1):
                q=p+t*s*3
                if not area.contains(Point(q)) and area.contains(Point(p-t*s*3)) and Point(q).distance(cl)<0.1 and min(np.linalg.norm(q-a),np.linalg.norm(q-b))>2:
                    if all(np.linalg.norm(p-o[0])>15 for o in out): out.append((p,t*s))
    return out
def panel(ax,m,k0=0):
    area=m["area"]; x0,y0,x1,y1=area.bounds; pad=16
    ax.imshow(img,extent=EXT,origin="upper",interpolation="bilinear")
    holepatch(ax,box(-25,-25,668,285),area,fc="#111",ec="none",alpha=.62,zorder=5)
    ax.plot(*area.exterior.xy,color="#ffd400",lw=2.5,zorder=6)
    for i,(p,d) in enumerate(blocks(area)):
        nrm=np.array([-d[1],d[0]]); q=p+d*2
        ax.add_patch(MPoly([q+nrm*s1*7+d*s2*2 for s1,s2 in((-1,-1),(1,-1),(1,1),(-1,1))],fc="#e8452c",ec="k",lw=1,zorder=7))
        r=q+d*10
        ax.text(*r,KINDS[(k0+i)%len(KINDS)],fontsize=6.5,weight="bold",ha="center",va="center",zorder=9,bbox=dict(fc="#ffd400",ec="k",pad=1.2))
    def tag(xy,txt,dy=0,ha="center"):
        ax.text(xy[0],xy[1]+dy,txt,ha=ha,va="center",fontsize=7.5,weight="bold",zorder=9,bbox=dict(fc="w",ec="none",alpha=.9,pad=1))
    ax.add_patch(Circle(m["att"],4,fc="#ff8a1f",ec="k",zorder=8)); tag(m["att"],"ATTACKERS",-7)
    ax.add_patch(Circle(m["dfn"],4,fc="#18b8b8",ec="k",zorder=8)); tag(m["dfn"],"DEFENDERS",-7)
    for key in "AB":
        x,y,t=m[key]; ax.add_patch(Circle((x,y),7,fc="#3d8cff",ec="#0d3d99",lw=2,alpha=.6,zorder=8))
        ax.text(x,y,key,fontsize=15,weight="bold",color="#0d3d99",ha="center",va="center",zorder=9); tag((x,y),t,-10.5)
    x,y,t=m["mid"]; tag((x,y),"mid: "+t)
    ax.set_xlim(max(-25,x0-pad-14),min(668,x1+pad+14)); ax.set_ylim(y0-pad,y1+pad); ax.set_aspect("equal"); ax.set_xticks([]); ax.set_yticks([])
    ax.set_title(m["name"],fontsize=12,weight="bold",loc="left")
    ax.text(0,-0.02,m["note"],transform=ax.transAxes,fontsize=8,va="top",wrap=True)
    for t in ax.texts[:-1]: t.set_clip_on(True)
    return len(blocks(area))
# individual sheets + overview
import os; os.makedirs("drawings",exist_ok=True)
k=0
for i,m in enumerate(MAPS):
    w=m["area"].bounds[2]-m["area"].bounds[0]+60; h=m["area"].bounds[3]-m["area"].bounds[1]+32
    fig,ax=plt.subplots(figsize=(min(9,max(5,w/22)), min(10,max(5,h/22))+0.8))
    k+=panel(ax,m,k); print(m["name"],round(m["area"].area),m["area"].geom_type)
    fig.tight_layout(); fig.savefig(f"drawings/map-{i+1}.png",dpi=140); plt.close(fig)
fig,ax=plt.subplots(figsize=(16,8))
ax.imshow(img,extent=EXT,origin="upper")
cols=["#ffd400","#ff5bd6","#5bf0ff","#ff8a1f","#7dff5b","#ffffff"]
for i,m in enumerate(MAPS):
    a=m["area"]; ax.add_patch(MPoly(list(a.exterior.coords),fc=cols[i],alpha=.25,ec=cols[i],lw=2.5,zorder=5))
    c=a.representative_point(); ax.text(c.x,c.y,str(i+1),fontsize=20,weight="bold",ha="center",va="center",zorder=6,color="k",bbox=dict(fc=cols[i],ec="k",boxstyle="circle"))
ax.set_xlim(-25,668); ax.set_ylim(-25,285); ax.set_aspect("equal"); ax.set_xticks([]); ax.set_yticks([])
ax.set_title("Six example match maps cut out of Maple Park (numbers match the zoomed sheets)",fontsize=13)
fig.tight_layout(); fig.savefig("drawings/overview.png",dpi=120)
