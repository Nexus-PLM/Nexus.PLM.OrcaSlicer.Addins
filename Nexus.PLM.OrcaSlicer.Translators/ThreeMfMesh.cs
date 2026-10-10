using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace Nexus.PLM.OrcaSlicer.Translators;

/// <summary>
/// Reads the mesh out of a 3MF file — pure .NET: a 3MF is an OPC zip whose model part is XML
/// (3MF core spec), so no slicer is needed on the server. All build items are flattened into
/// one vertex/triangle soup with their transforms applied, which is what a mesh re-encode
/// (OBJ, glTF) wants.
/// </summary>
public sealed class ThreeMfMesh
{
    private static readonly XNamespace Core =
        "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";

    /// <summary>Flattened vertices, in model units (3MF defaults to millimetres).</summary>
    public List<(float X, float Y, float Z)> Vertices { get; } = [];

    /// <summary>Triangles as indices into <see cref="Vertices"/>.</summary>
    public List<(int A, int B, int C)> Triangles { get; } = [];

    /// <summary>Parses the model part of the 3MF in <paramref name="input"/>.</summary>
    /// <exception cref="InvalidOperationException">Not a 3MF, or it carries no mesh.</exception>
    public static ThreeMfMesh Load(Stream input)
    {
        ZipArchive zip;
        try
        {
            zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException(
                "The input is not a 3MF file (not a zip archive). The 3mf_to_* translators read model/3mf content.", ex);
        }

        using (zip)
        {
            var entry = FindModelEntry(zip)
                ?? throw new InvalidOperationException(
                    "The input zip carries no 3D model part — not a 3MF file.");

            XDocument doc;
            using (var s = entry.Open()) doc = XDocument.Load(s);

            var model = doc.Root ?? throw new InvalidOperationException("Empty 3MF model part.");
            var resources = model.Element(Core + "resources")
                ?? throw new InvalidOperationException("The 3MF model part has no resources.");

            var objects = resources.Elements(Core + "object")
                .Where(o => o.Attribute("id") is not null)
                .ToDictionary(o => o.Attribute("id")!.Value);

            var mesh = new ThreeMfMesh();
            var build = model.Element(Core + "build");
            var items = build?.Elements(Core + "item").ToList() ?? [];

            if (items.Count > 0)
            {
                foreach (var item in items)
                {
                    var id = item.Attribute("objectid")?.Value;
                    if (id is not null && objects.TryGetValue(id, out var obj))
                        mesh.Append(obj, objects, Matrix.Parse(item.Attribute("transform")?.Value));
                }
            }
            else
            {
                // No build section: take every mesh object as-is.
                foreach (var obj in objects.Values)
                    mesh.Append(obj, objects, Matrix.Identity);
            }

            if (mesh.Triangles.Count == 0)
                throw new InvalidOperationException("The 3MF contains no triangles.");
            return mesh;
        }
    }

    private static ZipArchiveEntry? FindModelEntry(ZipArchive zip) =>
        // Every slicer writes the conventional part name; fall back to any .model part.
        zip.GetEntry("3D/3dmodel.model")
        ?? zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".model", StringComparison.OrdinalIgnoreCase));

    private void Append(XElement obj, Dictionary<string, XElement> objects, Matrix transform, int depth = 0)
    {
        if (depth > 8) return; // cycles in component references are malformed input, not our stack

        var meshElement = obj.Element(Core + "mesh");
        if (meshElement is not null)
        {
            var baseIndex = Vertices.Count;
            foreach (var v in meshElement.Element(Core + "vertices")?.Elements(Core + "vertex") ?? [])
            {
                var p = transform.Apply(
                    F(v.Attribute("x")), F(v.Attribute("y")), F(v.Attribute("z")));
                Vertices.Add(p);
            }
            foreach (var t in meshElement.Element(Core + "triangles")?.Elements(Core + "triangle") ?? [])
            {
                Triangles.Add((
                    baseIndex + I(t.Attribute("v1")),
                    baseIndex + I(t.Attribute("v2")),
                    baseIndex + I(t.Attribute("v3"))));
            }
        }

        foreach (var component in obj.Element(Core + "components")?.Elements(Core + "component") ?? [])
        {
            var id = component.Attribute("objectid")?.Value;
            if (id is not null && objects.TryGetValue(id, out var referenced))
                Append(referenced, objects,
                    Matrix.Parse(component.Attribute("transform")?.Value).Then(transform), depth + 1);
        }
    }

    private static float F(XAttribute? a) =>
        float.Parse(a?.Value ?? "0", CultureInfo.InvariantCulture);

    private static int I(XAttribute? a) =>
        int.Parse(a?.Value ?? "0", CultureInfo.InvariantCulture);

    /// <summary>A 3MF affine transform: 4 rows × 3 columns, row-major, last row translation.</summary>
    private readonly struct Matrix(float[] m)
    {
        public static Matrix Identity { get; } = new([1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0]);

        public static Matrix Parse(string? transform)
        {
            if (string.IsNullOrWhiteSpace(transform)) return Identity;
            var parts = transform.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 12)
                throw new InvalidOperationException($"Malformed 3MF transform '{transform}'.");
            return new Matrix([.. parts.Select(p => float.Parse(p, CultureInfo.InvariantCulture))]);
        }

        public (float, float, float) Apply(float x, float y, float z) => (
            x * m[0] + y * m[3] + z * m[6] + m[9],
            x * m[1] + y * m[4] + z * m[7] + m[10],
            x * m[2] + y * m[5] + z * m[8] + m[11]);

        /// <summary>This transform followed by <paramref name="outer"/>.</summary>
        public Matrix Then(Matrix outer)
        {
            var a = m; var b = outer.m;
            var r = new float[12];
            for (var row = 0; row < 4; row++)
                for (var col = 0; col < 3; col++)
                {
                    r[row * 3 + col] =
                        a[row * 3 + 0] * b[0 * 3 + col] +
                        a[row * 3 + 1] * b[1 * 3 + col] +
                        a[row * 3 + 2] * b[2 * 3 + col] +
                        (row == 3 ? b[3 * 3 + col] : 0);
                }
            return new Matrix(r);
        }

        private readonly float[] m = m;
    }
}
