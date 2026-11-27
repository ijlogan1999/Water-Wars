// NavGraph.cs
// ---------------------------------------------------------------------------
// How bots find their way around. We sprinkle a grid of invisible "stepping
// stones" over the map, throw away any stone that lands inside a wall, and
// connect neighbors that have a clear straight line between them. A bot that
// wants to go somewhere asks for the shortest chain of stones (the A* search
// below) and walks from stone to stone.
//
// You never place these by hand: the map is scanned automatically when the
// game starts, so moving a fence or a house in MapBuilder just works.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace WaterWars
{
    public class NavGraph
    {
        public readonly List<Vector3> Nodes = new List<Vector3>();
        public readonly List<List<int>> Links = new List<List<int>>();

        const float KidRadius = 0.45f;

        public int AddNode(Vector3 p)
        {
            Nodes.Add(p);
            Links.Add(new List<int>());
            return Nodes.Count - 1;
        }

        public void Link(int a, int b)
        {
            if (a == b) return;
            if (!Links[a].Contains(b)) Links[a].Add(b);
            if (!Links[b].Contains(a)) Links[b].Add(a);
        }

        /// True when a kid standing at p would not be inside anything solid.
        public static bool SpotIsFree(Vector3 p)
        {
            return !Physics.CheckCapsule(p + Vector3.up * 0.55f, p + Vector3.up * 1.5f, KidRadius,
                Shapes.WalkMask, QueryTriggerInteraction.Ignore);
        }

        /// True when a kid could walk in a straight line from a to b without bumping into anything.
        public static bool PathIsClear(Vector3 a, Vector3 b)
        {
            Vector3 from = a + Vector3.up * 1f;
            Vector3 to = b + Vector3.up * 1f;
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.01f) return true;
            return !Physics.SphereCast(from, KidRadius * 0.9f, d / len, out _, len,
                Shapes.WalkMask, QueryTriggerInteraction.Ignore);
        }

        /// Fill a rectangle of ground (y = 0) with stones every `spacing` meters.
        /// `keep` (optional) throws away stones outside the play area.
        public void BuildGrid(float minX, float maxX, float minZ, float maxZ, float spacing, System.Func<Vector3, bool> keep = null)
        {
            Physics.SyncTransforms();
            int cols = Mathf.FloorToInt((maxX - minX) / spacing) + 1;
            int rows = Mathf.FloorToInt((maxZ - minZ) / spacing) + 1;
            var grid = new int[cols, rows];
            for (int x = 0; x < cols; x++)
                for (int z = 0; z < rows; z++)
                {
                    var p = new Vector3(minX + x * spacing, 0f, minZ + z * spacing);
                    grid[x, z] = (keep == null || keep(p)) && SpotIsFree(p) ? AddNode(p) : -1;
                }

            // Connect each stone to its 8 neighbors when the way is clear.
            for (int x = 0; x < cols; x++)
                for (int z = 0; z < rows; z++)
                {
                    int a = grid[x, z];
                    if (a < 0) continue;
                    for (int dx = 0; dx <= 1; dx++)
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz <= 0) continue;
                            int nx = x + dx, nz = z + dz;
                            if (nx >= cols || nz < 0 || nz >= rows) continue;
                            int b = grid[nx, nz];
                            if (b < 0) continue;
                            if (PathIsClear(Nodes[a], Nodes[b])) Link(a, b);
                        }
                }
        }

        /// Add a hand-placed stone (for ramps and treehouse floors) and link it to nearby ground stones it can see.
        public int AddAndConnect(Vector3 p, float reach)
        {
            int id = AddNode(p);
            for (int i = 0; i < Nodes.Count - 1; i++)
            {
                if (Mathf.Abs(Nodes[i].y - p.y) > 0.3f) continue;
                if ((Nodes[i] - p).sqrMagnitude > reach * reach) continue;
                if (PathIsClear(Nodes[i], p)) Link(i, id);
            }
            return id;
        }

        /// The closest stone that a kid at `p` can walk straight to.
        public int Nearest(Vector3 p)
        {
            int best = -1;
            float bestD = float.MaxValue;
            int fallback = -1;
            float fallbackD = float.MaxValue;
            for (int i = 0; i < Nodes.Count; i++)
            {
                float d = (Nodes[i] - p).sqrMagnitude;
                if (d < fallbackD) { fallbackD = d; fallback = i; }
                if (d < bestD && d < 15f * 15f && Mathf.Abs(Nodes[i].y - p.y) < 2.5f && PathIsClear(p, Nodes[i]))
                {
                    bestD = d; best = i;
                }
            }
            return best >= 0 ? best : fallback;
        }

        /// The shortest walkable route from `from` to `to`, as a list of points ending at `to`.
        public List<Vector3> FindPath(Vector3 from, Vector3 to)
        {
            var result = new List<Vector3>();
            if (Nodes.Count == 0 || PathIsClear(from, to) && Mathf.Abs(from.y - to.y) < 0.5f)
            {
                result.Add(to);
                return result;
            }
            int start = Nearest(from);
            int goal = Nearest(to);
            if (start < 0 || goal < 0) { result.Add(to); return result; }

            // A* search: always expand the stone that looks closest to the goal.
            int n = Nodes.Count;
            var cost = new float[n];
            var cameFrom = new int[n];
            var closed = new bool[n];
            for (int i = 0; i < n; i++) { cost[i] = float.MaxValue; cameFrom[i] = -1; }
            cost[start] = 0f;
            var open = new List<int> { start };
            while (open.Count > 0)
            {
                int bestIdx = 0;
                float bestScore = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    int o = open[i];
                    float score = cost[o] + Vector3.Distance(Nodes[o], Nodes[goal]);
                    if (score < bestScore) { bestScore = score; bestIdx = i; }
                }
                int cur = open[bestIdx];
                open.RemoveAt(bestIdx);
                if (cur == goal) break;
                if (closed[cur]) continue;
                closed[cur] = true;
                foreach (int next in Links[cur])
                {
                    if (closed[next]) continue;
                    float c = cost[cur] + Vector3.Distance(Nodes[cur], Nodes[next]);
                    if (c < cost[next])
                    {
                        cost[next] = c;
                        cameFrom[next] = cur;
                        open.Add(next);
                    }
                }
            }

            if (cameFrom[goal] < 0 && goal != start)
            {
                // No route found. Just head straight there and hope.
                result.Add(to);
                return result;
            }
            for (int at = goal; at >= 0; at = cameFrom[at]) result.Add(Nodes[at]);
            result.Reverse();
            result.Add(to);
            return result;
        }
    }
}
