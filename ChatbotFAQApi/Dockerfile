FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ChatbotFAQApi.csproj ./
RUN dotnet restore "ChatbotFAQApi.csproj"

# Copy the remaining source and publish
COPY . .
RUN dotnet publish "ChatbotFAQApi.csproj" -c Release -o /app/out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/out ./

# Document the port (Render will set PORT env var)
EXPOSE 8080

ENTRYPOINT ["dotnet", "ChatbotFAQApi.dll"]
