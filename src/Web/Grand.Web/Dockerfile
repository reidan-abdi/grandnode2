# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
LABEL stage=build
WORKDIR /src

# Copy source code (expect build context at repository root)
COPY ./src/ /src/

# Build plugins first so their outputs are available during publish
RUN for plugin in /src/Plugins/*; do \
    if [ -d "$plugin" ]; then \
      dotnet build "$plugin" -c Release; \
    fi; \
  done

# Publish Grand.Web
RUN dotnet publish /src/Web/Grand.Web/Grand.Web.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Expose common HTTP/HTTPS ports used by Kestrel (informational)
EXPOSE 80
EXPOSE 443

# Copy published output
COPY --from=build /out .

# Default ASP.NET Core URLs can be configured via environment or compose
# ENV ASPNETCORE_URLS="http://+:80;https://+:443"

ENTRYPOINT ["dotnet", "Grand.Web.dll"]
