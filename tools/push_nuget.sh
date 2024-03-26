#!/bin/bash

cd ../src

dotnet build
dotnet publish
dotnet pack

latest_package=$(ls ./bin/Release/ChatAIze.GenerativeCS.*.*.*.nupkg | sort -V | tail -n 1)
dotnet nuget push "$latest_package" --api-key $CHATAIZE_NUGET_API_KEY --source https://api.nuget.org/v3/index.json
