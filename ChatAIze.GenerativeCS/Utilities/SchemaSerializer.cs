using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ChatAIze.Abstractions.Chat;
using ChatAIze.Utilities.Extensions;

namespace ChatAIze.GenerativeCS.Utilities;

public static class SchemaSerializer
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

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

                var isRequired = IsRequired(parameter);
                if (isRequired)
                {
                    requiredArray.Add(parameterName);
                }
                else
                {
                    allRequired = false;
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
            ["schema"] = SerializeProperty(type, useOpenAIFeatures, requireAllProperties: false)
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
            propertyObject["default"] = JsonValue.Create(parameter.DefaultValue);
        }

        return propertyObject;
    }

    private static JsonObject SerializeProperty(Type propertyType, bool useOpenAIFeatures, bool requireAllProperties = false)
    {
        var actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var (typeName, builtinDescription) = GetTypeInfo(actualType);
        var propertyObject = new JsonObject
        {
            ["type"] = typeName.ToSnakeLower()
        };

        var typeDescription = GetDescription(actualType) ?? builtinDescription;

        if (typeDescription is not null)
        {
            propertyObject["description"] = typeDescription;
        }

        if (TryGetEnumerableItemType(actualType, out var itemType))
        {
            propertyObject["items"] = SerializeProperty(itemType, useOpenAIFeatures, requireAllProperties);
        }
        else if (TryGetDictionaryTypes(actualType, out var _, out var valueType))
        {
            propertyObject["additionalProperties"] = SerializeProperty(valueType, useOpenAIFeatures, requireAllProperties);
        }
        else if (actualType.IsEnum)
        {
            var membersArray = new JsonArray();
            foreach (var enumMember in Enum.GetNames(actualType))
            {
                membersArray.Add(enumMember.ToSnakeLower());
            }

            propertyObject["enum"] = membersArray;
        }
        else if (actualType.IsClass || actualType.IsInterface)
        {
            var properties = actualType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p =>
                {
                    if (p.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
                        return false;

                    var hasJsonInclude = p.GetCustomAttribute<JsonIncludeAttribute>() is not null;
                    var canWrite = p.CanWrite && p.SetMethod is not null && p.SetMethod.IsPublic;

                    return canWrite || hasJsonInclude;
                });
            if (properties.Any())
            {
                var propertiesObject = new JsonObject();
                var requiredArray = new JsonArray();

                foreach (var property in properties)
                {
                    var propertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name.ToSnakeLower();

                    var propertyJson = SerializeProperty(property.PropertyType, useOpenAIFeatures, requireAllProperties);
                    var propertyDescription = GetDescription(property);

                    if (!string.IsNullOrWhiteSpace(propertyDescription))
                    {
                        propertyJson["description"] = propertyDescription;
                    }

                    propertiesObject[propertyName] = propertyJson;

                    var isDictionaryProperty = IsDictionaryType(property.PropertyType);
                    var isRequired = IsRequired(property);

                    if (requireAllProperties)
                    {
                        if (!isDictionaryProperty)
                        {
                            requiredArray.Add(propertyName);
                        }
                    }
                    else if (isRequired && !isDictionaryProperty)
                    {
                        requiredArray.Add(propertyName);
                    }
                }

                propertyObject["properties"] = propertiesObject;
                if (requiredArray.Count > 0)
                {
                    propertyObject["required"] = requiredArray;
                }

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
        if (parameter.GetCustomAttribute<RequiredAttribute>() is not null)
        {
            return true;
        }

        if (parameter.IsOptional)
        {
            return false;
        }

        var parameterType = parameter.ParameterType;
        if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
        {
            return true;
        }

        if (parameterType.IsClass || parameterType.IsInterface)
        {
            var nullabilityInfo = NullabilityContext.Create(parameter);
            return nullabilityInfo.WriteState == NullabilityState.NotNull || nullabilityInfo.ReadState == NullabilityState.NotNull;
        }

        return false;
    }

    private static bool IsRequired(PropertyInfo property)
    {
        if (property.GetCustomAttribute<RequiredAttribute>() is not null)
        {
            return true;
        }

        var propertyType = property.PropertyType;
        if (propertyType.IsValueType && Nullable.GetUnderlyingType(propertyType) is null)
        {
            return true;
        }

        if (propertyType.IsClass || propertyType.IsInterface)
        {
            var nullabilityInfo = NullabilityContext.Create(property);
            return nullabilityInfo.WriteState == NullabilityState.NotNull || nullabilityInfo.ReadState == NullabilityState.NotNull;
        }

        return false;
    }

    private static string? GetDescription(MemberInfo member)
    {
        return member.GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    private static string? GetDescription(Type type)
    {
        return type.GetCustomAttribute<DescriptionAttribute>()?.Description;
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
            return ("number", "floating point number");
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
