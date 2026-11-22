// MapBuilder.cs
// ---------------------------------------------------------------------------
// Builds the grey-box test map, "Maple Court (grey-box)", out of plain blocks
// when the game starts. It follows the design doc's map rules:
//
//   * Three houses. The attackers spawn at the Fort House (south end).
//     The defenders hold the other two: House A (northwest) and House B
//     (northeast). Each defender house has a plant site.
//   * Site A is the Treehouse in House A's front yard (height advantage).
//   * Site B is House B's open garage and driveway.
//   * Only garages and treehouses can be entered, not the houses themselves.
//   * A long hedge across the middle with three gaps, plus fences, parked
//     cars and yard clutter for cover.
//
// Map of the layout (north is up, attackers start at the bottom):
//
//        [House A][gar]   defender spawn   [gar][House B]
//          (Treehouse = A)                  (Garage = B)
//      ======= hedge ===== gap ===== hedge ======= (gaps at left, middle, right)
//              fences, cars, shed, sandbox, street down the middle
//                          [ Fort House ]  <- attackers
//
// All positions are in meters. X goes east (right), Z goes north (up).
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace WaterWars
{
    /// One plant site (A or B).
    public class Site
    {
        public string Name;
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
        public readonly List<Vector3> AttackerSpawns = new List<Vector3>();
        public readonly List<Vector3> DefenderSpawns = new List<Vector3>();
        public readonly List<Vector3> AttackerCurb = new List<Vector3>();
        public readonly List<Vector3> DefenderCurb = new List<Vector3>();
        public Site SiteA, SiteB;
        public Site[] Sites;
        public NavGraph Nav = new NavGraph();
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

        public static MapData Build(Transform root)
        {
            var map = new MapData();

            // ---- Ground, street, outer fence -------------------------------
            Shapes.Box("Ground", new Vector3(0f, -0.5f, -5f), new Vector3(150f, 1f, 160f), Grass, root);
            Shapes.Box("Street", new Vector3(0f, 0.01f, -5f), new Vector3(9f, 0.02f, 140f), Asphalt, root, false);
            Shapes.Box("Sidewalk West", new Vector3(-6f, 0.015f, -5f), new Vector3(2.5f, 0.02f, 140f), Sidewalk, root, false);
            Shapes.Box("Sidewalk East", new Vector3(6f, 0.015f, -5f), new Vector3(2.5f, 0.02f, 140f), Sidewalk, root, false);

            Shapes.BoxOnGround("Outer Fence West", new Vector3(-72f, 0f, -5f), new Vector3(0.4f, 3.5f, 160f), FenceWood, root);
            Shapes.BoxOnGround("Outer Fence East", new Vector3(72f, 0f, -5f), new Vector3(0.4f, 3.5f, 160f), FenceWood, root);
            Shapes.BoxOnGround("Outer Fence South", new Vector3(0f, 0f, -84f), new Vector3(144f, 3.5f, 0.4f), FenceWood, root);
            Shapes.BoxOnGround("Outer Fence North", new Vector3(0f, 0f, 74f), new Vector3(144f, 3.5f, 0.4f), FenceWood, root);

            // ---- The three houses ------------------------------------------
            // Fort House: attackers' home base at the south end.
            BuildHouse(root, "Fort House", new Vector3(0f, 0f, -76f), new Vector3(20f, 7f, 10f), new Color(0.85f, 0.78f, 0.6f), facingNorth: true);
            // A little "tree fort" sign post so the attackers' house reads as theirs.
            Shapes.BoxOnGround("Fort Flag Pole", new Vector3(-9f, 0f, -69f), new Vector3(0.2f, 5f, 0.2f), Bark, root);
            Shapes.Box("Fort Flag", new Vector3(-8.2f, 4.5f, -69f), new Vector3(1.5f, 0.9f, 0.05f), TeamInfo.Color(Team.Orange), root, false);

            // House A (northwest) + its garage.
            BuildHouse(root, "House A", new Vector3(-40f, 0f, 52f), new Vector3(18f, 7f, 12f), new Color(0.75f, 0.82f, 0.92f), facingNorth: false);
            BuildGarage(root, "Garage A", new Vector3(-26.5f, 0f, 50.5f));

            // House B (northeast) + its garage (that garage is Site B).
            BuildHouse(root, "House B", new Vector3(40f, 0f, 52f), new Vector3(18f, 7f, 12f), new Color(0.93f, 0.8f, 0.8f), facingNorth: false);
            BuildGarage(root, "Garage B", new Vector3(26.5f, 0f, 50.5f));

            // ---- Treehouses (Site A's is the big one) ----------------------
            BuildTreehouse(root, "Treehouse A", new Vector3(-40f, 0f, 32f), 7f, 3.6f, out var treehouseA);
            BuildTreehouse(root, "Treehouse B-side", new Vector3(50f, 0f, 24f), 5f, 3f, out var treehouseB);

            // ---- The middle: hedge with three gaps -------------------------
            // Gaps at x = -40 (A lane), x = 0 (Hedge Gap / mid), x = +40 (B lane). Each gap is 5 m wide.
            HedgeRow(root, -72f, -42.5f, 5f);
            HedgeRow(root, -37.5f, -2.5f, 5f);
            HedgeRow(root, 2.5f, 37.5f, 5f);
            HedgeRow(root, 42.5f, 72f, 5f);

            // Tall backyard fences closer to the attackers, with gaps for the lanes.
            Shapes.BoxOnGround("Fence SW", new Vector3(-56f, 0f, -32f), new Vector3(32f, 1.9f, 0.25f), FenceWood, root);
            Shapes.BoxOnGround("Fence SW2", new Vector3(-24f, 0f, -32f), new Vector3(24f, 1.9f, 0.25f), FenceWood, root);
            Shapes.BoxOnGround("Fence SE", new Vector3(24f, 0f, -32f), new Vector3(24f, 1.9f, 0.25f), FenceWood, root);
            Shapes.BoxOnGround("Fence SE2", new Vector3(56f, 0f, -32f), new Vector3(32f, 1.9f, 0.25f), FenceWood, root);

            // Short white picket fences around the defenders' front yards (you can see over them, crouch behind them).
            Shapes.BoxOnGround("Picket A1", new Vector3(-60f, 0f, 20f), new Vector3(16f, 1.1f, 0.15f), PicketWhite, root);
            Shapes.BoxOnGround("Picket A2", new Vector3(-22f, 0f, 20f), new Vector3(12f, 1.1f, 0.15f), PicketWhite, root);
            Shapes.BoxOnGround("Picket B1", new Vector3(62f, 0f, 20f), new Vector3(16f, 1.1f, 0.15f), PicketWhite, root);
            Shapes.BoxOnGround("Picket B2", new Vector3(22f, 0f, 20f), new Vector3(12f, 1.1f, 0.15f), PicketWhite, root);

            // Parked cars on the street.
            Car(root, new Vector3(2.5f, 0f, -45f), new Color(0.8f, 0.2f, 0.2f));
            Car(root, new Vector3(-2.5f, 0f, -18f), new Color(0.2f, 0.35f, 0.75f));
            Car(root, new Vector3(2.5f, 0f, 18f), new Color(0.9f, 0.85f, 0.4f));
            Car(root, new Vector3(-2.5f, 0f, 36f), new Color(0.3f, 0.6f, 0.35f));

            // Yard clutter in the middle.
            Shapes.BoxOnGround("Tool Shed", new Vector3(-20f, 0f, -12f), new Vector3(4f, 3f, 4f), new Color(0.55f, 0.6f, 0.5f), root);
            Shapes.BoxOnGround("Sandbox", new Vector3(-30f, 0f, -18f), new Vector3(4f, 0.5f, 4f), new Color(0.9f, 0.82f, 0.55f), root);
            Shapes.BoxOnGround("Play Fort", new Vector3(20f, 0f, -12f), new Vector3(3f, 2.2f, 3f), new Color(0.8f, 0.5f, 0.3f), root);
            Shapes.BoxOnGround("Trash Can 1", new Vector3(9f, 0f, 9f), new Vector3(0.9f, 1.2f, 0.9f), new Color(0.35f, 0.4f, 0.35f), root);
            Shapes.BoxOnGround("Trash Can 2", new Vector3(10.3f, 0f, 9.3f), new Vector3(0.9f, 1.2f, 0.9f), new Color(0.35f, 0.4f, 0.35f), root);
            Shapes.BoxOnGround("Trash Can 3", new Vector3(-9.5f, 0f, -4f), new Vector3(0.9f, 1.2f, 0.9f), new Color(0.35f, 0.4f, 0.35f), root);
            Shapes.BoxOnGround("Bush W", new Vector3(-55f, 0f, -12f), new Vector3(3f, 2f, 3f), HedgeGreen, root);
            Shapes.BoxOnGround("Bush E", new Vector3(55f, 0f, -12f), new Vector3(3f, 2f, 3f), HedgeGreen, root);
            Shapes.BoxOnGround("Bush NW", new Vector3(-55f, 0f, 30f), new Vector3(3f, 2f, 3f), HedgeGreen, root);
            Shapes.BoxOnGround("Bush NE", new Vector3(58f, 0f, 36f), new Vector3(3f, 2f, 3f), HedgeGreen, root);
            Shapes.BoxOnGround("Swing Set", new Vector3(-45f, 0f, -50f), new Vector3(5f, 2.5f, 0.3f), new Color(0.6f, 0.2f, 0.2f), root);
            Shapes.BoxOnGround("Above-Ground Pool", new Vector3(45f, 0f, -50f), new Vector3(6f, 1.2f, 6f), new Color(0.4f, 0.7f, 0.9f), root);

            // Site A cover (around the treehouse).
            Shapes.BoxOnGround("Picnic Table", new Vector3(-31f, 0f, 29f), new Vector3(2.2f, 0.9f, 1.6f), Plank, root);
            Shapes.BoxOnGround("Crate A1", new Vector3(-47f, 0f, 26f), new Vector3(1.4f, 1.4f, 1.4f), Plank, root);
            Shapes.BoxOnGround("Crate A2", new Vector3(-47f, 1.4f, 26f), new Vector3(1f, 1f, 1f), Plank, root);
            Shapes.BoxOnGround("Garden Wall A", new Vector3(-34f, 0f, 38f), new Vector3(4f, 1.2f, 0.6f), new Color(0.6f, 0.55f, 0.5f), root);

            // Site B cover (around the garage and driveway).
            Shapes.BoxOnGround("Kiddie Pool", new Vector3(33f, 0f, 37f), new Vector3(3f, 0.6f, 3f), new Color(0.4f, 0.75f, 0.95f), root);
            Shapes.BoxOnGround("Crate B1", new Vector3(19f, 0f, 40f), new Vector3(1.4f, 1.4f, 1.4f), Plank, root);
            Shapes.BoxOnGround("Wheelbarrow", new Vector3(29f, 0f, 33f), new Vector3(1f, 0.9f, 1.8f), new Color(0.7f, 0.25f, 0.2f), root);
            Shapes.BoxOnGround("Driveway", new Vector3(26.5f, 0f, 38f), new Vector3(7f, 0.02f, 10f), Sidewalk, root, false);

            // ---- Sites ------------------------------------------------------
            map.SiteA = new Site { Name = "A", Center = new Vector3(-39f, 0f, 31f), Radius = 9f };
            map.SiteA.HoldSpots.AddRange(treehouseA.Floor);
            map.SiteA.HoldSpots.Add(new Vector3(-33f, 0f, 40f));
            map.SiteA.HoldSpots.Add(new Vector3(-48f, 0f, 30f));
            map.SiteA.HoldSpots.Add(new Vector3(-30f, 0f, 31f));
            map.SiteA.AttackSpots.Add(new Vector3(-39f, 0f, 27f));
            map.SiteA.AttackSpots.Add(new Vector3(-35f, 0f, 30f));
            map.SiteA.AttackSpots.Add(new Vector3(-44f, 0f, 30f));
            map.SiteA.AttackSpots.Add(new Vector3(-38f, 0f, 35f));

            map.SiteB = new Site { Name = "B", Center = new Vector3(26.5f, 0f, 43f), Radius = 8f };
            map.SiteB.HoldSpots.Add(new Vector3(24f, 0f, 49f));  // inside the garage
            map.SiteB.HoldSpots.Add(new Vector3(29f, 0f, 49f));
            map.SiteB.HoldSpots.Add(new Vector3(34f, 0f, 40f));
            map.SiteB.HoldSpots.Add(new Vector3(18f, 0f, 43f));
            map.SiteB.HoldSpots.AddRange(treehouseB.Floor);
            map.SiteB.AttackSpots.Add(new Vector3(26.5f, 0f, 42f));
            map.SiteB.AttackSpots.Add(new Vector3(23f, 0f, 38f));
            map.SiteB.AttackSpots.Add(new Vector3(31f, 0f, 41f));
            map.SiteB.AttackSpots.Add(new Vector3(26.5f, 0f, 47f));
            map.Sites = new[] { map.SiteA, map.SiteB };

            SiteMarker(root, map.SiteA);
            SiteMarker(root, map.SiteB);

            // ---- Spawns and the curb where Soaked kids sit -----------------
            for (int i = 0; i < Tuning.TeamSize; i++)
            {
                map.AttackerSpawns.Add(new Vector3(-6f + i * 3f, 0f, -64f));
                map.DefenderSpawns.Add(new Vector3(-6f + i * 3f, 0f, 55f));
                map.AttackerCurb.Add(new Vector3(-20f - i * 1.4f, 0f, -80f));
                map.DefenderCurb.Add(new Vector3(-10f + i * 1.4f, 0f, 70f));
            }

            // ---- Bot stepping stones (see NavGraph.cs) ---------------------
            map.Nav.BuildGrid(-70f, 70f, -82f, 72f, 3f);
            ConnectTreehouse(map.Nav, treehouseA);
            ConnectTreehouse(map.Nav, treehouseB);
            return map;
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

        // An open garage you CAN go inside. The door side faces south (toward the attackers).
        static void BuildGarage(Transform root, string name, Vector3 bottomCenter)
        {
            const float w = 8f, d = 9f, h = 3.4f, t = 0.3f;
            var g = new GameObject(name).transform;
            g.SetParent(root, false);
            Shapes.BoxOnGround(name + " Back Wall", bottomCenter + new Vector3(0f, 0f, d * 0.5f), new Vector3(w, h, t), GarageGrey, g);
            Shapes.BoxOnGround(name + " Left Wall", bottomCenter + new Vector3(-w * 0.5f, 0f, 0f), new Vector3(t, h, d), GarageGrey, g);
            Shapes.BoxOnGround(name + " Right Wall", bottomCenter + new Vector3(w * 0.5f, 0f, 0f), new Vector3(t, h, d), GarageGrey, g);
            Shapes.Box(name + " Roof", bottomCenter + new Vector3(0f, h + t * 0.5f, 0f), new Vector3(w + 0.6f, t, d + 0.6f), Roof, g);
            // Half-open roll-up door above the opening.
            Shapes.Box(name + " Door", bottomCenter + new Vector3(0f, h - 0.4f, -d * 0.5f), new Vector3(w, 0.8f, 0.15f), PicketWhite, g);
            // Stuff inside to hide behind.
            Shapes.BoxOnGround(name + " Workbench", bottomCenter + new Vector3(-2.8f, 0f, 3f), new Vector3(1.6f, 1f, 2.4f), Plank, g);
            Shapes.BoxOnGround(name + " Boxes", bottomCenter + new Vector3(2.6f, 0f, 1f), new Vector3(1.4f, 1.3f, 1.4f), new Color(0.75f, 0.6f, 0.4f), g);
            Shapes.BoxOnGround(name + " Bike", bottomCenter + new Vector3(2.8f, 0f, 3.6f), new Vector3(0.3f, 1f, 1.8f), new Color(0.2f, 0.5f, 0.9f), g, false);
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
            public Vector3 Bottom, Top;
            public readonly List<Vector3> Floor = new List<Vector3>();
        }

        static void ConnectTreehouse(NavGraph graph, TreehouseNav t)
        {
            int bottomNode = graph.AddAndConnect(t.Bottom, 5f);
            int topNode = graph.AddNode(t.Top);
            graph.Link(bottomNode, topNode);
            foreach (var s in t.Floor) graph.Link(graph.AddNode(s), topNode);
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
