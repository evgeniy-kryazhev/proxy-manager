FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ProxyManager.slnx", "./"]
COPY ["src/ProxyManager.Domain/ProxyManager.Domain.csproj", "src/ProxyManager.Domain/"]
COPY ["src/ProxyManager.Application/ProxyManager.Application.csproj", "src/ProxyManager.Application/"]
COPY ["src/ProxyManager.Infrastructure/ProxyManager.Infrastructure.csproj", "src/ProxyManager.Infrastructure/"]
COPY ["src/ProxyManager.Web/ProxyManager.Web.csproj", "src/ProxyManager.Web/"]

RUN dotnet restore "src/ProxyManager.Web/ProxyManager.Web.csproj"

COPY . .
RUN dotnet publish "src/ProxyManager.Web/ProxyManager.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV Database__Provider=Sqlite
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/proxy-manager.db"

RUN mkdir -p /app/data /opt/hysteria

COPY --from=build /app/publish .

EXPOSE 8080
VOLUME ["/app/data", "/opt/hysteria"]

ENTRYPOINT ["dotnet", "ProxyManager.Web.dll"]
