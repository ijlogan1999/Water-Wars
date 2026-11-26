// NeighborhoodTypes.cs
// ---------------------------------------------------------------------------
// The shapes of the data in NeighborhoodData.cs: what a house lot, a map, a
// plant site, a roadblock and a map edge look like as numbers.
//
// Positions are 2D points seen from above, in meters: x goes east, y goes
// north. When the map is built, the drawing's y becomes Unity's z.
// ---------------------------------------------------------------------------

using UnityEngine;

namespace WaterWars
{
    public enum SiteKind { Spot, Garage, Treehouse, Yard }
    public enum Yard { None, Pool, Treehouse, Shed, Trampoline, Swings }

    /// One house lot: its property line, its house, garage, driveway and backyard stuff.
    public class LotRec
    {
        public readonly string Street;
        public readonly int Number;
        public readonly Vector2[] Poly;   // property line, counterclockwise
        public readonly Vector2 Front;    // middle of the lot's street edge
        public readonly Vector2 Along;    // direction along the street
        public readonly Vector2 In;       // direction from the street into the lot
        public readonly float Frontage, Depth;
        public readonly Vector2 House;
        public readonly float HouseWidth, HouseDepth, HouseHeight;
        public readonly int ColorIndex;
        public readonly bool HasGarage;
        public readonly Vector2 Garage;
        public readonly Vector2 Drive;
        public readonly float DriveLength;
        public readonly Yard Thing;
        public readonly Vector2 ThingAt;
        public readonly Vector2 Backyard; // a good open spot in the middle of the backyard
        public readonly Vector2[] Trees;
        public readonly bool HasTreeSpot;
        public readonly Vector2 TreeSpot;  // where a treehouse fits in this backyard (ramp toward the house)

        public string Address { get { return Number + " " + Street; } }

        public LotRec(string street, int number, float[] poly, float[] front, float[] along, float[] inward, float frontage,
            float[] house, float houseW, float houseD, float houseH, int color, float[] garage, float[] drive, float driveLen,
            Yard thing, float[] thingAt, float[] backyard, float[] trees, float depth, float[] treeSpot)
        {
            Street = street; Number = number;
            Poly = Pts(poly);
            Front = V(front); Along = V(along); In = V(inward);
            Frontage = frontage; Depth = depth;
            House = V(house); HouseWidth = houseW; HouseDepth = houseD; HouseHeight = houseH;
            ColorIndex = color;
            HasGarage = garage.Length == 2;
            if (HasGarage) Garage = V(garage);
            Drive = V(drive); DriveLength = driveLen;
            Thing = thing; ThingAt = V(thingAt);
            Backyard = V(backyard);
            Trees = Pts(trees);
            HasTreeSpot = treeSpot.Length == 2;
            if (HasTreeSpot) TreeSpot = V(treeSpot);
        }

        public static Vector2 V(float[] a) { return new Vector2(a[0], a[1]); }
        public static Vector2[] Pts(float[] a)
        {
            var p = new Vector2[a.Length / 2];
            for (int i = 0; i < p.Length; i++) p[i] = new Vector2(a[i * 2], a[i * 2 + 1]);
            return p;
        }
    }

    /// One plant site on a map.
    public class SiteDef
    {
        public readonly string Label;
        public readonly SiteKind Kind;
        public readonly int Lot;          // index into NeighborhoodData.Lots, or -1
        public readonly Vector2 Center;
        public readonly float Radius;
        public SiteDef(string label, SiteKind kind, int lot, float[] center, float radius)
        {
            Label = label; Kind = kind; Lot = lot; Center = LotRec.V(center); Radius = radius;
        }
    }

    /// Something blocking a street where it leaves the map.
    public class Roadblock
    {
        public enum Kind { CarCrash, MovingTruck, RoadWork, GarbageTruck, PoliceTape, IceCreamTruck, FireTruck, DeliveryVan }
        public readonly Kind What;
        public readonly Vector2 At;   // where the street crosses the map edge
        public readonly Vector2 Out;  // pointing away from the map
        public Roadblock(Kind what, float[] at, float[] dir) { What = what; At = LotRec.V(at); Out = LotRec.V(dir); }
    }

    /// One straight piece of a map's edge, and what's on the other side of it.
    public class Edge
    {
        public enum Kind { Street, Curb, Lot, LotFenced, CurbFenced, Park, School, Woods, Verge }
        public readonly Vector2 A, B;
        public readonly Kind What;
        public Edge(float[] a, float[] b, Kind what) { A = LotRec.V(a); B = LotRec.V(b); What = what; }
    }

    /// Extra cover a map adds on top of the neighborhood: parked cars, vans,
    /// portable classrooms, sheds, a snack stand, party tents, new fences and
    /// hedges, trees. Added after the Map Playbook review (tactical shooter
    /// lessons): they break up long sightlines and split open areas into lanes.
    /// The numbers mean (sizes in meters, angles in degrees, 0 = pointing east):
    ///   Car, Van, Truck:               x, y, angle (the way the long side points)
    ///   Portable, Shed, Stand, Tent:   x, y, angle, width, depth
    ///   Fence, Hedge:                  x1, y1, x2, y2, gateAt, gateWidth
    ///                                  (a gap gateWidth wide, gateAt meters from the first end; gateAt below 0 = no gate)
    ///   Tree:                          x, y, radius
    public class Prop
    {
        public enum Kind { Car, Van, Truck, Portable, Shed, Stand, Tent, Fence, Hedge, Tree, Townhome, RV }
        public readonly Kind What;
        public readonly float[] V;
        public Prop(Kind what, float[] v) { What = what; V = v; }
    }

    /// One map: a piece of the neighborhood with spawns, two sites and closed-off edges.
    public class MapDef
    {
        public readonly string Name, Note, AttackerStreet;
        public readonly Vector2[] Area;    // the play area's outline, counterclockwise
        public readonly Vector2 Attackers, Defenders;
        public readonly SiteDef[] Sites;
        public readonly Roadblock[] Roadblocks;
        public readonly Edge[] Edges;
        public readonly Prop[] Props;      // extra cover just for this map
        public readonly int[] LotIds, FenceIds;  // what to build (the map plus a ring of scenery around it)

        public MapDef(string name, string note, string attackerStreet, float[] area, float[] attackers, float[] defenders,
            SiteDef[] sites, Roadblock[] roadblocks, Edge[] edges, Prop[] props, int[] lots, int[] fences)
        {
            Name = name; Note = note; AttackerStreet = attackerStreet;
            Area = LotRec.Pts(area);
            Attackers = LotRec.V(attackers); Defenders = LotRec.V(defenders);
            Sites = sites; Roadblocks = roadblocks; Edges = edges; Props = props; LotIds = lots; FenceIds = fences;
        }

        /// True when a point (seen from above) is inside the play area.
        public bool Contains(Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = Area.Length - 1; i < Area.Length; j = i++)
            {
                Vector2 a = Area[i], b = Area[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        public Rect Bounds
        {
            get
            {
                Vector2 min = Area[0], max = Area[0];
                foreach (var p in Area) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
                return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }
        }
    }
}
