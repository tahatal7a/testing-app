# Build Instructions for Desktop Task Aid (WPF)

## Prerequisites

- **Visual Studio 2019 or 2022** (Community, Professional, or Enterprise)
- **.NET Framework 4.8 SDK** (usually included with Visual Studio)
- **Windows 10/11** operating system

## Building the Application

### Option 1: Using Visual Studio

1. Open Visual Studio
2. Click **File** → **Open** → **Project/Solution**
3. Navigate to `netapp` folder and open `DesktopTaskAid.csproj`
4. Wait for NuGet packages to restore (this happens automatically)
5. Press **F6** or click **Build** → **Build Solution**
6. Press **F5** to run the application in debug mode

### Option 2: Using Command Line

Open PowerShell or Command Prompt in the `netapp` directory and run:

```powershell
# Restore NuGet packages
dotnet restore

# Build the project
dotnet build --configuration Release

## NuGet Packages

The following packages will be automatically restored:

- **Google.Apis.Calendar.v3** (1.69.0.3746) - Google Calendar API
- **Google.Apis.Auth** (1.72.0) - OAuth authentication
- **Newtonsoft.Json** (13.0.4) - JSON serialization

## Data Storage

The application stores data in:
```
C:\Users\{YourUsername}\AppData\Roaming\DesktopTaskAid\appState.json
```


