FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY "Manager/Manager.csproj" "Manager/"
COPY "Components/Components.csproj" "Components/"
RUN dotnet restore --verbosity d Manager
COPY "Components/" "Components/"
COPY "Manager/" "Manager/"
WORKDIR "/src/Manager"
RUN dotnet build "Manager.csproj" --no-restore -c Release -o /build/app

FROM build AS publish
RUN dotnet publish "Manager.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 443
ENV ASPNETCORE_ENVIRONMENT=Development
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Manager.dll"]
