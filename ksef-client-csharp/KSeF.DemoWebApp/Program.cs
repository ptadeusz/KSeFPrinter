using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.DI;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

builder.Services.AddKSeFClient(options =>
{
    options.BaseUrl = 
        builder.Configuration.GetSection("ApiSettings")
                .GetValue<string>("BaseUrl")
                ?? KsefEnviromentsUris.TEST;
    
    options.CustomHeaders =
        builder.Configuration
                .GetSection("ApiSettings:customHeaders")
                .Get<Dictionary<string, string>>()
              ?? new Dictionary<string, string>();
});

builder.Services.AddCryptographyClient(options =>
{
    options.WarmupOnStart = WarmupMode.NonBlocking; // domyślnie umożliwiamy kontynuację startu aplikacji bez względu na status pobierania certyfikatów publicznych KSeF.
},
async (serviceProvider, cancellationToken) =>
{
    var cryptographyClient = serviceProvider.GetRequiredService<ICryptographyClient>();
    return await cryptographyClient.GetPublicCertificatesAsync(cancellationToken);
});

builder.Services.AddHostedService(provider =>
{
    var cryptographyService = provider.GetRequiredService<ICryptographyService>();
    var options = provider.GetRequiredService<IOptions<CryptographyClientOptions>>();
    return new CryptographyWarmupHostedService(cryptographyService, options);
});

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase))
    );
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "KSeF.DemoWebApp.xml"));
    c.CustomSchemaIds(t =>
   (t.Namespace + "_" + t.Name)
     .Replace(".", "_")
     .Replace("+", "_")
     .Replace("`", "_")
 );
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
    options.SerializerOptions.AllowTrailingCommas = true;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;

    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

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
