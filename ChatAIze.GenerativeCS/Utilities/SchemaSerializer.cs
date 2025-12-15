using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Nodes;
using ChatAIze.Abstractions.Chat;
using ChatAIze.Utilities.Extensions;

namespace ChatAIze.GenerativeCS.Utilities;

public static class SchemaSerializer
{
    internal static JsonObject SerializeFunction(IChatFunction function, bool useOpenAIFeatures, bool isStrictModeOn)
    {
        var propertiesObject = new JsonObject();
        var requiredArray = new JsonArray();
        var allRequired = true;

        if (function.Parameters is not null)
        {
            foreach (var parameter in function.Parameters)
            {
                var normalizedName = parameter.Name.ToSnakeLower();
                if (string.IsNullOrWhiteSpace(normalizedName))
                {
                    continue;
                }

                var propertyObject = SerializeProperty(parameter.Type, useOpenAIFeatures);

                if (!string.IsNullOrWhiteSpace(parameter.Description))
                {
                    propertyObject["description"] = parameter.Description;
                }

                if (parameter.EnumValues.Count > 0)
                {
                    _ = propertiesObject.Remove("enum");

                    var enumValuesArray = new JsonArray();
                    var normalizedEnumValues = parameter.EnumValues.Select(v => v.ToSnakeLower()).Distinct();

                    foreach (var enumValue in normalizedEnumValues)
                    {
                        enumValuesArray.Add(enumValue);
                    }

                    propertyObject["enum"] = enumValuesArray;
                }

                propertiesObject[normalizedName] = propertyObject;

                if (parameter.IsRequired)
                {
                    requiredArray.Add(normalizedName);
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

                var parameterName = parameter.Name!.ToSnakeLower();
                var propertyObject = SerializeParameter(parameter, useOpenAIFeatures);

                propertiesObject[parameterName] = propertyObject;

                if (!parameter.IsOptional || isStrictModeOn || IsRequired(parameter))
                {
                    requiredArray.Add(parameterName);
                }
            }
        }

        var parametersObject = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = propertiesObject
        };

        if (useOpenAIFeatures)
        {
            parametersObject["additionalProperties"] = true;
        }

        if (requiredArray.Count != 0)
        {
            parametersObject["required"] = requiredArray;
        }

        var functionObject = new JsonObject
        {
            ["name"] = function.Name.ToSnakeLower()
        };

        var description = function.Description;

        if (description is null && function.Callback is not null)
        {
            description = GetDescription(function.Callback.Method);
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            functionObject["description"] = description;
        }

        if (allRequired && isStrictModeOn)
        {
            functionObject["strict"] = true;
        }

        if (propertiesObject.Count > 0)
        {
            functionObject["parameters"] = parametersObject;
        }

        return functionObject;
    }

    public static JsonObject SerializeResponseFormat(Type type, bool useOpenAIFeatures)
    {
        var name = type.Name.ToSnakeLower();
        if (name.Length > 64)
        {
            name = name[..64];
        }

        var jsonSchemaObject = new JsonObject
        {
            ["name"] = name,
            ["schema"] = SerializeProperty(type, useOpenAIFeatures),
            ["strict"] = true,
        };

        var formatObject = new JsonObject
        {
            ["type"] = "json_schema",
            ["json_schema"] = jsonSchemaObject
        };

        return formatObject;
    }

    private static JsonObject SerializeParameter(ParameterInfo parameter, bool useOpenAIFeatures)
    {
        var parameterType = parameter.ParameterType;
        var propertyObject = SerializeProperty(parameterType, useOpenAIFeatures);
        var description = GetDescription(parameter);

        if (description is not null)
        {
            propertyObject["description"] = description;
        }

        if (parameter.IsOptional && parameter.DefaultValue is not null)
        {
            propertyObject["default"] = parameter.DefaultValue.ToString();
        }

        return propertyObject;
    }

