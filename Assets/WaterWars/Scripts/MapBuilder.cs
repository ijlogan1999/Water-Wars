// MapBuilder.cs
// ---------------------------------------------------------------------------
// Builds the map out of grey-box blocks when a match starts.
//
// THE NEIGHBORHOOD ("Maple Park")
//   One big, realistic neighborhood: about 110 houses on a grid of streets
//   (Maple St, Birch Ln, Cedar Ln, Oak St, crossed by Elm St, Hickory Rd,
//   Pine St and Creek Rd), a cul-de-sac (Willow Ct), Maple Park with its
//   pavilion, playground and courts, Maple Elementary, and Turtle Creek with
//   a footbridge. Every number for it lives in NeighborhoodData.cs, written
//   by a script from the same drawings Ian approved.
//
// HOW A MAP IS MADE
//   A map is a small piece of that neighborhood: a few spots close together.
//   Attackers start on a street, run a narrow lane through two houses' yards,
//   and come out where the two plant sites are. Everything else is closed
//   off: roadblocks (a car crash, a moving truck, road work...) where streets
//   leave the map, closed fences and hedges everywhere else. There are six
//   maps for now (see NeighborhoodData.Maps); each match picks one.
//
// CHOKEPOINTS
//   Backyards are fenced. Side yards between houses lead into each backyard,
//   and gates in the fences connect yards. Each way in is narrow.
//
// COVER (added after the Map Playbook review)
//   Each map also adds its own props (MapDef.Props): parked cars and trucks
//   that break up long streets, portable classrooms, sheds, a snack stand,
//   party tents to plant under, and new tall fences or hedges with gates that
//   split open areas into lanes. See BuildProps.
//
// Positions: the drawings use x = east, y = north. In Unity that's x and z.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace WaterWars
{
    /// One plant site (A or B).
    public class Site
    {
        public string Name;
        public string Address;
        public Vector3 Center;
        public float Radius;
        public readonly List<Vector3> HoldSpots = new List<Vector3>();   // Where defenders wait.
        public readonly List<Vector3> AttackSpots = new List<Vector3>(); // Where attackers push to.

        public bool Contains(Vector3 p)
        {
            var d = p - Center;
            d.y = 0f;
            return d.magnitude <= Radius && p.y < Center.y + 6f;
        }
    }

    /// Everything the rest of the game needs to know about the map.
    public class MapData
    {
        public string Name;
        public string AttackerAddress;
        public MapDef Def;
        public Vector3 Center;
        public readonly List<Vector3> AttackerSpawns = new List<Vector3>();
        public readonly List<Vector3> DefenderSpawns = new List<Vector3>();
        public readonly List<Vector3> AttackerCurb = new List<Vector3>();
        public readonly List<Vector3> DefenderCurb = new List<Vector3>();
        public Site SiteA, SiteB;
        public Site[] Sites;
        public NavGraph Nav = new NavGraph();
        public float AttackerFacing, DefenderFacing; // which way each crew looks when it spawns

        public string Describe()
        {
            return "Map: " + Name + "\nAttackers start on " + AttackerAddress +
                   "\nSite A: " + SiteA.Address + "\nSite B: " + SiteB.Address;
        }

        /// True when a point is inside the play area.
        public bool InPlay(Vector3 p) { return Def.Contains(new Vector2(p.x, p.z)); }
    }

    public static class MapBuilder
    {
        // Grey-box colors. Simple and readable, warm like a summer afternoon.
        static readonly Color Grass = new Color(0.45f, 0.68f, 0.32f);
        static readonly Color ParkGrass = new Color(0.52f, 0.74f, 0.38f);
        static readonly Color Asphalt = new Color(0.32f, 0.32f, 0.34f);
        static readonly Color Sidewalk = new Color(0.78f, 0.76f, 0.72f);
        static readonly Color Verge = new Color(0.5f, 0.7f, 0.36f);
        static readonly Color HedgeGreen = new Color(0.2f, 0.42f, 0.18f);
        static readonly Color FenceWood = new Color(0.62f, 0.45f, 0.3f);
        // ---- How tall things are -------------------------------------------------
        // The whole neighborhood is built at "kid's eye" scale: a third taller than
        // real life, the way a fence looks like a wall when you're ten. It also does
        // a job: in tactical shooters the walls beside a path are about as tall as the
        // path is wide, and that is what makes a path feel like a path, not a field.
        // Nothing meant to block the view is under 3 m, because a jumping kid's eyes
        // reach 2.7 m.
        const float FenceHeight = 3.0f;     // wooden privacy fences and locked gates
        const float HedgeHeight = 4.2f;     // tall hedges (in yards, as props, on the map's edge)
        const float TreeLineHeight = 4.8f;  // the hedge wall where the map meets a park or the woods
        const float ScreenHeight = 3.2f;    // green construction screens that close off street ends
        const float HouseTall = 1.35f;      // houses are built this many times taller than the map data says
        const float GarageHeight = 4.2f;
        static readonly Color ScreenGreen = new Color(0.16f, 0.36f, 0.26f);
        static readonly Color PicketWhite = new Color(0.93f, 0.92f, 0.88f);
        static readonly Color ChainLink = new Color(0.6f, 0.62f, 0.64f);
        static readonly Color Bark = new Color(0.42f, 0.3f, 0.2f);
        static readonly Color Leaves = new Color(0.24f, 0.47f, 0.2f);
        static readonly Color Plank = new Color(0.7f, 0.52f, 0.33f);
        static readonly Color Roof = new Color(0.45f, 0.25f, 0.22f);
        static readonly Color GarageGrey = new Color(0.72f, 0.72f, 0.7f);
        static readonly Color DarkWindow = new Color(0.2f, 0.25f, 0.32f);
        static readonly Color Sand = new Color(0.88f, 0.8f, 0.56f);
        static readonly Color Dirt = new Color(0.78f, 0.62f, 0.43f);
        static readonly Color Water = new Color(0.35f, 0.65f, 0.85f);
        static readonly Color Orange = new Color(1f, 0.5f, 0.1f);
        static readonly Color SiteMark = new Color(0.25f, 0.55f, 1f);

        static readonly Color[] HouseColors =
        {
            new Color(0.95f, 0.84f, 0.82f), new Color(0.85f, 0.89f, 0.95f), new Color(0.94f, 0.89f, 0.75f),
            new Color(0.84f, 0.91f, 0.81f), new Color(0.91f, 0.85f, 0.94f), new Color(0.95f, 0.94f, 0.9f),
            new Color(0.91f, 0.81f, 0.68f), new Color(0.94f, 0.88f, 0.78f),
        };
        static readonly Color[] CarColors =
        {
            new Color(0.7f, 0.15f, 0.15f), new Color(0.2f, 0.3f, 0.6f), new Color(0.85f, 0.85f, 0.8f),
            new Color(0.25f, 0.45f, 0.3f), new Color(0.55f, 0.5f, 0.45f), new Color(0.15f, 0.15f, 0.18f),
        };

        // ---- The street grid (same numbers as the drawing) ------------------
        const float StreetRow = 15f;   // property line to property line
        const float RoadWidth = 9f;    // asphalt
        static readonly float[] StreetX = { 0f, 195f, 390f, 585f };   // Elm St, Hickory Rd, Pine St, Creek Rd
        static readonly float[] StreetY = { 0f, 87f, 174f, 261f };    // Maple St, Birch Ln, Cedar Ln, Oak St
        static readonly Vector2 BulbCenter = new Vector2(97.5f, 172f); // Willow Ct cul-de-sac
        const float BulbRadius = 15f;
        static readonly Rect Park = Rect.MinMaxRect(202.5f, 94.5f, 382.5f, 166.5f);
        static readonly Rect School = Rect.MinMaxRect(397.5f, 181.5f, 577.5f, 253.5f);

        static int lastMap = -1;
        static Rect near;   // only scenery inside this rectangle gets built

        static Vector3 W(Vector2 p, float y = 0f) { return new Vector3(p.x, y, p.y); }
        static Quaternion Facing(Vector2 dir) { return Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y), Vector3.up); }
        static bool Near(Vector2 p) { return near.Contains(p); }

        public static MapData Build(Transform root)
        {
            // ---- Pick this match's map ------------------------------------------
            var maps = NeighborhoodData.Maps;
            int pick = Tuning.MapChoice >= 1 && Tuning.MapChoice <= maps.Length ? Tuning.MapChoice - 1 : Random.Range(0, maps.Length);
            if (Tuning.MapChoice < 1 && pick == lastMap && maps.Length > 1) pick = (pick + 1 + Random.Range(0, maps.Length - 1)) % maps.Length;
            lastMap = pick;
            var def = maps[pick];

            var map = new MapData { Name = def.Name, Def = def, AttackerAddress = def.AttackerStreet };
            Rect b = def.Bounds;
            map.Center = new Vector3(b.center.x, 0f, b.center.y);
            near = Rect.MinMaxRect(b.xMin - 60f, b.yMin - 60f, b.xMax + 60f, b.yMax + 60f);

            var treehouses = new List<TreehouseNav>();
            var siteTrees = new Dictionary<SiteDef, TreehouseNav>();
            if (def.Blueprint)
            {
                // ---- A map drawn from a blank page: grass, then only what the drawing lists ----
                Shapes.Box("Ground", new Vector3(b.center.x, -0.5f, b.center.y), new Vector3(760f, 1f, 760f), Grass, root);
                BuildBackdrop(root, def);
            }
            else
            {
                // ---- Ground and streets --------------------------------------------
                Shapes.Box("Ground", new Vector3(325f, -0.5f, 130f), new Vector3(760f, 1f, 380f), Grass, root);
                BuildStreets(root);

                // ---- Houses and fences near the map -------------------------------
                var lots = NeighborhoodData.Lots;
                var siteLots = new Dictionary<int, SiteDef>();
                foreach (var s in def.Sites) if (s.Lot >= 0) siteLots[s.Lot] = s;
                foreach (int i in def.LotIds)
                {
                    siteLots.TryGetValue(i, out var site);
                    var th = BuildLot(root, lots[i], site);
                    if (th != null)
                    {
                        treehouses.Add(th);
                        if (site != null && site.Kind == SiteKind.Treehouse) siteTrees[site] = th;
                    }
                }
                BuildFences(root, def);

                // ---- Places ------------------------------------------------------------
                if (near.Overlaps(Park)) BuildPark(root);
                if (near.Overlaps(School)) BuildSchool(root);
                if (near.xMax > 560f) BuildCreek(root);
                BuildWoods(root);
            }

            // ---- Close off everything outside the map ------------------------
            BuildEdges(root, def);
            foreach (var rb in def.Roadblocks) BuildRoadblock(root, rb);

            // ---- This map's pieces (on a blueprint map, all of it; on the others,
            //      the extra cover: cars, portables, tents, new fences...) ---
            BuildProps(root, def, treehouses);

            Physics.SyncTransforms();

            // ---- Sites -------------------------------------------------------------
            var sites = new Site[2];
            for (int i = 0; i < 2; i++)
            {
                var sd = def.Sites[i];
                Vector3 center = W(sd.Center);
                if (siteTrees.TryGetValue(sd, out var tree)) center = tree.Frame.position;
                center.y = 0f;
                sites[i] = new Site { Name = i == 0 ? "A" : "B", Address = sd.Label, Center = center, Radius = sd.Radius };
                SiteMarker(root, sites[i]);
            }
            map.SiteA = sites[0];
            map.SiteB = sites[1];
            map.Sites = sites;

            // ---- Bot stepping stones (see NavGraph.cs) -----------------------
            if (def.Blueprint)
            {
                // One stone in the middle of every square of the drawing, so a stone never
                // sits on a wall and even a one-square gate gets its own stone.
                float half = BlueprintCell * 0.5f;
                map.Nav.BuildGrid(b.xMin + half, b.xMax - half, b.yMin + half, b.yMax - half, BlueprintCell, p => def.Contains(new Vector2(p.x, p.z)));
            }
            else
                map.Nav.BuildGrid(b.xMin + 0.5f, b.xMax - 0.5f, b.yMin + 0.5f, b.yMax - 0.5f, 3f, p => def.Contains(new Vector2(p.x, p.z)));
            foreach (var t in treehouses)
                if (def.Contains(XZ(t.W(t.Bottom)))) ConnectTreehouse(map.Nav, t);
            AddPortals(map.Nav, def);
            if (def.Blueprint)
            {
                // A treehouse standing on a site is part of that site: bots may hold it from up there.
                foreach (var t in treehouses)
                    foreach (var site in sites)
                        if (Vector3.Distance(Flat(t.Frame.position), Flat(site.Center)) < site.Radius) site.HoldSpots.AddRange(t.WorldFloor());
            }

            foreach (var kv in siteTrees)
            {
                int idx = System.Array.IndexOf(def.Sites, kv.Key);
                sites[idx].HoldSpots.AddRange(kv.Value.WorldFloor());
            }
            foreach (var site in sites)
            {
                site.HoldSpots.AddRange(OpenSpotsNear(map.Nav, site.Center, site.Radius, 4, 2.5f));
                site.AttackSpots.AddRange(OpenSpotsNear(map.Nav, site.Center, site.Radius * 0.7f, 4, 2f));
                if (site.AttackSpots.Count == 0) site.AttackSpots.Add(site.Center);
                if (site.HoldSpots.Count == 0) site.HoldSpots.Add(site.Center);
            }

            // ---- Spawns and the curb where Soaked kids sit -------------------
            Vector3 attack = W(def.Attackers), defend = W(def.Defenders);
            Vector3 sitesMid = (sites[0].Center + sites[1].Center) * 0.5f;
            map.AttackerSpawns.AddRange(OpenSpotsNear(map.Nav, attack, 14f, Tuning.TeamSize, 2.5f));
            map.DefenderSpawns.AddRange(OpenSpotsNear(map.Nav, defend, 14f, Tuning.TeamSize, 2.5f));
            while (map.AttackerSpawns.Count < Tuning.TeamSize) map.AttackerSpawns.Add(attack);
            while (map.DefenderSpawns.Count < Tuning.TeamSize) map.DefenderSpawns.Add(defend);
            map.AttackerFacing = YawToward(attack, sitesMid);
            map.DefenderFacing = YawToward(defend, attack);
            for (int i = 0; i < Tuning.TeamSize; i++)
            {
                map.AttackerCurb.Add(map.AttackerSpawns[i] + Vector3.right * 0.01f);
                map.DefenderCurb.Add(map.DefenderSpawns[i] + Vector3.right * 0.01f);
            }
            return map;
        }

        // ---- Blueprint maps (must match Tools/blueprint.py and Tools/props.py) ----
        const float BlueprintCell = 2f;          // one letter of the drawing is this many meters
        const float BlueprintGarageWidth = 6f, BlueprintGarageDepth = 8f, BlueprintGarageDoor = 4f;

        /// What you see past the edge of a blueprint map: a ring of houses and trees,
        /// so from a treehouse it looks like the neighborhood goes on.
        static void BuildBackdrop(Transform root, MapDef def)
        {
            var parent = Pivot(root, "Backdrop");
            Rect b = def.Bounds;
            int n = 0;
            for (int side = 0; side < 4; side++)
            {
                bool alongX = side < 2;
                float len = alongX ? b.width : b.height;
                Vector2 inward = side == 0 ? Vector2.up : side == 1 ? Vector2.down : side == 2 ? Vector2.right : Vector2.left;
                Vector2 along = alongX ? Vector2.right : Vector2.up;
                Vector2 corner = side == 1 ? new Vector2(b.xMin, b.yMax) : side == 3 ? new Vector2(b.xMax, b.yMin) : new Vector2(b.xMin, b.yMin);
                for (float t = -6f; t <= len + 6f; t += 17f, n++)
                {
                    Vector2 at = corner + along * t - inward * 9f;
                    var house = Pivot(parent, "Backdrop House");
                    BuildHouse(house, "House", Vector3.zero, new Vector3(11f, n % 3 == 0 ? 5.7f : 8.8f, 8f), HouseColors[n % HouseColors.Length]);
                    Place(house, at, inward);   // front doors face away from the map, toward their own street
                    Tree(parent, at + along * 8.5f - inward * 1.5f, 2.8f);
                }
            }
        }

        static Vector2 XZ(Vector3 p) { return new Vector2(p.x, p.z); }
        static Vector3 Flat(Vector3 p) { return new Vector3(p.x, 0f, p.z); }

        static float YawToward(Vector3 from, Vector3 to)
        {
            var d = to - from;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        /// Up to `count` open stepping stones near a point, at least `spacing` apart.
        static List<Vector3> OpenSpotsNear(NavGraph nav, Vector3 center, float radius, int count, float spacing)
        {
            var candidates = new List<Vector3>();
            foreach (var n in nav.Nodes)
                if (Mathf.Abs(n.y - center.y) < 0.5f && Vector3.Distance(n, center) <= radius) candidates.Add(n);
            candidates.Sort((p, q) => Vector3.Distance(p, center).CompareTo(Vector3.Distance(q, center)));
            var picked = new List<Vector3>();
            foreach (var c in candidates)
            {
                bool farEnough = true;
                foreach (var p in picked) if (Vector3.Distance(p, c) < spacing) farEnough = false;
                if (!farEnough) continue;
                picked.Add(c);
                if (picked.Count >= count) break;
            }
            return picked;
        }

        /// Extra stepping stones in the narrow spots the 3 m grid can miss:
        /// gates, side yards beside houses, and inside garages.
        static void AddPortals(NavGraph nav, MapDef def)
        {
            var spots = new List<Vector2>();
            var g = def.Blueprint ? new float[0] : NeighborhoodData.Gates;   // Maple Park's gates are not on a blueprint map
            for (int i = 0; i + 3 < g.Length; i += 4)
            {
                var c = new Vector2(g[i], g[i + 1]);
                var d = new Vector2(g[i + 2], g[i + 3]);
                spots.Add(c); spots.Add(c + d * 1.3f); spots.Add(c - d * 1.3f);
            }
            // Gates in this map's own new fences and hedges.
            foreach (var p in def.Props)
            {
                if (p.What != Prop.Kind.Fence && p.What != Prop.Kind.Hedge) continue;
                if (!PropGate(p, out var gc, out var gw)) continue;
                spots.Add(gc); spots.Add(gc + gw * 1.3f); spots.Add(gc - gw * 1.3f);
            }
            foreach (int id in def.LotIds)
            {
                var lot = NeighborhoodData.Lots[id];
                // Side yards are at least 3 m wide: put a stone down the middle of each.
                for (float k = 6f; k <= 20f; k += 2.5f)
                {
                    spots.Add(lot.Front + lot.In * k + lot.Along * (lot.Frontage * 0.5f - 1.5f));
                    spots.Add(lot.Front + lot.In * k - lot.Along * (lot.Frontage * 0.5f - 1.5f));
                }
                if (lot.HasGarage)
                    for (float k = -3f; k <= 4.5f; k += 2.5f) spots.Add(lot.Garage + lot.In * k);
            }
            foreach (var s in spots)
            {
                if (!def.Contains(s)) continue;
                var w = W(s);
                if (NavGraph.SpotIsFree(w)) nav.AddAndConnect(w, 4.5f);
            }
        }

        // =====================================================================
        // Streets
        // =====================================================================
        static void BuildStreets(Transform root)
        {
            float x0 = StreetX[0] - StreetRow * 0.5f, x1 = StreetX[3] + StreetRow * 0.5f;
            float y0 = StreetY[0] - StreetRow * 0.5f, y1 = StreetY[3] + StreetRow * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                float from = i == 2 ? StreetX[1] - StreetRow * 0.5f : x0;  // Cedar Ln starts at Hickory Rd
                StreetStrip(root, new Vector2(from, StreetY[i]), new Vector2(x1, StreetY[i]), RoadWidth);
                StreetStrip(root, new Vector2(StreetX[i], y0), new Vector2(StreetX[i], y1), RoadWidth);
            }
            // Willow Ct: a short street ending in a round cul-de-sac.
            StreetStrip(root, new Vector2(97.5f, StreetY[1]), BulbCenter, 8f);
            Disc(root, "Cul-de-sac Sidewalk", BulbCenter, BulbRadius + 0.5f, Sidewalk, 0.012f);
            Disc(root, "Cul-de-sac Verge", BulbCenter, BulbRadius - 1.5f, Verge, 0.016f);
            Disc(root, "Cul-de-sac", BulbCenter, BulbRadius - 3f, Asphalt, 0.022f);
            Disc(root, "Cul-de-sac Island", BulbCenter, 4f, Verge, 0.03f);
            if (Near(BulbCenter)) Tree(root, BulbCenter, 3.5f);

            // Street trees in the grass strip between the road and the sidewalk.
            for (int i = 0; i < 4; i++)
            {
                for (float d = x0 + 10f; d < x1; d += 24f)
                    for (int s = -1; s <= 1; s += 2) StreetTree(root, new Vector2(d, StreetY[i] + s * 6.4f), i == 2 && d < StreetX[1]);
                for (float d = y0 + 10f; d < y1; d += 24f)
                    for (int s = -1; s <= 1; s += 2) StreetTree(root, new Vector2(StreetX[i] + s * 6.4f, d), false);
            }
        }

        static void StreetTree(Transform root, Vector2 p, bool skip)
        {
            if (skip || !Near(p)) return;
            // Not in the middle of a crossing street.
            foreach (float x in StreetX) if (Mathf.Abs(p.x - x) < StreetRow * 0.5f && OnHorizontal(p)) return;
            Tree(root, p, 1.8f);
        }

        static bool OnHorizontal(Vector2 p)
        {
            foreach (float y in StreetY) if (Mathf.Abs(p.y - y) < StreetRow * 0.5f) return true;
            return false;
        }

        /// A road with its grass strips and sidewalks, flat on the ground.
        static void StreetStrip(Transform root, Vector2 a, Vector2 b, float road)
        {
            Flat(root, "Sidewalk", a, b, road + 6.4f, Sidewalk, 0.01f, 0f);
            Flat(root, "Grass Strip", a, b, road + 3.2f, Verge, 0.014f, 0f);
            Flat(root, "Street", a, b, road, Asphalt, 0.02f, 0f);
        }

        /// A flat strip from a to b, `width` wide, lying just above the ground (no collider).
        static GameObject Flat(Transform root, string name, Vector2 a, Vector2 b, float width, Color c, float y, float extend)
        {
            Vector2 d = b - a;
            float len = d.magnitude + extend * 2f;
            var go = Shapes.Box(name, W((a + b) * 0.5f, y), new Vector3(width, 0.02f, len), c, root, false);
            go.transform.rotation = Facing(d);
            return go;
        }

        static void FlatRect(Transform root, string name, Rect r, Color c, float y)
        {
            Shapes.Box(name, new Vector3(r.center.x, y, r.center.y), new Vector3(r.width, 0.02f, r.height), c, root, false);
        }

        static void Disc(Transform root, string name, Vector2 center, float radius, Color c, float y)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.position = W(center, y);
            go.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = Shapes.Mat(c);
        }

        // =====================================================================
        // One house lot
        // =====================================================================
        static TreehouseNav BuildLot(Transform root, LotRec lot, SiteDef site)
        {
            var t = new GameObject("Lot " + lot.Address).transform;
            t.SetParent(root, false);
            Color wall = HouseColors[lot.ColorIndex % HouseColors.Length];

            // House (built facing the street, then turned into place).
            var house = Pivot(t, "House " + lot.Address);
            BuildHouse(house, "House", Vector3.zero, new Vector3(lot.HouseWidth, lot.HouseHeight * HouseTall, lot.HouseDepth), wall);
            Place(house, lot.House, lot.In);

            if (lot.HasGarage)
            {
                var garage = Pivot(t, "Garage " + lot.Address);
                BuildGarage(garage, "Garage", Vector3.zero, site != null && site.Kind == SiteKind.Garage);
                Place(garage, lot.Garage, lot.In);
            }
            var drive = Flat(t, "Driveway", lot.Drive - lot.In * lot.DriveLength * 0.5f, lot.Drive + lot.In * lot.DriveLength * 0.5f, 4.8f, Sidewalk, 0.016f, 0f);
            drive.name = "Driveway";

            foreach (var tree in lot.Trees) Tree(t, tree, 2.6f);

            // Backyard stuff.
            TreehouseNav nav = null;
            bool wantTreehouse = lot.HasTreeSpot && (lot.Thing == Yard.Treehouse || (site != null && site.Kind == SiteKind.Treehouse));
            if (wantTreehouse)
            {
                var frame = Pivot(t, "Treehouse Frame");
                BuildTreehouse(frame, "Treehouse", Vector3.zero, 5f, 2.8f, out nav);
                // Sit it toward the back of the yard with the ramp facing the house.
                Place(frame, lot.TreeSpot, lot.In);
                nav.Frame = frame;
                if (site != null) Shapes.BoxOnGround("Crates", W(lot.TreeSpot - lot.In * 4f + lot.Along * 4f), new Vector3(1.4f, 1.4f, 1.4f), Plank, t);
            }
            else if (site == null || site.Kind != SiteKind.Yard)
            {
                // (A backyard plant site keeps its middle clear: no pool or shed in the way.)
                YardThing(t, lot.Thing, lot.ThingAt, lot.In);
            }
            if (site != null && site.Kind != SiteKind.Spot)
            {
                // A little cover on plant-site yards.
                Shapes.BoxOnGround("Picnic Table", W(lot.Backyard - lot.Along * 4.5f), new Vector3(2.2f, 0.9f, 1.6f), Plank, t);
                Shapes.BoxOnGround("Grill", W(lot.Backyard + lot.Along * 5f + lot.In * 2f), new Vector3(1f, 1.1f, 0.8f), new Color(0.15f, 0.15f, 0.15f), t);
            }
            return nav;
        }

        static Transform Pivot(Transform parent, string name)
        {
            var p = new GameObject(name).transform;
            p.SetParent(parent, false);
            return p;
        }

        /// Move a piece built around (0,0,0) facing +Z so its back points along `inward`.
        static void Place(Transform piece, Vector2 at, Vector2 inward)
        {
            piece.position = W(at);
            piece.rotation = Facing(inward);
        }

        static void YardThing(Transform t, Yard thing, Vector2 at, Vector2 inward)
        {
            if (thing == Yard.None) return;
            var p = Pivot(t, thing.ToString());
            switch (thing)
            {
                case Yard.Pool:
                    Shapes.BoxOnGround("Above-Ground Pool", Vector3.zero, new Vector3(6f, 1.2f, 3.6f), new Color(0.4f, 0.75f, 0.95f), p);
                    break;
                case Yard.Shed:
                    Shapes.BoxOnGround("Shed", Vector3.zero, new Vector3(3f, 2.6f, 3f), new Color(0.55f, 0.6f, 0.5f), p);
                    break;
                case Yard.Trampoline:
                    Shapes.BoxOnGround("Trampoline", Vector3.zero, new Vector3(4.2f, 0.8f, 4.2f), new Color(0.15f, 0.15f, 0.2f), p);
                    break;
                case Yard.Swings:
                    Shapes.BoxOnGround("Swing Set Bar", new Vector3(0f, 2.2f, 0f), new Vector3(4f, 0.15f, 0.15f), new Color(0.65f, 0.2f, 0.2f), p);
                    for (int s = -1; s <= 1; s += 2)
                        Shapes.BoxOnGround("Swing Set Leg", new Vector3(s * 2f, 0f, 0f), new Vector3(0.15f, 2.3f, 1.4f), new Color(0.65f, 0.2f, 0.2f), p);
                    break;
            }
            Place(p, at, inward);
        }

        // A house you can't go inside: a body, a pointy roof, a door and windows on the street side (-Z).
        static void BuildHouse(Transform root, string name, Vector3 bottomCenter, Vector3 size, Color wall)
        {
            Shapes.BoxOnGround(name + " Walls", bottomCenter, size, wall, root);
            float roofSide = size.z * 0.72f;
            var roof = Shapes.Box(name + " Roof", bottomCenter + Vector3.up * size.y, new Vector3(size.x + 0.6f, roofSide, roofSide), Roof, root);
            roof.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            float face = -size.z * 0.5f - 0.03f;
            Shapes.Box(name + " Door", bottomCenter + new Vector3(0f, 1.1f, face), new Vector3(1.4f, 2.2f, 0.05f), new Color(0.45f, 0.25f, 0.15f), root, false);
            for (int i = -1; i <= 1; i += 2)
            {
                Shapes.Box(name + " Window", bottomCenter + new Vector3(i * size.x * 0.28f, 1.7f, face), new Vector3(2f, 1.3f, 0.05f), DarkWindow, root, false);
                if (size.y > 7f)   // two-storey houses get an upstairs window
                    Shapes.Box(name + " Window Up", bottomCenter + new Vector3(i * size.x * 0.28f, size.y * 0.66f, face), new Vector3(2f, 1.2f, 0.05f), DarkWindow, root, false);
            }
        }

        // An open garage you CAN go inside. The big door faces -Z (the street).
        // With backDoor, there's also a doorway out the back into the yard.
        // `frontDoor` narrower than the garage leaves a piece of wall each side of the big door.
        static void BuildGarage(Transform root, string name, Vector3 bottomCenter, bool backDoor, float w = 6.6f, float d = 8.4f, float frontDoor = 0f)
        {
            const float h = GarageHeight, t = 0.25f;
            const float open = 2.6f;   // how tall the doorways are; the wall above them fills the rest
            if (frontDoor > 0f && frontDoor < w - 0.1f)
            {
                float stub = (w - frontDoor) * 0.5f;
                Shapes.BoxOnGround(name + " Front Wall L", bottomCenter + new Vector3(-w * 0.5f + stub * 0.5f, 0f, -d * 0.5f), new Vector3(stub, h, t), GarageGrey, root);
                Shapes.BoxOnGround(name + " Front Wall R", bottomCenter + new Vector3(w * 0.5f - stub * 0.5f, 0f, -d * 0.5f), new Vector3(stub, h, t), GarageGrey, root);
            }
            if (backDoor)
            {
                float piece = (w - 2.2f) * 0.5f;
                Shapes.BoxOnGround(name + " Back Wall L", bottomCenter + new Vector3(-w * 0.5f + piece * 0.5f, 0f, d * 0.5f), new Vector3(piece, h, t), GarageGrey, root);
                Shapes.BoxOnGround(name + " Back Wall R", bottomCenter + new Vector3(w * 0.5f - piece * 0.5f, 0f, d * 0.5f), new Vector3(piece, h, t), GarageGrey, root);
                Shapes.Box(name + " Back Door Top", bottomCenter + new Vector3(0f, (h + open) * 0.5f, d * 0.5f), new Vector3(2.2f, h - open, t), GarageGrey, root);
            }
            else
                Shapes.BoxOnGround(name + " Back Wall", bottomCenter + new Vector3(0f, 0f, d * 0.5f), new Vector3(w, h, t), GarageGrey, root);
            Shapes.BoxOnGround(name + " Left Wall", bottomCenter + new Vector3(-w * 0.5f, 0f, 0f), new Vector3(t, h, d), GarageGrey, root);
            Shapes.BoxOnGround(name + " Right Wall", bottomCenter + new Vector3(w * 0.5f, 0f, 0f), new Vector3(t, h, d), GarageGrey, root);
            Shapes.Box(name + " Roof", bottomCenter + new Vector3(0f, h + t * 0.5f, 0f), new Vector3(w + 0.6f, t, d + 0.6f), Roof, root);
            Shapes.Box(name + " Door", bottomCenter + new Vector3(0f, (h + open) * 0.5f, -d * 0.5f), new Vector3(w, h - open, 0.15f), PicketWhite, root);
            Shapes.BoxOnGround(name + " Workbench", bottomCenter + new Vector3(-w * 0.5f + 0.8f, 0f, 1.6f), new Vector3(1.2f, 1f, 2.4f), Plank, root);
            Shapes.BoxOnGround(name + " Boxes", bottomCenter + new Vector3(w * 0.5f - 1f, 0f, 0.6f), new Vector3(1.3f, 1.3f, 1.3f), new Color(0.75f, 0.6f, 0.4f), root);
            Shapes.BoxOnGround(name + " Bike", bottomCenter + new Vector3(w * 0.5f - 0.6f, 0f, 2.8f), new Vector3(0.3f, 1f, 1.8f), new Color(0.2f, 0.5f, 0.9f), root, false);
        }

        // A treehouse: a platform on posts, a ramp up the -Z side, railings, a roof, and a trunk.
        static void BuildTreehouse(Transform root, string name, Vector3 groundCenter, float size, float height, out TreehouseNav nav)
        {
            var th = Pivot(root, name);
            float half = size * 0.5f;
            float top = height + 0.15f;
            Shapes.Box(name + " Floor", groundCenter + Vector3.up * height, new Vector3(size, 0.3f, size), Plank, th);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Shapes.BoxOnGround(name + " Post", groundCenter + new Vector3(sx * (half - 0.2f), 0f, sz * (half - 0.2f)), new Vector3(0.3f, height + 2.6f, 0.3f), Bark, th);
            Shapes.BoxOnGround(name + " Trunk", groundCenter + new Vector3(0f, 0f, half + 0.9f), new Vector3(1.4f, height + 6f, 1.4f), Bark, th);
            // Leaves sit high above the roof so they never hide kids standing on the platform.
            Shapes.Ball(name + " Leaves", groundCenter + new Vector3(0f, height + 4.2f + (size + 3f) * 0.5f, half + 0.5f), size + 3f, Leaves, th);

            Vector3 railY = Vector3.up * (top + 0.5f);
            Shapes.Box(name + " Rail N", groundCenter + railY + new Vector3(0f, 0f, half - 0.1f), new Vector3(size, 1f, 0.15f), Plank, th);
            Shapes.Box(name + " Rail W", groundCenter + railY + new Vector3(-half + 0.1f, 0f, 0f), new Vector3(0.15f, 1f, size), Plank, th);
            Shapes.Box(name + " Rail E", groundCenter + railY + new Vector3(half - 0.1f, 0f, 0f), new Vector3(0.15f, 1f, size), Plank, th);
            float sideRail = (size - 2.4f) * 0.5f;
            Shapes.Box(name + " Rail S1", groundCenter + railY + new Vector3(-half + sideRail * 0.5f, 0f, -half + 0.1f), new Vector3(sideRail, 1f, 0.15f), Plank, th);
            Shapes.Box(name + " Rail S2", groundCenter + railY + new Vector3(half - sideRail * 0.5f, 0f, -half + 0.1f), new Vector3(sideRail, 1f, 0.15f), Plank, th);
            Shapes.Box(name + " Roof", groundCenter + Vector3.up * (height + 2.7f), new Vector3(size + 0.8f, 0.25f, size + 0.8f), Roof, th);

            float rampLength = height * 2.6f;
            Vector3 rampTop = groundCenter + new Vector3(0f, top - 0.13f, -half);
            Vector3 rampBottom = groundCenter + new Vector3(0f, -0.1f, -half - rampLength);
            Shapes.Ramp(name + " Ramp", rampBottom, rampTop, 2f, Plank, th);

            nav = new TreehouseNav
            {
                Bottom = new Vector3(groundCenter.x, 0f, rampBottom.z - 1f),
                Top = new Vector3(groundCenter.x, top, groundCenter.z - half + 0.8f),
            };
            float inset = half - 1.1f;
            nav.Floor.Add(groundCenter + new Vector3(-inset, top, -inset + 0.5f));
            nav.Floor.Add(groundCenter + new Vector3(inset, top, -inset + 0.5f));
            nav.Floor.Add(groundCenter + new Vector3(0f, top, inset - 0.5f));
        }

        class TreehouseNav
        {
            public Transform Frame;   // points below are relative to this; null = world
            public Vector3 Bottom, Top;
            public readonly List<Vector3> Floor = new List<Vector3>();
            public Vector3 W(Vector3 p) { return Frame != null ? Frame.TransformPoint(p) : p; }
            public List<Vector3> WorldFloor()
            {
                var list = new List<Vector3>();
                foreach (var f in Floor) list.Add(W(f));
                return list;
            }
        }

        static void ConnectTreehouse(NavGraph graph, TreehouseNav t)
        {
            int bottomNode = graph.AddAndConnect(t.W(t.Bottom), 5f);
            int topNode = graph.AddNode(t.W(t.Top));
            graph.Link(bottomNode, topNode);
            foreach (var s in t.Floor) graph.Link(graph.AddNode(t.W(s)), topNode);
        }

        // =====================================================================
        // Fences and gates
        // =====================================================================
        static void BuildFences(Transform root, MapDef def)
        {
            var parent = Pivot(root, "Fences");
            var f = NeighborhoodData.Fences;
            foreach (int id in def.FenceIds)
            {
                int i = id * 5;
                var a = new Vector2(f[i], f[i + 1]);
                var b = new Vector2(f[i + 2], f[i + 3]);
                switch ((int)f[i + 4])
                {
                    case 1: Wall(parent, "Picket Fence", a, b, 1.1f, 0.15f, PicketWhite); break;
                    case 2: Wall(parent, "Hedge", a, b, HedgeHeight, 0.9f, HedgeGreen); break;
                    default: Wall(parent, "Fence", a, b, FenceHeight, 0.2f, FenceWood); break;
                }
            }
            // Gates on the edge of the map are shut (padlocked).
            var g = NeighborhoodData.Gates;
            for (int i = 0; i + 3 < g.Length; i += 4)
            {
                var c = new Vector2(g[i], g[i + 1]);
                var walk = new Vector2(g[i + 2], g[i + 3]);
                if (!Near(c) || !OnEdge(def, c, 0.8f)) continue;
                var along = new Vector2(-walk.y, walk.x);
                Wall(parent, "Locked Gate", c - along * 1.3f, c + along * 1.3f, FenceHeight, 0.2f, FenceWood * 0.85f);
            }
        }

        static bool OnEdge(MapDef def, Vector2 p, float within)
        {
            var a = def.Area;
            for (int i = 0, j = a.Length - 1; i < a.Length; j = i++)
                if (DistanceToSegment(p, a[j], a[i]) < within) return true;
            return false;
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        /// A straight wall-like thing (fence, hedge, barrier) from a to b.
        static GameObject Wall(Transform parent, string name, Vector2 a, Vector2 b, float height, float thick, Color c, bool solid = true)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.05f) return null;
            var go = Shapes.BoxOnGround(name, W((a + b) * 0.5f), new Vector3(thick, height, len), c, parent, solid);
            go.transform.rotation = Facing(d);
            return go;
        }

        // =====================================================================
        // The map's edge: invisible walls, plus something you can SEE there.
        // Every edge is closed with something taller than a jumping kid can see
        // over, so the map has a clear end and you never look out across open
        // ground you can't reach.
        // =====================================================================
        static void BuildEdges(Transform root, MapDef def)
        {
            var parent = Pivot(root, "Map Edge");
            foreach (var e in def.Edges)
            {
                Vector2 d = e.B - e.A;
                float len = d.magnitude;
                if (len < 0.05f) continue;
                Vector2 dir = d / len;
                Vector2 outward = new Vector2(dir.y, -dir.x);   // the area is counterclockwise, so outside is on the right

                // Invisible wall: tall, so nobody jumps or climbs out.
                var wall = Wall(parent, "Invisible Wall", e.A + outward * 0.35f, e.B + outward * 0.35f, 12f, 0.4f, Color.white);
                if (wall != null) Object.DestroyImmediate(wall.GetComponent<Renderer>());

                switch (e.What)
                {
                    case Edge.Kind.Wall:
                        break;   // a blueprint map's outer ring is already solid: nothing more to show
                    case Edge.Kind.Street:
                        if (len > 17f)
                        {
                            // Along a street: a tall hedge at the curb, with cars parked behind it.
                            Wall(parent, "Hedge", e.A + outward * 0.5f, e.B + outward * 0.5f, HedgeHeight, 0.9f, HedgeGreen);
                            ParkedCars(parent, e.A + outward * 2.2f, e.B + outward * 2.2f);
                        }
                        else
                        {
                            // Across a street: sawhorses, then a pocket closed off by green road-work
                            // screens, deep enough to hold the crashed cars or parked truck out there.
                            Barricade(parent, e.A + outward * 0.6f, e.B + outward * 0.6f);
                            Vector2 backA = e.A + outward * 10f, backB = e.B + outward * 10f;
                            Wall(parent, "Road Work Screen", backA, backB, ScreenHeight, 0.1f, ScreenGreen, false);
                            Wall(parent, "Road Work Screen", e.A + outward * 0.5f, backA, ScreenHeight, 0.1f, ScreenGreen, false);
                            Wall(parent, "Road Work Screen", e.B + outward * 0.5f, backB, ScreenHeight, 0.1f, ScreenGreen, false);
                        }
                        break;
                    case Edge.Kind.Curb:
                        Wall(parent, "Hedge", e.A + outward * 0.5f, e.B + outward * 0.5f, HedgeHeight, 0.9f, HedgeGreen);
                        break;
                    case Edge.Kind.Lot:
                        Wall(parent, "Fence", e.A + outward * 0.15f, e.B + outward * 0.15f, FenceHeight, 0.2f, FenceWood);
                        break;
                    case Edge.Kind.Park:
                    case Edge.Kind.Verge:
                    case Edge.Kind.Woods:
                        Wall(parent, "Tall Hedge", e.A + outward * 0.6f, e.B + outward * 0.6f, TreeLineHeight, 1.2f, HedgeGreen);
                        break;
                    case Edge.Kind.School:
                        // Chain-link with green privacy slats, so you can't see through it.
                        Wall(parent, "Slatted Chain-Link Fence", e.A + outward * 0.2f, e.B + outward * 0.2f, ScreenHeight, 0.08f, ScreenGreen);
                        break;
                    default:
                        // LotFenced / CurbFenced: a yard fence is already there, but it may be a low
                        // picket fence, so a tall hedge grows just behind it.
                        Wall(parent, "Tall Hedge", e.A + outward * 1.1f, e.B + outward * 1.1f, HedgeHeight, 0.9f, HedgeGreen, false);
                        break;
                }
            }
        }

        /// Orange-and-white sawhorse barricades across a street or sidewalk.
        static void Barricade(Transform parent, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 2.2f));
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = a + d * (i / (float)n), p1 = a + d * ((i + 1) / (float)n);
                Vector2 mid = (p0 + p1) * 0.5f;
                Wall(parent, "Barricade", mid - (p1 - p0) * 0.45f, mid + (p1 - p0) * 0.45f, 1.1f, 0.25f, i % 2 == 0 ? Orange : PicketWhite);
            }
        }

        static void ParkedCars(Transform parent, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            int n = Mathf.Max(1, Mathf.FloorToInt(len / 5.2f));
            for (int i = 0; i < n; i++)
            {
                Vector2 at = a + d * ((i + 0.5f) / n);
                Car(parent, at, d, CarColors[(i * 7 + (int)a.x) % CarColors.Length]);
            }
        }

        // =====================================================================
        // A map's extra cover (see Prop in NeighborhoodTypes.cs). The sizes
        // here must match Tools/props.py, which the walkability check uses.
        // =====================================================================
        static readonly Color PortableTan = new Color(0.86f, 0.75f, 0.55f);
        static readonly Color ShedGreen = new Color(0.55f, 0.6f, 0.5f);
        static readonly Color StandOrange = new Color(0.95f, 0.6f, 0.25f);
        static readonly Color TentPink = new Color(1f, 0.55f, 0.7f);

        static Vector2 Dir(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }

        /// Where a Fence/Hedge prop's gate is, and the direction you walk through it.
        static bool PropGate(Prop p, out Vector2 center, out Vector2 walk)
        {
            var v = p.V;
            Vector2 a = new Vector2(v[0], v[1]), b = new Vector2(v[2], v[3]);
            Vector2 d = (b - a).normalized;
            center = a + d * (v[4] + v[5] * 0.5f);
            walk = new Vector2(-d.y, d.x);
            return v[4] >= 0f;
        }

        static void BuildProps(Transform root, MapDef def, List<TreehouseNav> treehouses)
        {
            if (def.Props == null) return;
            var parent = Pivot(root, "Map Cover");
            int n = 0;
            foreach (var p in def.Props)
            {
                var v = p.V;
                n++;
                switch (p.What)
                {
                    // ---- the big pieces of a blueprint map ----
                    case Prop.Kind.House:
                    {
                        // A house you can't go inside. BuildHouse puts the front door on -Z,
                        // so the house is turned until that side faces the angle in the data.
                        var house = Pivot(parent, "House");
                        BuildHouse(house, "House", Vector3.zero, new Vector3(v[3], v[5], v[4]), HouseColors[n % HouseColors.Length]);
                        Place(house, new Vector2(v[0], v[1]), -Dir(v[2]));
                        break;
                    }
                    case Prop.Kind.Lockup:
                    {
                        // A garage with its door shut: a solid grey box.
                        var t = BoxProp(parent, "Shut Garage", v, GarageHeight, GarageGrey, 0.3f, Roof, true);
                        Shapes.Box("Shut Garage Door", t.TransformPoint(new Vector3(0f, 1.3f, -v[4] * 0.5f - 0.03f)),
                            new Vector3(Mathf.Min(v[3] - 0.8f, 4.8f), 2.6f, 0.05f), PicketWhite, t, false);
                        break;
                    }
                    case Prop.Kind.Bush:
                        BoxProp(parent, "Hedge Block", v, HedgeHeight, HedgeGreen);
                        break;
                    case Prop.Kind.Thicket:
                    {
                        // A clump of trees with brush so thick you can't push through or see through.
                        var t = BoxProp(parent, "Thicket", v, TreeLineHeight, HedgeGreen * 0.85f);
                        int across = Mathf.Max(1, Mathf.RoundToInt(v[3] / 5f)), deep = Mathf.Max(1, Mathf.RoundToInt(v[4] / 5f));
                        for (int i = 0; i < across; i++)
                            for (int j = 0; j < deep; j++)
                            {
                                Vector3 at = t.TransformPoint(new Vector3(-v[3] * 0.5f + v[3] * (i + 0.5f) / across, 0f, -v[4] * 0.5f + v[4] * (j + 0.5f) / deep));
                                Tree(parent, XZ(at), 3.2f);
                            }
                        break;
                    }
                    case Prop.Kind.Crate:
                        // Low enough to shoot over, tall enough to duck behind.
                        BoxProp(parent, "Crates", v, 1.3f, Plank);
                        break;
                    case Prop.Kind.Pool:
                        BoxProp(parent, "Above-Ground Pool", v, 1.2f, new Color(0.4f, 0.75f, 0.95f));
                        break;
                    case Prop.Kind.Yard:
                    {
                        // Somebody's fenced-off back yard: you can see the tree in it, but
                        // nobody gets in. The box is invisible and only knee high: enough to
                        // keep the bots' stepping stones out without stopping water in mid-air.
                        var t = BoxProp(parent, "Closed Yard", v, 0.6f, Grass);
                        Object.DestroyImmediate(t.GetComponentInChildren<Renderer>());
                        if (v[3] >= 3.9f && v[4] >= 3.9f) Tree(parent, new Vector2(v[0], v[1]), 2.4f);
                        break;
                    }
                    case Prop.Kind.Garage:
                    {
                        // An open garage you CAN go inside: 6 m wide, 8 m deep, a 4 m big door.
                        var garage = Pivot(parent, "Garage");
                        BuildGarage(garage, "Garage", Vector3.zero, v[3] > 0.5f, BlueprintGarageWidth, BlueprintGarageDepth, BlueprintGarageDoor);
                        Place(garage, new Vector2(v[0], v[1]), -Dir(v[2]));
                        break;
                    }
                    case Prop.Kind.Treehouse:
                    {
                        var frame = Pivot(parent, "Treehouse Frame");
                        BuildTreehouse(frame, "Treehouse", Vector3.zero, 5f, 2.8f, out var nav);
                        Place(frame, new Vector2(v[0], v[1]), -Dir(v[2]));   // the ramp comes down the -Z side
                        nav.Frame = frame;
                        treehouses.Add(nav);
                        break;
                    }
                    case Prop.Kind.Paint:
                    {
                        Color c = v[4] < 0.5f ? Sidewalk : v[4] < 1.5f ? Asphalt : v[4] < 2.5f ? Dirt : Sand;
                        FlatRect(parent, "Ground Paint", Rect.MinMaxRect(v[0] - v[2] * 0.5f, v[1] - v[3] * 0.5f, v[0] + v[2] * 0.5f, v[1] + v[3] * 0.5f), c, 0.012f + 0.004f * v[4]);
                        break;
                    }
                    case Prop.Kind.Car:
                        Car(parent, new Vector2(v[0], v[1]), Dir(v[2]), CarColors[n % CarColors.Length]);
                        break;
                    case Prop.Kind.Van:
                    {
                        var van = Pivot(parent, "Van");
                        var paint = new Color(0.9f, 0.88f, 0.8f);
                        Shapes.BoxOnGround("Van Body", Vector3.zero, new Vector3(2.2f, 2.4f, 5.2f), paint, van);
                        Shapes.Box("Van Stripe", new Vector3(0f, 1.3f, 0f), new Vector3(2.25f, 0.3f, 5.25f), new Color(0.2f, 0.45f, 0.75f), van, false);
                        Place(van, new Vector2(v[0], v[1]), Dir(v[2]));
                        break;
                    }
                    case Prop.Kind.Truck:
                        Truck(parent, new Vector2(v[0], v[1]) + Dir(v[2]) * 0.15f, Dir(v[2]), CarColors[n % CarColors.Length], new Color(0.92f, 0.9f, 0.85f), 3.4f);
                        break;
                    case Prop.Kind.Portable:
                    {
                        // A portable classroom: a big box with a little porch and steps.
                        BoxProp(parent, "Portable Classroom", v, 3.2f, PortableTan, 0.4f, Roof, true);
                        break;
                    }
                    case Prop.Kind.Shed:
                        BoxProp(parent, "Shed", v, 3.2f, ShedGreen);
                        break;
                    case Prop.Kind.Stand:
                    {
                        // A snack stand: solid counter with an awning on top.
                        BoxProp(parent, "Snack Stand", v, 2.4f, StandOrange, 1.2f, PicketWhite, false);
                        break;
                    }
                    case Prop.Kind.Tent:
                    {
                        // A party tent: a roof on four poles. Arcing water can't reach under it.
                        var t = Pivot(parent, "Party Tent");
                        float hw = v[3] * 0.5f - 0.2f, hd = v[4] * 0.5f - 0.2f;
                        for (int sx = -1; sx <= 1; sx += 2)
                            for (int sz = -1; sz <= 1; sz += 2)
                                Shapes.BoxOnGround("Tent Pole", new Vector3(sx * hw, 0f, sz * hd), new Vector3(0.2f, 2.7f, 0.2f), PicketWhite, t);
                        Shapes.Box("Tent Roof", new Vector3(0f, 2.8f, 0f), new Vector3(v[3], 0.2f, v[4]), TentPink, t);
                        t.position = W(new Vector2(v[0], v[1]));
                        t.rotation = Facing(new Vector2(-Mathf.Sin(v[2] * Mathf.Deg2Rad), Mathf.Cos(v[2] * Mathf.Deg2Rad)));
                        break;
                    }
                    case Prop.Kind.Fence:
                    case Prop.Kind.Hedge:
                    {
                        bool hedge = p.What == Prop.Kind.Hedge;
                        Vector2 a = new Vector2(v[0], v[1]), b = new Vector2(v[2], v[3]);
                        Vector2 d = (b - a).normalized;
                        float len = Vector2.Distance(a, b);
                        var pieces = new List<Vector2[]>();
                        if (v[4] < 0f) pieces.Add(new[] { a, b });
                        else
                        {
                            if (v[4] > 0.05f) pieces.Add(new[] { a, a + d * v[4] });
                            float e = v[4] + v[5];
                            if (e < len - 0.05f) pieces.Add(new[] { a + d * e, b });
                        }
                        foreach (var pc in pieces)
                            Wall(parent, hedge ? "Tall Hedge" : "Tall Fence", pc[0], pc[1], hedge ? HedgeHeight : FenceHeight, hedge ? 0.9f : 0.2f, hedge ? HedgeGreen : FenceWood);
                        break;
                    }
                    case Prop.Kind.Tree:
                        Tree(parent, new Vector2(v[0], v[1]), v[2]);
                        break;
                    case Prop.Kind.Townhome:
                    {
                        // A row of joined two-storey homes: one long solid block that makes
                        // narrow alleys. Each home in the row gets its own color, door and windows.
                        // (BoxProp has already placed the row, so the trim uses TransformPoint.)
                        const float tall = 9f;
                        var t = BoxProp(parent, "Townhome Row", v, tall, HouseColors[n % HouseColors.Length], 0.3f, Roof, true);
                        int homes = Mathf.Max(1, Mathf.RoundToInt(v[3] / 6f));
                        float unit = v[3] / homes;
                        for (int i = 0; i < homes; i++)
                        {
                            float x = -v[3] * 0.5f + unit * (i + 0.5f);
                            var col = HouseColors[(n + i) % HouseColors.Length];
                            for (int side = -1; side <= 1; side += 2)
                            {
                                float z = side * (v[4] * 0.5f + 0.03f);
                                Shapes.Box("Townhome Front", t.TransformPoint(new Vector3(x, tall * 0.5f, z)), new Vector3(unit - 0.2f, tall, 0.05f), col, t, false);
                                Shapes.Box("Townhome Door", t.TransformPoint(new Vector3(x - unit * 0.25f, 1.1f, z * 1.01f)), new Vector3(1f, 2.2f, 0.06f), new Color(0.35f, 0.2f, 0.12f), t, false);
                                Shapes.Box("Townhome Window", t.TransformPoint(new Vector3(x + unit * 0.15f, 1.7f, z * 1.01f)), new Vector3(1.4f, 1.1f, 0.06f), new Color(0.6f, 0.75f, 0.85f), t, false);
                                Shapes.Box("Townhome Window", t.TransformPoint(new Vector3(x, 6.2f, z * 1.01f)), new Vector3(2.4f, 1.1f, 0.06f), new Color(0.6f, 0.75f, 0.85f), t, false);
                            }
                        }
                        break;
                    }
                    case Prop.Kind.RV:
                    {
                        // A parked motorhome: 10 m long and taller than any kid. Blocks the view.
                        var rv = Pivot(parent, "RV");
                        Shapes.BoxOnGround("RV Body", new Vector3(0f, 0.5f, 0f), new Vector3(2.6f, 3f, 10f), new Color(0.95f, 0.93f, 0.86f), rv);
                        Shapes.Box("RV Stripe", new Vector3(0f, 1.8f, 0f), new Vector3(2.65f, 0.35f, 10.05f), new Color(0.75f, 0.45f, 0.2f), rv, false);
                        Shapes.Box("RV Windshield", new Vector3(0f, 2.5f, 5.03f), new Vector3(2.2f, 0.9f, 0.06f), new Color(0.3f, 0.4f, 0.5f), rv, false);
                        Shapes.BoxOnGround("RV Wheels", Vector3.zero, new Vector3(2.65f, 0.6f, 9f), new Color(0.1f, 0.1f, 0.1f), rv, false);
                        Place(rv, new Vector2(v[0], v[1]), Dir(v[2]));
                        break;
                    }
                }
            }
        }

        /// A solid box prop: x, y, angle, width, depth. Width runs east-west at angle 0.
        /// roofOverhang >= 0 adds a flat roof that sticks out that far on every side.
        static Transform BoxProp(Transform parent, string name, float[] v, float height, Color c,
            float roofOverhang = -1f, Color roofColor = default(Color), bool roofSolid = true)
        {
            var t = Pivot(parent, name);
            Shapes.BoxOnGround(name + " Body", Vector3.zero, new Vector3(v[3], height, v[4]), c, t);
            if (roofOverhang >= 0f)
                Shapes.Box(name + " Roof", new Vector3(0f, height + 0.1f, 0f),
                    new Vector3(v[3] + roofOverhang * 2f, 0.2f, v[4] + roofOverhang * 2f), roofColor, t, roofSolid);
            t.position = W(new Vector2(v[0], v[1]));
            t.rotation = Facing(new Vector2(-Mathf.Sin(v[2] * Mathf.Deg2Rad), Mathf.Cos(v[2] * Mathf.Deg2Rad)));
            return t;
        }

        // =====================================================================
        // Roadblocks
        // =====================================================================
        static void BuildRoadblock(Transform root, Roadblock rb)
        {
            var t = Pivot(root, "Roadblock " + rb.What);
            Vector2 across = new Vector2(-rb.Out.y, rb.Out.x);
            Vector2 at = rb.At + rb.Out * 3.5f;
            switch (rb.What)
            {
                case Roadblock.Kind.CarCrash:
                    Car(t, at - across * 2f, Rotate(across, 25f), CarColors[0]);
                    Car(t, at + across * 2.2f + rb.Out * 1.5f, Rotate(across, -40f), CarColors[1]);
                    Cones(t, rb.At + rb.Out * 1.6f, across, 5);
                    break;
                case Roadblock.Kind.MovingTruck:
                    Truck(t, at, across, new Color(0.95f, 0.55f, 0.15f), new Color(0.92f, 0.9f, 0.85f), 3.6f);
                    break;
                case Roadblock.Kind.RoadWork:
                    Shapes.BoxOnGround("Dirt Pile", W(at), new Vector3(5f, 1.6f, 3f), Dirt, t).transform.rotation = Facing(rb.Out);
                    Truck(t, at + rb.Out * 4f, across, new Color(0.95f, 0.78f, 0.1f), new Color(0.95f, 0.78f, 0.1f), 2.6f);
                    Cones(t, rb.At + rb.Out * 1.6f, across, 6);
                    break;
                case Roadblock.Kind.GarbageTruck:
                    Truck(t, at, across, new Color(0.2f, 0.45f, 0.25f), new Color(0.25f, 0.5f, 0.3f), 3.4f);
                    break;
                case Roadblock.Kind.PoliceTape:
                    Car(t, at, Rotate(across, 15f), new Color(0.1f, 0.1f, 0.12f));
                    Shapes.Box("Police Tape", W(rb.At + rb.Out * 1.2f, 0.95f), new Vector3(15f, 0.08f, 0.03f), new Color(1f, 0.85f, 0.1f), t, false)
                        .transform.rotation = Facing(rb.Out);
                    Cones(t, rb.At + rb.Out * 1.6f, across, 4);
                    break;
                case Roadblock.Kind.IceCreamTruck:
                    Truck(t, at, across, new Color(0.97f, 0.97f, 0.97f), new Color(1f, 0.7f, 0.8f), 3f);
                    break;
                case Roadblock.Kind.FireTruck:
                    Truck(t, at, across, new Color(0.8f, 0.1f, 0.1f), new Color(0.85f, 0.12f, 0.12f), 3.3f);
                    break;
                case Roadblock.Kind.DeliveryVan:
                    Truck(t, at, across, new Color(0.45f, 0.3f, 0.18f), new Color(0.5f, 0.33f, 0.2f), 2.8f);
                    break;
            }
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        static void Cones(Transform t, Vector2 at, Vector2 across, int n)
        {
            for (int i = 0; i < n; i++)
            {
                Vector2 p = at + across * ((i - (n - 1) * 0.5f) * 2.2f);
                Shapes.BoxOnGround("Traffic Cone", W(p), new Vector3(0.4f, 0.75f, 0.4f), Orange, t, false);
            }
        }

        static void Car(Transform parent, Vector2 at, Vector2 dir, Color paint)
        {
            var car = Pivot(parent, "Car");
            Shapes.BoxOnGround("Car Body", new Vector3(0f, 0.25f, 0f), new Vector3(1.9f, 0.85f, 4.6f), paint, car);
            Shapes.BoxOnGround("Car Cabin", new Vector3(0f, 1.1f, -0.3f), new Vector3(1.75f, 0.65f, 2.4f), paint * 0.85f, car);
            Shapes.BoxOnGround("Car Wheels", Vector3.zero, new Vector3(1.95f, 0.5f, 3.6f), new Color(0.1f, 0.1f, 0.1f), car, false);
            Place(car, at, dir);
        }

        static void Truck(Transform parent, Vector2 at, Vector2 dir, Color cab, Color body, float height)
        {
            var truck = Pivot(parent, "Truck");
            Shapes.BoxOnGround("Truck Body", new Vector3(0f, 0.6f, -1.3f), new Vector3(2.5f, height, 7f), body, truck);
            Shapes.BoxOnGround("Truck Cab", new Vector3(0f, 0.5f, 3.4f), new Vector3(2.4f, 2.2f, 2.2f), cab, truck);
            Shapes.BoxOnGround("Truck Wheels", Vector3.zero, new Vector3(2.55f, 0.7f, 8f), new Color(0.1f, 0.1f, 0.1f), truck, false);
            Place(truck, at, dir);
        }

        // =====================================================================
        // Maple Park, Maple Elementary, Turtle Creek, the woods
        // =====================================================================
        static void BuildPark(Transform root)
        {
            var t = Pivot(root, "Maple Park");
            FlatRect(t, "Park Lawn", Park, ParkGrass, 0.008f);
            // Baseball diamond (a square turned 45 degrees) and its backstop.
            var dia = Shapes.Box("Baseball Diamond", new Vector3(250f, 0.012f, 122f), new Vector3(31f, 0.02f, 31f), Dirt, t, false);
            dia.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            Wall(t, "Backstop", new Vector2(245f, 97.5f), new Vector2(255f, 97.5f), 3.5f, 0.1f, ChainLink);
            Wall(t, "Dugout Bench", new Vector2(236f, 102f), new Vector2(240f, 106f), 0.9f, 0.6f, Plank);
            Wall(t, "Dugout Bench", new Vector2(264f, 102f), new Vector2(260f, 106f), 0.9f, 0.6f, Plank);
            // Walking path.
            var path = new[] { new Vector2(202.5f, 130f), new Vector2(230f, 150f), new Vector2(300f, 155f), new Vector2(382.5f, 140f) };
            for (int i = 0; i + 1 < path.Length; i++) Flat(t, "Path", path[i], path[i + 1], 2.4f, Sidewalk, 0.014f, 1.2f);

            // Basketball courts (the middle of the Park Run map): open, with hoops and a low wall to duck behind.
            FlatRect(t, "Courts", Rect.MinMaxRect(300f, 100f, 328f, 116f), new Color(0.55f, 0.55f, 0.57f), 0.014f);
            foreach (float x in new[] { 301.5f, 326.5f })
            {
                Shapes.BoxOnGround("Hoop Pole", new Vector3(x, 0f, 108f), new Vector3(0.25f, 3.3f, 0.25f), new Color(0.3f, 0.3f, 0.3f), t);
                Shapes.Box("Backboard", new Vector3(x + (x < 314f ? 0.4f : -0.4f), 3.4f, 108f), new Vector3(0.1f, 1.1f, 1.8f), PicketWhite, t);
            }
            Wall(t, "Court Wall", new Vector2(306f, 116.8f), new Vector2(322f, 116.8f), 1.2f, 0.4f, new Color(0.7f, 0.68f, 0.64f));
            Shapes.BoxOnGround("Bleacher", new Vector3(314f, 0f, 98.6f), new Vector3(8f, 0.9f, 1.4f), ChainLink, t);

            // Playground (Site B on Park Run): sand, a slide tower, swings, a climbing dome.
            FlatRect(t, "Playground Sand", Rect.MinMaxRect(335f, 120f, 370f, 150f), Sand, 0.014f);
            var tower = Pivot(t, "Slide Tower");
            Shapes.Box("Tower Deck", new Vector3(0f, 1.8f, 0f), new Vector3(3f, 0.25f, 3f), new Color(0.9f, 0.3f, 0.25f), tower);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Shapes.BoxOnGround("Tower Post", new Vector3(sx * 1.35f, 0f, sz * 1.35f), new Vector3(0.25f, 3.2f, 0.25f), new Color(0.2f, 0.4f, 0.8f), tower);
            Shapes.Box("Tower Wall", new Vector3(0f, 2.4f, 1.45f), new Vector3(3f, 1f, 0.12f), new Color(0.95f, 0.8f, 0.2f), tower);
            Shapes.Ramp("Slide", new Vector3(0f, 0f, -5.5f), new Vector3(0f, 1.8f, -1.5f), 1.2f, new Color(0.95f, 0.8f, 0.2f), tower);
            Shapes.Ramp("Climbing Ramp", new Vector3(5f, 0f, 0f), new Vector3(1.5f, 1.8f, 0f), 1.4f, new Color(0.2f, 0.6f, 0.3f), tower);
            tower.position = new Vector3(343f, 0f, 129f);
            var swings = Pivot(t, "Swings");
            Shapes.Box("Swing Bar", new Vector3(0f, 2.6f, 0f), new Vector3(6f, 0.15f, 0.15f), new Color(0.2f, 0.4f, 0.8f), swings);
            for (int s = -1; s <= 1; s += 2)
                Shapes.BoxOnGround("Swing Leg", new Vector3(s * 3f, 0f, 0f), new Vector3(0.15f, 2.7f, 1.8f), new Color(0.2f, 0.4f, 0.8f), swings);
            swings.position = new Vector3(355f, 0f, 142f);
            Shapes.BoxOnGround("Climbing Dome", new Vector3(363f, 0f, 128f), new Vector3(3.5f, 1.6f, 3.5f), new Color(0.9f, 0.3f, 0.25f), t);
            Shapes.BoxOnGround("Bench", new Vector3(352f, 0f, 121f), new Vector3(2.5f, 0.8f, 0.7f), Plank, t);

            // Pavilion (Site A on Park Run): a roof on posts over picnic tables.
            var pav = Pivot(t, "Pavilion");
            Shapes.Box("Pavilion Floor", new Vector3(0f, 0.08f, 0f), new Vector3(18f, 0.16f, 15f), new Color(0.75f, 0.73f, 0.7f), pav);
            for (int sx = -1; sx <= 1; sx++)
                for (int sz = -1; sz <= 1; sz += 2)
                    Shapes.BoxOnGround("Pavilion Post", new Vector3(sx * 8.5f, 0f, sz * 7f), new Vector3(0.4f, 3.4f, 0.4f), Bark, pav);
            Shapes.Box("Pavilion Roof", new Vector3(0f, 3.6f, 0f), new Vector3(19.5f, 0.35f, 16.5f), Roof, pav);
            for (int i = -1; i <= 1; i++)
                Shapes.BoxOnGround("Picnic Table", new Vector3(i * 5.2f, 0f, i == 0 ? 2f : -1.5f), new Vector3(2.4f, 0.9f, 1.7f), Plank, pav);
            Shapes.BoxOnGround("Trash Can", new Vector3(-7.5f, 0f, -6f), new Vector3(0.9f, 1.1f, 0.9f), new Color(0.3f, 0.4f, 0.35f), pav);
            pav.position = new Vector3(309f, 0f, 142.5f);

            var trees = NeighborhoodData.ParkTrees;
            for (int i = 0; i + 2 < trees.Length; i += 3) Tree(t, new Vector2(trees[i], trees[i + 1]), trees[i + 2]);
        }

        static void BuildSchool(Transform root)
        {
            var t = Pivot(root, "Maple Elementary");
            FlatRect(t, "School Lawn", School, ParkGrass, 0.008f);
            var red = new Color(0.78f, 0.6f, 0.45f);
            Shapes.BoxOnGround("School Building", new Vector3(435f, 0f, 231.5f), new Vector3(70f, 8f, 33f), red, t);
            Shapes.BoxOnGround("School Wing", new Vector3(412.5f, 0f, 202.5f), new Vector3(25f, 7f, 25f), red, t);
            for (int i = 0; i < 2; i++)  // back doors facing the parking lot
                Shapes.Box("School Doors", new Vector3(440f + i * 14f, 1.2f, 214.97f), new Vector3(2.4f, 2.4f, 0.05f), new Color(0.3f, 0.35f, 0.55f), t, false);
            Shapes.BoxOnGround("Dumpster", new Vector3(462f, 0f, 212.5f), new Vector3(3.6f, 1.7f, 2f), new Color(0.2f, 0.4f, 0.3f), t);
            Shapes.BoxOnGround("Bike Rack", new Vector3(433f, 0f, 213f), new Vector3(4f, 0.9f, 0.6f), ChainLink, t);

            FlatRect(t, "Parking Lot", Rect.MinMaxRect(430f, 186f, 480f, 210f), new Color(0.4f, 0.4f, 0.42f), 0.014f);
            Car(t, new Vector2(436f, 192f), Vector2.up, CarColors[2]);
            Car(t, new Vector2(450f, 192f), Vector2.up, CarColors[4]);
            Car(t, new Vector2(472f, 203f), Vector2.up, CarColors[1]);
            Shapes.BoxOnGround("School Bus", new Vector3(458f, 0f, 199f), new Vector3(10f, 3f, 2.5f), new Color(0.98f, 0.75f, 0.1f), t);

            FlatRect(t, "Soccer Field", Rect.MinMaxRect(495f, 190f, 575f, 250f), new Color(0.48f, 0.76f, 0.36f), 0.014f);
            foreach (float x in new[] { 496.5f, 573.5f })
            {
                Wall(t, "Goal Post", new Vector2(x, 216.3f), new Vector2(x, 216.5f), 2.4f, 0.15f, PicketWhite);
                Wall(t, "Goal Post", new Vector2(x, 223.5f), new Vector2(x, 223.7f), 2.4f, 0.15f, PicketWhite);
                Shapes.Box("Crossbar", new Vector3(x, 2.4f, 220f), new Vector3(0.15f, 0.15f, 7.4f), PicketWhite, t, false);
            }
            // Bleachers by the field (Site B on the Schoolyard map): three steps you can climb and hide behind.
            var bl = Pivot(t, "Bleachers");
            for (int i = 0; i < 3; i++)
                Shapes.BoxOnGround("Bleacher Step", new Vector3(0f, 0f, i * 1.1f), new Vector3(11f, 0.55f * (i + 1), 1.1f), ChainLink, bl);
            bl.position = new Vector3(540f, 0f, 225f);
            Shapes.BoxOnGround("Equipment Shed", new Vector3(527f, 0f, 215f), new Vector3(3f, 2.4f, 2.4f), new Color(0.55f, 0.6f, 0.5f), t);
            FlatRect(t, "Playground Sand", Rect.MinMaxRect(440f, 240f, 480f, 252f), Sand, 0.014f);

            // Chain-link fence around the school grounds, with the front gates open onto Cedar Ln.
            float x0 = 398.5f, x1 = 576.5f, y0 = 182.5f, y1 = 252.5f;
            Wall(t, "Chain-Link Fence", new Vector2(x0, y0), new Vector2(462f, y0), 2.4f, 0.08f, ChainLink);
            Wall(t, "Chain-Link Fence", new Vector2(513f, y0), new Vector2(x1, y0), 2.4f, 0.08f, ChainLink);
            Wall(t, "Chain-Link Fence", new Vector2(x0, y1), new Vector2(x1, y1), 2.4f, 0.08f, ChainLink);
            Wall(t, "Chain-Link Fence", new Vector2(x0, y0), new Vector2(x0, y1), 2.4f, 0.08f, ChainLink);
            Wall(t, "Chain-Link Fence", new Vector2(x1, y0), new Vector2(x1, y1), 2.4f, 0.08f, ChainLink);
        }

        static float CreekX(float y) { return 625f + 10f * Mathf.Sin(y / 27f) + 4f * Mathf.Sin(y / 9f); }

        static void BuildCreek(Transform root)
        {
            var t = Pivot(root, "Turtle Creek");
            for (float y = -25f; y < 285f; y += 4f)
            {
                var a = new Vector2(CreekX(y), y);
                var b = new Vector2(CreekX(y + 4f), y + 4f);
                if (!Near(a)) continue;
                Flat(t, "Creek", a, b, 7f, Water, 0.02f, 0.6f);
            }
            float xb = CreekX(130f);
            Flat(t, "Creek Path", new Vector2(592.5f, 130f), new Vector2(xb - 7f, 130f), 2f, Sidewalk, 0.016f, 0f);
            var bridge = Pivot(t, "Footbridge");
            Shapes.Box("Bridge Deck", new Vector3(0f, 0.15f, 0f), new Vector3(14f, 0.3f, 3.2f), Plank, bridge);
            Shapes.Box("Bridge Rail", new Vector3(0f, 1.1f, 1.5f), new Vector3(14f, 0.12f, 0.12f), Plank, bridge);
            Shapes.Box("Bridge Rail", new Vector3(0f, 1.1f, -1.5f), new Vector3(14f, 0.12f, 0.12f), Plank, bridge);
            for (int i = -2; i <= 2; i++)
                for (int s = -1; s <= 1; s += 2)
                    Shapes.BoxOnGround("Bridge Post", new Vector3(i * 3.4f, 0f, s * 1.5f), new Vector3(0.15f, 1.2f, 0.15f), Plank, bridge);
            bridge.position = new Vector3(xb, 0f, 130f);
        }

        static void BuildWoods(Transform root)
        {
            var t = Pivot(root, "Woods");
            var rng = new System.Random(42);
            for (int i = 0; i < 220; i++)
            {
                float y = (float)(rng.NextDouble() * 310 - 25), x = (float)(rng.NextDouble() * 72 + 596);
                float r = (float)(3 + rng.NextDouble() * 2.5);
                if (Mathf.Abs(x - CreekX(y)) < 6f || (Mathf.Abs(y - 130f) < 4f)) continue;
                if (Near(new Vector2(x, y))) Tree(t, new Vector2(x, y), r);
            }
            for (int i = 0; i < 90; i++)
            {
                float x = (float)(rng.NextDouble() * 620 - 25);
                float y = rng.NextDouble() < 0.5 ? (float)(rng.NextDouble() * 15 - 25) : (float)(rng.NextDouble() * 14 + 271);
                float r = (float)(3 + rng.NextDouble() * 2);
                if (Near(new Vector2(x, y))) Tree(t, new Vector2(x, y), r);
            }
        }

        /// A tree: a solid trunk and a leafy ball up high (no collider on the leaves).
        static void Tree(Transform t, Vector2 at, float radius)
        {
            float leaves = Mathf.Clamp(radius * 1.6f, 2.5f, 7f);
            Shapes.BoxOnGround("Tree Trunk", W(at), new Vector3(0.6f, 2.6f + leaves * 0.4f, 0.6f), Bark, t);
            Shapes.Ball("Tree Leaves", W(at, 2.6f + leaves * 0.85f), leaves, Leaves, t);
        }

        // A flat blue ring on the ground so you can see where a site is.
        static void SiteMarker(Transform root, Site site)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Site " + site.Name + " Area";
            ring.transform.SetParent(root, false);
            ring.transform.position = site.Center + Vector3.up * 0.04f;
            ring.transform.localScale = new Vector3(site.Radius * 2f, 0.01f, site.Radius * 2f);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            var m = Shapes.UniqueMat(SiteMark);
            m.color = Color.Lerp(Grass, SiteMark, 0.35f);
            ring.GetComponent<Renderer>().sharedMaterial = m;
        }
    }
}
