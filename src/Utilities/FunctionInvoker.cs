using System.Text.Encodings.Web;
using System.Text.Json;

namespace ChatAIze.GenerativeCS.Utilities;

internal static class FunctionInvoker
{
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    internal static async Task<string> InvokeAsync(Delegate callback, string? arguments, CancellationToken cancellationToken = default)
    {
        var parsedArguments = new List<object?>();
        using var argumentsDocument = arguments != null ? JsonDocument.Parse(arguments) : JsonDocument.Parse("{}");

        foreach (var parameter in callback.Method.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                parsedArguments.Add(cancellationToken);
                continue;
            }

            if (argumentsDocument.RootElement.TryGetProperty(parameter.Name!, out var argument) && argument.ValueKind != JsonValueKind.Null)
            {
                var rawValue = argument.GetRawText();
                var stringValue = argument.ValueKind == JsonValueKind.String ? argument.GetString() : rawValue;

                try
                {
                    if (parameter.ParameterType.IsEnum)
                    {
                        if (!Enum.TryParse(parameter.ParameterType, stringValue, true, out var enumValue))
                        {
                            return JsonSerializer.Serialize(new { Error = $"Value '{stringValue}' is not a valid enum member for parameter '{parameter.Name}'." }, JsonOptions);
                        }

                        parsedArguments.Add(enumValue);
                    }
                    else
                    {
                        var parsedValue = JsonSerializer.Deserialize(rawValue, parameter.ParameterType, JsonOptions);
                        parsedArguments.Add(parsedValue);
                    }
                }
                catch
                {
                    return JsonSerializer.Serialize(new { Error = $"Value '{stringValue}' is not valid for parameter '{parameter.Name}'. Expected type: '{parameter.ParameterType.Name}'." }, JsonOptions);
                }
            }
            else if (parameter.IsOptional && parameter.DefaultValue != DBNull.Value)
            {
                parsedArguments.Add(parameter.DefaultValue);
            }
            else
            {
                return JsonSerializer.Serialize(new { Error = $"You must provide a value for the required parameter '{parameter.Name}'." }, JsonOptions);
            }
        }

        var invocationResult = callback.DynamicInvoke([.. parsedArguments]);
        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);

            var taskResultProperty = task.GetType().GetProperty("Result");
            if (taskResultProperty != null)
            {
                invocationResult = taskResultProperty.GetValue(task);
            }
        }

        if (invocationResult == null)
        {
            return JsonSerializer.Serialize(new { IsSuccess = true }, JsonOptions);
        }

        return JsonSerializer.Serialize(invocationResult, JsonOptions);
    }
}
