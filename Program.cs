using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UnifiedMonitoring.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавление сервисов
builder.Services.AddSingleton<IWalletMonitoringService, WalletMonitoringService>();
builder.Services.AddHostedService<MonitoringBackgroundService>();
builder.Services.AddSingleton<ITelegramService, TelegramService>();
builder.Services.AddSingleton<IWalletMonitoringService, WalletMonitoringService>();
builder.Services.AddHostedService<MonitoringBackgroundService>();


var app = builder.Build();

// Настройка HTTP-запросов
app.MapGet("/", () => "Программа мониторинга работает");

// Запуск приложения
app.Run();
