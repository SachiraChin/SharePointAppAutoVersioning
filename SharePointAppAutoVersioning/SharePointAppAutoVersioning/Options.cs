using CommandLine;
using CommandLine.Text;

namespace SharePointAppAutoVersioning
{
    class Options
    {
        [Option('p', "path", Required = false, HelpText = "Directory of application project. Default: Application working directory.", DefaultValue = null)]
        public string BasePath { get; set; }

        [Option('m', "mode", Required = false, HelpText = "Mode of the application running. " +
                                                          "AppPackage: Increases the version of application package(.app)" +
                                                          "Wsp: Build a sandbox compatible package with feature versioning" +
                                                          "AppPackageAndWsp: Update app package and create sandbox solution", DefaultValue = "AppPackage")]
        public string Mode { get; set; }

        [Option('b', "build-config", Required = false, HelpText = "Application build configuration. Can be taken from $(Configuration) MSBUILD paramater.", DefaultValue = "Release")]
        public string BuildConfig { get; set; }

        [Option('j', "build-js", Required = false, HelpText = "Build JavaScript file to use in versioning for client side versioning. This will work with parameters --js-path and --js-class.", DefaultValue = false)]
        public bool BuildJs { get; set; }

        [Option("js-path", Required = false, HelpText = "Save path for JavaScript file. Default: {BasePath}\\applicationVersion.js", DefaultValue = null)]
        public string JsPath { get; set; }

        [Option("js-class", Required = false, HelpText = "Class to contain application version.", DefaultValue = "appVersion")]
        public string JsClass { get; set; }
        
        [Option("versioning-lib", Required = false, HelpText = "Library used to get build version.", DefaultValue = "AutoIncrementBuildVersion.dll")]
        public string VersioningLibrary { get; set; }
        [Option("versioning-class", Required = false, HelpText = "Full path of class in versioning library.", DefaultValue = "SharePointAppAutoVersioning.AutoIncrementBuildVersion.VersionProvider")]
        public string VersioningClass { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

    }
}