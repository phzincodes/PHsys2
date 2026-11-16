FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props global.json PHsystem.slnx ./
COPY src/ src/
COPY site/ site/
RUN dotnet publish src/PHsystem.Server -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
# A fresh named volume inherits this directory's ownership. Existing volumes
# remain untouched: never fix a running environment with a blanket chmod.
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R "$APP_UID:$APP_UID" /home/app/.aspnet
USER $APP_UID
EXPOSE 8443
ENTRYPOINT ["dotnet", "PHsystem.Server.dll"]
