using System.Globalization;
using System.Text;
using Nexus.PLM.Addin.Sdk.Translators;

namespace Nexus.PLM.OrcaSlicer.Translators;

/// <summary>Re-encodes a 3MF's mesh as a Wavefront OBJ — pure .NET, no slicer needed.</summary>
public sealed class ThreeMfToObjTranslator : ITranslator
{
    /// <inheritdoc/>
    public string Key => "3mf_to_obj";

    /// <inheritdoc/>
    public string Label => "3MF → OBJ";

    /// <inheritdoc/>
    public string Description =>
        "Re-encodes a 3MF project's mesh (all build items flattened) as a Wavefront OBJ. Pure .NET — no slicer needed on the server.";

    /// <inheritdoc/>
    public string InputMimeType => MimeTypes.ThreeMf;

    /// <inheritdoc/>
    public string OutputMimeType => MimeTypes.Obj;

    /// <inheritdoc/>
    public string OutputFileExtension => ".obj";

    /// <inheritdoc/>
    public Task<Stream> TranslateAsync(Stream input, CancellationToken cancellationToken = default)
    {
        var mesh = ThreeMfMesh.Load(input);

        var sb = new StringBuilder("# Converted from 3MF by Nexus PLM\n");
        foreach (var (x, y, z) in mesh.Vertices)
            sb.Append(CultureInfo.InvariantCulture, $"v {x} {y} {z}\n");
        foreach (var (a, b, c) in mesh.Triangles)
            sb.Append(CultureInfo.InvariantCulture, $"f {a + 1} {b + 1} {c + 1}\n");

        return Task.FromResult<Stream>(new MemoryStream(Encoding.ASCII.GetBytes(sb.ToString())));
    }
}
