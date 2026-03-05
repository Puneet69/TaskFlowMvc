using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.UI.Services;
using TaskFlowMvc.Hubs;
using Microsoft.EntityFrameworkCore;
using TaskFlowMvc.Data;
using TaskFlowMvc.Filters;
using TaskFlowMvc.Models;
using TaskFlowMvc.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = ResolveContentRootPath()
});
builder.WebHost.UseStaticWebAssets();
var requireConfirmedAccount = builder.Configuration.GetValue<bool>("Authentication:RequireConfirmedAccount");

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var sessionTimeoutMinutes = Math.Clamp(builder.Configuration.GetValue<int?>("Authentication:SessionTimeoutMinutes") ?? 30, 5, 720);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionTimeoutMinutes);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.IsEssential = true;
    options.Events = new CookieAuthenticationEvents
    {
        OnSigningIn = async context =>
        {
            var principal = context.Principal;
            var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var services = context.HttpContext.RequestServices;
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var securityService = services.GetRequiredService<ISecurityService>();
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return;
            }

            var identity = principal!.Identities.FirstOrDefault(i => i.IsAuthenticated);
            if (identity is null)
            {
                return;
            }

            var sessionKey = identity.FindFirst(SecurityClaimTypes.DeviceSessionKey)?.Value;
            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                sessionKey = await securityService.CreateDeviceSessionAsync(userId, context.HttpContext);
                identity.AddClaim(new Claim(SecurityClaimTypes.DeviceSessionKey, sessionKey));
            }
        },
        OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
                return;
            }

            var services = context.HttpContext.RequestServices;
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var securityService = services.GetRequiredService<ISecurityService>();
            var user = await userManager.FindByIdAsync(userId);
            if (user is null || user.IsDisabled)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
                return;
            }

            var sessionKey = principal?.FindFirstValue(SecurityClaimTypes.DeviceSessionKey);
            if (!string.IsNullOrWhiteSpace(sessionKey))
            {
                var isValid = await securityService.ValidateDeviceSessionAsync(userId, sessionKey);
                if (!isValid)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync();
                    return;
                }

                await securityService.TouchDeviceSessionAsync(sessionKey, context.HttpContext);
            }
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.Scope.Add("email");
            options.Scope.Add("profile");
            options.SaveTokens = true;
        });
}

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeoutMinutes);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IUserVerificationService, UserVerificationService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserAdministrationService, UserAdministrationService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<ITimeTrackingService, TimeTrackingService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddHostedService<OverdueTaskNotificationBackgroundService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<EmailVerificationReminderFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<EmailVerificationReminderFilter>();
});
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITeamService, TeamService>();

var app = builder.Build();
await EnsureRolesAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.MapHub<NotificationsHub>("/hubs/notifications");

app.Run();

static async Task EnsureRolesAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    foreach (var role in AppRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminUsers = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
    if (adminUsers.Count == 0)
    {
        var firstUser = await userManager.Users.OrderBy(u => u.CreatedAtUtc).FirstOrDefaultAsync();
        if (firstUser is not null)
        {
            await userManager.AddToRoleAsync(firstUser, AppRoles.Admin);
        }
    }
}

static string ResolveContentRootPath()
{
    var currentDirectory = Directory.GetCurrentDirectory();
    var assemblyDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
    var baseDirectory = AppContext.BaseDirectory;

    var startingDirectories = new[] { currentDirectory, assemblyDirectory, baseDirectory }
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Distinct(StringComparer.OrdinalIgnoreCase);

    foreach (var start in startingDirectories)
    {
        var resolved = FindContentRoot(start!);
        if (resolved is not null)
        {
            return resolved;
        }
    }

    return currentDirectory;
}

static string? FindContentRoot(string startDirectory)
{
    var directory = new DirectoryInfo(startDirectory);
    while (directory is not null)
    {
        var candidate = directory.FullName;
        var hasWebRoot = Directory.Exists(Path.Combine(candidate, "wwwroot"));
        var hasAppSettings = File.Exists(Path.Combine(candidate, "appsettings.json"));

        if (hasWebRoot && hasAppSettings)
        {
            return candidate;
        }

        if (File.Exists(Path.Combine(candidate, "TaskFlowMvc.csproj")))
        {
            return candidate;
        }

        directory = directory.Parent;
    }

    return null;
}