    private static JsonObject SerializeProperty(Type propertyType, bool useOpenAIFeatures)
    {
        var (typeName, typeDescription) = GetTypeInfo(propertyType);
        var propertyObject = new JsonObject
        {
            ["type"] = typeName.ToSnakeLower()
        };

        if (typeDescription is not null)
        {
            propertyObject["description"] = typeDescription;
        }

        if (TryGetEnumerableItemType(propertyType, out var itemType))
        {
            propertyObject["items"] = SerializeProperty(itemType, useOpenAIFeatures);
        }
        else if (TryGetDictionaryTypes(propertyType, out var _, out var valueType))
        {
            propertyObject["additionalProperties"] = SerializeProperty(valueType, useOpenAIFeatures);
        }
        else if (propertyType.IsEnum)
        {
            var membersArray = new JsonArray();
            foreach (var enumMember in Enum.GetNames(propertyType))
            {
                membersArray.Add(enumMember.ToSnakeLower());
            }

            propertyObject["enum"] = membersArray;
        }
        else if (propertyType.IsClass)
        {
            var properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite);
            if (properties.Any())
            {
                var propertiesObject = new JsonObject();
                var requiredArray = new JsonArray();

                foreach (var property in properties)
                {
                    var propertyName = property.Name.ToSnakeLower();

                    var propertyJson = SerializeProperty(property.PropertyType, useOpenAIFeatures);
                    var propertyDescription = GetDescription(property);

                    if (!string.IsNullOrWhiteSpace(propertyDescription))
                    {
                        propertyJson["description"] = propertyDescription;
                    }

                    propertiesObject[propertyName] = propertyJson;
                    requiredArray.Add(propertyName);
                }

                propertyObject["properties"] = propertiesObject;
                propertyObject["required"] = requiredArray;

                if (useOpenAIFeatures)
                {
                    propertyObject["additionalProperties"] = false;
                }
            }
        }

        return propertyObject;
    }

    private static bool IsRequired(ParameterInfo parameter)
    {
        return parameter.GetCustomAttribute<RequiredAttribute>() is not null;
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

        if (TryGetEnumerableItemType(type, out _))
        {
            return ("array", null);
        }

        return ("object", null);
    }

    private static bool TryGetEnumerableItemType(Type type, out Type itemType)
    {
        itemType = null!;

        if (type == typeof(string) || IsDictionaryType(type))
        {
            return false;
        }

        if (type.IsArray && type.HasElementType)
        {
            itemType = type.GetElementType()!;
            return true;
        }

        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type) && type.GenericTypeArguments.Length == 1)
        {
            itemType = type.GenericTypeArguments[0];
            return true;
        }

        var enumerableInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerableInterface is not null)
        {
            itemType = enumerableInterface.GenericTypeArguments[0];
            return true;
        }

        return false;
    }

    private static bool TryGetDictionaryTypes(Type type, out Type keyType, out Type valueType)
    {
        keyType = null!;
        valueType = null!;

        if (type.IsGenericType && (IsGenericDictionary(type) || IsGenericReadOnlyDictionary(type)))
        {
            keyType = type.GenericTypeArguments[0];
            valueType = type.GenericTypeArguments[1];
            return true;
        }

        var dictionaryInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && (IsGenericDictionary(i) || IsGenericReadOnlyDictionary(i)));
        if (dictionaryInterface is not null)
        {
            keyType = dictionaryInterface.GenericTypeArguments[0];
            valueType = dictionaryInterface.GenericTypeArguments[1];
            return true;
        }

        if (typeof(IDictionary).IsAssignableFrom(type))
        {
            keyType = typeof(string);
            valueType = typeof(object);
            return true;
        }

        return false;
    }

    private static bool IsDictionaryType(Type type)
    {
        if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) || type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)))
        {
            return true;
        }

        if (typeof(IDictionary).IsAssignableFrom(type))
        {
            return true;
        }

        return type.GetInterfaces().Any(i => i.IsGenericType &&
            (i.GetGenericTypeDefinition() == typeof(IDictionary<,>) || i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
    }

    private static bool IsGenericDictionary(Type type)
    {
        return type.GetGenericTypeDefinition() == typeof(IDictionary<,>) || type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }

    private static bool IsGenericReadOnlyDictionary(Type type)
    {
        return type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) || type.GetGenericTypeDefinition() == typeof(ReadOnlyDictionary<,>);
    }
}
