using GreenPipes;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;
using Polly;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//register repositories
builder.Services.AddMongo()
    .AddMongoRepository<InventoryItem>(collectionName: "inventoryitems")
    .AddMongoRepository<CatalogItem>(collectionName: "catalogitems")
    .AddMassTrannsitWithRabbitMq(retryConfigurator =>
    {
        retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
        retryConfigurator.Ignore(typeof(UnknownItemException));
    })
    .AddJwtBearerAuthentication();


AddCatalogClient(builder);

var app = builder.Build();

string AllowedOriginSetting = "AllowedOrigin";

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors(Corsbuilder =>
    {
        Corsbuilder.WithOrigins(builder.Configuration[AllowedOriginSetting])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

static void AddCatalogClient(WebApplicationBuilder builder)
{
    builder.Services.AddHttpClient<CatalogClient>(client =>
    {
        client.BaseAddress = new Uri("https://localhost:5001");

    })  //Implementing retries with exponential backoff
        .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt) =>
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                serviceProvider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
            }
        ))
        //Implementing the circuit breaker pattern
        .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(15),
            onBreak: (outcome, timespan) =>
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                serviceProvider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Opening the circuit for {timespan.TotalSeconds} seconds...");
            },
            onReset: () =>
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                serviceProvider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Closing the circuit...");
            }
        ))
        //Implementing a timeout policy via Polly
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(seconds: 1));
}