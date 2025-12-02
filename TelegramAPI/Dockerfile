# build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /repo

COPY ["TelegramAPI/TelegramAPI.csproj", "TelegramAPI/"]
COPY ["Domain/Domain.csproj", "Domain/"]

WORKDIR /repo/TelegramAPI
RUN dotnet restore "TelegramAPI.csproj"

# copy full source
WORKDIR /repo
COPY . .

# publish
WORKDIR /repo/TelegramAPI
RUN dotnet publish "TelegramAPI.csproj" -c Release -o /app/publish

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "TelegramAPI.dll"]
