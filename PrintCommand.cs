using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UiPathTeam.ActivityReader
{
    public class PrintCommand : ICommand
    {
        private List<string> filenames = new List<string>();

        private ClassDatabase db = new ClassDatabase();

        private int verboseLevel = 0;

        public PrintCommand()
        {
        }

        bool ICommand.Parse(string[] args)
        {
            for (int i = 1; i < args.Length; i++)
            {
                var next = args[i];
                if (next == "-debug")
                {
                    Program.debug = true;
                }
                else if (next == "-verbose" || next == "-v")
                {
                    verboseLevel++;
                }
                else if (Directory.Exists(next))
                {
                    var ss = Directory.GetFiles(next, "*.nupkg");
                    foreach (var filename in ss)
                    {
                        filenames.Add(filename);
                    }
                }
                else if (File.Exists(next))
                {
                    filenames.Add(next);
                }
                else
                {
                    var ss = Directory.GetFiles(Path.GetDirectoryName(next), Path.GetFileName(next));
                    foreach (var filename in ss)
                    {
                        filenames.Add(filename);
                    }
                }
            }
            return true;
        }

        void ICommand.Run()
        {
            foreach (var filename in filenames)
            {
                var ext = Path.GetExtension(filename).ToUpperInvariant();
                if (ext == ".NUPKG")
                {
                    db.ReadFromPackage(filename, Program.PrintError);
                }
                else if (ext == ".DLL")
                {
                    try
                    {
                        db.ReadFromAssembly(filename, Program.PrintError);
                    }
                    catch (Exception ex)
                    {
                        Program.PrintError(ex, filename);
                    }
                }
                else
                {
                    Program.PrintError("Unsupported file type.", filename);
                }
            }
            Print(db);
        }

        private void Print(ClassDatabase db)
        {
            foreach (var packageName in db.PackageNames)
            {
                Console.WriteLine("{0}", packageName);
                foreach (var c in db.SelectByPackageName(packageName))
                {
                    if (c.IsPublic && !c.IsAbstract && c.IsActivity)
                    {
                        Console.WriteLine("    {0}{1}", c.Name, FormatSuperClassName(" ({0})", c));
                        foreach (var property in c.Properties)
                        {
                            Console.WriteLine("        {0} ({1})", property.Name, TypeString.Symplify(property.Type));
                        }
                    }
                    else if (Program.debug)
                    {
                        Console.WriteLine("# TYPE={0} IsPub={1} IsAbs={2} IsAct={3}", c.Name, c.IsPublic, c.IsAbstract, c.IsActivity);
                    }
                }
            }
        }

        private string FormatSuperClassName(string format, ClassRecord cr)
        {
            if (verboseLevel > 1)
            {
                var sb = new StringBuilder();
                sb.Append(TypeString.Symplify(cr.SuperClassFullName));
                bool found;
                do
                {
                    found = false;
                    foreach (var packageName in db.PackageNames)
                    {
                        try
                        {
                            var cr2 = db.SelectByPackageName(packageName).Single((cr1) => cr1.FullName == cr.SuperClassFullName);
                            if (cr2 != null)
                            {
                                cr = cr2;
                                sb.Append(" : ");
                                sb.Append(TypeString.Symplify(cr.SuperClassFullName));
                                found = true;
                                break;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                while (found);
                return string.Format(format, sb.ToString());
            }
            else if (verboseLevel == 1)
            {
                return string.Format(format, TypeString.Symplify(cr.SuperClassFullName));
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
