# Install local tool (per project/repo):
First, create a tool manifest (if not already created):
```
dotnet new tool-manifest
```
Then install or update dotnet-ef locally:
```
dotnet tool install dotnet-ef
```
# or if already installed:
```
dotnet tool update dotnet-ef
```
Then you can use it with:
```
dotnet tool run dotnet-ef
```
# or add this directory to your PATH if preferred
To Check Which One You're Using Now
Run:
```
dotnet ef --version
```
--------------------------------------- Migrations
dotnet ef migrations add init
--------------------------------------- Database update
dotnet ef database update