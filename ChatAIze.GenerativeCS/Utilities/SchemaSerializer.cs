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

/// <summary>
/// Serializes function definitions and response schemas into provider-specific JSON representations.
/// </summary>
public static class SchemaSerializer
{
    /// <summary>
    /// Nullability inspection context used to infer required properties.
    /// </summary>
    private static readonly NullabilityInfoContext NullabilityContext = new();

    /// <summary>
    /// Serializes a function definition into a JSON schema payload.
    /// </summary>
    /// <param name="function">Function to serialize.</param>
    /// <param name="useOpenAIFeatures">Indicates whether OpenAI-specific schema features should be enabled.</param>
    /// <param name="isStrictModeOn">Indicates whether strict validation should be enforced.</param>
    /// <returns>JSON object describing the function.</returns>
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

    /// <summary>
    /// Serializes a response type into the expected response_format payload.
    /// </summary>
    /// <param name="type">Type to describe.</param>
    /// <param name="useOpenAIFeatures">Indicates whether OpenAI-specific schema features should be enabled.</param>
    /// <returns>JSON object describing the response format.</returns>
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

    /// <summary>
    /// Serializes a method parameter into a JSON schema property object.
    /// </summary>
    /// <param name="parameter">Parameter information to serialize.</param>
    /// <param name="useOpenAIFeatures">Indicates whether OpenAI-specific schema features should be enabled.</param>
    /// <returns>JSON object describing the parameter.</returns>
    private static JsonObject SerializeParameter(ParameterInfo parameter, bool useOpenAIFeatures)
    {
        var parameterType = parameter.ParameterType;
        var propertyObject = SerializeProperty(parameterType, useOpenAIFeatures, requireAllProperties: false, member: parameter);
        var description = GetDescription(parameter);

        var isRequiredString = parameterType == typeof(string) && parameter.GetCustomAttribute<RequiredAttribute>() is not null;
        if (isRequiredString)
        {
            propertyObject["minLength"] = 1;
            propertyObject["pattern"] = @"\S";
        }
        ApplyStringLengthConstraints(propertyObject, parameterType, parameter, isRequiredString ? 1 : null);
        ApplyDefaultValue(propertyObject, parameter);

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

    /// <summary>
    /// Serializes a CLR type into a JSON schema property object.
    /// </summary>
    /// <param name="propertyType">Type to serialize.</param>
    /// <param name="useOpenAIFeatures">Indicates whether OpenAI-specific schema features should be enabled.</param>
    /// <param name="requireAllProperties">When true, marks all properties as required.</param>
    /// <param name="member">Optional member information used to infer nullability and constraints.</param>
    /// <returns>JSON object describing the property.</returns>
    private static JsonObject SerializeProperty(Type propertyType, bool useOpenAIFeatures, bool requireAllProperties = false, ICustomAttributeProvider? member = null)
    {
        var actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var (typeName, builtinDescription) = GetTypeInfo(actualType);
        var typeNameNormalized = typeName.ToSnakeLower();
        var isNullable = IsMemberNullable(propertyType, member);

        var propertyObject = new JsonObject
        {
            ["type"] = isNullable ? new JsonArray { typeNameNormalized, "null" } : typeNameNormalized
        };

        var typeDescription = GetDescription(actualType) ?? builtinDescription;

        if (typeDescription is not null)
        {
            propertyObject["description"] = typeDescription;
        }

        if (TryGetEnumerableItemType(actualType, out var itemType))
        {
            propertyObject["items"] = SerializeProperty(itemType, useOpenAIFeatures, requireAllProperties);
            ApplyArrayLengthConstraints(propertyObject, actualType, member);
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
        else if (IsComplexObject(actualType))
        {
            var properties = actualType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p =>
                {
                    if (p.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
                        return false;

                    // Mirror System.Text.Json behavior: non-public setters are allowed only when JsonInclude is present.
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

                    var propertyJson = SerializeProperty(property.PropertyType, useOpenAIFeatures, requireAllProperties, property);
                    var isRequiredString = property.PropertyType == typeof(string) && property.GetCustomAttribute<RequiredAttribute>() is not null;
                    if (isRequiredString)
                    {
                        propertyJson["minLength"] = 1;
                        propertyJson["pattern"] = @"\S";
                    }
                    ApplyStringLengthConstraints(propertyJson, property.PropertyType, property, isRequiredString ? 1 : null);
                    var propertyDescription = GetDescription(property);

                    if (!string.IsNullOrWhiteSpace(propertyDescription))
                    {
                        propertyJson["description"] = propertyDescription;
                    }
                    ApplyDefaultValue(propertyJson, property);

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
        else if (IsNumericType(actualType))
        {
            ApplyNumericRangeConstraints(propertyObject, actualType, member);
        }

        return propertyObject;
    }

    /// <summary>
    /// Determines whether a method parameter should be marked as required.
    /// </summary>
    /// <param name="parameter">Parameter information to inspect.</param>
    /// <returns>True when the parameter is required.</returns>
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
            // Respect C# nullable annotations: non-nullable reference types become required in the schema.
            var nullabilityInfo = NullabilityContext.Create(parameter);
            return nullabilityInfo.WriteState == NullabilityState.NotNull || nullabilityInfo.ReadState == NullabilityState.NotNull;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a property should be marked as required.
    /// </summary>
    /// <param name="property">Property information to inspect.</param>
    /// <returns>True when the property is required.</returns>
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
            // Respect C# nullable annotations: non-nullable reference types become required in the schema.
            var nullabilityInfo = NullabilityContext.Create(property);
            return nullabilityInfo.WriteState == NullabilityState.NotNull || nullabilityInfo.ReadState == NullabilityState.NotNull;
        }

        return false;
    }

    /// <summary>
    /// Attempts to read a description from a member attribute.
    /// </summary>
    /// <param name="member">Member to inspect.</param>
    /// <returns>Description when present; otherwise null.</returns>
    private static string? GetDescription(MemberInfo member)
    {
        return member.GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    /// <summary>
    /// Attempts to read a description from a type attribute.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>Description when present; otherwise null.</returns>
    private static string? GetDescription(Type type)
    {
        return type.GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    /// <summary>
    /// Attempts to read a description from a parameter attribute.
    /// </summary>
    /// <param name="parameter">Parameter to inspect.</param>
    /// <returns>Description when present; otherwise null.</returns>
    private static string? GetDescription(ParameterInfo parameter)
    {
        return parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    /// <summary>
    /// Resolves a JSON schema type name and built-in description for a CLR type.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>Tuple of type name and optional description.</returns>
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

    /// <summary>
    /// Attempts to infer the element type of an enumerable.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <param name="itemType">Discovered item type when successful.</param>
    /// <returns>True when the type represents a collection.</returns>
    private static bool TryGetEnumerableItemType(Type type, out Type itemType)
    {
        itemType = null!;

        if (type == typeof(string) || IsDictionaryType(type))
        {
            // Treat strings and dictionaries as scalars in this context to avoid being serialized as arrays.
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

    /// <summary>
    /// Attempts to infer key and value types from a dictionary-like type.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <param name="keyType">Discovered key type.</param>
    /// <param name="valueType">Discovered value type.</param>
    /// <returns>True when the type represents a dictionary.</returns>
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

    /// <summary>
    /// Determines whether the supplied type is dictionary-like.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>True when the type is a dictionary.</returns>
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

    /// <summary>
    /// Determines whether the supplied type is a generic dictionary.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>True when the type is <see cref="IDictionary{TKey,TValue}"/> or <see cref="Dictionary{TKey,TValue}"/>.</returns>
    private static bool IsGenericDictionary(Type type)
    {
        return type.GetGenericTypeDefinition() == typeof(IDictionary<,>) || type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }

    /// <summary>
    /// Determines whether the supplied type is a generic read-only dictionary.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>True when the type is <see cref="IReadOnlyDictionary{TKey,TValue}"/> or <see cref="ReadOnlyDictionary{TKey,TValue}"/>.</returns>
    private static bool IsGenericReadOnlyDictionary(Type type)
    {
        return type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) || type.GetGenericTypeDefinition() == typeof(ReadOnlyDictionary<,>);
    }

    /// <summary>
    /// Determines whether a member allows null values.
    /// </summary>
    /// <param name="originalType">Declared member type.</param>
    /// <param name="member">Member metadata.</param>
    /// <returns>True when the member is nullable.</returns>
    private static bool IsMemberNullable(Type originalType, ICustomAttributeProvider? member)
    {
        if (Nullable.GetUnderlyingType(originalType) is not null)
        {
            return true;
        }

        if (member is ParameterInfo parameterInfo)
        {
            var nullability = NullabilityContext.Create(parameterInfo);
            return nullability.WriteState == NullabilityState.Nullable || nullability.ReadState == NullabilityState.Nullable;
        }

        if (member is PropertyInfo propertyInfo)
        {
            var nullability = NullabilityContext.Create(propertyInfo);
            return nullability.WriteState == NullabilityState.Nullable || nullability.ReadState == NullabilityState.Nullable;
        }

        return false;
    }

    /// <summary>
    /// Applies string length constraints derived from attributes.
    /// </summary>
    /// <param name="propertyJson">JSON schema object to update.</param>
    /// <param name="targetType">Target CLR type.</param>
    /// <param name="attributeProvider">Member that may contain length attributes.</param>
    /// <param name="requiredMinLength">Optional minimum length enforced by the caller.</param>
    private static void ApplyStringLengthConstraints(JsonObject propertyJson, Type targetType, ICustomAttributeProvider attributeProvider, int? requiredMinLength)
    {
        if (targetType != typeof(string))
        {
            return;
        }

        int? minLength = requiredMinLength;
        int? maxLength = null;

        if (propertyJson.TryGetPropertyValue("minLength", out var minNode) && minNode is JsonValue minValue && minValue.TryGetValue<int>(out var existingMin))
        {
            minLength = minLength.HasValue ? Math.Max(minLength.Value, existingMin) : existingMin;
        }

        if (propertyJson.TryGetPropertyValue("maxLength", out var maxNode) && maxNode is JsonValue maxValue && maxValue.TryGetValue<int>(out var existingMax))
        {
            maxLength = existingMax;
        }

        if (attributeProvider.GetCustomAttributes(typeof(MinLengthAttribute), true).OfType<MinLengthAttribute>().FirstOrDefault() is { } minLengthAttribute)
        {
            minLength = minLength.HasValue ? Math.Max(minLength.Value, minLengthAttribute.Length) : minLengthAttribute.Length;
        }

        if (attributeProvider.GetCustomAttributes(typeof(StringLengthAttribute), true).OfType<StringLengthAttribute>().FirstOrDefault() is { } stringLengthAttribute)
        {
            if (stringLengthAttribute.MinimumLength > 0)
            {
                minLength = minLength.HasValue ? Math.Max(minLength.Value, stringLengthAttribute.MinimumLength) : stringLengthAttribute.MinimumLength;
            }

            maxLength = maxLength.HasValue ? Math.Min(maxLength.Value, stringLengthAttribute.MaximumLength) : stringLengthAttribute.MaximumLength;
        }

        if (attributeProvider.GetCustomAttributes(typeof(MaxLengthAttribute), true).OfType<MaxLengthAttribute>().FirstOrDefault() is { } maxLengthAttribute)
        {
            maxLength = maxLength.HasValue ? Math.Min(maxLength.Value, maxLengthAttribute.Length) : maxLengthAttribute.Length;
        }

        if (minLength.HasValue)
        {
            propertyJson["minLength"] = minLength.Value;
        }

        if (maxLength.HasValue)
        {
            propertyJson["maxLength"] = maxLength.Value;
        }
    }

    /// <summary>
    /// Applies array length constraints derived from attributes.
    /// </summary>
    /// <param name="propertyJson">JSON schema object to update.</param>
    /// <param name="targetType">Target CLR type.</param>
    /// <param name="attributeProvider">Member that may contain length attributes.</param>
    private static void ApplyArrayLengthConstraints(JsonObject propertyJson, Type targetType, ICustomAttributeProvider? attributeProvider)
    {
        if (attributeProvider is null || targetType == typeof(string) || IsDictionaryType(targetType) || !typeof(IEnumerable).IsAssignableFrom(targetType))
        {
            return;
        }

        int? minItems = null;
        int? maxItems = null;

        if (attributeProvider.GetCustomAttributes(typeof(MinLengthAttribute), true).OfType<MinLengthAttribute>().FirstOrDefault() is { } minLengthAttribute)
        {
            minItems = minLengthAttribute.Length;
        }

        if (attributeProvider.GetCustomAttributes(typeof(MaxLengthAttribute), true).OfType<MaxLengthAttribute>().FirstOrDefault() is { } maxLengthAttribute)
        {
            maxItems = maxLengthAttribute.Length;
        }

        if (minItems.HasValue)
        {
            propertyJson["minItems"] = minItems.Value;
        }

        if (maxItems.HasValue)
        {
            propertyJson["maxItems"] = maxItems.Value;
        }
    }

    /// <summary>
    /// Applies numeric range constraints derived from attributes.
    /// </summary>
    /// <param name="propertyJson">JSON schema object to update.</param>
    /// <param name="targetType">Target CLR type.</param>
    /// <param name="attributeProvider">Member that may contain range attributes.</param>
    private static void ApplyNumericRangeConstraints(JsonObject propertyJson, Type targetType, ICustomAttributeProvider? attributeProvider)
    {
        if (attributeProvider is null || !IsNumericType(targetType))
        {
            return;
        }

        if (attributeProvider.GetCustomAttributes(typeof(RangeAttribute), true).OfType<RangeAttribute>().FirstOrDefault() is not { } rangeAttribute)
        {
            return;
        }

        if (TryConvertToDouble(rangeAttribute.Minimum, out var minimum))
        {
            propertyJson["minimum"] = minimum;
        }

        if (TryConvertToDouble(rangeAttribute.Maximum, out var maximum))
        {
            propertyJson["maximum"] = maximum;
        }
    }

    /// <summary>
    /// Attempts to convert an arbitrary value to a <see cref="double"/>.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <param name="result">Converted result when successful.</param>
    /// <returns>True when conversion succeeded.</returns>
    private static bool TryConvertToDouble(object value, out double result)
    {
        try
        {
            result = Convert.ToDouble(value);
            return true;
        }
        catch
        {
            result = 0;
            return false;
        }
    }

    /// <summary>
    /// Applies default value metadata when present.
    /// </summary>
    /// <param name="propertyJson">JSON schema object to update.</param>
    /// <param name="attributeProvider">Member that may declare a default value.</param>
    private static void ApplyDefaultValue(JsonObject propertyJson, ICustomAttributeProvider? attributeProvider)
    {
        if (attributeProvider is null)
        {
            return;
        }

        if (attributeProvider.GetCustomAttributes(typeof(DefaultValueAttribute), true).OfType<DefaultValueAttribute>().FirstOrDefault() is { } defaultAttribute)
        {
            propertyJson["default"] = JsonValue.Create(defaultAttribute.Value);
        }
    }

    /// <summary>
    /// Determines whether a type is numeric.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>True when the type is numeric.</returns>
    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(nint) || type == typeof(nuint) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    /// <summary>
    /// Determines whether a type should be treated as a complex object when generating schemas.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>True when the type should be serialized as an object.</returns>
    private static bool IsComplexObject(Type type)
    {
        // Treat structs with properties as objects as well (excluding primitives/enums)
        if (type.IsClass || type.IsInterface)
        {
            return true;
        }

        if (type.IsValueType && !type.IsPrimitive && !type.IsEnum)
        {
            return true;
        }

        return false;
    }
}
