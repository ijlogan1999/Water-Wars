import re, math
import os
SRC=os.path.join(os.path.dirname(os.path.abspath(__file__)),'../../Assets/WaterWars/Scripts/NeighborhoodData.cs')
s=open(SRC).read()
def floats(t):
    return [float(x.rstrip('f')) for x in re.findall(r'-?[\d.]+f?',t)] if t.strip() else []
def arr(block): return floats(block)
# Lots
lots=[]
for m in re.finditer(r'new LotRec\((.*?)\),?\n',s):
    body=m.group(1)
    parts=[]; depth=0; cur=''
    for ch in body:
        if ch=='{': depth+=1
        if ch=='}': depth-=1
        if ch==',' and depth==0: parts.append(cur.strip()); cur=''
        else: cur+=ch
    parts.append(cur.strip())
    def A(x): return floats(re.sub(r'new float\[\]','',x).strip('{}'))
    def N(x): return float(x.rstrip('f'))
    L=dict(street=parts[0].strip('"'),num=int(parts[1]),poly=A(parts[2]),front=A(parts[3]),along=A(parts[4]),inw=A(parts[5]),
      frontage=N(parts[6]),house=A(parts[7]),hw=N(parts[8]),hd=N(parts[9]),hh=N(parts[10]),color=int(parts[11]),garage=A(parts[12]),
      drive=A(parts[13]),dlen=N(parts[14]),thing=parts[15].split('.')[1],thingAt=A(parts[16]),backyard=A(parts[17]),trees=A(parts[18]),depth=N(parts[19]),treespot=A(parts[20]))
    lots.append(L)
def block(name):
    i=s.index('public static readonly float[] '+name); j=s.index('{',i); k=s.index('}',j)
    return floats(s[j+1:k])
fences=block('Fences'); gates=block('Gates'); parktrees=block('ParkTrees')
maps=[]
for m in re.finditer(r'new MapDef\("([^"]+)", "([^"]*)", "([^"]+)",\n(.*?)\}\),?\n',s,re.S):
    body=m.group(4)
    lines=[l.strip() for l in body.split('\n') if l.strip()]
    area=floats(lines[0].replace('new float[]',''))
    sp=re.findall(r'new float\[\]\{([^}]*)\}',lines[1]); att=floats(sp[0]); dfn=floats(sp[1])
    sites=[(a,k,int(l),floats(c),float(r.rstrip('f'))) for a,k,l,c,r in re.findall(r'new SiteDef\("([^"]+)",SiteKind\.(\w+),(-?\d+),new float\[\]\{([^}]*)\},([\d.]+f?)\)',lines[2])]
    rbs=[(k,floats(a),floats(d)) for k,a,d in re.findall(r'Roadblock\.Kind\.(\w+),new float\[\]\{([^}]*)\},new float\[\]\{([^}]*)\}',lines[3])]
    edges=[(floats(a),floats(b),k) for a,b,k in re.findall(r'new Edge\(new float\[\]\{([^}]*)\},new float\[\]\{([^}]*)\},Edge\.Kind\.(\w+)\)',lines[4])]
    props=[(k.lower(),)+tuple(floats(v)) for k,v in re.findall(r'Prop\.Kind\.(\w+),new float\[\]\{([^}]*)\}',lines[5])]
    lotids=[int(x) for x in re.findall(r'\d+',lines[6].replace('new int[]',''))]
    fenceids=[int(x) for x in re.findall(r'\d+',lines[7].replace('new int[]',''))]
    maps.append(dict(name=m.group(1),area=area,att=att,dfn=dfn,sites=sites,rbs=rbs,edges=edges,props=props,lots=lotids,fences=fenceids))
if __name__=='__main__':
    print(len(lots),len(fences)//5,len(gates)//4,len(maps),[m['name'] for m in maps],[len(m['edges']) for m in maps])
