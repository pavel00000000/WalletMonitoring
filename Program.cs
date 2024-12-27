using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UnifiedMonitoring.Services;

var builder = WebApplication.CreateBuilder(args);

// ���������� ��������
builder.Services.AddSingleton<IWalletMonitoringService, WalletMonitoringService>();
builder.Services.AddHostedService<MonitoringBackgroundService>();
builder.Services.AddSingleton<ITelegramService, TelegramService>();
builder.Services.AddSingleton<IWalletMonitoringService, WalletMonitoringService>();
builder.Services.AddHostedService<MonitoringBackgroundService>();


var app = builder.Build();

// ��������� HTTP-��������
app.MapGet("/", () => "��������� ����������� ��������");

// ������ ����������
app.Run();
