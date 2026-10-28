# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EskaCMS.WebHost/EskaCMS.WebHost.csproj", "EskaCMS.WebHost/"]
COPY ["Entitys/EskaCMS.Entities.csproj", "Entitys/"]
COPY ["EskaCMS.Infrastructure/EskaCMS.Infrastructure.csproj", "EskaCMS.Infrastructure/"]
COPY ["EskaCMS.Filters/EskaCMS.Filters.csproj", "EskaCMS.Filters/"]
COPY ["EskaCMS.Chat/EskaCMS.Chat.csproj", "EskaCMS.Chat/"]
COPY ["EskaCMSModules/EskaCMS.Core/EskaCMS.Core.csproj", "EskaCMSModules/EskaCMS.Core/"]
COPY ["EskaCMS.DataAccess/EskaCMS.DataAccess.csproj", "EskaCMS.DataAccess/"]
COPY ["EskaCMSModules/EskaCMS.Security/EskaCMS.Security.csproj", "EskaCMSModules/EskaCMS.Security/"]
COPY ["EskaCommerceModules/EskaCommerce.LoyaltyPoints/EskaCommerce.LoyaltyPoints.csproj", "EskaCommerceModules/EskaCommerce.LoyaltyPoints/"]
COPY ["EskaCommerceModules/EskaCommerce.Notifications/EskaCommerce.Notifications.csproj", "EskaCommerceModules/EskaCommerce.Notifications/"]
COPY ["EskaCMSModules/EskaCMS.EmailSender.SMTP/EskaCMS.EmailSender.SMTP.csproj", "EskaCMSModules/EskaCMS.EmailSender.SMTP/"]
COPY ["EskaCMSModules/EskaCommerce.SignalR/EskaCMS.SignalR.csproj", "EskaCMSModules/EskaCommerce.SignalR/"]
COPY ["EskaCommerceModules/EskaCommerce.HangfireJobs/EskaCommerce.HangfireJobs.csproj", "EskaCommerceModules/EskaCommerce.HangfireJobs/"]
COPY ["EskaCMSModules/EskaCMS.Applications/EskaCMS.Sites.csproj", "EskaCMSModules/EskaCMS.Applications/"]
COPY ["EskaCMSModules/EskaCMS.BlogArchive1/EskaCMS.BlogArchive.csproj", "EskaCMSModules/EskaCMS.BlogArchive1/"]
COPY ["EskaCMSModules/EskaCMS.Pages/EskaCMS.Pages.csproj", "EskaCMSModules/EskaCMS.Pages/"]
COPY ["EskaCMSModules/EskaCMS.Component/EskaCMS.Components.csproj", "EskaCMSModules/EskaCMS.Component/"]
COPY ["EskaCMSModules/ESKACMS.Elements/EskaCMS.Elements.csproj", "EskaCMSModules/ESKACMS.Elements/"]
COPY ["EskaCMSModules/EskaCMSMedia/EskaCMS.Media.csproj", "EskaCMSModules/EskaCMSMedia/"]
COPY ["EskaCMSModules/EskaCMS.Theming/EskaCMS.Theming.csproj", "EskaCMSModules/EskaCMS.Theming/"]
COPY ["EskaCMSModules/EskaCMS.UserDashboard/EskaCMS.UserDashboard.csproj", "EskaCMSModules/EskaCMS.UserDashboard/"]
COPY ["EskaCMSModules/EskaCMS.Catalog/EskaCommerce.Catalog.csproj", "EskaCMSModules/EskaCMS.Catalog/"]
COPY ["EskaCMSModules/EskaCMS.Tax/EskaCMS.Tax.csproj", "EskaCMSModules/EskaCMS.Tax/"]
COPY ["EskaCMSModules/EskaCMS.Addresses/EskaCMS.Addresses.csproj", "EskaCMSModules/EskaCMS.Addresses/"]
COPY ["EskaCMSModules/EskaCMS.Forms/EskaCMS.Forms.csproj", "EskaCMSModules/EskaCMS.Forms/"]
COPY ["EskaCMSModules/WorkFlow/EskaCMS.WorkFlow.csproj", "EskaCMSModules/WorkFlow/"]
COPY ["EskaCMSModules/EskaCMS.Localization/EskaCMS.Localization.csproj", "EskaCMSModules/EskaCMS.Localization/"]
COPY ["EskaCms.CoreShared/EskaCms.CoreShared.csproj", "EskaCms.CoreShared/"]
COPY ["EskaCMS.Shared/EskaCMS.ADIntegration.csproj", "EskaCMS.Shared/"]
COPY ["EskaCMSModules/EskaCMS.DocumentsNumbering/EskaCMS.DocumentsNumbering.csproj", "EskaCMSModules/EskaCMS.DocumentsNumbering/"]
COPY ["EskaCMS.DynamicForms/EskaCMS.DynamicForms.csproj", "EskaCMS.DynamicForms/"]
COPY ["EskaCMS.EskaBillingIntegrations/EskaCMS.EskaBilling.csproj", "EskaCMS.EskaBillingIntegrations/"]
COPY ["EskaCMS.HOC/EskaCMS.HOC.csproj", "EskaCMS.HOC/"]
COPY ["EskaCMS.ActiveDirectory/EskaDCMSActiveDirectory.csproj", "EskaCMS.ActiveDirectory/"]
COPY ["EskaCMS.Middlewares/EskaCMS.Middlewares.csproj", "EskaCMS.Middlewares/"]
COPY ["EskaCMS.NotificationManager/EskaCMS.NotificationManager.csproj", "EskaCMS.NotificationManager/"]
COPY ["EskaCMS.RateLimit/EskaCMS.RateLimit.csproj", "EskaCMS.RateLimit/"]
COPY ["EskaCMSModules/EskaCMS.BLL/EskaCMS.General.csproj", "EskaCMSModules/EskaCMS.BLL/"]
COPY ["EskaCMSModules/EskaCMS.Menus/EskaCMS.Menus.csproj", "EskaCMSModules/EskaCMS.Menus/"]
COPY ["EskaCMSModules/EskaCMS.Survey/EskaCMS.Survey.csproj", "EskaCMSModules/EskaCMS.Survey/"]
COPY ["EskaCMSModules/EskaCMS.Dashbaord/EskaCMS.Dashboard.csproj", "EskaCMSModules/EskaCMS.Dashbaord/"]
COPY ["EskaCMSModules/EskaCMS.EskaCoreReports/EskaCMS.EskaCoreReports.csproj", "EskaCMSModules/EskaCMS.EskaCoreReports/"]
COPY ["EskaCMSModules/EskaCMS.Events/EskaCMS.Events.csproj", "EskaCMSModules/EskaCMS.Events/"]
COPY ["EskaCMSModules/EskaCMS.LocalStorage/EskaCMS.FileStorage.csproj", "EskaCMSModules/EskaCMS.LocalStorage/"]
COPY ["EskaCMSModules/ESKACMS.Logging/EskaCMS.Logging.csproj", "EskaCMSModules/ESKACMS.Logging/"]
COPY ["EskaCMSModules/EskaCMS.Search/EskaCMS.Search.csproj", "EskaCMSModules/EskaCMS.Search/"]
COPY ["EskaCommerce.PaymentMEPS/EskaCommerce.PaymentMEPS.csproj", "EskaCommerce.PaymentMEPS/"]
COPY ["EskaCommerceModules/EskaCommerce.Orders/EskaCommerce.Orders.csproj", "EskaCommerceModules/EskaCommerce.Orders/"]
COPY ["EskaCommerce.DomesticShipment/EskaCommerce.DomesticShipment.csproj", "EskaCommerce.DomesticShipment/"]
COPY ["EskaCommerceModules/EskaCommerce.DinkToPdf/EskaCommerce.DinkToPdf.csproj", "EskaCommerceModules/EskaCommerce.DinkToPdf/"]
COPY ["EskaCommerceModules/EskaCommerce.Discount/EskaCommerce.Discount.csproj", "EskaCommerceModules/EskaCommerce.Discount/"]
COPY ["EskaCommerceModules/EskaCommerce.Inventory/EskaCommerce.Inventory.csproj", "EskaCommerceModules/EskaCommerce.Inventory/"]
COPY ["EskaCommerceModules/EskaCommerce.Vendors/EskaCommerce.Vendors.csproj", "EskaCommerceModules/EskaCommerce.Vendors/"]
COPY ["EskaCommerceModules/EskaCommerce.Payments/EskaCommerce.Payments.csproj", "EskaCommerceModules/EskaCommerce.Payments/"]
COPY ["EskaCommerceModules/EskaCommerce.ShoppingCart/EskaCommerce.ShoppingCart.csproj", "EskaCommerceModules/EskaCommerce.ShoppingCart/"]
COPY ["EskaCommerce.Aramex/EskaCommerce.Aramex.csproj", "EskaCommerce.Aramex/"]
COPY ["EskaCommerceModules/EskaCommerce.Pricing/EskaCommerce.Pricing.csproj", "EskaCommerceModules/EskaCommerce.Pricing/"]
COPY ["EskaCommerceModules/EskaCommerce.ShippingPrices/EskaCommerce.ShippingPrices.csproj", "EskaCommerceModules/EskaCommerce.ShippingPrices/"]
COPY ["EskaCommerceModules/EskaCommerce.Shipping/EskaCommerce.Shipping.csproj", "EskaCommerceModules/EskaCommerce.Shipping/"]
COPY ["EskaCommerceModules/EskaCMS.UserCatalog/EskaCommerce.UserCatalog.csproj", "EskaCommerceModules/EskaCMS.UserCatalog/"]
COPY ["EskaCommerceModules/EskaCommerce.Reviews/EskaCommerce.Reviews.csproj", "EskaCommerceModules/EskaCommerce.Reviews/"]
COPY ["EskaCommerceModules/EskaCommerce.ActivityLog/EskaCommerce.ActivityLog.csproj", "EskaCommerceModules/EskaCommerce.ActivityLog/"]
COPY ["EskaCommerceModules/EskaCommerce.Comments/EskaCommerce.Comments.csproj", "EskaCommerceModules/EskaCommerce.Comments/"]
COPY ["EskaCommerceModules/EskaCommerce.PaymentCoD/EskaCommerce.PaymentCoD.csproj", "EskaCommerceModules/EskaCommerce.PaymentCoD/"]
COPY ["EskaCommerceModules/EskaCommerce.PaymentHyperPay/EskaCommerce.PaymentHyperPay.csproj", "EskaCommerceModules/EskaCommerce.PaymentHyperPay/"]
COPY ["EskaCommerceModules/EskaCommerce.Shipments/EskaCommerce.Shipments.csproj", "EskaCommerceModules/EskaCommerce.Shipments/"]
COPY ["EskaCommerceModules/EskaCommerce.ProductComparison/EskaCommerce.ProductComparison.csproj", "EskaCommerceModules/EskaCommerce.ProductComparison/"]
COPY ["EskaCommerceModules/EskaCommerce.ProductRecentlyViewed/EskaCommerce.ProductRecentlyViewed.csproj", "EskaCommerceModules/EskaCommerce.ProductRecentlyViewed/"]
COPY ["EskaCommerceModules/EskaCommerce.ShippingFree/EskaCommerce.ShippingFree.csproj", "EskaCommerceModules/EskaCommerce.ShippingFree/"]
COPY ["EskaCommerceModules/EskaCommerce.ShippingJeebly/EskaCommerce.ShippingJeebly.csproj", "EskaCommerceModules/EskaCommerce.ShippingJeebly/"]
COPY ["EskaCommerceModules/EskaCommerce.ShippingTableRate/EskaCommerce.ShippingTableRate.csproj", "EskaCommerceModules/EskaCommerce.ShippingTableRate/"]
COPY ["EskaCommerceModules/EskaCommerce.TelrPayment/EskaCommerce.TelrPayment.csproj", "EskaCommerceModules/EskaCommerce.TelrPayment/"]
COPY ["EskaCMS.UserAchievements/EskaCMS.UserAchievements.csproj", "EskaCMS.UserAchievements/"]
COPY ["EskaCommerce.PayFort/EskaCommerce.PayFort.csproj", "EskaCommerce.PayFort/"]
COPY ["EskaCommerceModules/EskaCommerce.PaymentPaypalExpress/EskaCommerce.PaymentPaypalExpress.csproj", "EskaCommerceModules/EskaCommerce.PaymentPaypalExpress/"]
COPY ["EskaCommerceModules/EskaCommerce.PaymentQuickPay/EskaCommerce.PaymentQuickPay.csproj", "EskaCommerceModules/EskaCommerce.PaymentQuickPay/"]
RUN dotnet restore "./EskaCMS.WebHost/EskaCMS.WebHost.csproj"
COPY . .
WORKDIR "/src/EskaCMS.WebHost"
RUN dotnet build "./EskaCMS.WebHost.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EskaCMS.WebHost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EskaCMS.WebHost.dll"]