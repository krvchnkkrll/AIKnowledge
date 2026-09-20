using Application;
using Assistant;
using Opensearch;
using Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder
    .AddApplication()
    .AddAssistant()
    .AddOpensearch()
    .AddWeb();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
