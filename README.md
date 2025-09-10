"# jason-phillips-test-project" 
# Jason Phillips Test Project

A straightforward .NET Web API for file handling file/folder operations

## Quick Start

**Prerequisites:** .NET SDK 8.0+

```bash
git clone https://github.com/raisingcode/jason-phillips-test-project.git
cd jason-phillips-test-project
dotnet build
dotnet run
```

The API will run on `https://localhost:7146` and `http://localhost:5120`
To UI is loaded on index, ie https://localhost:7146/index.html

## Configuration

Configure the application by modifying `appsettings.json` is storagepath

- **StoragePath**: Set where files are stored. Default is `uploads` folder 


For different environments, create `appsettings.Development.json` or `appsettings.Production.json` to override these values.

## Why the Simple File Controller?

The File Controller intentionally avoids complex patterns (Repository/UoW, etc.) for a reason:

- **Direct file system access** - No unnecessary abstraction layers slowing things down
- **Immediate productivity** - New developers can understand and modify the code instantly
- **Easier debugging** - You can see exactly what's happening without digging through layers
- **Perfect for most use cases** - Unless you need multiple storage providers or complex business rules, this approach just works


## Design Philosophy

This project follows KISS (Keep It Simple, Stupid) principles. The File Controller does exactly what you need without over-engineering. It's pragmatic code that prioritizes clarity and functionality over architectural complexity.

## Author

**Jason Phillips** 
