using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using SharePointAppAutoVersioning.Shared;
using Version = SharePointAppAutoVersioning.Shared.Version;

namespace SharePointAppAutoVersioning.AutoIncrementBuildVersion
{
    public class VersionProvider : IVersionProvider
    {
        public Version GetVersion(Version oldVersion)
        {
            //Debugger.Launch();
            var version = new Version
            {
                Major = oldVersion.Major,
                Minor = oldVersion.Minor,
                Patch = oldVersion.Patch, //oldVersion.Patch + DateTime.UtcNow.Date.Subtract(oldVersion.BuildDate.UtcDateTime.Date).Days,
                Build = oldVersion.Build + 1,
                BuildDate = DateTimeOffset.Now
            };

            return version;
        }
    }
}
