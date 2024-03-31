FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY "Worker/Worker.csproj" "Worker/"
COPY "Components/Components.csproj" "Components/"
RUN dotnet restore --verbosity d "Worker/Worker.csproj" 
COPY "Components/" "Components/"
COPY "Worker/" "Worker/"
WORKDIR "/src/Worker"
RUN dotnet build "Worker.csproj" --no-restore -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Worker.csproj" -c Release -o /app/publish

FROM build AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Worker.dll"]
