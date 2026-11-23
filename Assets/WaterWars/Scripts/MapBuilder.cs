// MapBuilder.cs
// ---------------------------------------------------------------------------
// Builds the neighborhood out of grey-box blocks when a match starts.
//
// THE NEIGHBORHOOD ("Maple Court")
//   24 house lots on three east-west streets (Maple Street, Birch Lane,
//   Oak Drive), cut in half by a north-south street (Elm Court). Every lot
//   has a house you can't enter, often a garage you can, a front lawn, and a
//   fenced backyard with pools, sheds, trampolines and sometimes a treehouse.
//
// PICKING THE MAP
//   Each match picks 3 of the 24 houses, like the design doc says:
//     * the attackers' house (they spawn on the street in front of it)
//     * two defender houses, which become plant sites:
//         Site A = a backyard TREEHOUSE
//         Site B = a GARAGE with a back door into the yard
//   The houses are picked so the two sites are a fair distance from the
//   attackers. A different 3 every match means a different map every match.
//
// CHOKEPOINTS
//   Backyards are fenced. To reach a site you squeeze through a side yard
//   between two houses, a gate in a fence, or (for B) the garage itself.
//   Every site has several ways in, but each one is narrow.
//
// Positions are in meters. X goes east, Z goes north.
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
        public string AttackerAddress;
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
            return "Attackers: " + AttackerAddress + "\nSite A (treehouse): " + SiteA.Address + "\nSite B (garage): " + SiteB.Address;
        }
    }

    public static class MapBuilder
    {
        // Grey-box colors. Simple and readable, warm like a summer afternoon.
        static readonly Color Grass = new Color(0.45f, 0.68f, 0.32f);
        static readonly Color Asphalt = new Color(0.32f, 0.32f, 0.34f);
        static readonly Color Sidewalk = new Color(0.78f, 0.76f, 0.72f);
        static readonly Color HedgeGreen = new Color(0.2f, 0.42f, 0.18f);
        static readonly Color FenceWood = new Color(0.62f, 0.45f, 0.3f);
        static readonly Color PicketWhite = new Color(0.93f, 0.92f, 0.88f);
        static readonly Color Bark = new Color(0.42f, 0.3f, 0.2f);
        static readonly Color Plank = new Color(0.7f, 0.52f, 0.33f);
        static readonly Color Roof = new Color(0.45f, 0.25f, 0.22f);
        static readonly Color GarageGrey = new Color(0.72f, 0.72f, 0.7f);
        static readonly Color DarkWindow = new Color(0.2f, 0.25f, 0.32f);
        static readonly Color SiteMark = new Color(0.25f, 0.55f, 1f);

        static readonly Color[] HouseColors =
        {
            new Color(0.75f, 0.82f, 0.92f), new Color(0.93f, 0.8f, 0.8f), new Color(0.85f, 0.78f, 0.6f),
            new Color(0.8f, 0.9f, 0.78f), new Color(0.95f, 0.92f, 0.75f), new Color(0.85f, 0.8f, 0.92f),
            new Color(0.9f, 0.88f, 0.85f), new Color(0.7f, 0.78f, 0.75f),
        };

        // ---- Neighborhood layout -------------------------------------------
        const float LotWidth = 26f, LotDepth = 28f, StreetWidth = 12f;
        static readonly float[] ColumnX = { -71f, -45f, -19f, 19f, 45f, 71f };
        static readonly float[] RowZ = { -48f, -20f, 20f, 48f };
        static readonly bool[] RowFacesSouth = { true, false, true, false };
        static readonly float[] StreetZ = { -68f, 0f, 68f };
        static readonly string[] StreetNames = { "Maple St", "Birch Ln", "Oak Dr" };
        const float HalfWidth = 84f, HalfDepth = 74f;

        enum LotRole { Plain, Attackers, SiteA, SiteB }

        class Lot
        {
            public int Row, Col;
            public Vector3 Center;
            public float Yaw;            // 0 = front faces south, 180 = front faces north
            public string Address;
            public LotRole Role;
            public Transform Root;
            public readonly List<Vector3> Portals = new List<Vector3>(); // local points bots must be able to squeeze through
            public Vector3 W(Vector3 local) { return Root.TransformPoint(local); }
        }

        public static MapData Build(Transform root)
        {
            var map = new MapData();

            // ---- Make the 24 lots and pick the 3 houses for this match -------
            var lots = new List<Lot>();
            for (int r = 0; r < RowZ.Length; r++)
                for (int c = 0; c < ColumnX.Length; c++)
                {
                    int street = r == 0 ? 0 : r == 3 ? 2 : 1;
                    lots.Add(new Lot
                    {
                        Row = r, Col = c,
                        Center = new Vector3(ColumnX[c], 0f, RowZ[r]),
                        Yaw = RowFacesSouth[r] ? 0f : 180f,
                        Address = (100 + c * 12 + (RowFacesSouth[r] ? 0 : 1) + r * 2) + " " + StreetNames[street],
                    });
                }
            PickHouses(lots, out var attackLot, out var lotA, out var lotB);
            attackLot.Role = LotRole.Attackers;
            lotA.Role = LotRole.SiteA;
            lotB.Role = LotRole.SiteB;
            map.AttackerAddress = attackLot.Address;

            // ---- Ground, streets, outer fence --------------------------------
            Shapes.Box("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(HalfWidth * 2f + 4f, 1f, HalfDepth * 2f + 4f), Grass, root);
            foreach (float z in StreetZ)
                Shapes.Box("Street", new Vector3(0f, 0.01f, z), new Vector3(HalfWidth * 2f, 0.02f, StreetWidth), Asphalt, root, false);
            Shapes.Box("Elm Court", new Vector3(0f, 0.012f, 0f), new Vector3(StreetWidth, 0.02f, HalfDepth * 2f), Asphalt, root, false);
            Shapes.BoxOnGround("Outer Fence West", new Vector3(-HalfWidth - 0.5f, 0f, 0f), new Vector3(0.4f, 3.5f, HalfDepth * 2f + 2f), FenceWood, root);
            Shapes.BoxOnGround("Outer Fence East", new Vector3(HalfWidth + 0.5f, 0f, 0f), new Vector3(0.4f, 3.5f, HalfDepth * 2f + 2f), FenceWood, root);
            Shapes.BoxOnGround("Outer Fence South", new Vector3(0f, 0f, -HalfDepth - 0.5f), new Vector3(HalfWidth * 2f + 2f, 3.5f, 0.4f), FenceWood, root);
            Shapes.BoxOnGround("Outer Fence North", new Vector3(0f, 0f, HalfDepth + 0.5f), new Vector3(HalfWidth * 2f + 2f, 3.5f, 0.4f), FenceWood, root);

            // ---- Build every lot ---------------------------------------------
            var treehouses = new List<TreehouseNav>();
            TreehouseNav siteATree = null;
            foreach (var lot in lots)
            {
                var th = BuildLot(root, lot);
                if (th != null) treehouses.Add(th);
                if (lot.Role == LotRole.SiteA) siteATree = th;
            }

            // Street clutter: parked cars, trash cans. Kept away from the 3 chosen houses' fronts.
            for (int i = 0; i < 18; i++)
            {
                int street = Random.Range(0, StreetZ.Length);
                float x = Random.Range(-HalfWidth + 6f, HalfWidth - 6f);
                if (Mathf.Abs(x) < 10f) continue;
                float side = Random.value < 0.5f ? -1f : 1f;
                var at = new Vector3(x, 0f, StreetZ[street] + side * 3.6f);
                bool nearChosen = false;
                foreach (var l in new[] { attackLot, lotA, lotB })
                    if (Vector3.Distance(new Vector3(l.Center.x, 0f, l.Center.z), at) < 22f) nearChosen = true;
                if (nearChosen && Random.value < 0.7f) continue;
                if (Random.value < 0.75f) Car(root, at, HouseColors[Random.Range(0, HouseColors.Length)] * 0.8f);
                else Shapes.BoxOnGround("Trash Cans", at, new Vector3(1.8f, 1.2f, 0.9f), new Color(0.35f, 0.4f, 0.35f), root);
            }
            // A little cover where Elm Court crosses the streets (the middle).
            Shapes.BoxOnGround("Mailbox Cluster", new Vector3(-4f, 0f, -4f), new Vector3(1.2f, 1.3f, 1.2f), new Color(0.3f, 0.3f, 0.5f), root);
            Shapes.BoxOnGround("Bus Stop Bench", new Vector3(4.5f, 0f, 4.5f), new Vector3(0.8f, 1f, 3f), Plank, root);

            Physics.SyncTransforms();

            // ---- Sites -------------------------------------------------------
            map.SiteA = new Site { Name = "A", Address = lotA.Address, Center = lotA.W(new Vector3(3f, 0f, 8f)), Radius = 8.5f };
            map.SiteB = new Site { Name = "B", Address = lotB.Address, Center = lotB.W(new Vector3(7f, 0f, 2f)), Radius = 8f };
            map.Sites = new[] { map.SiteA, map.SiteB };
            SiteMarker(root, map.SiteA);
            SiteMarker(root, map.SiteB);

            // ---- Bot stepping stones (see NavGraph.cs) -----------------------
            map.Nav.BuildGrid(-HalfWidth + 1f, HalfWidth - 1f, -HalfDepth + 1f, HalfDepth - 1f, 3f);
            foreach (var t in treehouses) ConnectTreehouse(map.Nav, t);
            // Extra stepping stones in gates, side yards and garages (the grid can miss narrow gaps).
            foreach (var lot in lots)
                foreach (var p in lot.Portals)
                {
                    Vector3 w = lot.W(p);
                    if (NavGraph.SpotIsFree(w)) map.Nav.AddAndConnect(w, 4.5f);
                }

            // Spots for defenders to hold and attackers to push to, inside each site.
            if (siteATree != null) map.SiteA.HoldSpots.AddRange(siteATree.WorldFloor());
            foreach (var site in map.Sites)
            {
                site.HoldSpots.AddRange(OpenSpotsNear(map.Nav, site.Center, site.Radius, 4, 2.5f));
                site.AttackSpots.AddRange(OpenSpotsNear(map.Nav, site.Center, site.Radius * 0.7f, 4, 2f));
                if (site.AttackSpots.Count == 0) site.AttackSpots.Add(site.Center);
                if (site.HoldSpots.Count == 0) site.HoldSpots.Add(site.Center);
            }

            // ---- Spawns and the curb where Soaked kids sit -------------------
            Vector3 attackFront = attackLot.W(new Vector3(0f, 0f, -20f)); // middle of the street out front
            Vector3 defendCenter = (map.SiteA.Center + map.SiteB.Center) * 0.5f;
            map.AttackerSpawns.AddRange(OpenSpotsNear(map.Nav, attackFront, 14f, Tuning.TeamSize, 2.5f));
            map.DefenderSpawns.AddRange(OpenSpotsNear(map.Nav, defendCenter, 30f, Tuning.TeamSize, 2.5f));
            while (map.AttackerSpawns.Count < Tuning.TeamSize) map.AttackerSpawns.Add(attackFront);
            while (map.DefenderSpawns.Count < Tuning.TeamSize) map.DefenderSpawns.Add(defendCenter);
            map.AttackerFacing = YawToward(attackFront, defendCenter);
            map.DefenderFacing = YawToward(defendCenter, attackFront);
            for (int i = 0; i < Tuning.TeamSize; i++)
            {
                map.AttackerCurb.Add(attackLot.W(new Vector3(-11f + i * 1.4f, 0f, -14.6f)));
                map.DefenderCurb.Add(map.DefenderSpawns[0] + new Vector3(-3f + i * 1.4f, 0f, 0f));
            }
            return map;
        }

        static float YawToward(Vector3 from, Vector3 to)
        {
            var d = to - from;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        /// Pick an attacker house and two defender houses that make a fair triangle.
        static void PickHouses(List<Lot> lots, out Lot attack, out Lot a, out Lot b)
        {
            for (int tries = 0; tries < 2000; tries++)
            {
                attack = lots[Random.Range(0, lots.Count)];
                a = lots[Random.Range(0, lots.Count)];
                b = lots[Random.Range(0, lots.Count)];
                if (attack == a || attack == b || a == b) continue;
                float ab = Vector3.Distance(a.Center, b.Center);
                float da = Vector3.Distance(attack.Center, a.Center);
                float db = Vector3.Distance(attack.Center, b.Center);
                if (ab < 40f || ab > 85f) continue;          // sites not too close, not too far apart
                if (da < 65f || db < 65f || da > 130f || db > 130f) continue; // a real walk for the attackers
                if (Mathf.Abs(da - db) > 25f) continue;     // both sites about equally far
                return;
            }
            // Fallback that always works: attackers bottom middle, sites top left/right.
            attack = lots.Find(l => l.Row == 0 && l.Col == 2);
            a = lots.Find(l => l.Row == 3 && l.Col == 1);
            b = lots.Find(l => l.Row == 3 && l.Col == 4);
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

        // =====================================================================
        // One house lot. Built facing south around (0,0,0), then turned and
        // moved into place. Local layout (front = -Z = the street):
        //
        //    z=+14  ---------- back fence (with a gate) -------------
        //           |  backyard: pool / shed / treehouse / ...      |
        //    z=+1.5 |[ side   ][      HOUSE      ][ GARAGE ][side]  |
        //           |[ yard   ][                 ][        ][yard]  |
        //    z=-7.5     front lawn, tree, mailbox
        //    z=-14  ---------------- curb / street ------------------
        //          x=-13                                         x=+13
        // =====================================================================
        static TreehouseNav BuildLot(Transform root, Lot lot)
        {
            var t = new GameObject("Lot " + lot.Address).transform;
            t.SetParent(root, false);
            lot.Root = t;
            TreehouseNav tree = null;

            bool hasGarage = lot.Role == LotRole.SiteB || Random.value < 0.7f;
            float houseHeight = Random.value < 0.5f ? 6.5f : 4.2f;
            Color wall = HouseColors[Random.Range(0, HouseColors.Length)];
            BuildHouse(t, "House " + lot.Address, new Vector3(-3.5f, 0f, -3f), new Vector3(12f, houseHeight, 9f), wall, false);
            if (hasGarage)
            {
                BuildGarage(t, "Garage " + lot.Address, new Vector3(7f, 0f, -3f), lot.Role == LotRole.SiteB);
                Shapes.Box("Driveway", new Vector3(7f, 0.015f, -10.75f), new Vector3(6f, 0.02f, 6.5f), Sidewalk, t, false);
            }
            else
                Shapes.BoxOnGround("Side Hedge", new Vector3(7f, 0f, -6f), new Vector3(5f, 1.6f, 1.2f), HedgeGreen, t);

            // Front lawn bits.
            if (Random.value < 0.6f) Tree(t, new Vector3(-9f, 0f, -11f));
            if (Random.value < 0.5f) Shapes.BoxOnGround("Mailbox", new Vector3(11.5f, 0f, -13f), new Vector3(0.3f, 1.2f, 0.3f), new Color(0.2f, 0.2f, 0.25f), t);
            if (Random.value < 0.3f) Shapes.BoxOnGround("Bush", new Vector3(-2f, 0f, -9.5f), new Vector3(2.5f, 1.2f, 1.2f), HedgeGreen, t);

            // Backyard fences. Each lot builds the fence on its own left side; lots next to
            // Elm Court also fence their right side. Front-row lots build the shared back fence.
            float gateL = BuildSideFence(t, -13f);
            lot.Portals.Add(new Vector3(-13f, 0f, gateL));
            bool rightOnStreet = (lot.Yaw == 0f && (lot.Col == 2)) || (lot.Yaw == 180f && lot.Col == 3);
            if (rightOnStreet) lot.Portals.Add(new Vector3(13f, 0f, BuildSideFence(t, 13f)));
            if (lot.Row == 0 || lot.Row == 2)
            {
                float gateX = BuildBackFence(t);
                if (!float.IsNaN(gateX)) lot.Portals.Add(new Vector3(gateX, 0f, 14f));
            }

            // Side yards between houses are narrow. Make sure bots have stepping stones through them.
            foreach (float z in new[] { -10f, -6f, -2f, 2.5f, 5f })
            {
                lot.Portals.Add(new Vector3(-11.3f, 0f, z));
                lot.Portals.Add(new Vector3(hasGarage ? 12f : 10f, 0f, z));
            }
            if (hasGarage)
                foreach (float z in new[] { -10f, -6f, -2.5f, 0.5f })
                    lot.Portals.Add(new Vector3(7f, 0f, z));
            if (lot.Role == LotRole.SiteB) lot.Portals.Add(new Vector3(7f, 0f, 3f));

            // Backyard stuff.
            if (lot.Role == LotRole.SiteA || (lot.Role == LotRole.Plain && Random.value < 0.2f))
            {
                // Treehouse in the middle-right of the yard, ramp running toward the left.
                var frame = new GameObject("Treehouse Frame").transform;
                frame.SetParent(t, false);
                BuildTreehouse(frame, "Treehouse", Vector3.zero, 6f, 3.4f, out tree);
                frame.localPosition = new Vector3(6f, 0f, 8f);
                frame.localRotation = Quaternion.Euler(0f, 90f, 0f);
                tree.Frame = frame;
                Shapes.BoxOnGround("Crates", new Vector3(-10.5f, 0f, 11.5f), new Vector3(1.4f, 1.4f, 1.4f), Plank, t);
                Shapes.BoxOnGround("Picnic Table", new Vector3(-6f, 0f, 3.5f), new Vector3(2.2f, 0.9f, 1.6f), Plank, t);
            }
            else if (lot.Role == LotRole.SiteB)
            {
                Shapes.BoxOnGround("Kiddie Pool", new Vector3(-5f, 0f, 9f), new Vector3(3f, 0.6f, 3f), new Color(0.4f, 0.75f, 0.95f), t);
                Shapes.BoxOnGround("Crates", new Vector3(3f, 0f, 10f), new Vector3(1.4f, 1.4f, 1.4f), Plank, t);
                Shapes.BoxOnGround("Crates 2", new Vector3(3f, 1.4f, 10f), new Vector3(1f, 1f, 1f), Plank, t);
                Shapes.BoxOnGround("Wheelbarrow", new Vector3(10.5f, 0f, 6f), new Vector3(1f, 0.9f, 1.8f), new Color(0.7f, 0.25f, 0.2f), t);
                Shapes.BoxOnGround("Grill", new Vector3(-9f, 0f, 4f), new Vector3(1f, 1.1f, 0.8f), new Color(0.15f, 0.15f, 0.15f), t);
            }
            else
            {
                YardThing(t, new Vector3(-6f, 0f, 8.5f));
                YardThing(t, new Vector3(7f, 0f, 9f));
            }

            // Move the finished lot into place.
            t.position = lot.Center;
            t.rotation = Quaternion.Euler(0f, lot.Yaw, 0f);
            return tree;
        }

        /// A random backyard item: pool, shed, trampoline, swing set, hedge, or nothing.
        static void YardThing(Transform t, Vector3 at)
        {
            switch (Random.Range(0, 6))
            {
                case 0: Shapes.BoxOnGround("Above-Ground Pool", at, new Vector3(5f, 1.2f, 5f), new Color(0.4f, 0.7f, 0.9f), t); break;
                case 1: Shapes.BoxOnGround("Shed", at, new Vector3(3.5f, 2.6f, 3f), new Color(0.55f, 0.6f, 0.5f), t); break;
                case 2: Shapes.BoxOnGround("Trampoline", at, new Vector3(4f, 0.8f, 4f), new Color(0.15f, 0.15f, 0.2f), t); break;
                case 3: Shapes.BoxOnGround("Swing Set", at, new Vector3(4.5f, 2.4f, 0.3f), new Color(0.6f, 0.2f, 0.2f), t); break;
                case 4: Shapes.BoxOnGround("Hedge", at, new Vector3(4f, 2f, 1.4f), HedgeGreen, t); break;
                default: break;
            }
        }

        /// A side fence from the back of the house to the back of the lot, with a gate gap.
        /// Sometimes it's a low picket (see over it) or a hedge instead of a tall wooden fence.
        static float BuildSideFence(Transform t, float x)
        {
            float from = 1.5f, to = 14f;
            float gate = Random.Range(4f, 11f);  // where the 2.5 m gap is
            float roll = Random.value;
            float height = roll < 0.25f ? 1.1f : 1.9f;
            Color c = roll < 0.25f ? PicketWhite : roll < 0.4f ? HedgeGreen : FenceWood;
            float thick = c == HedgeGreen ? 1f : 0.2f;
            FenceSegment(t, new Vector3(x, 0f, from), new Vector3(x, 0f, gate - 1.25f), height, thick, c);
            FenceSegment(t, new Vector3(x, 0f, gate + 1.25f), new Vector3(x, 0f, to), height, thick, c);
            return gate;
        }

        /// The back fence shared with the lot behind. Half of them have a gate.
        static float BuildBackFence(Transform t)
        {
            float z = 14f;
            if (Random.value < 0.5f)
            {
                float gate = Random.Range(-8f, 8f);
                FenceSegment(t, new Vector3(-13f, 0f, z), new Vector3(gate - 1.25f, 0f, z), 1.9f, 0.2f, FenceWood);
                FenceSegment(t, new Vector3(gate + 1.25f, 0f, z), new Vector3(13f, 0f, z), 1.9f, 0.2f, FenceWood);
                return gate;
            }
            FenceSegment(t, new Vector3(-13f, 0f, z), new Vector3(13f, 0f, z), 1.9f, 0.2f, FenceWood);
            return float.NaN;
        }

        static void FenceSegment(Transform t, Vector3 a, Vector3 b, float height, float thick, Color c)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.3f) return;
            bool alongX = Mathf.Abs(d.x) > Mathf.Abs(d.z);
            var size = alongX ? new Vector3(len, height, thick) : new Vector3(thick, height, len);
            Shapes.BoxOnGround("Fence", (a + b) * 0.5f, size, c, t);
        }

        static void Tree(Transform t, Vector3 at)
        {
            Shapes.BoxOnGround("Tree Trunk", at, new Vector3(0.7f, 4f, 0.7f), Bark, t);
            Shapes.Ball("Tree Leaves", at + Vector3.up * 5f, 4.5f, HedgeGreen, t);
        }

        // A house you can't go inside: a body, a pointy roof, a door and windows.
        static void BuildHouse(Transform root, string name, Vector3 bottomCenter, Vector3 size, Color wall, bool facingNorth)
        {
            var house = new GameObject(name).transform;
            house.SetParent(root, false);
            Shapes.BoxOnGround(name + " Walls", bottomCenter, size, wall, house);

            // Roof: a long box turned 45 degrees so its top edge makes a ridge.
            float roofSide = size.z * 0.72f;
            var roof = Shapes.Box(name + " Roof", bottomCenter + Vector3.up * size.y, new Vector3(size.x + 0.6f, roofSide, roofSide), Roof, house);
            roof.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

            // Door and windows on the side facing the street (decoration only).
            float face = facingNorth ? size.z * 0.5f + 0.03f : -size.z * 0.5f - 0.03f;
            Shapes.Box(name + " Door", bottomCenter + new Vector3(0f, 1.1f, face), new Vector3(1.4f, 2.2f, 0.05f), new Color(0.45f, 0.25f, 0.15f), house, false);
            for (int i = -1; i <= 1; i += 2)
            {
                Shapes.Box(name + " Window", bottomCenter + new Vector3(i * size.x * 0.28f, 1.7f, face), new Vector3(2f, 1.3f, 0.05f), DarkWindow, house, false);
                Shapes.Box(name + " Window Up", bottomCenter + new Vector3(i * size.x * 0.28f, 4.8f, face), new Vector3(2f, 1.2f, 0.05f), DarkWindow, house, false);
            }
        }

        // An open garage you CAN go inside. The big door faces -Z (the street).
        // With backDoor, there's also a doorway out the back into the yard.
        static void BuildGarage(Transform root, string name, Vector3 bottomCenter, bool backDoor = false)
        {
            const float w = 8f, d = 9f, h = 3.4f, t = 0.3f;
            var g = new GameObject(name).transform;
            g.SetParent(root, false);
            if (backDoor)
            {
                float piece = (w - 2.4f) * 0.5f;
                Shapes.BoxOnGround(name + " Back Wall L", bottomCenter + new Vector3(-w * 0.5f + piece * 0.5f, 0f, d * 0.5f), new Vector3(piece, h, t), GarageGrey, g);
                Shapes.BoxOnGround(name + " Back Wall R", bottomCenter + new Vector3(w * 0.5f - piece * 0.5f, 0f, d * 0.5f), new Vector3(piece, h, t), GarageGrey, g);
                Shapes.Box(name + " Back Door Top", bottomCenter + new Vector3(0f, h - 0.4f, d * 0.5f), new Vector3(2.4f, 0.8f, t), GarageGrey, g);
            }
            else
                Shapes.BoxOnGround(name + " Back Wall", bottomCenter + new Vector3(0f, 0f, d * 0.5f), new Vector3(w, h, t), GarageGrey, g);
            Shapes.BoxOnGround(name + " Left Wall", bottomCenter + new Vector3(-w * 0.5f, 0f, 0f), new Vector3(t, h, d), GarageGrey, g);
            Shapes.BoxOnGround(name + " Right Wall", bottomCenter + new Vector3(w * 0.5f, 0f, 0f), new Vector3(t, h, d), GarageGrey, g);
            Shapes.Box(name + " Roof", bottomCenter + new Vector3(0f, h + t * 0.5f, 0f), new Vector3(w + 0.6f, t, d + 0.6f), Roof, g);
            // Half-open roll-up door above the opening.
            Shapes.Box(name + " Door", bottomCenter + new Vector3(0f, h - 0.4f, -d * 0.5f), new Vector3(w, 0.8f, 0.15f), PicketWhite, g);
            // Stuff inside to hide behind.
            Shapes.BoxOnGround(name + " Workbench", bottomCenter + new Vector3(-2.8f, 0f, 2.2f), new Vector3(1.6f, 1f, 2.4f), Plank, g);
            Shapes.BoxOnGround(name + " Boxes", bottomCenter + new Vector3(2.6f, 0f, 1f), new Vector3(1.4f, 1.3f, 1.4f), new Color(0.75f, 0.6f, 0.4f), g);
            Shapes.BoxOnGround(name + " Bike", bottomCenter + new Vector3(2.8f, 0f, 3.2f), new Vector3(0.3f, 1f, 1.8f), new Color(0.2f, 0.5f, 0.9f), g, false);
        }

        // A treehouse: a platform on posts, a ramp up the south side, railings, a roof, and a trunk.
        static void BuildTreehouse(Transform root, string name, Vector3 groundCenter, float size, float height, out TreehouseNav nav)
        {
            var th = new GameObject(name).transform;
            th.SetParent(root, false);
            float half = size * 0.5f;
            float top = height + 0.15f;
            Shapes.Box(name + " Floor", groundCenter + Vector3.up * height, new Vector3(size, 0.3f, size), Plank, th);

            // Corner posts.
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Shapes.BoxOnGround(name + " Post", groundCenter + new Vector3(sx * (half - 0.2f), 0f, sz * (half - 0.2f)), new Vector3(0.3f, height + 2.6f, 0.3f), Bark, th);

            // The tree trunk behind it, poking through the roof.
            Shapes.BoxOnGround(name + " Trunk", groundCenter + new Vector3(0f, 0f, half + 0.9f), new Vector3(1.4f, height + 6f, 1.4f), Bark, th);
            // Leaves sit high above the roof so they never hide kids standing on the platform.
            Shapes.Ball(name + " Leaves", groundCenter + new Vector3(0f, height + 4.2f + (size + 3f) * 0.5f, half + 0.5f), size + 3f, HedgeGreen, th);

            // Railings (waist-high cover). The south side has a gap for the ramp.
            Vector3 railY = Vector3.up * (top + 0.5f);
            Shapes.Box(name + " Rail N", groundCenter + railY + new Vector3(0f, 0f, half - 0.1f), new Vector3(size, 1f, 0.15f), Plank, th);
            Shapes.Box(name + " Rail W", groundCenter + railY + new Vector3(-half + 0.1f, 0f, 0f), new Vector3(0.15f, 1f, size), Plank, th);
            Shapes.Box(name + " Rail E", groundCenter + railY + new Vector3(half - 0.1f, 0f, 0f), new Vector3(0.15f, 1f, size), Plank, th);
            float sideRail = (size - 2.4f) * 0.5f;
            Shapes.Box(name + " Rail S1", groundCenter + railY + new Vector3(-half + sideRail * 0.5f, 0f, -half + 0.1f), new Vector3(sideRail, 1f, 0.15f), Plank, th);
            Shapes.Box(name + " Rail S2", groundCenter + railY + new Vector3(half - sideRail * 0.5f, 0f, -half + 0.1f), new Vector3(sideRail, 1f, 0.15f), Plank, th);

            // Roof.
            Shapes.Box(name + " Roof", groundCenter + Vector3.up * (height + 2.7f), new Vector3(size + 0.8f, 0.25f, size + 0.8f), Roof, th);

            // Ramp up from the south.
            float rampLength = height * 2.6f;
            Vector3 rampTop = groundCenter + new Vector3(0f, top - 0.13f, -half);
            Vector3 rampBottom = groundCenter + new Vector3(0f, -0.1f, -half - rampLength);
            Shapes.Ramp(name + " Ramp", rampBottom, rampTop, 2f, Plank, th);

            // Stepping stones so bots can climb it. They get hooked up after the
            // ground grid exists (see ConnectTreehouse below).
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

        static void HedgeRow(Transform root, float fromX, float toX, float z)
        {
            Shapes.BoxOnGround("Hedge", new Vector3((fromX + toX) * 0.5f, 0f, z), new Vector3(toX - fromX, 2.4f, 1.6f), HedgeGreen, root);
        }

        static void Car(Transform root, Vector3 at, Color paint)
        {
            var car = new GameObject("Parked Car").transform;
            car.SetParent(root, false);
            Shapes.BoxOnGround("Car Body", at, new Vector3(2f, 1f, 4.6f), paint, car);
            Shapes.BoxOnGround("Car Cabin", at + new Vector3(0f, 1f, -0.3f), new Vector3(1.8f, 0.7f, 2.4f), paint * 0.85f, car);
        }

        // A flat blue ring on the ground and a floating letter post so you can see where a site is.
        static void SiteMarker(Transform root, Site site)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Site " + site.Name + " Area";
            ring.transform.SetParent(root, false);
            ring.transform.position = site.Center + Vector3.up * 0.02f;
            ring.transform.localScale = new Vector3(site.Radius * 2f, 0.01f, site.Radius * 2f);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            var m = Shapes.UniqueMat(new Color(SiteMark.r, SiteMark.g, SiteMark.b, 1f));
            m.color = Color.Lerp(Grass, SiteMark, 0.35f);
            ring.GetComponent<Renderer>().sharedMaterial = m;
        }
    }
}
