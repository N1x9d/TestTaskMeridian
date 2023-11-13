FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TestTaskMeridian/TestTaskMeridian.csproj", "TestTaskMeridian/"]
RUN dotnet restore "TestTaskMeridian/TestTaskMeridian.csproj"
COPY . .
WORKDIR "/src/TestTaskMeridian"
RUN dotnet build "TestTaskMeridian.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestTaskMeridian.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestTaskMeridian.dll"]
