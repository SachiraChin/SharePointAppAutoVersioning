# Auto versioning for SharePoint Apps and SandBox Solutions

## Introduction
```spav``` is a simple tool which can be used to automatically version SharePoint App packages and SandBox solution packages. It also can be used to generate application version JavaScript file which can later used to inject into pages and use in client side.

## Usage

In order to use this tool inside Visual Studio, you have to add pre or post deployment step to SharePoint add-in project. Whether to use pre-deployement or post-deployment is up to your requirement.

**-p, --path** [Required] Directory of application project. Can be taken from $(ProjectDir) MSBUILD parameter.

**-m, --mode** [Required] Running mode. 
- AppPackage: Increases the version of application package(.app)
- Wsp: Build a sandbox compatible package with feature versioning
- AppPackageAndWsp: Update app package and create sandbox solution

**-b, --build-config**  (Default: Debug) Application build configuration. Can be taken from $(Configuration) MSBUILD paramater.

**-j, --build-js** (Default: False) Build JavaScript file to use in versioning for client side versioning. This will work with parameters --js-path and --js-class.

**--js-path** (Default: {Path}\applicationVersion.js) Save path for JavaScript file. 

**--js-class**  (Default: appVersion) JavaScript namespace to contain application version.

**--versioning-lib** (Default: AutoIncrementBuildVersion.dll) Library used to get build version.

**--versioning-class** (Default: SharePointAppAutoVersioning.AutoIncrementBuildVersion.VersionProvider) Full path of class in versioning library.

**--help** Display help screen.

## Examples

### Simple usage

#### Auto increment patch and build version of the SharePoint App package

```
$(ProjectDir)spav\spav.exe -p $(ProjectDir) -m AppPackage -b $(Configuration)
```

#### Generate SandBox compatible solution with feature updates

```
$(ProjectDir)spav\spav.exe -p $(ProjectDir) -m Wsp -b $(Configuration)
```

#### Update app package version and generate SandBox solution

```
$(ProjectDir)spav\spav.exe -p $(ProjectDir) -m AppPackageAndWsp -b $(Configuration)
```

### Advanced usage

#### Build JavaScript file which can be used inside the application while updating version of app package

```
$(ProjectDir)spav\spav.exe -p $(ProjectDir) -m AppPackageAndWsp -b $(Configuration) --build-js --js-path AppData\applicationVersion.js --js-class contosoApp.appVersion
```

This will generate below JS file.

```JavaScript
//1
//0
//55
//383
//2016-08-22T18:47:50.0823318+05:30
(function(contosoApp) {
    (function(appVersion) {
        appVersion.versionString = '1.0.55.383';
        appVersion.buildNumber = 383;
        appVersion.buildDate = '8/22/2016 6:47:50 PM'
    }(contosoApp.appVersion = contosoApp.appVersion || {}));
}(window.contosoApp = window.contosoApp || {}));
```

***NOTE: Application will use 5 commented lines which is on top of JavaScript file to generate the next version. If you erase these lines, version will be resetted to 1.0.0.0. If you want to change major or minor version, you can change it in here and it'll be reflected it next build.***

You can access version values as below,

```JavaScript
var buildNumber = contosoApp.appVersion.buildNumber;
```

#### Use custom versioning library

You can write your own custom versioning library for your versioning purposes. In order to do that, you have to create new class library and add reference to ```SharePointAppAutoVersioning.Shared.dll``` file which you can find in application folder.

```C#
using SharePointAppAutoVersioning.Shared;
using Version = SharePointAppAutoVersioning.Shared.Version;

namespace ContosoApp.CustomBuildVersion
{
    public class VersionProvider : IVersionProvider
    {
        public Version GetVersion(Version oldVersion)
        {
            // You can attach debugger to application and debug it from your solution
            //  Debugger.Launch();
            var version = new Version
            {
                Major = oldVersion.Major,
                Minor = oldVersion.Minor,
                Patch = oldVersion.Patch,
                Build = oldVersion.Build + 1,
                BuildDate = DateTimeOffset.Now
            };

            return version;
        }
    }
}
```

Lets assume that dll from your project is ```ContosoApp.CustomBuildVersion.dll```. In order to use this inside the application, you have to copy this file to location of ```spav.exe``` path. After you copy it, you can use it like below.

```
$(ProjectDir)spav\spav.exe -p $(ProjectDir) -m AppPackageAndWsp -b $(Configuration) --versioning-lib ContosoApp.CustomBuildVersion.dll --versioning-class ContosoApp.CustomBuildVersion.VersionProvider
```
