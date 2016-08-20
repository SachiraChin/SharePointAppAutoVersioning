using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharePointAppAutoVersioning.Shared
{
    public interface IVersionProvider
    {
        Version GetVersion(Version oldVersion);
    }
}
