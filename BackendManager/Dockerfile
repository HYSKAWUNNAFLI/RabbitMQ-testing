FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /src

COPY BackendManager/BackendManager.csproj BackendManager/
RUN dotnet restore BackendManager/BackendManager.csproj

COPY BackendManager/ BackendManager/
WORKDIR /src/BackendManager
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS final
WORKDIR /app


COPY --from=build /app/publish .


ENV ASPNETCORE_URLS=http://+:4000
EXPOSE 4000

ENTRYPOINT ["dotnet", "BackendManager.dll"]
