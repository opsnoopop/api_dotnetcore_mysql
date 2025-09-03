# ---- Build ----
  FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
  WORKDIR /src
  
  COPY ./src/Api.csproj ./src/
  RUN dotnet restore ./src/Api.csproj
  
  COPY ./src ./src
  RUN dotnet publish ./src/Api.csproj -c Release -o /app/publish /p:UseAppHost=false
  
  # ---- Runtime ----
  FROM mcr.microsoft.com/dotnet/aspnet:8.0
  WORKDIR /app
  ENV ASPNETCORE_URLS=http://0.0.0.0:3000
  COPY --from=build /app/publish ./
  EXPOSE 3000
  ENTRYPOINT ["dotnet", "Api.dll"]
  