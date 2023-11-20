FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime

COPY /src/PiBox.Example.Service/bin/Release/net7.0/publish/ /app
WORKDIR /app

EXPOSE 8080

ENTRYPOINT ["dotnet", "PiBox.Example.Service.dll"]
