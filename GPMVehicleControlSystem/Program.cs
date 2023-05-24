using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.Tools.Database;
using GPMVehicleControlSystem.Models;
using GPMVehicleControlSystem.Models.Buzzer;
using GPMVehicleControlSystem.Models.Emulators;
using GPMVehicleControlSystem.Tools;
using GPMVehicleControlSystem.ViewModels;
using Microsoft.AspNetCore.Http.Json;
using System.Reflection;

StaStored.APPVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
var builder = WebApplication.CreateBuilder(args);

AlarmManager.LoadAlarmList(AppSettingsHelper.GetValue<string>("VCS:AlarmList_json_Path"));

//StaEmuManager.Start();

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

app.UseSwagger();
app.UseSwaggerUI();

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
