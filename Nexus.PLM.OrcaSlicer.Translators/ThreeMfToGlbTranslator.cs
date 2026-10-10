using System.Text;
using System.Text.Json;
using Nexus.PLM.Addin.Sdk.Translators;

namespace Nexus.PLM.OrcaSlicer.Translators;

/// <summary>
/// Re-encodes a 3MF's mesh as binary glTF 2.0, the format the Nexus web 3D viewer renders —
/// which makes this the most useful of the repo's examples: a checked-in Orca project becomes
/// viewable in the browser with no slicer installed anywhere.
/// </summary>
public sealed class ThreeMfToGlbTranslator : ITranslator
{
    /// <inheritdoc/>
    public string Key => "3mf_to_glb";

    /// <inheritdoc/>
    public string Label => "3MF → 3D (glTF)";

    /// <inheritdoc/>
    public string Description =>
        "Re-encodes a 3MF project's mesh as binary glTF 2.0 for the web 3D viewer. Pure .NET — no slicer needed on the server.";

    /// <inheritdoc/>
    public string InputMimeType => MimeTypes.ThreeMf;

    /// <inheritdoc/>
    public string OutputMimeType => MimeTypes.GltfBinary;

    /// <inheritdoc/>
    public string OutputFileExtension => ".glb";

    /// <inheritdoc/>
    public Task<Stream> TranslateAsync(Stream input, CancellationToken cancellationToken = default)
    {
        var mesh = ThreeMfMesh.Load(input);
        return Task.FromResult<Stream>(new MemoryStream(WriteGlb(mesh)));
    }

    private static byte[] WriteGlb(ThreeMfMesh mesh)
    {
        // Binary buffer: uint32 indices, then float32 positions (both naturally 4-aligned).
        var indexBytes = new byte[mesh.Triangles.Count * 3 * 4];
        var offset = 0;
        foreach (var (a, b, c) in mesh.Triangles)
            foreach (var i in (ReadOnlySpan<int>)[a, b, c])
            {
                BitConverter.TryWriteBytes(indexBytes.AsSpan(offset), (uint)i);
                offset += 4;
            }

        var positionBytes = new byte[mesh.Vertices.Count * 3 * 4];
        offset = 0;
        var min = new[] { float.MaxValue, float.MaxValue, float.MaxValue };
        var max = new[] { float.MinValue, float.MinValue, float.MinValue };
        foreach (var (x, y, z) in mesh.Vertices)
        {
            var p = (ReadOnlySpan<float>)[x, y, z];
            for (var axis = 0; axis < 3; axis++)
            {
                BitConverter.TryWriteBytes(positionBytes.AsSpan(offset), p[axis]);
                offset += 4;
                min[axis] = Math.Min(min[axis], p[axis]);
                max[axis] = Math.Max(max[axis], p[axis]);
            }
        }

        var bin = new byte[indexBytes.Length + positionBytes.Length];
        indexBytes.CopyTo(bin, 0);
        positionBytes.CopyTo(bin, indexBytes.Length);

        var gltf = new
        {
            asset = new { version = "2.0", generator = "Nexus PLM 3mf_to_glb" },
            scene = 0,
            scenes = new[] { new { nodes = new[] { 0 } } },
            nodes = new[] { new { mesh = 0 } },
            meshes = new[] { new { primitives = new[] { new { attributes = new { POSITION = 1 }, indices = 0 } } } },
            buffers = new[] { new { byteLength = bin.Length } },
            bufferViews = new object[]
            {
                new { buffer = 0, byteOffset = 0, byteLength = indexBytes.Length, target = 34963 },
                new { buffer = 0, byteOffset = indexBytes.Length, byteLength = positionBytes.Length, target = 34962 },
            },
            accessors = new object[]
            {
                new { bufferView = 0, componentType = 5125, count = mesh.Triangles.Count * 3, type = "SCALAR" },
                new { bufferView = 1, componentType = 5126, count = mesh.Vertices.Count, type = "VEC3", min, max },
            },
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(gltf);
        var jsonPadded = Pad(json, 0x20);
        var binPadded = Pad(bin, 0x00);

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        w.Write(0x46546C67u);                                     // 'glTF'
        w.Write(2u);                                              // version
        w.Write((uint)(12 + 8 + jsonPadded.Length + 8 + binPadded.Length));
        w.Write((uint)jsonPadded.Length); w.Write(0x4E4F534Au);   // 'JSON'
        w.Write(jsonPadded);
        w.Write((uint)binPadded.Length); w.Write(0x004E4942u);    // 'BIN'
        w.Write(binPadded);
        w.Flush();
        return ms.ToArray();
    }

    private static byte[] Pad(byte[] bytes, byte filler)
    {
        var rem = bytes.Length % 4;
        if (rem == 0) return bytes;
        var padded = new byte[bytes.Length + (4 - rem)];
        bytes.CopyTo(padded, 0);
        for (var i = bytes.Length; i < padded.Length; i++) padded[i] = filler;
        return padded;
    }
}
