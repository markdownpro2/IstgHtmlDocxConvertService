using IstgHtmlDocxConvertService.Logging;
using IstgHtmlDocxConvertService.Services;
using IstgHtmlDocxConvertService.WebSockets;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen( c =>
{
    c.EnableAnnotations();
});

builder.Services.AddSingleton<SystemEventLogger>();
builder.Services.AddSingleton<SessionStorageService>();
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddScoped<ConversionService>();
builder.Services.AddTransient<HtmlService>();
builder.Services.AddScoped<WebSocketHandler>();
builder.Services.AddScoped<TokenValidationService>();

// Read allowed origins from config
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

// Add and configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins(allowedOrigins ?? Array.Empty<string>())
               .AllowAnyHeader()
               .AllowAnyMethod();
    });

});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var scope = app.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<WebSocketHandler>();

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await handler.HandleAsync(webSocket);
    }
    else
    {
        await next();
    }
});


app.UseAuthorization();

app.MapControllers();

// Read the License Path from appsettings.json
var licensePath = builder.Configuration["Aspose:LicensePath"];

// Set the Aspose license
if (File.Exists(licensePath))
{
    new Aspose.Words.License().SetLicense(licensePath);
}
else
{
    // Handle the case where the license path is missing or invalid
    Console.WriteLine("License path is missing or invalid.");
}

app.Run();
