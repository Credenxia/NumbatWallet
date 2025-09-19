using System.Text.Json;

namespace NumbatWallet.Domain.Credentials;

/// <summary>
/// Validates presentations against credential manifest requirements
/// </summary>
public class PresentationValidator
{
    public bool ValidateAgainstManifest(Dictionary<string, object> presentation, CredentialManifest manifest)
    {
        if (presentation == null || manifest == null)
        {
            return false;
        }

        if (manifest.PresentationDefinition == null)
        {
            return true; // No requirements to validate
        }

        foreach (var inputDescriptor in manifest.PresentationDefinition.InputDescriptors)
        {
            if (!ValidateInputDescriptor(presentation, inputDescriptor))
            {
                return false;
            }
        }

        return true;
    }

    private bool ValidateInputDescriptor(Dictionary<string, object> presentation, InputDescriptor descriptor)
    {
        if (descriptor.Constraints == null || descriptor.Constraints.Fields == null)
        {
            return true; // No constraints to validate
        }

        foreach (var fieldConstraint in descriptor.Constraints.Fields)
        {
            if (!ValidateFieldConstraint(presentation, fieldConstraint))
            {
                // Check if field is optional
                if (fieldConstraint.Optional == true)
                {
                    continue;
                }

                return false;
            }
        }

        return true;
    }

    private bool ValidateFieldConstraint(Dictionary<string, object> presentation, FieldConstraint constraint)
    {
        foreach (var path in constraint.Path)
        {
            var value = ExtractValueFromPath(presentation, path);
            if (value != null)
            {
                if (constraint.Filter != null)
                {
                    if (!ValidateFilter(value, constraint.Filter))
                    {
                        return false;
                    }
                }
                return true; // Found valid value in one of the paths
            }
        }

        return false; // No value found in any path
    }

    private object? ExtractValueFromPath(Dictionary<string, object> data, string path)
    {
        // Simple JSONPath implementation for basic paths like "$.credentialSubject.age"
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var cleanPath = path.TrimStart('$', '.');
        var parts = cleanPath.Split('.');

        object current = data;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue(part, out var value))
                {
                    current = value;
                }
                else
                {
                    return null;
                }
            }
            else if (current is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(part, out var property))
                {
                    current = property;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    private bool ValidateFilter(object value, Dictionary<string, object> filter)
    {
        if (filter.TryGetValue("type", out var typeObj) && typeObj is string type)
        {
            if (!ValidateType(value, type))
            {
                return false;
            }
        }

        if (filter.TryGetValue("minimum", out var minObj))
        {
            if (!ValidateMinimum(value, minObj))
            {
                return false;
            }
        }

        if (filter.TryGetValue("maximum", out var maxObj))
        {
            if (!ValidateMaximum(value, maxObj))
            {
                return false;
            }
        }

        if (filter.TryGetValue("format", out var formatObj) && formatObj is string format)
        {
            if (!ValidateFormat(value, format))
            {
                return false;
            }
        }

        return true;
    }

    private bool ValidateType(object value, string type)
    {
        return type.ToLowerInvariant() switch
        {
            "string" => value is string,
            "number" => value is int or long or float or double or decimal,
            "boolean" => value is bool,
            "object" => value is Dictionary<string, object>,
            "array" => value is Array or IEnumerable<object>,
            _ => false
        };
    }

    private bool ValidateMinimum(object value, object minimum)
    {
        if (TryConvertToNumber(value, out var valueNum) && TryConvertToNumber(minimum, out var minNum))
        {
            return valueNum >= minNum;
        }
        return false;
    }

    private bool ValidateMaximum(object value, object maximum)
    {
        if (TryConvertToNumber(value, out var valueNum) && TryConvertToNumber(maximum, out var maxNum))
        {
            return valueNum <= maxNum;
        }
        return false;
    }

    private bool ValidateFormat(object value, string format)
    {
        if (value is not string stringValue)
        {
            return false;
        }

        return format.ToLowerInvariant() switch
        {
            "date" => DateTime.TryParse(stringValue, out _),
            "date-time" => DateTime.TryParse(stringValue, out _),
            "email" => stringValue.Contains('@') && stringValue.Contains('.'),
            "uri" => Uri.TryCreate(stringValue, UriKind.Absolute, out _),
            _ => true
        };
    }

    private bool TryConvertToNumber(object value, out double number)
    {
        number = 0;

        if (value == null)
        {
            return false;
        }

        if (value is int intVal)
        {
            number = intVal;
            return true;
        }

        if (value is long longVal)
        {
            number = longVal;
            return true;
        }

        if (value is float floatVal)
        {
            number = floatVal;
            return true;
        }

        if (value is double doubleVal)
        {
            number = doubleVal;
            return true;
        }

        if (value is decimal decimalVal)
        {
            number = (double)decimalVal;
            return true;
        }

        if (value is string stringVal && double.TryParse(stringVal, out number))
        {
            return true;
        }

        return false;
    }
}