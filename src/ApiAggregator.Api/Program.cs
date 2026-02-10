using System.Text;
using ApiAggregator.Api.Clients;
using ApiAggregator.Api.Configuration;
using ApiAggregator.Api.Middleware;
using ApiAggregator.Api.Services;
using ApiAggregator.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// Configuration
// ============================================
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();

// ============================================
// JWT Authentication
// ============================================
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
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };
});

builder.Services.AddAuthorization();

// ============================================
// Core Services
// ============================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Aggregator",
        Version = "v1",
        Description = "A service that aggregates data from multiple external APIs (Weather, News, GitHub)",
        Contact = new OpenApiContact
        {
            Name = "API Support"
        }
    });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    // Include XML comments for API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Memory Cache
builder.Services.AddMemoryCache();

// ============================================
// Application Services (Singleton for thread-safe statistics)
// ============================================
builder.Services.AddSingleton<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IAggregationService, AggregationService>();

// ============================================
// Polly Resilience Policies
// ============================================

// Create a logger for Polly policies
var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole());
var pollyLogger = loggerFactory.CreateLogger("Polly");

// Retry policy: 3 retries with exponential backoff
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            pollyLogger.LogWarning(
                "Retry {RetryAttempt} after {DelaySeconds}s delay due to: {Reason}",
                retryAttempt, 
                timespan.TotalSeconds,
                outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
        });

// Circuit breaker: Opens after 5 failures, stays open for 30 seconds
var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
        onBreak: (outcome, timespan) =>
        {
            pollyLogger.LogWarning(
                "Circuit breaker opened for {DurationSeconds}s due to: {Reason}",
                timespan.TotalSeconds,
                outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
        },
        onReset: () => pollyLogger.LogInformation("Circuit breaker reset"));

// Timeout policy: 10 seconds per request
var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));

// Combined policy: Retry -> Circuit Breaker -> Timeout
var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);

// ============================================
// HTTP Clients with Polly policies
// ============================================
builder.Services.AddHttpClient<WeatherApiClient>()
    .AddPolicyHandler(combinedPolicy);

builder.Services.AddHttpClient<NewsApiClient>()
    .AddPolicyHandler(combinedPolicy);

builder.Services.AddHttpClient<GitHubApiClient>()
    .AddPolicyHandler(combinedPolicy);

// ============================================
// API Plugins (auto-discovered by AggregationService)
// To add a new API: 1) Create client implementing IApiPlugin
//                   2) Add registration line below
// ============================================
builder.Services.AddScoped<IApiPlugin>(sp => sp.GetRequiredService<WeatherApiClient>());
builder.Services.AddScoped<IApiPlugin>(sp => sp.GetRequiredService<NewsApiClient>());
builder.Services.AddScoped<IApiPlugin>(sp => sp.GetRequiredService<GitHubApiClient>());

// ============================================
// Build Application
// ============================================
var app = builder.Build();

// Exception handling middleware (first in pipeline)
app.UseExceptionHandling();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Aggregator v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
