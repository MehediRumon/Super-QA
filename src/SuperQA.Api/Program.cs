using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;
using SuperQA.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("https://localhost:5001", "http://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    
    // Allow browser extensions to access the API
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useInMemoryDb = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");

if (useInMemoryDb || string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<SuperQADbContext>(options =>
        options.UseInMemoryDatabase("SuperQA"));
}
else
{
    builder.Services.AddDbContext<SuperQADbContext>(options =>
        options.UseSqlServer(connectionString, b => b.MigrationsAssembly("SuperQA.Infrastructure")));
}

// Register services
builder.Services.AddHttpClient<IMCPService, MCPService>();
builder.Services.AddScoped<IAITestGeneratorService, AITestGeneratorService>();
builder.Services.AddScoped<ITestExecutionService, TestExecutionService>();
builder.Services.AddSingleton<IBackgroundTestRunner, BackgroundTestRunnerService>();
builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IPlaywrightTestExecutor, PlaywrightTestExecutor>();
builder.Services.AddScoped<IPageInspectorService, PageInspectorService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
// Use AllowAll policy to support both Blazor app and browser extensions
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
