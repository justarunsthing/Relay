FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json ./
COPY src/Relay.Shared/ src/Relay.Shared/
COPY src/Relay.Client/ src/Relay.Client/
COPY src/Relay.Api/ src/Relay.Api/
RUN dotnet publish src/Relay.Api/Relay.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Relay.Api.dll"]
