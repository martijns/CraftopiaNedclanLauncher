dotnet publish --self-contained true -r win-x64 -c Debug -o prebuilt/ -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true
dotnet publish --self-contained true -r linux-x64 -c Debug -o prebuilt/ -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true