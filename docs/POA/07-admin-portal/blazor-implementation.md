# Blazor Server Implementation Guide

**Version:** 1.0  
**Date:** September 10, 2025  
**Phase:** Proof-of-Operation (POA)  
**Framework:** Blazor Server with .NET 9 and MudBlazor

## Table of Contents
- [Project Setup](#project-setup)
- [MudBlazor Configuration](#mudblazor-configuration)
- [Page Implementation](#page-implementation)
- [Component Development](#component-development)
- [State Management](#state-management)
- [Real-time Features](#real-time-features)
- [Error Handling](#error-handling)
- [Testing Strategy](#testing-strategy)

## Project Setup

### Create Blazor Server Project

```bash
# Create solution and projects
dotnet new sln -n NumbatWallet.Admin
dotnet new blazorserver -n NumbatWallet.Admin.Web -f net9.0
dotnet new classlib -n NumbatWallet.Admin.Core -f net9.0
dotnet new xunit -n NumbatWallet.Admin.Tests -f net9.0

# Add projects to solution
dotnet sln add NumbatWallet.Admin.Web
dotnet sln add NumbatWallet.Admin.Core
dotnet sln add NumbatWallet.Admin.Tests

# Add project references
dotnet add NumbatWallet.Admin.Web reference NumbatWallet.Admin.Core
dotnet add NumbatWallet.Admin.Tests reference NumbatWallet.Admin.Web
```

### Install NuGet Packages

```xml
<!-- NumbatWallet.Admin.Web.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>13.0</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- UI Framework -->
    <PackageReference Include="MudBlazor" Version="6.11.0" />
    
    <!-- Authentication -->
    <PackageReference Include="Microsoft.Identity.Web" Version="2.15.0" />
    <PackageReference Include="Microsoft.Identity.Web.UI" Version="2.15.0" />
    
    <!-- GraphQL Client -->
    <PackageReference Include="StrawberryShake.Blazor" Version="13.7.0" />
    
    <!-- Caching -->
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="9.0.0" />
    
    <!-- Logging -->
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="4.0.0" />
    
    <!-- Validation -->
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.8.0" />
  </ItemGroup>
</Project>
```

### Program.cs Configuration

```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MudBlazor.Services;
using NumbatWallet.Admin.Web.Services;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Traces)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "NumbatWallet.Admin")
        .WriteTo.Console()
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<TelemetryConfiguration>(),
            TelemetryConverter.Traces));
    
    // Add authentication with Azure Entra ID
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
    
    // Add authorization
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy =>
            policy.RequireRole("Administrator", "SuperAdmin"));
        
        options.AddPolicy("RequireIssuerRole", policy =>
            policy.RequireRole("CredentialIssuer", "Administrator", "SuperAdmin"));
        
        options.AddPolicy("RequireAuditorRole", policy =>
            policy.RequireRole("Auditor", "Administrator", "SuperAdmin"));
    });
    
    // Add MudBlazor
    builder.Services.AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
        config.SnackbarConfiguration.PreventDuplicates = true;
        config.SnackbarConfiguration.NewestOnTop = true;
        config.SnackbarConfiguration.ShowCloseIcon = true;
        config.SnackbarConfiguration.VisibleStateDuration = 5000;
        config.SnackbarConfiguration.HideTransitionDuration = 500;
        config.SnackbarConfiguration.ShowTransitionDuration = 500;
    });
    
    // Add GraphQL client
    builder.Services
        .AddNumbatGraphQLClient()
        .ConfigureHttpClient(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["GraphQL:Endpoint"]!);
        })
        .AddHttpMessageHandler<AuthorizationMessageHandler>();
    
    // Add Redis cache
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "NumbatAdmin";
    });
    
    // Add application services
    builder.Services.AddScoped<ICredentialService, CredentialService>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<ITenantService, TenantService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<ITenantContext, TenantContext>();
    
    // Add Blazor services
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddHttpContextAccessor();
    
    // Add health checks
    builder.Services.AddHealthChecks()
        .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
        .AddUrlGroup(new Uri(builder.Configuration["GraphQL:Endpoint"]!), "GraphQL API");
    
    var app = builder.Build();
    
    // Configure pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }
    
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.UseSerilogRequestLogging();
    
    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");
    app.MapHealthChecks("/health");
    app.MapControllers();
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

## MudBlazor Configuration

### App.razor

```razor
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
            <NotAuthorized>
                <RedirectToLogin />
            </NotAuthorized>
            <Authorizing>
                <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
            </Authorizing>
        </AuthorizeRouteView>
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout="@typeof(MainLayout)">
            <MudContainer>
                <MudAlert Severity="Severity.Warning">
                    Sorry, there's nothing at this address.
                </MudAlert>
            </MudContainer>
        </LayoutView>
    </NotFound>
</Router>
```

### _Host.cshtml

```html
@page "/"
@namespace NumbatWallet.Admin.Web.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = "_Layout";
}

<component type="typeof(App)" render-mode="ServerPrerendered" />
```

### _Layout.cshtml

```html
@using Microsoft.AspNetCore.Components.Web
@namespace NumbatWallet.Admin.Web.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>NumbatWallet Admin Portal</title>
    <base href="~/" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link href="css/site.css" rel="stylesheet" />
    <component type="typeof(HeadOutlet)" render-mode="ServerPrerendered" />
</head>
<body>
    @RenderBody()

    <script src="_framework/blazor.server.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    
    <script>
        // Reconnection handling
        Blazor.start({
            reconnectionHandler: {
                onConnectionDown: () => console.log("Connection down"),
                onConnectionUp: () => console.log("Connection up")
            }
        });
    </script>
</body>
</html>
```

### MainLayout.razor

```razor
@inherits LayoutComponentBase
@inject NavigationManager Navigation
@inject ISnackbar Snackbar

<MudThemeProvider Theme="@_theme" IsDarkMode="@_isDarkMode" />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" 
                       Color="Color.Inherit" 
                       Edge="Edge.Start" 
                       OnClick="@DrawerToggle" />
        <MudText Typo="Typo.h6">NumbatWallet Admin</MudText>
        <MudSpacer />
        <MudText Typo="Typo.body2" Class="mx-4">@_tenantName</MudText>
        <UserMenu />
    </MudAppBar>
    
    <MudDrawer @bind-Open="_drawerOpen" 
               Elevation="2"
               ClipMode="DrawerClipMode.Always">
        <MudDrawerHeader>
            <MudText Typo="Typo.h6">Navigation</MudText>
        </MudDrawerHeader>
        <NavMenu />
    </MudDrawer>
    
    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.Large" Class="my-4">
            <ErrorBoundary @ref="errorBoundary">
                <ChildContent>
                    @Body
                </ChildContent>
                <ErrorContent>
                    <MudAlert Severity="Severity.Error">
                        An error has occurred. Please refresh the page or contact support.
                    </MudAlert>
                </ErrorContent>
            </ErrorBoundary>
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool _drawerOpen = true;
    private bool _isDarkMode = false;
    private string _tenantName = "Loading...";
    private ErrorBoundary? errorBoundary;
    
    private MudTheme _theme = new()
    {
        Palette = new PaletteLight()
        {
            Primary = Colors.Blue.Darken2,
            Secondary = Colors.Green.Accent4,
            AppbarBackground = Colors.Blue.Darken2,
        },
        PaletteDark = new PaletteDark()
        {
            Primary = Colors.Blue.Lighten1,
            Secondary = Colors.Green.Accent3,
        }
    };
    
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationState != null)
        {
            var authState = await AuthenticationState;
            _tenantName = authState.User.FindFirst("tenant_name")?.Value ?? "Default Tenant";
        }
    }
    
    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
    
    protected override void OnParametersSet()
    {
        errorBoundary?.Recover();
    }
}
```

## Page Implementation

### Dashboard Page

```razor
@page "/"
@attribute [Authorize]
@inject ICredentialService CredentialService
@inject IAuditService AuditService

<PageTitle>Dashboard</PageTitle>

<MudText Typo="Typo.h4" Class="mb-4">Dashboard</MudText>

<MudGrid>
    <!-- Statistics Cards -->
    <MudItem xs="12" sm="6" md="3">
        <MudCard>
            <MudCardContent>
                <MudText Typo="Typo.body2" Color="Color.Secondary">Total Credentials</MudText>
                <MudText Typo="Typo.h4">@(_statistics?.TotalCredentials ?? 0)</MudText>
                <MudText Typo="Typo.caption" Color="Color.Success">
                    <MudIcon Icon="@Icons.Material.Filled.TrendingUp" Size="Size.Small" />
                    12% from last month
                </MudText>
            </MudCardContent>
        </MudCard>
    </MudItem>
    
    <MudItem xs="12" sm="6" md="3">
        <MudCard>
            <MudCardContent>
                <MudText Typo="Typo.body2" Color="Color.Secondary">Active Wallets</MudText>
                <MudText Typo="Typo.h4">@(_statistics?.ActiveWallets ?? 0)</MudText>
                <MudText Typo="Typo.caption" Color="Color.Success">
                    <MudIcon Icon="@Icons.Material.Filled.TrendingUp" Size="Size.Small" />
                    8% from last month
                </MudText>
            </MudCardContent>
        </MudCard>
    </MudItem>
    
    <MudItem xs="12" sm="6" md="3">
        <MudCard>
            <MudCardContent>
                <MudText Typo="Typo.body2" Color="Color.Secondary">Issued Today</MudText>
                <MudText Typo="Typo.h4">@(_statistics?.IssuedToday ?? 0)</MudText>
                <MudText Typo="Typo.caption">
                    Daily average: @(_statistics?.DailyAverage ?? 0)
                </MudText>
            </MudCardContent>
        </MudCard>
    </MudItem>
    
    <MudItem xs="12" sm="6" md="3">
        <MudCard>
            <MudCardContent>
                <MudText Typo="Typo.body2" Color="Color.Secondary">Verifications</MudText>
                <MudText Typo="Typo.h4">@(_statistics?.VerificationsToday ?? 0)</MudText>
                <MudText Typo="Typo.caption" Color="Color.Info">
                    Success rate: @(_statistics?.SuccessRate ?? 0)%
                </MudText>
            </MudCardContent>
        </MudCard>
    </MudItem>
    
    <!-- Recent Activity -->
    <MudItem xs="12" md="8">
        <MudCard>
            <MudCardHeader>
                <CardHeaderContent>
                    <MudText Typo="Typo.h6">Recent Activity</MudText>
                </CardHeaderContent>
            </MudCardHeader>
            <MudCardContent>
                <MudTimeline TimelineOrientation="TimelineOrientation.Vertical">
                    @foreach (var activity in _recentActivities)
                    {
                        <MudTimelineItem Color="@GetActivityColor(activity.Type)" Size="Size.Small">
                            <ItemContent>
                                <MudText Typo="Typo.body2">@activity.Description</MudText>
                                <MudText Typo="Typo.caption">@activity.Timestamp.ToRelativeTime()</MudText>
                            </ItemContent>
                        </MudTimelineItem>
                    }
                </MudTimeline>
            </MudCardContent>
        </MudCard>
    </MudItem>
    
    <!-- Quick Actions -->
    <MudItem xs="12" md="4">
        <MudCard>
            <MudCardHeader>
                <CardHeaderContent>
                    <MudText Typo="Typo.h6">Quick Actions</MudText>
                </CardHeaderContent>
            </MudCardHeader>
            <MudCardContent>
                <MudStack Spacing="2">
                    <MudButton Variant="Variant.Filled" 
                               Color="Color.Primary" 
                               StartIcon="@Icons.Material.Filled.Add"
                               FullWidth="true"
                               Href="/credentials/issue">
                        Issue Credential
                    </MudButton>
                    
                    <MudButton Variant="Variant.Outlined" 
                               Color="Color.Secondary" 
                               StartIcon="@Icons.Material.Filled.Search"
                               FullWidth="true"
                               Href="/credentials">
                        View All Credentials
                    </MudButton>
                    
                    <MudButton Variant="Variant.Outlined" 
                               StartIcon="@Icons.Material.Filled.Assessment"
                               FullWidth="true"
                               Href="/audit/logs">
                        View Audit Logs
                    </MudButton>
                    
                    <MudButton Variant="Variant.Outlined" 
                               StartIcon="@Icons.Material.Filled.Settings"
                               FullWidth="true"
                               Href="/tenants/manage">
                        Tenant Settings
                    </MudButton>
                </MudStack>
            </MudCardContent>
        </MudCard>
    </MudItem>
</MudGrid>

@code {
    private DashboardStatistics? _statistics;
    private List<ActivityItem> _recentActivities = new();
    private System.Threading.Timer? _refreshTimer;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardData();
        
        // Auto-refresh every 30 seconds
        _refreshTimer = new System.Threading.Timer(
            async _ => await InvokeAsync(LoadDashboardData),
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
    }
    
    private async Task LoadDashboardData()
    {
        _statistics = await CredentialService.GetDashboardStatisticsAsync();
        _recentActivities = await AuditService.GetRecentActivityAsync(10);
        StateHasChanged();
    }
    
    private Color GetActivityColor(ActivityType type) => type switch
    {
        ActivityType.CredentialIssued => Color.Success,
        ActivityType.CredentialRevoked => Color.Error,
        ActivityType.CredentialVerified => Color.Info,
        _ => Color.Default
    };
    
    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }
}
```

## Component Development

### Credential Card Component

```razor
@* Components/Credentials/CredentialCard.razor *@
@inject IDialogService DialogService
@inject ISnackbar Snackbar

<MudCard>
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">@Credential.Type.GetDisplayName()</MudText>
            <MudChip Size="Size.Small" Color="@GetStatusColor()">@Credential.Status</MudChip>
        </CardHeaderContent>
        <CardHeaderActions>
            <MudIconButton Icon="@Icons.Material.Filled.MoreVert" 
                           OnClick="@ShowActions" />
        </CardHeaderActions>
    </MudCardHeader>
    
    <MudCardContent>
        <MudGrid>
            <MudItem xs="6">
                <MudText Typo="Typo.caption">Credential ID</MudText>
                <MudText Typo="Typo.body2">@Credential.Id</MudText>
            </MudItem>
            <MudItem xs="6">
                <MudText Typo="Typo.caption">Wallet ID</MudText>
                <MudText Typo="Typo.body2">@Credential.WalletId</MudText>
            </MudItem>
            <MudItem xs="6">
                <MudText Typo="Typo.caption">Issued</MudText>
                <MudText Typo="Typo.body2">@Credential.IssuedAt.ToLocalTime()</MudText>
            </MudItem>
            <MudItem xs="6">
                <MudText Typo="Typo.caption">Expires</MudText>
                <MudText Typo="Typo.body2">
                    @(Credential.ExpiresAt?.ToLocalTime().ToString() ?? "Never")
                </MudText>
            </MudItem>
        </MudGrid>
        
        @if (ShowClaims)
        {
            <MudDivider Class="my-2" />
            <MudText Typo="Typo.subtitle2">Claims</MudText>
            <MudSimpleTable Dense="true">
                @foreach (var claim in Credential.Claims)
                {
                    <tr>
                        <td>@claim.Key</td>
                        <td>@claim.Value</td>
                    </tr>
                }
            </MudSimpleTable>
        }
    </MudCardContent>
    
    <MudCardActions>
        <MudButton Color="Color.Primary" OnClick="@ViewDetails">View Details</MudButton>
        @if (Credential.Status == CredentialStatus.Active)
        {
            <MudButton Color="Color.Warning" OnClick="@SuspendCredential">Suspend</MudButton>
            <MudButton Color="Color.Error" OnClick="@RevokeCredential">Revoke</MudButton>
        }
    </MudCardActions>
</MudCard>

@code {
    [Parameter] public CredentialDto Credential { get; set; } = null!;
    [Parameter] public bool ShowClaims { get; set; } = false;
    [Parameter] public EventCallback<CredentialDto> OnStatusChanged { get; set; }
    
    private Color GetStatusColor() => Credential.Status switch
    {
        CredentialStatus.Active => Color.Success,
        CredentialStatus.Suspended => Color.Warning,
        CredentialStatus.Revoked => Color.Error,
        CredentialStatus.Expired => Color.Dark,
        _ => Color.Default
    };
    
    private async Task RevokeCredential()
    {
        var parameters = new DialogParameters
        {
            ["CredentialId"] = Credential.Id,
            ["CredentialType"] = Credential.Type
        };
        
        var dialog = await DialogService.ShowAsync<RevokeCredentialDialog>(
            "Revoke Credential", 
            parameters);
        
        var result = await dialog.Result;
        
        if (!result.Cancelled && result.Data is string reason)
        {
            // Perform revocation
            await OnStatusChanged.InvokeAsync(Credential);
            Snackbar.Add("Credential revoked successfully", Severity.Success);
        }
    }
}
```

## State Management

### Cascading State Provider

```csharp
// Services/AppStateProvider.cs
public class AppStateProvider : IDisposable
{
    private readonly ILogger<AppStateProvider> _logger;
    
    public event Action? OnStateChanged;
    
    private TenantInfo? _currentTenant;
    public TenantInfo? CurrentTenant
    {
        get => _currentTenant;
        set
        {
            _currentTenant = value;
            NotifyStateChanged();
        }
    }
    
    private UserInfo? _currentUser;
    public UserInfo? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            NotifyStateChanged();
        }
    }
    
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
    
    public void Dispose()
    {
        OnStateChanged = null;
    }
}
```

### Using Cascading Parameters

```razor
@* App.razor *@
<CascadingValue Value="@AppState">
    <Router AppAssembly="@typeof(App).Assembly">
        <!-- Router content -->
    </Router>
</CascadingValue>

@code {
    [Inject] private AppStateProvider AppState { get; set; } = null!;
}

@* In any component *@
@code {
    [CascadingParameter] private AppStateProvider AppState { get; set; } = null!;
    
    protected override void OnInitialized()
    {
        AppState.OnStateChanged += StateHasChanged;
    }
    
    public void Dispose()
    {
        AppState.OnStateChanged -= StateHasChanged;
    }
}
```

## Real-time Features

### SignalR Hub

```csharp
// Hubs/NotificationHub.cs
[Authorize]
public class NotificationHub : Hub
{
    private readonly ITenantContext _tenantContext;
    
    public override async Task OnConnectedAsync()
    {
        var tenantId = _tenantContext.TenantId;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = _tenantContext.TenantId;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
        await base.OnDisconnectedAsync(exception);
    }
}
```

### Client-side SignalR Service

```csharp
// Services/NotificationService.cs
public class NotificationService : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ISnackbar _snackbar;
    
    public event Action<CredentialIssuedNotification>? OnCredentialIssued;
    
    public NotificationService(NavigationManager navigation, ISnackbar snackbar)
    {
        _snackbar = snackbar;
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigation.ToAbsoluteUri("/notificationHub"))
            .WithAutomaticReconnect()
            .Build();
        
        _hubConnection.On<CredentialIssuedNotification>("CredentialIssued", notification =>
        {
            _snackbar.Add($"New credential issued: {notification.CredentialType}", Severity.Info);
            OnCredentialIssued?.Invoke(notification);
        });
        
        _hubConnection.Reconnecting += error =>
        {
            _snackbar.Add("Connection lost. Reconnecting...", Severity.Warning);
            return Task.CompletedTask;
        };
        
        _hubConnection.Reconnected += connectionId =>
        {
            _snackbar.Add("Connection restored", Severity.Success);
            return Task.CompletedTask;
        };
    }
    
    public async Task StartAsync()
    {
        await _hubConnection.StartAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
```

## Error Handling

### Global Error Boundary

```razor
@* Components/Shared/GlobalErrorBoundary.razor *@
@implements IErrorBoundary
@inject ILogger<GlobalErrorBoundary> Logger

@if (HasError)
{
    <MudAlert Severity="Severity.Error" Class="my-4">
        <MudText Typo="Typo.h6">Oops! Something went wrong</MudText>
        <MudText Typo="Typo.body2">@ErrorMessage</MudText>
        @if (ShowDetails)
        {
            <MudCollapse Expanded="@_showDetails">
                <MudCard Outlined="true" Class="mt-2">
                    <MudCardContent>
                        <pre>@ErrorDetails</pre>
                    </MudCardContent>
                </MudCard>
            </MudCollapse>
        }
        <MudButton Variant="Variant.Filled" 
                   Color="Color.Primary" 
                   OnClick="Recover"
                   Class="mt-2">
            Try Again
        </MudButton>
    </MudAlert>
}
else
{
    @ChildContent
}

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool ShowDetails { get; set; } = false;
    
    private bool HasError { get; set; }
    private string ErrorMessage { get; set; } = string.Empty;
    private string ErrorDetails { get; set; } = string.Empty;
    private bool _showDetails;
    
    public void Recover()
    {
        HasError = false;
        ErrorMessage = string.Empty;
        ErrorDetails = string.Empty;
        StateHasChanged();
    }
    
    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        return Task.CompletedTask;
    }
    
    public void HandleException(Exception exception)
    {
        HasError = true;
        ErrorMessage = exception.Message;
        ErrorDetails = exception.ToString();
        
        Logger.LogError(exception, "Unhandled error in component");
        
        StateHasChanged();
    }
}
```

## Testing Strategy

### Component Tests

```csharp
// Tests/Components/CredentialCardTests.cs
public class CredentialCardTests : TestContext
{
    private readonly IDialogService _dialogService;
    private readonly ISnackbar _snackbar;
    
