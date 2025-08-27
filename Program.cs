using Microsoft.EntityFrameworkCore;
using SimulacaoCredito.Api.Infrastructure.Persistence;
using System.Diagnostics;
using SimulacaoCredito.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API de Crédito",
        Version = "v1"
    });
});

// EF Core (SQLite) para persistência local
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("SqlLocal")));

// (Opcional) Interface de repositório ainda pode existir, mas agora via EF:
builder.Services.AddScoped<ISimulacaoRepository, EfSimulacaoRepository>();

builder.Services.AddSingleton<RequestTelemetria>();

builder.Services.AddSingleton<IEventHubProducer, EventHubProducer>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Crédito v1"));
}

app.Use(async (ctx, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();

    var ep = $"{ctx.Request.Path.Value?.ToLowerInvariant()} {ctx.Request.Method}";
    var status = ctx.Response.StatusCode;
    var telem = ctx.RequestServices.GetRequiredService<RequestTelemetria>();
    telem.Record(ep, sw.Elapsed.TotalMilliseconds, status);
});


app.UseHttpsRedirection();
app.MapControllers();

// cria o banco/tabelas se não existirem
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // simples e suficiente para hackathon
}

app.Run();
