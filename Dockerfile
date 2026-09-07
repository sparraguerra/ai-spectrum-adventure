FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "src/AI.SpectrumAdventure.Web/AI.SpectrumAdventure.Web.csproj"

FROM build AS publish
RUN dotnet publish "src/AI.SpectrumAdventure.Web/AI.SpectrumAdventure.Web.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AI.SpectrumAdventure.Web.dll"]