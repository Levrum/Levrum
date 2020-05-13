echo Starting Build...
if "%1"=="" (set buildnumber=9999) else (set buildnumber=%1)
echo Build Number: %buildnumber%
REM call ant -f ant-build-script.xml backup-version-number
REM call ant -f ant-build-script.xml -Denv.BUILD_NUMBER=%buildnumber% update-version-number
call ant -f ant-build-script.xml create-output-folder
msbuild ..\Levrum.sln /t:Rebuild /p:Configuration=Debug
msbuild ..\Levrum.sln /t:Rebuild /p:Configuration=Release
dotnet publish --configuration "Release" -r win-x64 ..\DataBridge\DataBridge.csproj
call ant -f ant-build-script.xml -Denv.BUILD_NUMBER=%buildnumber% build-installer-local
REM call ant -f ant-build-script.xml restore-version-number
echo Build complete. Artifact(s) are located in %CD%\..\output