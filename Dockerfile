# syntax=docker/dockerfile:1
#
# One-image build: compiles the React SPA, publishes the ASP.NET Core API, and
# ships a runtime image where the API serves the SPA from wwwroot (build-plan.md §4).
# Build:  docker build -t zmg-tracker .
# Run:    docker run -p 8080:8080 -v zmg-data:/data zmg-tracker
#         → app on http://localhost:8080  (SQLite persisted in the zmg-data volume)

# --- Stage 1: build the SPA ---------------------------------------------------
FROM node:22-alpine AS web
WORKDIR /web
COPY src/Zmg.Web/package*.json ./
RUN npm ci
COPY src/Zmg.Web/ ./
# Override the config's outDir (../Zmg.Api/wwwroot, absent in this stage) to a local dist.
RUN npm run build -- --outDir dist --emptyOutDir

# --- Stage 2: publish the API -------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY global.json ./
COPY src/Zmg.Domain/ src/Zmg.Domain/
COPY src/Zmg.Api/ src/Zmg.Api/
RUN dotnet restore src/Zmg.Api/Zmg.Api.csproj
RUN dotnet publish src/Zmg.Api/Zmg.Api.csproj -c Release -o /app --no-restore
# Drop the compiled SPA into the published wwwroot.
COPY --from=web /web/dist /app/wwwroot

# --- Stage 3: runtime ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
# Persist the SQLite file outside the image (mount a volume at /data).
ENV ConnectionStrings__Zmg="Data Source=/data/zmg.db"
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
RUN mkdir -p /data
EXPOSE 8080
ENTRYPOINT ["dotnet", "Zmg.Api.dll"]
