import sightlines as s
from parse import maps
for i,mp in enumerate(maps):
    area,ob,lines=s.all_long(mp,cut=60.0)
    print('%-18s lines over 60 m: %5d   longest %3.0f m'%(mp['name'],len(lines),max([l.length for l in lines] or [0])))
