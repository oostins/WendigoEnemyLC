# Wendigo

This repository contains the full source code for the Wendigo Enemy for Lethal Company, including the Unity project which can be used to build its asset bundle.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
        <GameDirectory>%programfiles(x86)%/Steam/steamapps/Common/Lethal Company/</GameDirectory>
        <!-- Paste a path to where your mod files get copied to when building.  Include the last slash '/' -->
        <PluginsDirectory>/my/path/to/BepInEx/plugins/</PluginsDirectory>
    </PropertyGroup>

    <!-- Game Directories - Do Not Modify -->
    <PropertyGroup>
        <ManagedDirectory>$(GameDirectory)Lethal Company_Data/Managed/</ManagedDirectory>
    </PropertyGroup>

    <!-- Our mod files get copied over after NetcodePatcher has processed our DLL -->
    <Target Name="CopyToTestProfile" DependsOnTargets="NetcodePatch" AfterTargets="PostBuildEvent">
        <MakeDir
            Directories="$(PluginsDirectory)$(AssemblyName)-DEV/"
            Condition="!Exists('$(PluginsDirectory)$(AssemblyName)-DEV/')"
        />
        <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(PluginsDirectory)$(AssemblyName)-DEV/"/>
        <!-- We will copy the asset bundle named "wendigoenemyassets" over -->
        <Copy SourceFiles="../UnityProject/AssetBundles/StandaloneWindows/wendigoenemyassets" DestinationFolder="$(PluginsDirectory)$(AssemblyName)-DEV/"/>
        <Exec Command="echo '[csproj.user] Mod files copied to $(PluginsDirectory)$(AssemblyName)-DEV/'" />
    </Target>
</Project>
```
You might also want to add this to the `csproj.user` file to also copy it over to our Unity project:
```xml
<!-- Copy the dll to our Unity project -->
<Copy SourceFiles="$(TargetPath)" DestinationFolder="../UnityProject/Assets/Plugins/"/>    
```

### Dependencies

You need to install the following dependencies for this mod to work in the game (these are not installed by the setup script):

- [LethalLib](https://thunderstore.io/c/lethal-company/p/Evaisa/LethalLib/) for registering and adding our enemy.
    - LethalLib depends on [HookGenPatcher](https://thunderstore.io/c/lethal-company/p/Evaisa/HookGenPatcher/).

If you didn't run the setup script you will also need to, in the `Plugin` directory where our plugin code is, run `dotnet tool restore` on the command-line to install the rest of the dependencies.

### Thunderstore Packages

I have configured [WendigoEnemy.csproj](/Plugin/WendigoEnemy.csproj) to build a Thunderstore package to [/Plugin/Thunderstore/Packages/](/Plugin/Thunderstore/Packages/) using [tcli](https://github.com/thunderstore-io/thunderstore-cli/wiki) each time I make a release build of the mod. A release build can be done for example from the command-line like this: `dotnet build -c release`. This will use configuration options from [thunderstore.toml](/Plugin/Thunderstore/thunderstore.toml)

## Credits


[EvaisaDev](https://github.com/EvaisaDev) - [LethalLib](https://github.com/EvaisaDev/LethalLib)  
[Lordfirespeed](https://github.com/Lordfirespeed) - reference tcli usage in LethalLib  
[Xilophor](https://github.com/Xilophor) - csproj files taken from Xilo's [mod templates](https://github.com/Xilophor/Lethal-Company-Mod-Templates)  
[Hamunii](https://github.com/Hamunii) [LC-ExampleEnemy](https://github.com/Hamunii/LC-ExampleEnemy)- Example template for Unity project  
[nomnomab](https://github.com/nomnomab) - [Lethal Company Project Patcher](https://github.com/nomnomab/lc-project-patcher) - used for the Unity Project  
[AlbinoGeek](https://github.com/AlbinoGeek) - issue template  