using System.Text.Json;

namespace GenerativeCS.Utilities;

public static class FunctionInvoker
{
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static async Task<object> InvokeAsync(Delegate function, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var parsedArguments = new List<object?>();
        foreach (var parameter in function.Method.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                parsedArguments.Add(cancellationToken);
                continue;
            }

            if (arguments.TryGetProperty(parameter.Name!, out var argument) && argument.ValueKind != JsonValueKind.Null)
            {
                var argumentValue = argument.GetRawText();
                if (parameter.ParameterType.IsEnum)
                {
                    if (!Enum.TryParse(parameter.ParameterType, argumentValue, true, out var enumValue))
                    {
                        return new { Error = $"Value '{argumentValue}' is not a valid enum member for parameter '{parameter.Name}'." };
                    }

                    parsedArguments.Add(enumValue);
                }
                else
                {
                    try
                    {
                        var paredValue = JsonSerializer.Deserialize(argumentValue, parameter.ParameterType, JsonOptions);
                        parsedArguments.Add(argumentValue);
                    }
                    catch
                    {
                        return new { Error = $"Value '{argumentValue}' is not valid for parameter '{parameter.Name}'. Expected type: '{parameter.ParameterType.Name}'" };
                    }
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

        var invocationResult = function.DynamicInvoke([.. parsedArguments]);
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
