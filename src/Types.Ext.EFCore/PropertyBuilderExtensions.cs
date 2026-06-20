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

    public static OwnedNavigationBuilder<T, LabColor> OwnLabColor<T>(this OwnedNavigationBuilder<T, LabColor> builder) where T : class {
        builder.Property(l => l.L).HasColumnType("double precision");
        builder.Property(l => l.A).HasColumnType("double precision");
        builder.Property(l => l.B).HasColumnType("double precision");
        return builder;
    }
}
