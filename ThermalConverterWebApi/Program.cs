using Microsoft.OpenApi.Models;
using ThermalConverter;
using ThermalConverterWebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvc();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
});


var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapControllers();
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "API V1");
});



var maxObjectsCountStr = 
    Environment.GetEnvironmentVariable("ThermalConverter.maxObjectsCountPerMessage", EnvironmentVariableTarget.User)
    ?? "400";
if(int.TryParse(maxObjectsCountStr, out var maxObjectsCount))
{
    ReportGenerator.maxObjectsCountPerMessage = maxObjectsCount;
}
else
{
    throw new ArgumentException("ThermalConverter.maxObjectsCountPerMessage");
}


app.Run();