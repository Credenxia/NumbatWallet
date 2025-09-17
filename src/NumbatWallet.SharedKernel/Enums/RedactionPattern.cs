namespace NumbatWallet.SharedKernel.Enums;

/// <summary>
/// Defines how sensitive data should be redacted for display
/// </summary>
public enum RedactionPattern
{
    /// <summary>
    /// Fully redacted: ****
    /// </summary>
    Full = 0,

    /// <summary>
    /// Show first character: J***
    /// </summary>
    ShowFirst = 1,

    /// <summary>
    /// Show last character: ***n
    /// </summary>
    ShowLast = 2,

    /// <summary>
    /// Show first and last: J***n
    /// </summary>
    ShowFirstLast = 3,

    /// <summary>
    /// Show first three characters: Joh***
    /// </summary>
    ShowFirstThree = 4,

    /// <summary>
    /// Show domain only for emails: ***@example.com
    /// </summary>
    ShowDomain = 5,

    /// <summary>
    /// Show year only for dates: **/**/1990
    /// </summary>
    ShowYear = 6,

    /// <summary>
    /// Show last four digits: ****1234
    /// </summary>
    ShowLastFour = 7,

    /// <summary>
    /// Custom pattern defined by tenant policy
    /// </summary>
    Custom = 99
}