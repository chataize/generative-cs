#!/bin/bash
dotnet nuget push ./src/bin/Release/ChatAIze.GenerativeCS.0.2.0.nupkg --api-key $CHATAIZE_NUGET_API_KEY --source https://api.nuget.org/v3/index.json
