using BookApi.Api.Data;
using BookApi.Api.Repositories;
using BookApi.Api.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:9999");

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

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(contentDirectory),
    RequestPath = "/Content"
});

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.Run();
