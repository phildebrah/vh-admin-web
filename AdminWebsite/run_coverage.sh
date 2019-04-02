#!/bin/bash

rm -rf Artifacts

exclude=\"[*]Testing.Common.*,[*]AdminWebsite.Views.*,[*]AdminWebsite.Pages.*,[*]AdminWebsite.UserAPI.Client.*\"
dotnet test AdminWebsite.UnitTests/AdminWebsite.UnitTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat="\"opencover,cobertura,json,lcov\"" /p:CoverletOutput=../Artifacts/Coverage/ /p:MergeWith='../Artifacts/Coverage/coverage.json' /p:Exclude="${exclude}"
dotnet test AdminWebsite.IntegrationTests/AdminWebsite.IntegrationTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat="\"opencover,cobertura,json,lcov\"" /p:CoverletOutput=../Artifacts/Coverage/ /p:MergeWith='../Artifacts/Coverage/coverage.json' /p:Exclude="${exclude}"

~/.dotnet/tools/reportgenerator -reports:Artifacts/Coverage/coverage.opencover.xml -targetDir:Artifacts/Coverage/Report -reporttypes:HtmlInline_AzurePipelines

open Artifacts/Coverage/Report/index.htm

cd AdminWebsite/ClientApp
npm run test-once-ci
open coverage/index.html
cd ../../