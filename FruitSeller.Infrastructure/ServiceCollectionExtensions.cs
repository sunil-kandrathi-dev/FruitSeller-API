using FruitSeller.Application.Services;
using FruitSeller.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FruitSeller.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFruitSellerInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IFruitService, FruitService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ISupplierPaymentService, SupplierPaymentService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
