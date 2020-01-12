echo Starting Build...
call ant -f ant-build-script.xml backup-version-number
call ant -f ant-build-script.xml -Denv.BUILD_NUMBER=9999 update-version-number
call ant -f ant-build-script.xml create-output-folder
msbuild ..\Levrum.sln /t:Rebuild /p:Configuration=Debug
msbuild ..\Levrum.sln /t:Rebuild /p:Configuration=Release
call ant -f ant-build-script.xml -Denv.BUILD_NUMBER=9999 build-installer-local
call ant -f ant-build-script.xml restore-version-number
echo Build complete. Artifact(s) are located in %CD%\..\output