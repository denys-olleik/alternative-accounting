using Accounting.Business;
using Accounting.Common;
using Accounting.Events;
using Accounting.Filters;
using Accounting.Service;
using Microsoft.AspNetCore.Authentication.Cookies;
using Newtonsoft.Json;
using static Accounting.Business.Claim;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
  options.Filters.Add<PreventIdentifiableResponseFilter>();
})
.AddNewtonsoftJson(options =>
{
  options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
  {
    NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
  };
}); //

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(options =>
  {
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.EventsType = typeof(CustomCookieAuthenticationEventsHandler);
    options.LoginPath = new PathString("/user-account/login");
    options.AccessDeniedPath = new PathString("/unauthorized");
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
  });

builder.Services.AddScoped<CustomCookieAuthenticationEventsHandler>();
builder.Services.AddScoped<RequestContext>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<BusinessEntityService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<JournalInvoiceInvoiceLinePaymentService>();
builder.Services.AddScoped<JournalInvoiceInvoiceLineService>();
builder.Services.AddScoped<JournalService>();
builder.Services.AddScoped<InvoiceAttachmentService>();
builder.Services.AddScoped<InvoiceLineService>();
builder.Services.AddScoped<InvoiceInvoiceLinePaymentService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<IronPdfService>();
builder.Services.AddScoped<ItemService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<PaymentTermsService>();
builder.Services.AddScoped<UserOrganizationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ReconciliationTransactionService>();
builder.Services.AddScoped<ReconciliationService>();
builder.Services.AddScoped<ReconciliationAttachmentService>();
builder.Services.AddScoped<JournalReconciliationTransactionService>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<RequestLogService>();
builder.Services.AddScoped<InventoryAdjustmentService>();
builder.Services.AddScoped<JournalInventoryAdjustmentService>();
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<SecretService>();
builder.Services.AddScoped<CloudServices>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<EmailService>();
//builder.Services.AddScoped<LoginWithoutPasswordService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<UserTaskService>();
builder.Services.AddScoped<ToDoService>();
builder.Services.AddScoped<ToDoTagService>();
builder.Services.AddScoped<BlogService>();
builder.Services.AddScoped<RequestLogService>();
builder.Services.AddScoped<ExceptionService>();
builder.Services.AddScoped<ClaimService>();
builder.Services.AddScoped<PlayerService>();

ConfigurationSingleton.Instance.ApplicationName =
    !string.IsNullOrEmpty(builder.Configuration["Whitelabel"])
    ? builder.Configuration["Whitelabel"]
    : builder.Configuration["ApplicationName5"];
ConfigurationSingleton.Instance.Whitelabel = builder.Configuration["Whitelabel"];
//ConfigurationSingleton.Instance.ConnectionStringDefaultPsql = builder.Configuration["ConnectionStrings:Psql"];
//ConfigurationSingleton.Instance.ConnectionStringAdminPsql = builder.Configuration["ConnectionStrings:AdminPsql"];
ConfigurationSingleton.Instance.DatabasePassword = builder.Configuration["DatabasePassword"];
ConfigurationSingleton.Instance.SpotifyClientID = builder.Configuration["SpotifyClientID"];
ConfigurationSingleton.Instance.SpotifyClientSecret = builder.Configuration["SpotifyClientSecret"];

#region Configure Paths
bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

string tempPath = isWindows ? builder.Configuration["TempPathWindows"]! : builder.Configuration["TempPathLinux"]!;
string permPath = isWindows ? builder.Configuration["PermPathWindows"]! : builder.Configuration["PermPathLinux"]!;

ConfigurationSingleton.Instance.TempPath = tempPath;
ConfigurationSingleton.Instance.PermPath = permPath;
#endregion

var app = builder.Build();