    public CredentialCardTests()
    {
        _dialogService = Substitute.For<IDialogService>();
        _snackbar = Substitute.For<ISnackbar>();
        
        Services.AddSingleton(_dialogService);
        Services.AddSingleton(_snackbar);
        Services.AddMudServices();
    }
    
    [Fact]
    public void Should_Display_Credential_Information()
    {
        // Arrange
        var credential = new CredentialDto
        {
            Id = "cred_123",
            Type = CredentialType.DriverLicense,
            Status = CredentialStatus.Active,
            IssuedAt = DateTime.UtcNow
        };
        
        // Act
        var component = RenderComponent<CredentialCard>(parameters => parameters
            .Add(p => p.Credential, credential)
            .Add(p => p.ShowClaims, false));
        
        // Assert
        Assert.Contains("cred_123", component.Markup);
        Assert.Contains("Driver License", component.Markup);
        Assert.Contains("Active", component.Markup);
    }
    
    [Fact]
    public async Task Should_Show_Revoke_Dialog_When_Revoke_Clicked()
    {
        // Arrange
        var credential = new CredentialDto
        {
            Status = CredentialStatus.Active
        };
        
        var component = RenderComponent<CredentialCard>(parameters => parameters
            .Add(p => p.Credential, credential));
        
        // Act
        await component.Find("button:contains('Revoke')").ClickAsync();
        
        // Assert
        await _dialogService.Received(1).ShowAsync<RevokeCredentialDialog>(
            Arg.Any<string>(), 
            Arg.Any<DialogParameters>());
    }
}
```

### Integration Tests

```csharp
// Tests/Pages/DashboardTests.cs
public class DashboardIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public DashboardIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override services with test implementations
                services.AddSingleton<ICredentialService, MockCredentialService>();
            });
        });
    }
    
    [Fact]
    public async Task Dashboard_Should_Load_Statistics()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Dashboard", content);
        Assert.Contains("Total Credentials", content);
    }
}
```

## Summary

This Blazor implementation guide provides:
1. **Complete project setup** with .NET 9 and MudBlazor
2. **Authentication integration** with Azure Entra ID
3. **Component architecture** following best practices
4. **Real-time features** using SignalR
5. **State management** patterns
6. **Comprehensive error handling**
7. **Testing strategies** for components and integration

The implementation ensures rapid development for POA while maintaining production-quality code that can scale to the full pilot phase.