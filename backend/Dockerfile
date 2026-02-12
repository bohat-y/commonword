# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Commonword.sln ./
COPY Directory.Build.props Directory.Packages.props ./
COPY src ./src

RUN dotnet restore src/Commonword.Api/Commonword.Api.csproj
RUN dotnet publish src/Commonword.Api/Commonword.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Commonword.Api.dll"]
