using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shiron.Lib.Types;

namespace Shiron.Lib.Types.Ext.EFCore;

public static class PropertyBuilderExtensions {
    public static PropertyBuilder<Color32> IsColor32(this PropertyBuilder<Color32> builder) {
        builder.HasConversion(
            new Color32ValueConverter()
        );
        return builder;
    }

    /// <summary>
    /// Applies the <see cref="Color32ArrayValueConverter"/> for a palette stored
    /// as <c>jsonb</c>. Pair with <c>HasColumnType("jsonb")</c> on the property.
    /// </summary>
    public static PropertyBuilder<Color32[]> IsColor32Array(this PropertyBuilder<Color32[]> builder) {
        builder.HasConversion(new Color32ArrayValueConverter());
        return builder;
    }

    /// <summary>
    /// Applies the <see cref="LabColorArrayValueConverter"/> for a palette stored
    /// as <c>jsonb</c>. Pair with <c>HasColumnType("jsonb")</c> on the property.
    /// </summary>
    public static PropertyBuilder<LabColor[]> IsLabColorArray(this PropertyBuilder<LabColor[]> builder) {
        builder.HasConversion(new LabColorArrayValueConverter());
        return builder;
    }

    public static OwnedNavigationBuilder<T, LabColor> OwnLabColor<T>(this OwnedNavigationBuilder<T, LabColor> builder) where T : class {
        builder.Property(l => l.L).HasColumnType("double precision");
        builder.Property(l => l.A).HasColumnType("double precision");
        builder.Property(l => l.B).HasColumnType("double precision");
        return builder;
    }
}
