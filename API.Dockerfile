# Identity API image. Build context is the repo root so restore can see NuGet.config + ./nuget-local
# (the repo-local feed holding the pinned GM.* versions not yet on nuget.org).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish GM.Identity.Sample.API/GM.Identity.Sample.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GM.Identity.Sample.API.dll"]
