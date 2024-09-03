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

            // ����������� IFileProvider ��� ������� � ������ � �������� "Data"
            builder.Services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Data")));

            // ����������� �������
            builder.Services.AddScoped<IBalanceService, BalanceService>();

            // ���������� ������������ � �����������
            builder.Services.AddControllers()
                .AddXmlSerializerFormatters(); // �������� ��������� XML

            // ��������� Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });

            // ��������� ������������ �����������
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(); // ��������� ���������� �����������
            builder.Logging.AddDebug();   // ��������� ���������� �����������

            var app = builder.Build();

            // �������� ILogger �� DI ����������
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            // �������� �������� �������
            app.Use(async (context, next) =>
            {
                logger.LogInformation("������: {Method} {Path}", context.Request.Method, context.Request.Path);
                await next.Invoke();
                logger.LogInformation("�����: {StatusCode}", context.Response.StatusCode);
            });

            // �������� ������ � Swagger ������
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/swagger"))
                {
                    logger.LogInformation("������ � Swagger �����: {Path}", context.Request.Path);
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

            // ��������� Swagger Middleware
            app.UseSwagger();

            // ��������� Swagger UI
            app.UseSwaggerUI(c =>
            {
                // ������� ���� � ������ YAML �����
                c.SwaggerEndpoint("/swagger/swagger.yaml", "Balance API v1");
                c.RoutePrefix = string.Empty; // Swagger ����� �������� �� ����� ���������� ("/")
            });


            app.MapControllers();

            app.Run();
        }
    }
}
