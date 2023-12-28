using System.Text.Json;

namespace GenerativeCS.Utilities;

public static class FunctionInvoker
{
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static async Task<object> InvokeAsync(Delegate callback, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var parsedArguments = new List<object?>();
        foreach (var parameter in callback.Method.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                parsedArguments.Add(cancellationToken);
                continue;
            }

            if (arguments.TryGetProperty(parameter.Name!, out var argument) && argument.ValueKind != JsonValueKind.Null)
            {
                var rawValue = argument.GetRawText();
                var stringValue = argument.ValueKind == JsonValueKind.String ? argument.GetString() : rawValue;

                try
                {
                    if (parameter.ParameterType.IsEnum)
                    {
                        if (!Enum.TryParse(parameter.ParameterType, stringValue, true, out var enumValue))
                        {
                            return new { Error = $"Value '{stringValue}' is not a valid enum member for parameter '{parameter.Name}'." };
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
                    return new { Error = $"Value '{stringValue}' is not valid for parameter '{parameter.Name}'. Expected type: '{parameter.ParameterType.Name}'" };
                }
            }
            else if (parameter.IsOptional && parameter.DefaultValue != null)
            {
                parsedArguments.Add(parameter.DefaultValue);
            }
            else
            {
                return new { Error = $"You have not provided a value for the required parameter '{parameter.Name}'." };
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
            return new { IsSuccess = true };
        }

        return invocationResult;
    }
}
