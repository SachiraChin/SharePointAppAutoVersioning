using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using SharePointAppAutoVersioning.Shared;
using static System.String;

namespace SharePointAppAutoVersioning
{

    class Program
    {

        public static bool IsFullPath(string path)
        {
            return !IsNullOrWhiteSpace(path)
                && path.IndexOfAny(Path.GetInvalidPathChars().ToArray()) == -1
                && Path.IsPathRooted(path)
                && !Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }

        private static Shared.Version GetVersion(Options options)
        {
            var vpath = "";
            if (options.BuildJs)
            {
                if (options.JsPath == null)
                {
                    vpath = options.BasePath + "applicationVersion.js";
                }
                else
                {
                    if (IsFullPath(options.JsPath))
                        vpath = options.JsPath;
                    else
                        vpath = options.BasePath + options.JsPath;
                }
            }
            else
            {
                vpath = AppDomain.CurrentDomain.BaseDirectory + "applicationVersion.js";
            }

            Console.WriteLine($"Version file: {vpath}");

            var major = 1;
            var minor = 0;
            var patch = 0;
            var build = 0;
            var buildDate = DateTimeOffset.Now;
            if (File.Exists(vpath))
            {
                using (var f = File.OpenText(vpath))
                {
                    major = int.Parse(f.ReadLine()?.Replace("//", "") ?? "1");
                    minor = int.Parse(f.ReadLine()?.Replace("//", "") ?? "0");
                    patch = int.Parse(f.ReadLine()?.Replace("//", "") ?? "0");
                    build = int.Parse(f.ReadLine()?.Replace("//", "") ?? "0");
                    buildDate = DateTimeOffset.Parse(f.ReadLine()?.Replace("//", ""));
                }
            }
            IVersionProvider versionProvider;
            try
            {
                var asm = Assembly.LoadFile(AppDomain.CurrentDomain.BaseDirectory + options.VersioningLibrary);
                var type = asm.GetType(options.VersioningClass);
                versionProvider = Activator.CreateInstance(type) as IVersionProvider;

                if (versionProvider == null)
                {
                    Console.WriteLine("Versioning class not found. Make sure you enter full name of class and class implemented from IVersionProvider.");
                    return null;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Versioning library not found. Make sure that versioning library exists on same directory as spav.exe.");
                return null;
            }
            var oldVersion = new Shared.Version()
            {
                Major = major,
                Minor = minor,
                Patch = patch,
                Build = build,
                BuildDate = buildDate
            };
            var version = versionProvider.GetVersion(oldVersion);

            Console.WriteLine($"Old version: {oldVersion.VersionString}");
            Console.WriteLine($"New version: {version.VersionString}");

            var nameSpace = options.JsClass.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var js = "[body]";
            var lstNamespace = "window";
            var temp = "(function ([newClass]) {[body]}([lastClass].[newClass] = [lastClass].[newClass] || {}));";
            foreach (var namesp in nameSpace)
            {
                js = js.Replace("[body]", temp.Replace("[lastClass]", lstNamespace).Replace("[newClass]", namesp));
                lstNamespace = namesp;
            }
            var verInfo = $@"{lstNamespace}.versionString='{version.VersionString}';{lstNamespace}.buildNumber={version.Build};{lstNamespace}.buildDate='{version.BuildDate.ToString("G")}'";
            js = js.Replace("[body]", verInfo);

            using (var sw = new StreamWriter(vpath, false))
            {
                sw.WriteLine($"//{version.Major}");
                sw.WriteLine($"//{version.Minor}");
                sw.WriteLine($"//{version.Patch}");
                sw.WriteLine($"//{version.Build}");
                sw.WriteLine($"//{version.BuildDate.ToString("O")}");
                sw.WriteLine(js);
                sw.Flush();
                sw.Close();
            }

            Console.WriteLine("Version file updated.");
            return version;
        }
        
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                options.BasePath = options.BasePath ?? Environment.CurrentDirectory + "\\";
            }
            else
            {
                return;
            }

            var versionObj = GetVersion(options);

            if (versionObj == null)
                return;
            var version = versionObj.VersionString;

            //return;
            var temppath = Path.GetTempPath() + "app.wsp.cab";
            if (File.Exists(temppath))
                File.Delete(temppath);

