# build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /repo

# copy csproj files for restore caching (paths are relative to context root)
COPY ["Presentation/Presentation.csproj", "Presentation/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Shared/Shared.csproj", "Shared/"]

WORKDIR /repo/Presentation
RUN dotnet restore "Presentation.csproj"

# copy full source
WORKDIR /repo
COPY . .

# publish
WORKDIR /repo/Presentation
RUN dotnet publish "Presentation.csproj" -c Release -o /app/publish

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "Presentation.dll"]
