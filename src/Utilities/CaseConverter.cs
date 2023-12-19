using System.Text;

namespace GenerativeCS.Utilities;

internal static class CaseConverter
{
    internal static string ToSnakeCase(this string value)
    {
        var result = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && value[i - 1] != '_')
                {
                    _ = result.Append('_');
                }

                _ = result.Append(char.ToLower(c));
            }
            else
            {
                _ = result.Append(c);
            }
        }

        return result.ToString();
    }

    internal static string ToPascalCase(this string value)
    {
        var parts = value.Split('_');
        for (var i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrEmpty(parts[i]))
            {
                parts[i] = char.ToUpper(parts[i][0]) + parts[i][1..].ToLower();
            }
        }

        return string.Join("", parts);
    }
}