#region Reset-Database
if (app.Environment.IsDevelopment())
{
  var databaseResetConfigPath = Path.Combine(app.Environment.ContentRootPath, "database-reset.json");
  var databaseResetConfig = JsonConvert.DeserializeObject<DatabaseResetConfig>(System.IO.File.ReadAllText(databaseResetConfigPath));

  if (databaseResetConfig!.Reset)
  {
    DatabaseService databaseService = new DatabaseService();

    await databaseService.ResetDatabase();

    string sampleDataPath = Path.Combine(AppContext.BaseDirectory, "sample-data.sql");
    string sampleDataScript = System.IO.File.ReadAllText(sampleDataPath);

    await databaseService.RunSQLScript(sampleDataScript, DatabaseThing.DatabaseConstants.DatabaseName);

    databaseResetConfig.Reset = false;
    System.IO.File.WriteAllText(databaseResetConfigPath, JsonConvert.SerializeObject(databaseResetConfig, Formatting.Indented));
  }
}
#endregion

#region LoadTenantManagementConfiguration
ConfigurationSingleton.Instance.TenantManagement
    = Convert.ToBoolean(builder.Configuration["TenantManagement"]);
//if (!ConfigurationSingleton.Instance.TenantManagement)
//await LoadTenantManagementFromDatabase(app);
#endregion

// Exception handling
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler(errorApp =>
  {
    errorApp.Run(async context =>
    {
      // Get the exception
      var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
      var exception = exceptionHandlerPathFeature?.Error;

      // Log the exception
      //var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
      //logger.LogError(exception, "Unhandled exception");
      ExceptionService exceptionService = new ();
      await exceptionService.CreateAsync(new Accounting.Business.Exception()
      {
        Message = exception?.Message,
        StackTrace = exception?.StackTrace,
        InnerException = exception?.InnerException?.ToString(),
        Source = exception?.Source,
        TargetSite = exception?.TargetSite.ToString(),
        RequestLogId = (int?)context.Items["RequestLogId"],
      });

      // Redirect to the error page
      context.Response.Redirect("/Home/Error");
    });
  });
}
else
{
  app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
  var requestContext = context.RequestServices.GetRequiredService<RequestContext>();

  // for authenticated requests
  if (context.User.Identity?.IsAuthenticated == true)
  {
    var databaseNameClaim = context.User.Claims.FirstOrDefault(c => c.Type == CustomClaimTypeConstants.DatabaseName);
    var databasePasswordClaim = context.User.Claims.FirstOrDefault(c => c.Type == CustomClaimTypeConstants.DatabasePassword);

    requestContext.DatabaseName = databaseNameClaim.Value;
    requestContext.DatabasePassword = databasePasswordClaim.Value;
  }
  // for anonymous requests
  else
  {
    requestContext.DatabaseName = DatabaseThing.DatabaseConstants.DatabaseName;
    requestContext.DatabasePassword = builder.Configuration["DatabasePassword"];
  }

  await next();
});

app.UseMiddleware<UpdateOrganizationNameClaimMiddleware>();

app.Use(async (context, next) =>
{
  var logService = context.RequestServices.GetRequiredService<RequestLogService>();
  var sw = Stopwatch.StartNew();
  var originalBodyStream = context.Response.Body;

  using var responseBody = new MemoryStream();
  context.Response.Body = responseBody;

  // Create and persist the initial request log at the start of the request
  var remoteIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
    ?? context.Connection.RemoteIpAddress?.ToString();

  var log = new RequestLog
  {
    RemoteIp = remoteIp,
    CountryCode = "",
    Referer = context.Request.Headers["Referer"].ToString(),
    UserAgent = context.Request.Headers["User-Agent"].ToString(),
    Path = context.Request.Path,
    // Optionally add other initial fields
  };

  log = await logService.CreateAsync(log);
  context.Items["RequestLogId"] = log.RequestLogID;

  await next();

  sw.Stop();

  context.Response.Body.Seek(0, SeekOrigin.Begin);
  var responseLength = context.Response.Body.Length;
  context.Response.Body.Seek(0, SeekOrigin.Begin);
  await responseBody.CopyToAsync(originalBodyStream);

  // Update the initial log with new info
  log.StatusCode = context.Response.StatusCode.ToString();
  log.ResponseLengthBytes = responseLength;
  // (Optionally update CountryCode, etc.)

  int rowsUpdated = await logService.UpdateResponseAsync(log.RequestLogID, context.Response.StatusCode.ToString(), responseLength);
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();