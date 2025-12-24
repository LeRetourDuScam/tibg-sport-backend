using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using TIBG.API.Core.DataAccess;
using TIBG.API.Core.Configuration;
using TIBG.Contracts.DataAccess;
using TIBG.ENTITIES;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Disable file watching in production to avoid inotify issues on Render
builder.Configuration.Sources
    .OfType<Microsoft.Extensions.Configuration.Json.JsonConfigurationSource>()
    .ToList()
    .ForEach(s => s.ReloadOnChange = false);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Configure CORS to allow requests from frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddMemoryCache();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }
        )
    );

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please wait a moment before trying again.",
            retryAfter = 60
        }, cancellationToken);
    };
});

builder.Services.Configure<GroqSettings>(builder.Configuration.GetSection("Groq"));
builder.Services.AddHttpClient<IAiRecommendationService, GroqAiService>();
builder.Services.AddHttpClient<IChatService, GroqChatService>();

builder.Services.AddDbContext<FytAiDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IAiRecommendationService, GroqAiService>();
builder.Services.AddScoped<IChatService, GroqChatService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();

builder.Services.AddOpenApi();

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS
app.UseCors("AllowAll");

// Use Rate Limiting
app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();
