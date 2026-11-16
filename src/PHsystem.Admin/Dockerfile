FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props global.json ./
COPY src/PHsystem.Admin/ src/PHsystem.Admin/
RUN dotnet publish src/PHsystem.Admin -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
USER $APP_UID
EXPOSE 8443
ENTRYPOINT ["dotnet", "PHsystem.Admin.dll"]
