using historical_prices.Clients;
using historical_prices.Data;
using historical_prices.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddHttpClient<AuthService>();
builder.Services.AddSingleton<AuthService>();

builder.Services.AddHttpClient<FintachartsRestApiClient>();
builder.Services.AddScoped<FintachartsRestApiClient>();
builder.Services.AddScoped<FintachartsApiService>();

builder.Services.AddScoped<AssetSyncService>();

builder.Services.AddScoped<PriceService>();
builder.Services.AddScoped<PriceCacheService>();
builder.Services.AddSingleton<PriceAggregator>();

builder.Services.AddHostedService<FintachartsWebSocketClientService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
