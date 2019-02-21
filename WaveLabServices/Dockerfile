FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
#Installs python libr
#RUN apt-get update -y && apt-get install python3.7 -y
#RUN pip install -r wavelabscriptdependancies.txt

COPY ["WaveLabServices/WaveLabServices.csproj", "WaveLabServices/"]
COPY ["WaveLabAgent/WaveLabAgent.csproj", "WaveLabAgent/"]
RUN dotnet restore "WaveLabServices/WaveLabServices.csproj"
COPY . .
WORKDIR "/src/WaveLabServices"
RUN dotnet build "WaveLabServices.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "WaveLabServices.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "WaveLabServices.dll"]