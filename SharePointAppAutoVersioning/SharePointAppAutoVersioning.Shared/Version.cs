using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharePointAppAutoVersioning.Shared
{
    public class Version
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public int Build { get; set; }
        public DateTimeOffset BuildDate { get; set; }
        public string VersionString => $"{Major}.{Minor}.{Patch}.{Build}";
    }
}
