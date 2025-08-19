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
