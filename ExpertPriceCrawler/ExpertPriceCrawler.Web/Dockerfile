#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ExpertPriceCrawler.Web/ExpertPriceCrawler.Web.csproj", "ExpertPriceCrawler.Web/"]
RUN dotnet restore "ExpertPriceCrawler.Web/ExpertPriceCrawler.Web.csproj"
COPY . .
WORKDIR "/src/ExpertPriceCrawler.Web"
RUN dotnet build "ExpertPriceCrawler.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ExpertPriceCrawler.Web.csproj" -c Release -o /app/publish

FROM base AS final

#####################
#PUPPETEER RECIPE
#####################
# Install latest chrome dev package and fonts to support major charsets (Chinese, Japanese, Arabic, Hebrew, Thai and a few others)
# Note: this installs the necessary libs to make the bundled version of Chromium that Puppeteer
# installs, work.
ARG CHROME_VERSION="96.0.4664.93-1"
RUN apt-get update && apt-get -f install && apt-get -y install wget gnupg2 apt-utils
RUN wget --no-verbose -O /tmp/chrome.deb http://dl.google.com/linux/chrome/deb/pool/main/g/google-chrome-stable/google-chrome-stable_${CHROME_VERSION}_amd64.deb \
&& apt-get update \
&& apt-get install -y /tmp/chrome.deb --no-install-recommends --allow-downgrades fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst fonts-freefont-ttf \
&& rm /tmp/chrome.deb
#####################

ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome-stable"

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExpertPriceCrawler.Web.dll"]