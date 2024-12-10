FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App
COPY src/Web/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
EXPOSE 80
ENV DOTNET_URLS=http://*:80
ENTRYPOINT ["dotnet", "Web.dll"]
COPY --from=build-env /App/out .
