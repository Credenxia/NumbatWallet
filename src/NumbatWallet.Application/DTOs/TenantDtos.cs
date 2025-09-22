using System;
using System.Collections.Generic;

namespace NumbatWallet.Application.DTOs;

public class CreateTenantDto
{
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public class UpdateTenantDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string>? Settings { get; set; }
    public bool? IsActive { get; set; }
}