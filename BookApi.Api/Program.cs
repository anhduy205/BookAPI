using BookApi.Api.Data;
using BookApi.Api.Repositories;
using BookApi.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:9999");
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();

var app = builder.Build();

var contentDirectory = Path.Combine(app.Environment.ContentRootPath, "Content");
var imageDirectory = Path.Combine(contentDirectory, "ImageBooks");
Directory.CreateDirectory(imageDirectory);

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalException");
        if (feature?.Error is not null)
        {
            logger.LogError(feature.Error, "Unhandled request error.");
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            message = "The API is running, but the database request failed.",
            detail = feature?.Error?.Message
        });
    });
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(contentDirectory),
    RequestPath = "/Content"
});

var databaseReady = false;
var startupMessage = "API is running.";

try
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
    databaseReady = true;
    startupMessage = "API and database are ready.";
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex, "Database initialization failed. API will continue to run, but database-backed endpoints may fail until SQL Server is available.");
    startupMessage = "API is running, but database initialization failed. Check SQL Server connection settings.";
}

app.MapGet("/", () => Results.Ok(new
{
    message = startupMessage,
    databaseReady,
    port = 9999
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = databaseReady ? "Healthy" : "Degraded",
    databaseReady
}));

app.MapControllers();

app.Run();
