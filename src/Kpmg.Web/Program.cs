using System.Security.Cryptography;
using Kpmg.Web.Data;
using Kpmg.Web.Options;
using Kpmg.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    // The customer service portal is only available to signed-in staff.
    options.Conventions.AuthorizeFolder("/Staff");
});

builder.Services.Configure<CouncilRoutingOptions>(builder.Configuration.GetSection(CouncilRoutingOptions.SectionName));
builder.Services.Configure<PhotoStorageOptions>(builder.Configuration.GetSection(PhotoStorageOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<StaffPortalOptions>(builder.Configuration.GetSection(StaffPortalOptions.SectionName));

// The customer service team always needs a usable (but never hard coded) access code.
var generatedStaffAccessCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
builder.Services.PostConfigure<StaffPortalOptions>(options =>
{
    if (string.IsNullOrWhiteSpace(options.AccessCode))
    {
        options.AccessCode = generatedStaffAccessCode;
    }
});

builder.Services.AddDbContext<ComplaintDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ComplaintDatabase")
                      ?? "Data Source=App_Data/complaints.db"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICouncilRoutingService, CouncilRoutingService>();
builder.Services.AddSingleton<IRequestNumberGenerator, RequestNumberGenerator>();
builder.Services.AddSingleton<IPhotoStorage, FileSystemPhotoStorage>();
builder.Services.AddSingleton<IEmailSender, PickupDirectoryEmailSender>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddSingleton<IStaffAccessCodeValidator, StaffAccessCodeValidator>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/StaffLogin";
        options.AccessDeniedPath = "/StaffLogin";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
    var db = scope.ServiceProvider.GetRequiredService<ComplaintDbContext>();
    db.Database.EnsureCreated();

    var staffOptions = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<StaffPortalOptions>>().CurrentValue;
    if (staffOptions.AccessCode == generatedStaffAccessCode)
    {
        app.Logger.LogWarning(
            "StaffPortal:AccessCode is not configured. Generated a temporary access code for this run: {AccessCode}",
            generatedStaffAccessCode);
    }
}

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

app.MapRazorPages();

// Photographs are private evidence, so they are only served to signed-in staff.
app.MapGet("/staff/photos/{id:int}", async (int id, ComplaintDbContext db, IPhotoStorage storage, CancellationToken cancellationToken) =>
{
    var photo = await db.ComplaintPhotos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    if (photo is null)
    {
        return Results.NotFound();
    }

    var stream = storage.OpenRead(photo.StoredFileName);
    return stream is null
        ? Results.NotFound()
        : Results.File(stream, photo.ContentType);
}).RequireAuthorization();

app.Run();
