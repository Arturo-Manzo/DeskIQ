FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/DeskIQ.TicketSystem.API/DeskIQ.TicketSystem.API.csproj", "src/DeskIQ.TicketSystem.API/"]
COPY ["src/DeskIQ.TicketSystem.Application/DeskIQ.TicketSystem.Application.csproj", "src/DeskIQ.TicketSystem.Application/"]
COPY ["src/DeskIQ.TicketSystem.Core/DeskIQ.TicketSystem.Core.csproj", "src/DeskIQ.TicketSystem.Core/"]
COPY ["src/DeskIQ.TicketSystem.Infrastructure/DeskIQ.TicketSystem.Infrastructure.csproj", "src/DeskIQ.TicketSystem.Infrastructure/"]
RUN dotnet restore "src/DeskIQ.TicketSystem.API/DeskIQ.TicketSystem.API.csproj"
COPY . .
WORKDIR "/src/src/DeskIQ.TicketSystem.API"
RUN dotnet build "DeskIQ.TicketSystem.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DeskIQ.TicketSystem.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeskIQ.TicketSystem.API.dll"]
