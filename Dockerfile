FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /repo

# Скопировать только файлы проекта (для корректного кэширования restore)
# Укажи все ProjectReference, которые есть в Presentation.csproj
COPY ["Presentation/Presentation.csproj", "Presentation/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Shared/Shared.csproj", "Shared/"]

# Выполнить restore для Presentation
WORKDIR /repo/Presentation
RUN dotnet restore "Presentation.csproj"

# Скопировать весь исходник и собрать
WORKDIR /repo
COPY . .
WORKDIR /repo/Presentation
RUN dotnet publish "Presentation.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet","Presentation.dll"]
