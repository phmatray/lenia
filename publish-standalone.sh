#!/bin/bash

# This script creates a standalone WebAssembly deployment for GitHub Pages

echo "🧬 Creating standalone Blazor WebAssembly deployment..."

# Clean previous builds
rm -rf standalone-release

# Create a temporary project file for standalone publishing
cat > Lenia/Lenia.Client/Lenia.Client.Standalone.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <BlazorEnableCompression>true</BlazorEnableCompression>
    <PublishSingleFile>false</PublishSingleFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="9.0.5"/>
    <PackageReference Include="MudBlazor" Version="8.6.0"/>
  </ItemGroup>

</Project>
EOF

# Create standalone Program.cs
cat > Lenia/Lenia.Client/Program.Standalone.cs << 'EOF'
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Lenia.Client.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();

await builder.Build().RunAsync();
EOF

# Create App.razor for standalone
cat > Lenia/Lenia.Client/App.Standalone.razor << 'EOF'
@using Lenia.Client.Layout

<MudThemeProvider Theme="@_theme" IsDarkMode="true" />
<MudDialogProvider />
<MudSnackbarProvider />

<Router AppAssembly="@typeof(Program).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout="@typeof(MainLayout)">
            <MudContainer Class="mt-8">
                <MudAlert Severity="Severity.Warning">
                    <MudText Typo="Typo.h4">Sorry, there's nothing at this address.</MudText>
                    <MudButton Href="/" Variant="Variant.Filled" Color="Color.Primary" Class="mt-4">
                        Go Home
                    </MudButton>
                </MudAlert>
            </MudContainer>
        </LayoutView>
    </NotFound>
</Router>

@code {
    MudTheme _theme = new()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#7c5cfc",
            Secondary = "#ff4081",
            Success = "#4caf50",
            Warning = "#ff9800",
            Error = "#f44336",
            Info = "#2196f3",
            Surface = "#1e1e1e",
            Background = "#121212",
            DrawerBackground = "#1a1a1a",
            AppbarBackground = "#1a1a1a",
            TextPrimary = "rgba(255,255,255, 0.87)",
            TextSecondary = "rgba(255,255,255, 0.60)",
            ActionDefault = "#7c5cfc",
            ActionDisabled = "rgba(255,255,255, 0.26)",
            ActionDisabledBackground = "rgba(255,255,255, 0.12)"
        }
    };
}
EOF

# Create index.html
cat > Lenia/Lenia.Client/wwwroot/index.html << 'EOF'
<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Lenia - Artificial Life Simulation</title>
    <base href="/" />
    <link rel="stylesheet" href="lenia.css" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
</head>

<body>
    <div id="app">
        <div id="app-loading">
            <div class="loading-spinner"></div>
            <div class="loading-text">Loading Lenia...</div>
        </div>
    </div>

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <script src="_framework/blazor.webassembly.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>

</html>
EOF

# Backup original files
mv Lenia/Lenia.Client/Program.cs Lenia/Lenia.Client/Program.cs.bak
mv Lenia/Lenia.Client/Lenia.Client.csproj Lenia/Lenia.Client/Lenia.Client.csproj.bak

# Use standalone files
mv Lenia/Lenia.Client/Program.Standalone.cs Lenia/Lenia.Client/Program.cs
mv Lenia/Lenia.Client/App.Standalone.razor Lenia/Lenia.Client/App.razor
mv Lenia/Lenia.Client/Lenia.Client.Standalone.csproj Lenia/Lenia.Client/Lenia.Client.csproj

# Publish as standalone
dotnet publish Lenia/Lenia.Client/Lenia.Client.csproj -c Release -o standalone-release --nologo

# Restore original files
mv Lenia/Lenia.Client/Program.cs.bak Lenia/Lenia.Client/Program.cs
mv Lenia/Lenia.Client/Lenia.Client.csproj.bak Lenia/Lenia.Client/Lenia.Client.csproj
rm -f Lenia/Lenia.Client/App.razor

# Update base href for GitHub Pages
sed -i '' 's/<base href="\/" \/>/<base href="\/Lenia\/" \/>/g' standalone-release/wwwroot/index.html

# Create 404.html for client-side routing
cp standalone-release/wwwroot/index.html standalone-release/wwwroot/404.html

# Add .nojekyll to prevent Jekyll processing
touch standalone-release/wwwroot/.nojekyll

echo "✅ Standalone build complete! Files are in the 'standalone-release/wwwroot' directory"
echo "🌐 To test locally, run: python3 -m http.server 8080 --directory standalone-release/wwwroot"
echo "   Then navigate to: http://localhost:8080/Lenia/"