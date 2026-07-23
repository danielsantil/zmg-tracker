# syntax=docker/dockerfile:1
#
# One-image build: compiles the React SPA, publishes the ASP.NET Core API, and
# ships a runtime image where the API serves the SPA from wwwroot (build-plan.md §4).
# Build:  docker build -t zmg-tracker .
# Run:    docker run -p 8080:8080 -e ConnectionStrings__Zmg="<postgres-conn>" zmg-tracker
#         → app on http://localhost:8080
# Prod runs on Azure Container Apps against Neon Postgres; ConnectionStrings__Zmg is
# supplied as an ACA secret (secretref) at runtime — the image ships no DB default.

# --- Stage 1: build the SPA ---------------------------------------------------
FROM node:24-alpine AS web
WORKDIR /web
RUN corepack enable
COPY src/Zmg.Web/package.json src/Zmg.Web/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY src/Zmg.Web/ ./
# Override the config's outDir (../Zmg.Api/wwwroot, absent in this stage) to a local dist.
RUN pnpm exec tsc -b && pnpm exec vite build --outDir dist --emptyOutDir

# --- Stage 2: publish the API -------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY global.json ./
COPY src/Zmg.Domain/ src/Zmg.Domain/
COPY src/Zmg.Infra/ src/Zmg.Infra/
COPY src/Zmg.Api/ src/Zmg.Api/
RUN dotnet restore src/Zmg.Api/Zmg.Api.csproj
RUN dotnet publish src/Zmg.Api/Zmg.Api.csproj -c Release -o /app --no-restore
# Drop the compiled SPA into the published wwwroot.
COPY --from=web /web/dist /app/wwwroot

# --- Stage 3: runtime ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
# ConnectionStrings__Zmg (Postgres/Neon) is injected at runtime — as an ACA secretref
# in prod, or via -e for local runs. No default is baked into the image.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "Zmg.Api.dll"]
