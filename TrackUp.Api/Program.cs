using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Resend;
using TrackUp.Api_.Application.DTOS;
using TrackUp.Api_.Application.Interfaces;
using TrackUp.Api_.Application.Options;
using TrackUp.Api_.Application.validation.AuthValidation;
using TrackUp.Api_.Domain.Model;
using TrackUp.Api_.Infrastructure.Data;
using TrackUp.Api_.Infrastructure.IdentityAuth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen();

// Connect EF Core to PostgreSQL
builder.Services.AddDbContext<TrackUpDataContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(24);
});
var resendApiKey = builder.Configuration["Resend:ApiKey"];
//IConfiguration
//Reads your settings
//It retrieves the API key and sender information from configuration sources such as User Secrets.

if (string.IsNullOrWhiteSpace(resendApiKey))
{
    throw new InvalidOperationException(
        "Resend API key is missing from configuration.");
}

builder.Services.AddResend(options =>
{
    options.ApiToken = resendApiKey;
});

builder.Services.Configure<GoogleAuthOptions>(
    builder.Configuration.GetSection(GoogleAuthOptions.SectionName));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<AuthValidateSignUpRequest>();
builder.Services.AddScoped<AuthValidateGoogleSignUpRequest>();
builder.Services.AddScoped<SignUpResponse>();
builder.Services.AddScoped<IGoogleIdTokenVerifier, GoogleIdTokenVerifier>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IdentityAuth>();
builder.Services.AddScoped<EmailConfirmationService>();
//sign to Identity we need to connect the Application USer With a EF Core DbContext
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddEntityFrameworkStores<TrackUpDataContext>()
    .AddDefaultTokenProviders();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
    throw new InvalidOperationException("Jwt:SecretKey is missing from configuration.");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.NameIdentifier
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                context.Fail("Missing user identity.");
                return;
            }

            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                context.Fail("Account is unavailable.");
        }
    };
});
builder.Services.AddAuthorization();
    // This registers providers that generate and validate tokens for specific Identity operations, such as:
    //-Password reset.
    //- Email confirmation.
    //- Changing an email address.
    //- Two-factor authentication, where applicable.
var app = builder.Build();


var requestLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("RequestDiagnostics");

app.Use(async (context, next) =>
{
    requestLogger.LogInformation(
        "HTTP REQUEST: {Method} {Path}",
        context.Request.Method,
        context.Request.Path
    );

    try
    {
        await next();
    }
    catch (Exception ex)
    {
        requestLogger.LogError(
            ex,
            "HTTP REQUEST FAILED: {Method} {Path}",
            context.Request.Method,
            context.Request.Path
        );

        throw;
    }

    requestLogger.LogInformation(
        "HTTP RESPONSE: {Method} {Path} returned {StatusCode}",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode
    );
}); 
// Configure the HTTP request pipeline.
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exceptionFeature =
            context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");

        if (exceptionFeature?.Error is not null)
        {
            logger.LogError(
                exceptionFeature.Error,
                "Unhandled exception while processing {Path}.",
                context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(
            new SignUpResponse().CustomResponse("INTERNAL_ERROR"));
    });
});

// if (app.Environment.IsDevelopment())
    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseAuthentication();        
app.UseAuthorization();

app.MapControllers();

app.Run();
