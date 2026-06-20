using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shiron.Lib.Types;

namespace Shiron.Lib.Types.Ext.EFCore;

/// <summary>
/// Converts a <see cref="LabColor"/>[] palette to a compact JSON array of
/// <c>[L, A, B]</c> triples for storage in a <c>jsonb</c> column. The triple
/// format mirrors <see cref="LabColorValueConverter"/> (single color &#8594; double[]).
/// </summary>
public class LabColorArrayValueConverter : ValueConverter<LabColor[], string> {
    public LabColorArrayValueConverter() : base(
        colors => JsonSerializer.Serialize(colors.Select(l => new[] { l.L, l.A, l.B }).ToArray()),
        json => JsonSerializer.Deserialize<double[][]>(json)!
            .Select(d => new LabColor(d[0], d[1], d[2])).ToArray()
    ) { }
}
