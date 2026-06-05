using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blocks.EntityFrameworkCore.Extensions;

public static class EntityTypeBuilderExtensions
{
    public static void SeedFromJsonFile<T>(this EntityTypeBuilder<T> builder)
        where T : class
    {
        var fileName = $"{typeof(T).Name}.json";
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "Master", fileName);

        if (!File.Exists(filePath))
            return;

        var json = File.ReadAllText(filePath);
        var items = JsonSerializer.Deserialize<List<T>>(json);

        if (items is null || items.Count == 0)
            return;

        builder.HasData(items);
    }
}
