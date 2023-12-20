using System.Text.Json;

namespace GenerativeCS.Utilities;

internal static class FunctionInvoker
{
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    internal static async Task<string> InvokeAsync(Delegate function, string arguments, CancellationToken cancellationToken = default)
    {
        var modelArguments = JsonDocument.Parse(arguments);
        var parsedArguments = new List<object?>();

        foreach (var parameter in function.Method.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                parsedArguments.Add(cancellationToken);
                continue;
            }

            if (modelArguments.RootElement.TryGetProperty(parameter.Name!.ToSnakeCase(), out var argument))
            {
                try
                {
                    var argumentValue = JsonSerializer.Deserialize(argument.GetRawText(), parameter.ParameterType, JsonOptions);
                    parsedArguments.Add(argumentValue);
                }
                catch
                {
                    return $"{{\"is_success\":false,\"error\":\"Argument does not match parameter type.\",\"parameter\":\"{parameter.Name!.ToPascalCase()}\",\"type\":\"{parameter.ParameterType}\"}}";
                }
            }
            else if (parameter.IsOptional && parameter.DefaultValue != null)
            {
                parsedArguments.Add(parameter.DefaultValue);
            }
            else
            {
                return $"{{\"is_success\":false,\"error\":\"Value is missing for required parameter.\",\"parameter\":\"{parameter.Name!.ToSnakeCase()}\"}}";
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
            return "{\"is_success\":true}";
        }

        return JsonSerializer.Serialize(invocationResult);
    }
}
