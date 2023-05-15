using GPMVehicleControlSystem.Models.Database;
using GPMVehicleControlSystem.Models;
using GPMVehicleControlSystem.Models.Alarm;
using GPMVehicleControlSystem.Models.Buzzer;
using GPMVehicleControlSystem.Models.Emulators;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

AlarmManager.LoadAlarmList();
//StaEmuManager.StartWagoEmu();
//StaEmuManager.StartAGVROSEmu();

BuzzerPlayer.Initialize();
DBhelper.Initialize();

StaStored.CurrentVechicle = new GPMVehicleControlSystem.Models.VehicleControl.Vehicle();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.PropertyNameCaseInsensitive = false;
    options.SerializerOptions.WriteIndented = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.UseWebSockets();
app.UseDefaultFiles(new DefaultFilesOptions()
{
});
app.UseStaticFiles();

//app.UseHttpsRedirection();
app.UseCors(c => c.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
app.MapControllers();

app.Run();
