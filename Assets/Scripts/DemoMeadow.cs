using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class DemoMeadow : MonoBehaviour
{
    private enum Paint
    {
        Grass, GrassLight, GrassCool, GrassGold, Lawn, LawnDark,
        Sand, Water, WaterLight, Foam, Shadow, Stone, StoneLight,
        Leaf, LeafLight, LeafDark, Bark, Straw, StrawLight, Nest,
        Petal, Lilac, Gold, Cream, Mint
    }

    private static readonly uint[] Palette =
    {
        0x89AD78, 0x94B681, 0x81A676, 0xA1B782, 0x6E986E, 0x4D7D64,
        0xD3BE86, 0x659B9B, 0x8CB8AD, 0xD5E0C1, 0x628A67, 0x80998C,
        0xABC0A2, 0x477C65, 0x699D75, 0x365E51, 0x745E4B, 0xAF8054,
        0xD2A269, 0x8E674F, 0xEAA995, 0xB9AFCC, 0xF4CD7D, 0xF8EBC1, 0xDCF3B6
    };

    private readonly List<Mesh> meshes = new List<Mesh>();
    private Material[] materials;
    private System.Random random;
    private GameObject[] chickSignals;
    private Transform[] chickPulses;
    private GameObject homeSignal;
    private Transform homePulse;

    public void Build(Vector3 home, Vector3[] chicks)
    {
        random = new System.Random(48931);
        CreateMaterials();
        CreateLighting();

        var meadow = new Batch(Palette.Length);
        AddTerrain(meadow);
        AddRiver(meadow);
        AddPlants(meadow, home, chicks);
        AddNest(meadow, OnGround(home));

        chickSignals = new GameObject[chicks.Length];
        chickPulses = new Transform[chicks.Length];
        for (int index = 0; index < chicks.Length; index++)
        {
            Vector3 perch = OnGround(chicks[index]);
            AddPerch(meadow, perch);
            chickSignals[index] = CreateSignal("Chick perch signal", perch, .82f, Paint.Cream, out chickPulses[index]);
        }

        homeSignal = CreateSignal("Return to nest signal", OnGround(home), 1.72f, Paint.Mint, out homePulse);
        SetHomeReady(false);
        Bake(meadow, "Meadow geometry", transform);
    }

    public void SetChickWaiting(int index, bool waiting)
    {
        chickSignals[index].SetActive(waiting);
    }

    public void SetHomeReady(bool ready)
    {
        homeSignal.SetActive(ready);
    }

    private void Update()
    {
        if (chickPulses == null)
            return;

        float phase = Time.time * 2.8f;
        for (int index = 0; index < chickPulses.Length; index++)
        {
            float pulse = 1.04f + .08f * Mathf.Sin(phase + index * .85f);
            chickPulses[index].localScale = new Vector3(pulse, 1f, pulse);
        }

        float nestPulse = 1.08f + .07f * Mathf.Sin(phase);
        homePulse.localScale = new Vector3(nestPulse, 1f, nestPulse);
    }

    private void CreateMaterials()
    {
        Shader shader = Shader.Find("Standard");
        materials = new Material[Palette.Length];
        for (int index = 0; index < Palette.Length; index++)
        {
            uint hex = Palette[index];
            var color = new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 255);
            var material = new Material(shader) { name = ((Paint)index).ToString(), color = color };
            material.SetFloat("_Glossiness", .06f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.SetFloat("_GlossyReflections", 0f);
            material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            if ((Paint)index == Paint.Cream || (Paint)index == Paint.Mint)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", (Color)color * .24f);
            }
            materials[index] = material;
        }
    }

    private void CreateLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.70f, .78f, .73f);
        RenderSettings.ambientEquatorColor = new Color(.56f, .63f, .57f);
        RenderSettings.ambientGroundColor = new Color(.41f, .39f, .32f);
        RenderSettings.reflectionIntensity = .25f;

        var sun = new GameObject("Meadow sunlight").AddComponent<Light>();
        sun.transform.SetParent(transform, false);
        sun.transform.rotation = Quaternion.Euler(42f, -38f, 0f);
        sun.type = LightType.Directional;
        sun.color = new Color(1f, .89f, .72f);
        sun.intensity = .9f;
    }

    private void AddTerrain(Batch art)
    {
        const int columns = 14;
        const int rows = 10;
        var points = new Vector3[columns + 1, rows + 1];
        for (int z = 0; z <= rows; z++)
        for (int x = 0; x <= columns; x++)
        {
            float xWobble = x == 0 || x == columns ? 0f : Range(-1.3f, 1.3f);
            float zWobble = z == 0 || z == rows ? 0f : Range(-1.1f, 1.1f);
            points[x, z] = new Vector3(x * 6f - 42f + xWobble, 0f, z * 5f - 25f + zWobble);
        }

        for (int z = 0; z < rows; z++)
        for (int x = 0; x < columns; x++)
        {
            Vector3 a = points[x, z];
            Vector3 b = points[x + 1, z];
            Vector3 c = points[x + 1, z + 1];
            Vector3 d = points[x, z + 1];
            art.Triangle(GrassPaint(), a, d, b);
            art.Triangle(GrassPaint(), d, c, b);
        }
    }

    private Paint GrassPaint()
    {
        float shade = Range(0f, 1f);
        if (shade < .50f) return Paint.Grass;
        if (shade < .72f) return Paint.GrassLight;
        if (shade < .91f) return Paint.GrassCool;
        return Paint.GrassGold;
    }

    private void AddRiver(Batch art)
    {
        AddRiverBand(art, 1.65f, .015f, Paint.Lawn);
        AddRiverBand(art, 1.43f, .025f, Paint.Sand);
        AddRiverBand(art, 1.27f, .035f, Paint.WaterLight);
        AddRiverBand(art, 1.03f, .045f, Paint.Water);

        for (int i = 0; i < 43; i++)
        {
            float z = Range(-20f, 20f);
            float x = RiverCourse(z) + Range(-.85f, .85f);
            float length = Range(.09f, .41f);
            Disc(art, Paint.Foam, new Vector3(x, .052f, z), .035f, length, 5, -.31f);
        }
    }

    private void AddRiverBand(Batch art, float width, float lift, Paint color)
    {
        for (int z = -25; z < 25; z++)
        {
            float x0 = RiverCourse(z);
            float x1 = RiverCourse(z + 1);
            art.Quad(color, new Vector3(x0 - width, lift, z), new Vector3(x1 - width, lift, z + 1),
                new Vector3(x1 + width, lift, z + 1), new Vector3(x0 + width, lift, z));
        }
    }

    private static float RiverCourse(float z)
    {
        return 11.1f + 1.18f * Mathf.Sin(z * .29f - .7f) + .065f * z;
    }

    private void AddPlants(Batch art, Vector3 home, Vector3[] chicks)
    {
        for (int i = 0; i < 175; i++)
        {
            var point = new Vector3(Range(-25f, 24f), 0f, Range(-14f, 17f));
            if (IsNearLandmark(point, home, chicks) || Mathf.Abs(point.x - RiverCourse(point.z)) < 1.6f)
                continue;

            bool fringe = Mathf.Abs(point.x) > 11f || Mathf.Abs(point.z) > 7.1f;
            if (fringe && Range(0f, 1f) < .33f)
                AddThicket(art, point);
            else
                AddTuft(art, point, fringe);
        }

        for (int i = 0; i < 28; i++)
        {
            float z = Range(-15f, 19f);
            float x = RiverCourse(z) + (i % 2 == 0 ? -1.65f : 1.65f);
            var point = new Vector3(x, 0f, z);
            if (!IsNearLandmark(point, home, chicks))
                AddTuft(art, point, true);
        }

        AddGrove(art, new Vector3(-10.2f, 0f, 5.1f), 1.45f);
        AddGrove(art, new Vector3(-6.3f, 0f, 8.1f), 1.15f);
        AddGrove(art, new Vector3(3.1f, 0f, 7.3f), 1.22f);
        AddGrove(art, new Vector3(-11.4f, 0f, -6.9f), 1.06f);
        AddGrove(art, new Vector3(13.9f, 0f, 4.1f), 1.6f);
    }

    private static bool IsNearLandmark(Vector3 point, Vector3 home, Vector3[] chicks)
    {
        if ((point - OnGround(home)).sqrMagnitude < 5f)
            return true;

        foreach (Vector3 chick in chicks)
        {
            if ((point - OnGround(chick)).sqrMagnitude < 2.5f)
                return true;
        }
        return false;
    }

    private void AddGrove(Batch art, Vector3 root, float size)
    {
        Disc(art, Paint.Shadow, root + new Vector3(.53f, .02f, .31f) * size, 1.38f * size, .84f * size, 12, .36f);
        for (int i = 0; i < 3; i++)
        {
            Vector3 foot = root + new Vector3((i - 1) * .73f, 0f, i % 2 * .54f) * size;
            float scale = Range(.63f, 1.05f) * size;
            Vector3 canopy = foot + new Vector3(.18f, .97f, 0f) * scale;
            Beam(art, Paint.Bark, foot, canopy, scale * .095f);
            Mound(art, Paint.Leaf, Paint.LeafDark, canopy, new Vector3(.68f, 1.12f, .59f) * scale, 7, (float)i);
            Mound(art, Paint.LeafLight, Paint.Leaf, canopy + new Vector3(-.35f, .08f, -.22f) * scale,
                new Vector3(.47f, .69f, .42f) * scale, 6, i * .43f);
        }
    }

    private void AddThicket(Batch art, Vector3 point)
    {
        float size = Range(.38f, 1.2f);
        Disc(art, Paint.Shadow, point + new Vector3(.12f, .009f, .08f), size * 1.12f, size * .89f, 9, 0f);
        for (int i = 0; i < 3; i++)
        {
            float scale = Range(.28f, .56f) * size;
            Vector3 tuft = point + new Vector3(Range(-.42f, .42f), .01f, Range(-.46f, .46f)) * size;
            Mound(art, i == 0 ? Paint.StoneLight : Paint.LeafLight, i == 0 ? Paint.Stone : Paint.Leaf,
                tuft, new Vector3(scale, scale * 1.12f, scale * .85f), 6, Range(0f, 6f));
        }
    }

    private void AddTuft(Batch art, Vector3 point, bool fringe)
    {
        float scale = Range(.65f, 1.25f) * (fringe ? 1.15f : .77f);
        float angle = Range(0f, Mathf.PI);
        Paint petal = (Paint)((int)Paint.Petal + random.Next(3));
        for (int i = 0; i < 5; i++)
        {
            Vector3 basePoint = point + new Vector3(Range(-.25f, .25f), .012f, Range(-.23f, .23f)) * scale;
            Vector3 side = new Vector3(Mathf.Cos(angle + i * 1.3f), 0f, Mathf.Sin(angle + i * 1.3f));
            Vector3 tip = basePoint + Vector3.up * Range(.12f, .35f) * scale + side * .035f;
            art.DoubleTriangle(i % 2 == 0 ? Paint.Lawn : Paint.LawnDark, basePoint - side * .03f, tip, basePoint + side * .03f);
            if (i < 2 && Range(0f, 1f) < .52f)
                Mound(art, petal, petal, tip, new Vector3(.051f, .039f, .051f) * scale, 5, angle);
        }
    }

    private void AddNest(Batch art, Vector3 center)
    {
        Disc(art, Paint.Shadow, center + new Vector3(.12f, .006f, .10f), 1.75f, 1.53f, 26, 0f);
        Disc(art, Paint.Sand, center + Vector3.up * .013f, 1.64f, 1.43f, 26, 0f);
        Disc(art, Paint.Nest, center + Vector3.up * .065f, 1.41f, 1.22f, 24, 0f);
        Disc(art, Paint.StrawLight, center + Vector3.up * .085f, 1.10f, .94f, 24, 0f);
        Ring(art, Paint.Straw, center + Vector3.up * .094f, .84f, .035f, 24, true, .87f);
        for (int i = 0; i < 45; i++)
        {
            float angle = i * Mathf.PI * 2f / 45f;
            float radius = Range(1.05f, 1.50f);
            Vector3 twig = center + new Vector3(Mathf.Cos(angle) * radius, Range(.11f, .27f), Mathf.Sin(angle) * radius * .86f);
            Vector3 span = new Vector3(-Mathf.Sin(angle), Range(-.15f, .14f), Mathf.Cos(angle) * .86f) * Range(.22f, .48f);
            Paint straw = i % 3 == 0 ? Paint.StrawLight : Paint.Straw;
            Beam(art, straw, twig - span, twig + span, Range(.024f, .051f));
        }
    }

    private void AddPerch(Batch art, Vector3 center)
    {
        Disc(art, Paint.Shadow, center + new Vector3(.07f, .014f, .05f), .75f, .69f, 12, 0f);
        Disc(art, Paint.Sand, center + Vector3.up * .023f, .66f, .63f, 12, 0f);
        Mound(art, Paint.StoneLight, Paint.Stone, center + Vector3.up * .029f,
            new Vector3(.48f, .19f, .44f), 8, .19f);
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 2.16f + .57f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), .02f, Mathf.Sin(angle)) * .76f;
            AddTuft(art, center + offset, false);
        }
    }

    private GameObject CreateSignal(string label, Vector3 center, float radius, Paint color, out Transform pulse)
    {
        var signal = new GameObject(label);
        signal.transform.SetParent(transform, false);
        signal.transform.localPosition = center;

        var inner = new Batch(Palette.Length);
        Ring(inner, color, Vector3.up * .074f, radius, .045f, 36, false);
        Bake(inner, "Dashed ring", signal.transform);

        var outer = new Batch(Palette.Length);
        Ring(outer, color, Vector3.up * .079f, radius * 1.07f, .022f, 36, true);
        pulse = Bake(outer, "Pulse", signal.transform).transform;
        return signal;
    }

    private static void Disc(Batch art, Paint color, Vector3 center, float width, float depth, int sides, float rotation)
    {
        for (int i = 0; i < sides; i++)
        {
            float from = i * Mathf.PI * 2f / sides;
            float to = (i + 1) * Mathf.PI * 2f / sides;
            art.Triangle(color, center, OvalPoint(center, to, width, depth, rotation), OvalPoint(center, from, width, depth, rotation));
        }
    }

    private static void Ring(Batch art, Paint color, Vector3 center, float radius, float width, int sides, bool continuous, float depth = 1f)
    {
        float inner = radius - width * .5f;
        float outer = radius + width * .5f;
        for (int i = 0; i < sides; i++)
        {
            float from = i * Mathf.PI * 2f / sides;
            float to = (i + (continuous ? 1f : .53f)) * Mathf.PI * 2f / sides;
            art.Quad(color, OvalPoint(center, from, inner, inner * depth, 0f), OvalPoint(center, to, inner, inner * depth, 0f),
                OvalPoint(center, to, outer, outer * depth, 0f), OvalPoint(center, from, outer, outer * depth, 0f));
        }
    }

    private static Vector3 OvalPoint(Vector3 center, float angle, float width, float depth, float rotation)
    {
        float x = Mathf.Cos(angle) * width;
        float z = Mathf.Sin(angle) * depth;
        float cos = Mathf.Cos(rotation);
        float sin = Mathf.Sin(rotation);
        return center + new Vector3(x * cos - z * sin, 0f, x * sin + z * cos);
    }

    private static void Mound(Batch art, Paint topColor, Paint sideColor, Vector3 root, Vector3 size, int sides, float rotation)
    {
        Vector3 top = root + new Vector3(-size.x * .16f, size.y, -size.z * .14f);
        Vector3 shoulder = root + Vector3.up * size.y * .57f;
        Vector3 bottom = root - Vector3.up * size.y * .39f;
        for (int i = 0; i < sides; i++)
        {
            float from = rotation + i * Mathf.PI * 2f / sides;
            float to = rotation + (i + 1) * Mathf.PI * 2f / sides;
            Vector3 a = OvalPoint(shoulder, from, size.x, size.z, 0f);
            Vector3 b = OvalPoint(shoulder, to, size.x, size.z, 0f);
            Vector3 c = OvalPoint(bottom, to, size.x * .65f, size.z * .65f, 0f);
            Vector3 d = OvalPoint(bottom, from, size.x * .65f, size.z * .65f, 0f);
            art.Triangle(topColor, top, b, a);
            art.Quad(sideColor, a, b, c, d);
        }
    }

    private static void Beam(Batch art, Paint color, Vector3 start, Vector3 end, float radius)
    {
        Vector3 axis = (end - start).normalized;
        Vector3 side = Vector3.Cross(axis, Vector3.forward).normalized;
        if (side.sqrMagnitude < .1f)
            side = Vector3.Cross(axis, Vector3.up).normalized;
        Vector3 normal = Vector3.Cross(axis, side);
        const int sides = 5;
        for (int i = 0; i < sides; i++)
        {
            float from = i * Mathf.PI * 2f / sides;
            float to = (i + 1) * Mathf.PI * 2f / sides;
            Vector3 a = (side * Mathf.Cos(from) + normal * Mathf.Sin(from)) * radius;
            Vector3 b = (side * Mathf.Cos(to) + normal * Mathf.Sin(to)) * radius;
            art.Quad(color, start + a, start + b, end + b * .74f, end + a * .74f);
            art.Triangle(color, end, end + a * .74f, end + b * .74f);
            art.Triangle(color, start, start + b, start + a);
        }
    }

    private GameObject Bake(Batch art, string label, Transform parent)
    {
        var gameObject = new GameObject(label);
        gameObject.transform.SetParent(parent, false);
        var mesh = new Mesh { name = label };
        if (art.Vertices.Count > ushort.MaxValue)
            mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(art.Vertices);
        var activeMaterials = new List<Material>();
        for (int index = 0; index < Palette.Length; index++)
        {
            if (art.Faces[index].Count > 0)
                activeMaterials.Add(materials[index]);
        }

        mesh.subMeshCount = activeMaterials.Count;
        int submesh = 0;
        foreach (List<int> triangles in art.Faces)
        {
            if (triangles.Count > 0)
                mesh.SetTriangles(triangles, submesh++, false);
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.UploadMeshData(true);
        meshes.Add(mesh);
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = activeMaterials.ToArray();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return gameObject;
    }

    private static Vector3 OnGround(Vector3 point)
    {
        return new Vector3(point.x, 0f, point.z);
    }

    private float Range(float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in meshes)
            Destroy(mesh);
        if (materials == null)
            return;

        foreach (Material material in materials)
            Destroy(material);
    }

    private sealed class Batch
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<int>[] Faces;

        public Batch(int colorCount)
        {
            Faces = new List<int>[colorCount];
            for (int index = 0; index < colorCount; index++)
                Faces[index] = new List<int>();
        }

        public void Triangle(Paint paint, Vector3 a, Vector3 b, Vector3 c)
        {
            var indices = Faces[(int)paint];
            int first = Vertices.Count;
            Vertices.Add(a);
            Vertices.Add(b);
            Vertices.Add(c);
            indices.Add(first);
            indices.Add(first + 1);
            indices.Add(first + 2);
        }

        public void DoubleTriangle(Paint paint, Vector3 a, Vector3 b, Vector3 c)
        {
            Triangle(paint, a, b, c);
            Triangle(paint, a, c, b);
        }

        public void Quad(Paint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Triangle(paint, a, b, c);
            Triangle(paint, a, c, d);
        }
    }
}