            var publishPath = options.BasePath + @"bin\" + options.BuildConfig + @"\app.publish\";
            var newDir = publishPath + version;
            Directory.CreateDirectory(newDir);

            if (options.Mode == "Wsp")
            {
                var wspPath = (from f in new DirectoryInfo(options.BasePath + @"bin\" + options.BuildConfig).GetFiles("*.wsp", SearchOption.AllDirectories)
                               orderby f.LastWriteTime descending
                               select f).FirstOrDefault()?.FullName;

                if (wspPath == null)
                {
                    Console.WriteLine("Last built WSP not found.");
                    return;
                }
                var fileName = Path.GetFileName(wspPath);
                //Console.WriteLine(options.BasePath);
                //Console.WriteLine(publishPath);
                //Console.WriteLine(wspPath);

                File.Copy(wspPath, temppath, true);

                var newPath = MakeWsp(temppath, version);
                var wspNewPath = $@"{newDir}\{fileName.Substring(0, fileName.Length - 4)}.{version}.wsp";
                File.Copy(newPath, wspNewPath, true);
                File.Delete(newPath);
                File.Delete(temppath);

                Console.WriteLine($"WSP build completed: {wspNewPath}");
            }
            if (options.Mode == "AppPackage" || options.Mode == "AppPackageAndWsp")
            {
                //Console.WriteLine(options.BasePath);
                //Console.WriteLine(publishPath);

                var packagePath = (from f in new DirectoryInfo(options.BasePath + @"bin\" + options.BuildConfig).GetFiles("*.app", SearchOption.AllDirectories)
                                   orderby f.LastWriteTime descending
                                   select f).FirstOrDefault()?.FullName;
                Console.WriteLine($"App package: {packagePath}");
                if (packagePath == null)
                {
                    Console.WriteLine("Last built app package not found.");
                    return;
                }

                var fileName = Path.GetFileName(packagePath);

                var packageCopy = $@"{Path.GetTempPath()}build.app";
                File.Copy(packagePath, packageCopy, true);

                Console.WriteLine("Updating AppManifest.xml version");

                var package = Package.Open(packageCopy);
                //var r = package.GetRelationships();
                package.PackageProperties.Version = version;
                var manifestUri = PackUriHelper.CreatePartUri(new Uri("/AppManifest.xml", UriKind.Relative));
                var manifest = package.GetPart(manifestUri);
                var manifestStream = manifest.GetStream();
                using (var reader = new StreamReader(manifestStream))
                {
                    var content = reader.ReadToEnd();

                    var ex = new Regex("Version=\".*?\"");
                    var r = ex.Match(content);
                    var cv = r.Groups[0].Value;
                    var newXml = content.Replace(cv, "Version=\"" + version + "\"");
                    //var newXml = content.Replace("Version=\"1.0.0.0\"", "Version=\"" + version + "\"");

                    manifestStream.Position = 0;
                    manifestStream.SetLength(0);
                    using (var sr = new StreamWriter(manifestStream))
                    {
                        sr.Write(newXml);
                        sr.Flush();
                        //sr.Close();
                    }
                }

                Console.WriteLine("AppManifest.xml version updated");

                Console.WriteLine("Updating app WSP version");

                var wsp = (from part in package.GetParts()
                           where part.Uri.OriginalString.EndsWith(".wsp")
                    select part).FirstOrDefault();

                if (wsp != null)
                {
                    //var wspUri = PackUriHelper.CreatePartUri(new Uri("/ActiveCards.App.wsp", UriKind.Relative));
                    //var wsp = package.GetPart(wspUri);
                    var wspStream = wsp.GetStream();
                    var fileStream = File.Create(temppath);
                    wspStream.Seek(0, SeekOrigin.Begin);
                    wspStream.CopyTo(fileStream);
                    fileStream.Close();

                    var wspPath = MakeWsp(temppath, version);

                    wspStream.Position = 0;
                    wspStream.SetLength(0);

                    using (var fs = new FileStream(wspPath, FileMode.Open))
                    {
                        fs.Seek(0, SeekOrigin.Begin);
                        fs.CopyTo(wspStream);
                    }
                    wspStream.Close();

                    Console.WriteLine("App WSP version updated");

                    if (options.Mode == "AppPackageAndWsp")
                    {
                        var wspNewPath = $@"{newDir}\{fileName.Substring(0, fileName.Length - 4)}.{version}.wsp";
                        File.Copy(wspPath, wspNewPath, true);

                        Console.WriteLine($"WSP build completed: {wspNewPath}");
                    }
                    File.Delete(wspPath);
                    File.Delete(temppath);
                }
                package.Close();

                var newPackagePath = newDir + @"\" + fileName;
                File.Copy(packageCopy, newPackagePath, true);
                Console.WriteLine($"App package build completed: {newPackagePath}");

                File.Delete(packageCopy);
            }


        }

        private static string MakeWsp(string wspPath, string version)
        {
            Console.WriteLine($"WSP path: {wspPath}");

            var tempFol = Path.GetTempPath() + "appversionupdater";
            if (Directory.Exists(tempFol))
                DeleteFiles(tempFol);
            else
                Directory.CreateDirectory(tempFol);

            Console.WriteLine("Extracting WSP");

            var unzipper = new Process
            {
                StartInfo =
                {
                    FileName =  AppDomain.CurrentDomain.BaseDirectory + @"7zip\7za.exe",
                    Arguments = "x \"" + wspPath + "\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tempFol
                }
            };
            unzipper.Start();
            unzipper.WaitForExit();

            Console.WriteLine("WSP extracted");

            var featureFiles =
                from f in new DirectoryInfo(tempFol).GetFiles("Feature.xml", SearchOption.AllDirectories)
                orderby f.LastWriteTime descending
                select f;

            foreach (var featureFile in featureFiles)
            {
                var featurePath = featureFile.FullName;
                Console.WriteLine(featurePath);

                using (var reader = new StreamReader(featurePath))
                {
                    var content = reader.ReadToEnd();

                    var ex = new Regex("Version=\".*?\"");
                    var r = ex.Match(content);
                    var cv = r.Groups[0].Value;
                    var newXml = content.Replace(cv, "Version=\"" + version + "\"");
                    reader.Close();
                    using (var sr = new StreamWriter(featurePath, false))
                    {
                        sr.Write(newXml);
                        sr.Flush();
                        //sr.Close();
                    }
                }
                Console.WriteLine($"Updated WSP feature: {featurePath}");
            }


            Console.WriteLine("Repacking WSP");

            var ddf = @".OPTION EXPLICIT
.Set CabinetNameTemplate=app.wsp
.Set DiskDirectory1=.
.Set CompressionType=MSZIP
.Set Cabinet=on
.Set Compress=on
.Set CabinetFileCountThreshold=0
.Set FolderFileCountThreshold=0
.Set FolderSizeThreshold=0
.Set MaxCabinetSize=0
.Set MaxDiskFileCount=0
.Set MaxDiskSize=0
";
            var di = new DirectoryInfo(tempFol);
            foreach (var file in di.GetFiles("*", SearchOption.AllDirectories))
            {
                ddf += $"\"{file.FullName}\" \"{file.FullName.Replace(tempFol + "\\", "")}\"\r\n";
            }

            var ddfpath = Path.GetTempPath() + "makewsp.ddf";
            if (File.Exists(ddfpath))
                File.Delete(ddfpath);

            File.WriteAllText(ddfpath, ddf);

            var saveFol = Path.GetTempPath();

            var makeWsp = new Process
            {
                StartInfo =
                {
                    FileName = "c:\\windows\\system32\\makecab.exe",
                    Arguments = "/F " + ddfpath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = saveFol
                }
            };
            //p.StartInfo.RedirectStandardOutput = true;
            makeWsp.Start();
            makeWsp.WaitForExit();

            DeleteFiles(tempFol);
            Directory.Delete(tempFol);
            File.Delete(ddfpath);
            var packedWsp = saveFol + "\\app.wsp";
            Console.WriteLine($"WSP repacked: {packedWsp}");

            return packedWsp;
        }

        private static void DeleteFiles(string tempFol)
        {
            if (Directory.Exists(tempFol))
            {
                var di = new DirectoryInfo(tempFol);

                foreach (var file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (var dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
            else
            {
                Directory.CreateDirectory(tempFol);
            }
        }
    }

}
