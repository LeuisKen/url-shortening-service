FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /app
COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app
COPY --from=builder /app/out ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "UrlShortener.dll"]
