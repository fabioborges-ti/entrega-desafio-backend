using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Common.HealthChecks;
using Ambev.DeveloperEvaluation.Common.Logging;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.IoC;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;
using Ambev.DeveloperEvaluation.WebApi.StockAlert;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Ambev.DeveloperEvaluation.WebApi.Configuration;

public static class ServiceRegistrationExtensions
{
    public static WebApplicationBuilder AddWebApiServiceRegistrations(this WebApplicationBuilder builder)
    {
        builder.AddBasicHealthChecks();

        builder.Services
            .AddMessagingInfrastructure(builder.Configuration)
            .AddPresentation()
            .AddApiDocumentation()
            .AddPersistenceAndHealthChecks(builder.Configuration)
            .AddApplicationPipelines();

        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.RegisterDependencies();

        return builder;
    }

    private static IServiceCollection AddMessagingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<SalesMessagingRetryOptions>(
            configuration.GetSection(SalesMessagingRetryOptions.SectionName));

        services.AddSingleton<ISalesMessageStatusStore, InMemorySalesMessageStatusStore>();
        services.AddSingleton<ISaleCommandPublisher, RabbitMqSaleCommandPublisher>();
        services.AddHostedService<SaleCommandConsumerBackgroundService>();

        services.Configure<StockAlertOptions>(configuration.GetSection(StockAlertOptions.SectionName));
        services.AddHttpClient<IStockAlertEmailService, BrevoStockAlertEmailService>();
        services.AddHostedService<StockAlertHostedService>();

        return services;
    }

    private static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        services.AddEndpointsApiExplorer();

        return services;
    }

    private static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Developer Evaluation API",
                Version = "v1",
                Description =
                    "API REST para o setor de varejo, projetada para suportar autenticação, gestão de catálogo e execução do ciclo completo de vendas, garantindo rastreabilidade e observabilidade dos processos.\n\n" +
                    "### A solução contempla\n\n" +
                    "- **Autenticação e autorização**\n" +
                    "  Implementação de autenticação via JWT com controle de acesso baseado em perfis (RBAC).\n\n" +
                    "- **Gestão de domínio**\n" +
                    "  Gerenciamento completo de usuários, produtos, categorias, carrinhos, estoque, clientes e filiais.\n\n" +
                    "- **Processamento de vendas assíncrono**\n" +
                    "  Execução das operações de venda de forma assíncrona utilizando RabbitMQ, com uso de correlationId para rastreamento e acompanhamento do processamento.\n\n" +
                    "- **Observabilidade e saúde da aplicação**\n" +
                    "  Disponibilização de endpoints para health checks e monitoramento, garantindo suporte operacional e alta confiabilidade."
            });
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT no cabeçalho Authorization: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    private static IServiceCollection AddPersistenceAndHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DefaultContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("Ambev.DeveloperEvaluation.ORM")
            )
        );

        services.AddHealthChecks()
            .AddDbContextCheck<DefaultContext>(
                name: "postgresql",
                tags: ["readiness"])
            .AddRabbitMQ(
                async sp =>
                {
                    var rabbit = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                    var factory = new ConnectionFactory
                    {
                        HostName = rabbit.HostName,
                        Port = rabbit.Port,
                        UserName = rabbit.UserName,
                        Password = rabbit.Password,
                        VirtualHost = rabbit.VirtualHost
                    };
                    return await factory.CreateConnectionAsync();
                },
                name: "rabbitmq",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["readiness"]);

        services.AddHealthChecksUI()
            .AddInMemoryStorage();

        return services;
    }

    private static IServiceCollection AddApplicationPipelines(this IServiceCollection services)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(Program).Assembly,
            typeof(ApplicationLayer).Assembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(ApplicationLayer).Assembly,
                typeof(Program).Assembly
            );
        });
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MediatRLoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}

