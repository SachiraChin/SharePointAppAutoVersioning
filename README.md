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
