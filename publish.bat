dotnet publish DataAccessGeneration -p:PublishSingleFile=true -c Release --self-contained false -r win-x64
dotnet publish DataAccessGeneration -p:PublishSingleFile=true -c Release --self-contained false -r win-arm64
dotnet publish DataAccessGeneration -p:PublishSingleFile=true -c Release --self-contained false -r osx-x64
dotnet publish DataAccessGeneration -p:PublishSingleFile=true -c Release --self-contained false -r osx.13-arm64
dotnet publish DataAccessGeneration -p:PublishSingleFile=true -c Release --self-contained false -r linux-x64
dotnet publish DataAccessGeneration -p:PublishSingleFile=true -c Release --self-contained false -r linux-arm64 