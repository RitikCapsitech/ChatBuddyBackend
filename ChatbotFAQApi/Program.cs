using ChatbotFAQApi.Models;
using ChatbotFAQApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ? MongoDB Settings
builder.Services.Configure<FaqDatabaseSettings>(
    builder.Configuration.GetSection("FaqDatabaseSettings"));

// ? Register MongoDB Service
builder.Services.AddSingleton<FaqService>();
builder.Services.AddSingleton<ChatSessionService>();

// ? Add Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o => o.AddPolicy("Public",
         p => p.WithOrigins("https://ritikcapsitech.github.io", "https://your-site.com")
               .AllowAnyHeader().AllowAnyMethod()));

// Auto-bind to Render's PORT if present so ASPNETCORE_URLS is not required
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

// Fallback: if ConnectionString is empty, accept standard env names like MONGODB_URI
builder.Services.PostConfigure<FaqDatabaseSettings>(opts =>
{
    if (string.IsNullOrWhiteSpace(opts.ConnectionString))
    {
        var alt = Environment.GetEnvironmentVariable("MONGODB_URI")
                  ?? Environment.GetEnvironmentVariable("MONGO_URI")
                  ?? Environment.GetEnvironmentVariable("MONGO_URL");
        if (!string.IsNullOrWhiteSpace(alt))
        {
            opts.ConnectionString = alt;
        }
    }
});

var app = builder.Build();

// ? Swagger UI (Development or when ENABLE_SWAGGER=true)
var enableSwagger = app.Environment.IsDevelopment() ||
                    builder.Configuration.GetValue<bool>("ENABLE_SWAGGER", false);
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//app.UseCors(p => p
//    .AllowAnyMethod()
//    .AllowAnyHeader()
//    .SetIsOriginAllowed(_ => true) // allow any origin
//    .AllowCredentials()
//    .SetPreflightMaxAge(TimeSpan.FromSeconds(600))
//    .WithExposedHeaders("Content-Disposition"));
app.UseCors("Public");

// ? Middleware Pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
