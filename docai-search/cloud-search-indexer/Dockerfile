FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

# Copy local code to the container image.
COPY . ./
# RUN rm ./appsettings.json

RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS http://*:8080
ENTRYPOINT ["dotnet", "cloud-search-indexer.dll"]
