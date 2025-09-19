namespace NumbatWallet.Domain.Credentials;

/// <summary>
/// Validator for Credential Manifests
/// </summary>
public class CredentialManifestValidator
{
    public bool IsValid(CredentialManifest manifest)
    {
        if (manifest == null)
        {
            return false;
        }

        // Required fields
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            return false;
        }

        if (manifest.Issuer == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(manifest.Issuer.Id))
        {
            return false;
        }

        // Must have at least one output descriptor
        if (manifest.OutputDescriptors == null || manifest.OutputDescriptors.Count == 0)
        {
            return false;
        }

        // Validate each output descriptor
        foreach (var descriptor in manifest.OutputDescriptors)
        {
            if (string.IsNullOrWhiteSpace(descriptor.Id))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(descriptor.Schema))
            {
                return false;
            }
        }

        // Validate presentation definition if present
        if (manifest.PresentationDefinition != null)
        {
            if (string.IsNullOrWhiteSpace(manifest.PresentationDefinition.Id))
            {
                return false;
            }

            if (manifest.PresentationDefinition.InputDescriptors == null ||
                manifest.PresentationDefinition.InputDescriptors.Count == 0)
            {
                return false;
            }

            foreach (var inputDescriptor in manifest.PresentationDefinition.InputDescriptors)
            {
                if (string.IsNullOrWhiteSpace(inputDescriptor.Id))
                {
                    return false;
                }

                if (inputDescriptor.Schema == null || inputDescriptor.Schema.Count == 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public ValidationResult ValidateWithDetails(CredentialManifest manifest)
    {
        var result = new ValidationResult { IsValid = true };

        if (manifest == null)
        {
            result.IsValid = false;
            result.Errors.Add("Manifest is null");
            return result;
        }

        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            result.IsValid = false;
            result.Errors.Add("Manifest ID is required");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            result.IsValid = false;
            result.Errors.Add("Manifest version is required");
        }

        if (manifest.Issuer == null)
        {
            result.IsValid = false;
            result.Errors.Add("Manifest issuer is required");
        }
        else if (string.IsNullOrWhiteSpace(manifest.Issuer.Id))
        {
            result.IsValid = false;
            result.Errors.Add("Issuer ID is required");
        }

        if (manifest.OutputDescriptors == null || manifest.OutputDescriptors.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("At least one output descriptor is required");
        }
        else
        {
            for (int i = 0; i < manifest.OutputDescriptors.Count; i++)
            {
                var descriptor = manifest.OutputDescriptors[i];
                if (string.IsNullOrWhiteSpace(descriptor.Id))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Output descriptor [{i}] ID is required");
                }

                if (string.IsNullOrWhiteSpace(descriptor.Schema))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Output descriptor [{i}] schema is required");
                }
            }
        }

        return result;
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}