using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Tracker.Models;
using Tracker.Services;

static string? NormalizeBasePath(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return null;
    var p = raw.Trim();
    if (!p.StartsWith("/")) p = "/" + p;
    while (p.Contains("//")) p = p.Replace("//", "/");
    if (p.Length > 1 && p.EndsWith("/")) p = p.TrimEnd('/');
    return p;
}

// Resolver ruta de logs (relativa/~/absoluta)
static string ResolvePath(string? raw, IWebHostEnvironment env)
{
    if (string.IsNullOrWhiteSpace(raw)) return env.ContentRootPath;

    // Absoluta → dejar igual
    if (Path.IsPathRooted(raw)) return raw;

    // Comienza con "~/" → relativa a WebRoot (wwwroot)
    if (raw.StartsWith("~/") || raw.StartsWith("~\\"))
    {
        var rel = raw.Substring(2).TrimStart('/', '\\');
        return Path.Combine(env.WebRootPath ?? env.ContentRootPath, rel);
    }

    // Relativa "normal" → ContentRoot (carpeta de la app)
    return Path.Combine(env.ContentRootPath, raw);
}

var builder = WebApplication.CreateBuilder(args);

// 1) Config: appsettings.* + ENV (por si en algún despliegue lo usan)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true) // opcional
    .AddEnvironmentVariables();

// 2) PathBase desde config (Meta:PathBase) o fallback al ENV clásico
var basePath = NormalizeBasePath(
    builder.Configuration["Meta:PathBase"] ?? builder.Configuration["ASPNETCORE_PATHBASE"]
);

// --- Auth & Cookies ---
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Denied";

    o.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx =>
        {
            var pb = ctx.Request.PathBase.HasValue ? ctx.Request.PathBase.Value : "";
            var loginPath = pb + o.LoginPath;

            var returnUrl = ctx.Request.PathBase + ctx.Request.Path + ctx.Request.QueryString;
            var dest = loginPath + QueryString.Create("ReturnUrl", returnUrl);

            ctx.Response.Redirect(dest);
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = ctx =>
        {
            var pb = ctx.Request.PathBase.HasValue ? ctx.Request.PathBase.Value : "";
            var deniedPath = pb + (o.AccessDeniedPath.HasValue ? o.AccessDeniedPath.Value : "/Account/Denied");
            ctx.Response.Redirect(deniedPath);
            return Task.CompletedTask;
        }
    };
});

// --- Services ---
builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("TrackerDbConnectionString");
var commandTimeoutSeconds = builder.Configuration.GetSection("DatabaseSettings")["CommandTimeoutSeconds"] ?? "30";

builder.Services.AddDbContext<Tracker_DevelContext>(options =>
    options.UseSqlServer(connectionString, providerOptions =>
        providerOptions.EnableRetryOnFailure()
            .CommandTimeout(int.Parse(commandTimeoutSeconds))
    )
);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers()
    .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(options =>
{
    options.FormFieldName = "AntiforgeryFieldname";
    options.HeaderName = "X-CSRF-TOKEN-HEADERNAME";
    options.SuppressXFrameOptionsHeader = false;
});

// --- Opciones propias (incluye SoapLogging con path relativo resuelto) ---
builder.Services.Configure<SoapLoggingOptions>(opt =>
{
    builder.Configuration.GetSection("SoapLogging").Bind(opt);

    // Resolver a absoluta si vino relativa o con ~/
    var env = builder.Environment;
    opt.Path = ResolvePath(opt.Path, env);

    // Crear carpeta si no existe
    var dir = opt.Path;
    try
    {
        if (!string.IsNullOrWhiteSpace(dir))
        {
            var folder = Directory.Exists(dir) ? dir : Path.GetDirectoryName(dir);
            if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder!);
        }
    }
    catch { /* si no puede crear, el servicio que loguea debería manejar el error */ }
});

builder.Services.AddSignalR();
builder.Services.AddBackgroundQueue();
builder.Services.AddScoped<IEnvioService, EnvioService>();
builder.Services.AddScoped<IEnvioAuditService, EnvioAuditService>();

var app = builder.Build();

// --- PathBase si vino por config/ENV ---
if (!string.IsNullOrEmpty(basePath))
    app.UsePathBase(basePath);

// --- (Proxy) X-Forwarded-* y opcional X-Forwarded-Prefix ---
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});
app.Use(async (ctx, next) =>
{
    if (string.IsNullOrEmpty(basePath) &&
        ctx.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var prefix) &&
        !string.IsNullOrEmpty(prefix))
    {
        var p = prefix.ToString().TrimEnd('/');
        if (!ctx.Request.PathBase.HasValue && !string.IsNullOrWhiteSpace(p))
        {
            ctx.Request.PathBase = p;
        }
    }
    await next();
});

// --- Pipeline ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapHub<NotificacionHub>("/NotificacionHub");

app.UseMiddleware<ErrorHandlerMiddleware>();

// --- Cultura ---
System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

var cultureInfo = new CultureInfo("es-US");
cultureInfo.NumberFormat.NumberGroupSeparator = ",";
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

app.Run();
