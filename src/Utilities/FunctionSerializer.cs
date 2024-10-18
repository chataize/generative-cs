using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Nodes;
using ChatAIze.GenerativeCS.Models;
namespace ChatAIze.GenerativeCS.Utilities;

internal static class FunctionSerializer
{
    internal static JsonObject SerializeFunction(ChatFunction function, bool isStrictModeOn = false)
    {
        var propertiesObject = new JsonObject();
        var requiredArray = new JsonArray();
        var allRequired = true;

        if (function.Parameters != null)
        {
            foreach (var parameter in function.Parameters)
            {
                var propertyObject = SerializeProperty(parameter.Type);
                if (parameter.Description != null)
                {
                    propertyObject.Add("description", parameter.Description);
                }

                propertiesObject.Add(parameter.Name, propertyObject);

                if (parameter.IsRequired)
                {
                    requiredArray.Add(parameter.Name);
                }
                else
                {
                    allRequired = false;
                }
            }
        }
        else
        {
            foreach (var parameter in function.Callback!.Method.GetParameters())
            {
                if (parameter.ParameterType == typeof(CancellationToken))
                {
                    continue;
                }

                var parameterName = parameter.Name!;
                var propertyObject = SerializeParameter(parameter);

                propertiesObject.Add(parameterName, propertyObject);

                if (!parameter.IsOptional || isStrictModeOn || IsRequired(parameter))
                {
                    requiredArray.Add(parameterName);
                }
            }
        }

        var parametersObject = new JsonObject
        {
            { "type", "object" },
            { "properties", propertiesObject },
        };

        if (requiredArray.Count != 0)
        {
            parametersObject.Add("required", requiredArray);
            parametersObject.Add("additionalProperties", false);
        }

        var functionObject = new JsonObject
        {
            { "name", function.Name }
        };

        var description = function.Description;

        if (description is null && function.Callback != null)
        {
            description = GetDescription(function.Callback.Method);
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            functionObject.Add("description", description);
        }

        if (allRequired && isStrictModeOn)
        {
            functionObject.Add("strict", true);
        }

        if (propertiesObject.Count > 0)
        {
            functionObject.Add("parameters", parametersObject);
        }

        return functionObject;
    }

    private static JsonObject SerializeParameter(ParameterInfo parameter)
    {
        var parameterType = parameter.ParameterType;
        var propertyObject = SerializeProperty(parameterType);
        var description = GetDescription(parameter);

        if (description != null)
        {
            propertyObject.Add("description", description);
        }

        if (parameter.IsOptional && parameter.DefaultValue != null)
        {
            propertyObject.Add("default", parameter.DefaultValue.ToString());
        }

        return propertyObject;
    }

    private static JsonObject SerializeProperty(Type propertyType)
    {
        var (typeName, typeDescription) = GetTypeInfo(propertyType);
        var propertyObject = new JsonObject
        {
            { "type", typeName }
        };

        if (typeDescription != null)
        {
            propertyObject.Add("description", typeDescription);
        }

        if (propertyType.IsEnum)
        {
            var membersArray = new JsonArray();
            foreach (var enumMember in Enum.GetNames(propertyType))
            {
                membersArray.Add(enumMember);
            }

            propertyObject.Add("enum", membersArray);
        }
        else if (propertyType.IsArray && propertyType.HasElementType)
        {
            var itemType = propertyType.GetElementType()!;
            propertyObject.Add("items", SerializeProperty(itemType));
        }
        else if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType.GenericTypeArguments.Length == 1)
        {
            var itemType = propertyType.GenericTypeArguments[0];
            propertyObject.Add("items", SerializeProperty(itemType));
        }
        else if (propertyType.IsClass)
        {
            var properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite);
            if (properties.Any())
            {
                var propertiesObject = new JsonObject();
                foreach (var property in properties)
                {
                    propertiesObject.Add(property.Name, SerializeProperty(property.PropertyType));
                }

                propertyObject.Add("properties", propertiesObject);
            }
        }

        return propertyObject;
    }

    private static bool IsRequired(ParameterInfo parameter)
    {
        return parameter.GetCustomAttribute<RequiredAttribute>() != null;
    }

    private static string? GetDescription(MemberInfo member)
    {
        return member.GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    private static string? GetDescription(ParameterInfo parameter)
    {
        return parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    private static (string name, string? description) GetTypeInfo(Type type)
    {
        if (type == typeof(bool))
        {
            return ("boolean", null);
        }

        if (type == typeof(sbyte))
        {
            return ("integer", "8-bit signed integer from -128 to 127");
        }

        if (type == typeof(byte))
        {
            return ("integer", "8-bit unsigned integer from 0 to 255");
        }

        if (type == typeof(short))
        {
            return ("integer", "16-bit signed integer from -32,768 to 32,767");
        }

        if (type == typeof(ushort))
        {
            return ("integer", "16-bit unsigned integer from 0 to 65,535");
        }

        if (type == typeof(int) || type == typeof(long) || type == typeof(nint))
        {
            return ("integer", null);
        }

        if (type == typeof(uint) || type == typeof(ulong) || type == typeof(nuint))
        {
            return ("integer", "unsigned integer, greater than or equal to 0");
        }

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            return ("integer", "floating point number");
        }

        if (type == typeof(char))
        {
            return ("string", "single character");
        }

        if (type == typeof(string))
        {
            return ("string", null);
        }

        if (type == typeof(Uri))
        {
            return ("string", "URI in C# .NET format https://example.com/abc");
        }

        if (type == typeof(Guid))
        {
            return ("string", "GUID in C# .NET format separated by hyphens xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return ("string", "date and time in C# .NET ISO 8601 format yyyy-mm-ddThh:mm:ss");
        }

        if (type == typeof(TimeSpan))
        {
            return ("string", "time interval in C# .NET ISO 8601 format hh:mm:ss");
        }

        if (type == typeof(DateOnly))
        {
            return ("string", "date in C# .NET ISO 8601 format yyyy-mm-dd");
        }

        if (type == typeof(TimeOnly))
        {
            return ("string", "time in C# .NET ISO 8601 format hh:mm:ss");
        }

        if (type.IsEnum)
        {
            return ("string", null);
        }

        if (type.IsArray && type.HasElementType)
        {
            return ("array", null);
        }

        if (typeof(IEnumerable).IsAssignableFrom(type) && type.GenericTypeArguments.Length == 1)
        {
            return ("array", null);
        }

        return ("object", null);
    }
}
