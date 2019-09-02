using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace UiPathTeam.ActivityReader
{
    public class ClassDatabase
    {
        static private readonly string LIB_SLASH = "LIB/";

        static private readonly string DOT_DLL = ".DLL";

        static private readonly string DOT_RESOURCES_DOT_DLL = ".RESOURCES.DLL";

        static private readonly string DOT_WPF_DOT_DLL = ".WPF.DLL";

        private Dictionary<string, ClassRecord> dict;

        private List<string> packages;

        private readonly string tempDirPath = Path.GetTempPath();

        private string currentPackageFileName = null;

        public IEnumerable<ClassRecord> Classes
        {
            get
            {
                return dict.Values.OrderBy(x => x.Name);
            }
        }

        public IEnumerable<string> PackageNames
        {
            get
            {
                return packages.OrderBy(x => x);
            }
        }

        public ClassDatabase()
        {
            dict = new Dictionary<string, ClassRecord>();
            packages = new List<string>();
        }

        public void ReadFromAssembly(string filename, Action<Exception, string, string> action)
        {
            try
            {
                using (var ad = Mono.Cecil.AssemblyDefinition.ReadAssembly(filename))
                {
                    foreach (Mono.Cecil.ModuleDefinition md in ad.Modules)
                    {
                        if (Program.debug)
                        {
                            Console.WriteLine("# MODULE={0}", md.Name);
                        }
                        foreach (Mono.Cecil.TypeDefinition td in md.GetTypes())
                        {
                            if (td.IsClass)
                            {
                                if (Program.debug)
                                {
                                    Console.WriteLine("# TYPE={0} IsClass=T", td.Name);
                                }
                                var key = ClassRecord.GetSimpleName(td);
                                if (!dict.TryGetValue(key, out ClassRecord a))
                                {
                                    a = new ClassRecord(td, md, this);
                                    dict.Add(key, a);
                                    if (!packages.Contains(md.Name))
                                    {
                                        packages.Add(md.Name);
                                    }
                                    if (Program.debug)
                                    {
                                        Console.WriteLine("#   Super={0} IsPublic={1}", a.SuperClassName, a.IsPublic);
                                    }
                                }
                                else if (Program.debug)
                                {
                                    Console.WriteLine("# {0}: Duplicate.", key);
                                    Console.WriteLine("#     {0}", a.PackageName);
                                    Console.WriteLine("#     {0}", md.Name);
                                }
                            }
                            else if (Program.debug)
                            {
                                Console.WriteLine("# TYPE={0} IsClass=F", td.Name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                action(ex, filename, currentPackageFileName);
            }
        }

        public void ReadFromPackage(string filename, Action<Exception, string, string> action)
        {
            try
            {
                currentPackageFileName = filename;
                using (var za = ZipFile.OpenRead(filename))
                {
                    foreach (ZipArchiveEntry entry in za.Entries)
                    {
                        if (IsAssembly(entry.FullName))
                        {
                            var filename2 = GetTempPath(entry.Name);
                            try
                            {
                                entry.ExtractToFile(filename2);
                                ReadFromAssembly(filename2, action);
                            }
                            catch (Exception ex)
                            {
                                action(ex, filename, entry.FullName);
                            }
                            finally
                            {
                                File.Delete(filename2);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                action(ex, filename, null);
            }
            finally
            {
                currentPackageFileName = null;
            }
        }

        public bool FindClass(string name, out ClassRecord cr)
        {
            return dict.TryGetValue(name, out cr);
        }

        private bool IsAssembly(string fullName)
        {
            var us = fullName.ToUpperInvariant();
            return
                us.StartsWith(LIB_SLASH) &&
                us.EndsWith(DOT_DLL) &&
                !us.EndsWith(DOT_RESOURCES_DOT_DLL) &&
                !us.EndsWith(DOT_WPF_DOT_DLL);
        }

        private string GetTempPath(string name)
        {
            return Path.Combine(tempDirPath, name + "_" + Guid.NewGuid().ToString());
        }

        public IEnumerable<ClassRecord> SelectByPackageName(string packageName)
        {
            return dict.Where(x => x.Value.PackageName == packageName && x.Value.IsActivity).OrderBy(x => x.Key).Select(x => x.Value);
        }
    }
}
