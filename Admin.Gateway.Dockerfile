# Admin YARP gateway image. Build context is the repo root (see API.Dockerfile).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish GM.Identity.Sample.Admin.Gateway.API/GM.Identity.Sample.Admin.Gateway.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GM.Identity.Sample.Admin.Gateway.API.dll"]
