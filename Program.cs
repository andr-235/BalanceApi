using BalanceApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.IO;

namespace BalanceApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Регистрация IFileProvider для доступа к файлам в каталоге "Data"
            builder.Services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Data")));

            // Регистрация сервиса
            builder.Services.AddScoped<IBalanceService, BalanceService>();

            // Добавление контроллеров и форматтеров
            builder.Services.AddControllers()
                .AddXmlSerializerFormatters(); // Добавить поддержку XML

            // Добавляем Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });

            // Добавляем конфигурацию логирования
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(); // Добавляем консольное логирование
            builder.Logging.AddDebug();   // Добавляем отладочное логирование

            var app = builder.Build();

            // Получаем ILogger из DI контейнера
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            // Логируем входящие запросы
            app.Use(async (context, next) =>
            {
                logger.LogInformation("Запрос: {Method} {Path}", context.Request.Method, context.Request.Path);
                await next.Invoke();
                logger.LogInformation("Ответ: {StatusCode}", context.Response.StatusCode);
            });

            // Логируем доступ к Swagger файлам
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/swagger"))
                {
                    logger.LogInformation("Запрос к Swagger файлу: {Path}", context.Request.Path);
                }
                await next.Invoke();
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "swagger")),
                RequestPath = "/swagger"
            });

            // Добавляем Swagger Middleware
            app.UseSwagger();

            // Добавляем Swagger UI
            app.UseSwaggerUI(c =>
            {
                // Укажите путь к вашему YAML файлу
                c.SwaggerEndpoint("/swagger/swagger.yaml", "Balance API v1");
                c.RoutePrefix = string.Empty; // Swagger будет доступен по корню приложения ("/")
            });


            app.MapControllers();

            app.Run();
        }
    }
}
