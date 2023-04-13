using AutoEncodeAPI.Pipe;
using Microsoft.AspNetCore.HttpOverrides;
using System.Runtime.InteropServices;

string logFileLocation = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
                            @"/var/log/aeapi" :
                            $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\\AEAPI";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All;
    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IClientPipeManager, ClientPipeManager>();
builder.Services.AddSingleton<AutoEncodeUtilities.Logger.ILogger, AutoEncodeUtilities.Logger.Logger>(x => 
    ActivatorUtilities.CreateInstance<AutoEncodeUtilities.Logger.Logger>(x, logFileLocation, "aeapi.log", (long)102400, 2));
builder.Services.AddSwaggerGen();
// Allow long timeout to ensure Pipes can close
builder.Services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(45));
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseForwardedHeaders();
    app.UseExceptionHandler("/Error");
    //app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
