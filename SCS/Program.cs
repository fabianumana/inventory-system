using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Mail;
using System.Net;
using SCS.Services;
using Microsoft.EntityFrameworkCore;
using SCS.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using DinkToPdf.Contracts;
using DinkToPdf;
using SCS.Middleware;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//Conectar la base de datos
builder.Services.AddDbContextFactory<Service>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BloggingDatabase")));

// Configuración de la autenticación
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options => {
    options.LoginPath = "/Acceso/Login";
    options.AccessDeniedPath = "/Acceso/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    options.SlidingExpiration = true;
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.MaxAge = null;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

builder.Services.AddScoped<Correos>(); 
builder.Services.AddScoped<IEmailSender>(provider => provider.GetRequiredService<Correos>());

builder.Services.AddScoped<BitacorasService>();

builder.Services.AddScoped<FabricantesService>();

builder.Services.AddScoped<AlmacenamientoService>();

var gmailSettings = builder.Configuration.GetSection("SmtpSettings:Gmail").Get<SmtpSettings>();
if (gmailSettings == null)
{
    throw new InvalidOperationException("No se pudo cargar la configuración de SMTP para Gmail.");
}

builder.Services
    .AddFluentEmail(gmailSettings.Username)
    .AddRazorRenderer()
    .AddSmtpSender(new SmtpClient()
    {
        Host = gmailSettings.Host,
        Port = gmailSettings.Port,
        Credentials = new NetworkCredential(gmailSettings.Username, gmailSettings.Password),
        EnableSsl = gmailSettings.EnableSsl,
        UseDefaultCredentials = false
    });

var app = builder.Build();

app.UseMiddleware<RedirigirRegistrar>();
app.UseMiddleware<AprobacionRol>();

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
    pattern: "{controller=Acceso}/{action=Registrar}/{id?}");

app.Run();
