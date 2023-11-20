#!/usr/bin/env bash

function show_help {
    echo "USING Pack:";
    echo "";
    echo "Configuration per arguments:";
    echo "-b = generate nuget package version badges"
    echo "-j = generate dotnet list package json"
    echo "-n = generate nuget package name list"
    echo "-p = generate directory.packages.props"
    echo "-h = Show help"
    echo "";
    echo "Note: You need to specify -b or -p as one is required.";
    exit 0;
}

DEBUG="false"

while getopts "bjnp" opt
do
   case "$opt" in
      b ) BADGES="true" ;;
      j ) JSON="true" ;;
      n ) NAMES="true" ;;
      p ) PACKAGES="true" ;;
      * ) show_help;;
   esac
done

if [[ "$JSON" ]];
then
    dotnet list ./PiBox.sln package --format json
fi

if [[ "$BADGES" ]];
then
    # without link ![Static Badge](https://img.shields.io/badge/AspNetCore.HealthChecks.Hangfire-4.0.3-blue?logo=nuget)
    # with link [![Static Badge](https://img.shields.io/badge/jose--jwt-4.1.0-blue?logo=nuget)](https://www.nuget.org/packages/Microsoft.Exchange.WebServices.NETStandard)
    dotnet list ./PiBox.sln package --format json | jq -r '[.projects[].frameworks[].topLevelPackages[] | {package: .id , version: .resolvedVersion }] | unique | .[] | "* [![Static Badge](https://img.shields.io/badge/"+(.package| sub("-";"--";"g"))+"-"+(.version | sub("-";"--";"g"))+"-blue)](https://www.nuget.org/packages/"+(.package)+")"'
fi

if [[ "$NAMES" ]];
then
    dotnet list ./PiBox.sln package --format json | jq -r '[.projects[].frameworks[].topLevelPackages[] | {package: .id, version:.resolvedVersion }] | unique | .[].package'
fi

if [[ "$PACKAGES" ]];
then
    dotnet list ./PiBox.sln package --format json | jq -r '[.projects[].frameworks[].topLevelPackages[] | {package: .id, version:.resolvedVersion }] | unique | .[] | "<PackageVersion Version="+"\""+(.version) +"\" Include="+"\""+(.package)+"\" />"'
    echo "NETStandard.Library needs to be removed or there will be build errors"
fi
