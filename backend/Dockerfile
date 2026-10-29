FROM mcr.microsoft.com/dotnet/sdk:8.0 AS construccion
WORKDIR /src

COPY Ingrata.sln ./
COPY src/Ingrata.Api/Ingrata.Api.csproj src/Ingrata.Api/
RUN dotnet restore src/Ingrata.Api/Ingrata.Api.csproj

COPY src/Ingrata.Api/ src/Ingrata.Api/
RUN dotnet publish src/Ingrata.Api/Ingrata.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=construccion /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Ingrata.Api.dll"]
