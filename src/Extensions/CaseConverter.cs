using System.Text;
using System.Text.RegularExpressions;

namespace ChatAIze.GenerativeCS.Extensions;

public static partial class CaseConverter
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        input = MultipleSpacesRegex().Replace(input.Trim(), "_");

        var sb = new StringBuilder();
        var isPreviousUnderscore = false;
        var isPreviousUppercase = false;

        for (var i = 0; i < input.Length; i++)
        {
            var currentChar = input[i];

            if (char.IsUpper(currentChar))
            {
                if (i > 0 && !isPreviousUppercase && !isPreviousUnderscore)
                {
                    _ = sb.Append('_');
                }

                _ = sb.Append(char.ToLowerInvariant(currentChar));

                isPreviousUppercase = true;
                isPreviousUnderscore = false;
            }
            else
            {
                if (currentChar == '_')
                {
                    if (!isPreviousUnderscore)
                    {
                        _ = sb.Append(currentChar);
                        isPreviousUnderscore = true;
                    }
                }
                else
                {
                    _ = sb.Append(currentChar);
                    isPreviousUnderscore = false;
                }

                isPreviousUppercase = false;
            }
        }

        return sb.ToString();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesRegex();
}
