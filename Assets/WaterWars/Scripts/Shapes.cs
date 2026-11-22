// Shapes.cs
// ---------------------------------------------------------------------------
// Little helpers for making grey-box shapes (boxes, balls, ramps) in code and
// giving them simple colors. Grey-boxing means building a level out of plain
// blocks first, to test how it plays before anyone makes real art. When Ian's
// Blender models are ready, they replace these blocks.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace WaterWars
{
    public static class Shapes
    {
        // Unity's "Ignore Raycast" layer is built in (number 2). Kids' movement
        // capsules live on it so water shots pass through them and hit the
        // head/body/legs zones inside instead.
        public const int IgnoreRaycastLayer = 2;

        /// Layers that water and line-of-sight checks can hit (everything except Ignore Raycast).
        public static int ShotMask { get { return ~(1 << IgnoreRaycastLayer); } }

        static Material baseMaterial;
        static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

        /// A plain colored material. We copy Unity's default material (the one
        /// every new cube gets) so this works in any build without extra setup.
        public static Material Mat(Color color)
        {
            Material m;
            if (cache.TryGetValue(color, out m) && m != null) return m;
            if (baseMaterial == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseMaterial = temp.GetComponent<Renderer>().sharedMaterial;
                Object.DestroyImmediate(temp);
            }
            m = new Material(baseMaterial);
            m.color = color;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
            cache[color] = m;
            return m;
        }

        /// A unique (not shared) material, for things whose color changes, like kids getting wetter.
        public static Material UniqueMat(Color color)
        {
            var m = new Material(Mat(color));
            m.color = color;
            return m;
        }

        /// A solid box you can stand on and hide behind.
        public static GameObject Box(string name, Vector3 center, Vector3 size, Color color, Transform parent, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// A box whose bottom sits on the given point (easier for walls and houses).
        public static GameObject BoxOnGround(string name, Vector3 bottomCenter, Vector3 size, Color color, Transform parent, bool solid = true)
        {
            return Box(name, bottomCenter + Vector3.up * size.y * 0.5f, size, color, parent, solid);
        }

        /// A sloped plank from one point up to another (used for treehouse ramps).
        public static GameObject Ramp(string name, Vector3 bottom, Vector3 top, float width, Color color, Transform parent)
        {
            var dir = top - bottom;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = (bottom + top) * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            go.transform.localScale = new Vector3(width, 0.25f, dir.magnitude);
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        /// A ball with no collider (for decoration and effects).
        public static GameObject Ball(string name, Vector3 center, float diameter, Color color, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// Destroys an object's collider right away (used for purely visual pieces).
        public static void RemoveCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
    }
}
