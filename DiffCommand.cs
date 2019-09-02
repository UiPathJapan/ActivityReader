using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UiPathTeam.ActivityReader
{
    public class DiffCommand : ICommand
    {
        static private readonly string FORMAT_A0 = "  {0}";
        static private readonly string FORMAT_A1 = "- {0}";
        static private readonly string FORMAT_A2 = "+ {0}";
        static private readonly string FORMAT_P0 = "      {0}";
        static private readonly string FORMAT_P1 = "-     {0}";
        static private readonly string FORMAT_P2 = "+     {0}";
        static private readonly string FORMAT_P0_TYPE = "      {0} ({1})";
        static private readonly string FORMAT_P1_TYPE = "-     {0} ({1})";
        static private readonly string FORMAT_P2_TYPE = "+     {0} ({1})";
        static private readonly string FORMAT_HEADER = "diff {0} {1}";
        static private readonly string FORMAT_ONLY_IN_1 = "Only in OLD: {0}";
        static private readonly string FORMAT_ONLY_IN_2 = "Only in NEW: {0}";
        static private readonly string UIPATH_CORE_ACTIVITIES_DOT = "UIPATH.CORE.ACTIVITIES.";
        static private readonly string UIPATH_SYSTEM_ACTIVITIES_DOT = "UIPATH.SYSTEM.ACTIVITIES.";
        static private readonly string UIPATH_UIAUTOMATION_ACTIVITIES_DOT = "UIPATH.UIAUTOMATION.ACTIVITIES.";
        static private readonly string ANY_NUPKG = "*.nupkg";

        private string _filename1 = null;
        private string _filename2 = null;
        private string _filename3 = null;

        public DiffCommand()
        {
        }

        bool ICommand.Parse(string[] args)
        {
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-debug")
                {
                    Program.debug = true;
                }
                else if (args[i].ToLowerInvariant() == "-type=simple")
                {
                    ClassComparator.GetInstance().TypeFormat = TypeFormat.SIMPLE;
                }
                else if (args[i].ToLowerInvariant() == "-type=full")
                {
                    ClassComparator.GetInstance().TypeFormat = TypeFormat.FULL;
                }
                else if (args[i].ToLowerInvariant() == "-type=none")
                {
                    ClassComparator.GetInstance().TypeFormat = TypeFormat.NONE;
                }
                else if (_filename1 == null)
                {
                    _filename1 = TryExpand(args[i]);
                }
                else if (_filename2 == null)
                {
                    _filename2 = TryExpand(args[i]);
                }
                else if (_filename3 == null)
                {
                    _filename3 = TryExpand(args[i]);
                }
                else
                {
                    return false;
                }
            }
            if (_filename1 == null || _filename2 == null)
            {
                return false;
            }
            if (_filename3 != null)
            {
                if (Directory.Exists(_filename1) && Directory.Exists(_filename2))
                {
                    return false;
                }
            }
            return true;
        }

        private string TryExpand(string path)
        {
            if (File.Exists(path))
            {
                return path;
            }
            else
            {
                var ss = Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path));
                if (ss.Length == 1)
                {
                    return ss[0];
                }
                else
                {
                    return path;
                }
            }
        }

        void ICommand.Run()
        {
            if (Directory.Exists(_filename1) && Directory.Exists(_filename2))
            {
                new DirDiff(_filename1, _filename2).Run();
            }
            else
            {
                new PkgDiff(_filename1, _filename2, _filename3).Run();
            }
        }

        private class DirDiff
        {
            private string _dirname1;
            private string _dirname2;

            public DirDiff(string dirname1, string dirname2)
            {
                _dirname1 = dirname1;
                _dirname2 = dirname2;
            }

            public void Run()
            {
                var x1 = Directory.GetFiles(_dirname1, ANY_NUPKG).OrderBy(x => x.ToUpperInvariant());
                var x2 = Directory.GetFiles(_dirname2, ANY_NUPKG).OrderBy(x => x.ToUpperInvariant());
                var decoupled = HasDecouplingCase(x1, x2, out string d1, out string d2, out string d3);
                var e1 = x1.GetEnumerator();
                var e2 = x2.GetEnumerator();
                var b1 = e1.MoveNext();
                var b2 = e2.MoveNext();
                bool newline = true;
                while (b1 && b2)
                {
                    var s1 = e1.Current;
                    var s2 = e2.Current;
                    if (decoupled)
                    {
                        if (d1 != null && s1 == d1)
                        {
                            Console.WriteLine("");
                            Console.WriteLine(FORMAT_HEADER, d1, d2 + " " + d3);
                            new PkgDiff(d1, d2, d3).Run();
                            b1 = e1.MoveNext();
                            newline = true;
                            d1 = null;
                            continue;
                        }
                        if (d2 != null && s2 == d2)
                        {
                            b2 = e2.MoveNext();
                            d2 = null;
                            continue;
                        }
                        if (d3 != null && s2 == d3)
                        {
                            b2 = e2.MoveNext();
                            d3 = null;
                            decoupled = false;
                            continue;
                        }
                    }
                    var rc = Compare(s1, s2);
                    if (rc < 0)
                    {
                        if (newline)
                        {
                            Console.WriteLine("");
                            newline = false;
                        }
                        Console.WriteLine(FORMAT_ONLY_IN_1, s1);
                        b1 = e1.MoveNext();
                    }
                    else if (rc > 0)
                    {
                        if (newline)
                        {
                            Console.WriteLine("");
                            newline = false;
                        }
                        Console.WriteLine(FORMAT_ONLY_IN_2, s2);
                        b2 = e2.MoveNext();
                    }
                    else
                    {
                        Console.WriteLine("");
                        Console.WriteLine(FORMAT_HEADER, s1, s2);
                        new PkgDiff(s1, s2).Run();
                        b1 = e1.MoveNext();
                        b2 = e2.MoveNext();
                        newline = true;
                    }
                }
                while (b1)
                {
                    if (newline)
                    {
                        Console.WriteLine("");
                        newline = false;
                    }
                    Console.WriteLine(FORMAT_ONLY_IN_1, e1.Current);
                    b1 = e1.MoveNext();
                }
                while (b2)
                {
                    if (newline)
                    {
                        Console.WriteLine("");
                        newline = false;
                    }
                    Console.WriteLine(FORMAT_ONLY_IN_2, e2.Current);
                    b2 = e2.MoveNext();
                }
            }

            private bool HasDecouplingCase(IOrderedEnumerable<string> x1, IOrderedEnumerable<string> x2, out string d1, out string d2, out string d3)
            {
                d1 = null;
                d2 = null;
                d3 = null;
                var e1 = x1.GetEnumerator();
                var b1 = e1.MoveNext();
                while (b1)
                {
                    var s = Path.GetFileName(e1.Current).ToUpperInvariant();
                    if (s.StartsWith(UIPATH_CORE_ACTIVITIES_DOT))
                    {
                        var ss = s.Substring(UIPATH_CORE_ACTIVITIES_DOT.Length).Split('.');
                        if (ss.Length == 5)
                        {
                            if ((ss[0] == "16" || ss[0] == "17" || ss[0] == "18")
                                && (ss[1] == "1" || ss[1] == "2")
                                && ss[4] == "NUPKG")
                            {
                                var e2 = x2.GetEnumerator();
                                var b2 = e2.MoveNext();
                                while (b2)
                                {
                                    var t = Path.GetFileName(e2.Current).ToUpperInvariant();
                                    if (d2 == null)
                                    {
                                        if (t.StartsWith(UIPATH_SYSTEM_ACTIVITIES_DOT))
                                        {
                                            d2 = e2.Current;
                                        }
                                    }
                                    else if (d3 == null)
                                    {
                                        if (t.StartsWith(UIPATH_UIAUTOMATION_ACTIVITIES_DOT))
                                        {
                                            d3 = e2.Current;
                                            d1 = e1.Current;
                                            return true;
                                        }
                                    }
                                    b2 = e2.MoveNext();
                                }
                            }
                        }
                        return false;
                    }
                    b1 = e1.MoveNext();
                }
                return false;
            }

            private int Compare(string filename1, string filename2)
            {
                var ss1 = Path.GetFileName(filename1).ToUpperInvariant().Split('.');
                var ss2 = Path.GetFileName(filename2).ToUpperInvariant().Split('.');
                var rc = ss1[0].CompareTo(ss2[0]);
                if (rc != 0)
                {
                    return rc;
                }
                int idx = 1;
                while (idx < ss1.Length && idx < ss2.Length)
                {
                    char c1 = ss1[idx][0];
                    char c2 = ss2[idx][0];
                    if (Char.IsDigit(c1))
                    {
                        if (Char.IsDigit(c2))
                        {
                            return 0;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else if (Char.IsDigit(c2))
                    {
                        return 1;
                    }
                    else
                    {
                        rc = ss1[idx].CompareTo(ss2[idx]);
                        if (rc != 0)
                        {
                            return rc;
                        }
                        idx++;
                    }
                }
                if (idx < ss1.Length)
                {
                    return 1;
                }
                else if (idx < ss2.Length)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private class PkgDiff: IDiffActions
        {
            private string _filename1;
            private string _filename2;
            private string _filename3;

            private TypeFormat _typefmt;
            private List<ClassRecord> _activitiesDeleted;
            private List<ClassRecord> _activitiesAdded;
            private List<ClassRecord> _activitiesModified;
            private List<PropertyRecord> _propertiesDeleted;
            private List<PropertyRecord> _propertiesAdded;
            private string _currentActivityName;
            private bool _modified;

            public PkgDiff(string filename1, string filename2)
            {
                _filename1 = filename1;
                _filename2 = filename2;
                _filename3 = null;
            }

            public PkgDiff(string filename1, string filename2, string filename3)
            {
                _filename1 = filename1;
                _filename2 = filename2;
                _filename3 = filename3;
            }

            public void Run()
            {
                _typefmt = ClassComparator.GetInstance().TypeFormat;
                _activitiesDeleted = new List<ClassRecord>();
                _activitiesAdded = new List<ClassRecord>();
                _activitiesModified = new List<ClassRecord>();
                _propertiesDeleted = new List<PropertyRecord>();
                _propertiesAdded = new List<PropertyRecord>();
                _currentActivityName = null;
                _modified = false;
                var db1 = new ClassDatabase();
                var db2 = new ClassDatabase();
                db1.ReadFromPackage(_filename1, Program.PrintError);
                db2.ReadFromPackage(_filename2, Program.PrintError);
                if (_filename3 != null)
                {
                    db2.ReadFromPackage(_filename3, Program.PrintError);
                }
                Program.errors += ClassComparator.GetInstance().Run(db1, db2, this);
                Console.WriteLine("");
                Console.WriteLine("SUMMARY:");
                Console.WriteLine("  ---: {0}", _filename1);
                Console.WriteLine("  +++: {0}", _filename2);
                if (_filename3 != null)
                {
                    Console.WriteLine("  +++: {0}", _filename3);
                }
                Console.WriteLine("  DELETED classes: {0}", _activitiesDeleted.Count);
                foreach (var a in _activitiesDeleted)
                {
                    Console.WriteLine("    {0}", a.Name);
                }
                Console.WriteLine("  ADDED classes: {0}", _activitiesAdded.Count);
                foreach (var a in _activitiesAdded)
                {
                    Console.WriteLine("    {0}", a.Name);
                }
                Console.WriteLine("  MODIFIED classes: {0}", _activitiesModified.Count);
                foreach (var a in _activitiesModified)
                {
                    Console.WriteLine("    {0}", a.Name);
                }
            }

            void IDiffActions.ActivityOnlyInFirst(ClassRecord a)
            {
                Console.WriteLine(FORMAT_A1, a.Name);
                foreach (var p in a.Properties)
                {
                    PrintProperty1(p);
                }
                AddActivityDeleted(a);
            }

            void IDiffActions.ActivityOnlyInSecond(ClassRecord a)
            {
                Console.WriteLine(FORMAT_A2, a.Name);
                foreach (var p in a.Properties)
                {
                    PrintProperty2(p);
                }
                AddActivityAdded(a);
            }

            void IDiffActions.PropertyMatch(PropertyRecord p)
            {
                if (IsFirstProperty(p))
                {
                    Console.WriteLine(FORMAT_A0, p.Activity.Name);
                }
                PrintProperty0(p);
            }

            void IDiffActions.PropertyOnlyInFirst(PropertyRecord p)
            {
                if (IsFirstProperty(p))
                {
                    Console.WriteLine(FORMAT_A0, p.Activity.Name);
                }
                PrintProperty1(p);
                AddPropertyDeleted(p);
            }

            void IDiffActions.PropertyOnlyInSecond(PropertyRecord p)
            {
                if (IsFirstProperty(p))
                {
                    Console.WriteLine(FORMAT_A0, p.Activity.Name);
                }
                PrintProperty2(p);
                AddPropertyAdded(p);
            }

            private void PrintProperty0(PropertyRecord p)
            {
                PrintProperty(p, FORMAT_P0, FORMAT_P0_TYPE);
            }

            private void PrintProperty1(PropertyRecord p)
            {
                PrintProperty(p, FORMAT_P1, FORMAT_P1_TYPE);
            }

            private void PrintProperty2(PropertyRecord p)
            {
                PrintProperty(p, FORMAT_P2, FORMAT_P2_TYPE);
            }

            private void PrintProperty(PropertyRecord p, string format1, string format2)
            {
                if (_typefmt == TypeFormat.NONE)
                {
                    Console.WriteLine(format1, p.Name);
                }
                else if (_typefmt == TypeFormat.FULL)
                {
                    Console.WriteLine(format2, p.Name, p.Type);
                }
                else
                {
                    Console.WriteLine(format2, p.Name, TypeString.Symplify(p.Type));
                }
            }

            private bool IsFirstProperty(PropertyRecord p)
            {
                if (_currentActivityName == null || _currentActivityName != p.Activity.Name)
                {
                    _currentActivityName = p.Activity.Name;
                    _modified = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private void AddPropertyDeleted(PropertyRecord p)
            {
                _propertiesDeleted.Add(p);
                if (!_modified)
                {
                    _activitiesModified.Add(p.Activity);
                    _modified = true;
                }
            }

            private void AddPropertyAdded(PropertyRecord p)
            {
                _propertiesAdded.Add(p);
                if (!_modified)
                {
                    _activitiesModified.Add(p.Activity);
                    _modified = true;
                }
            }

            private void AddActivityDeleted(ClassRecord a)
            {
                _activitiesDeleted.Add(a);
                _currentActivityName = null;
                _modified = false;
            }

            private void AddActivityAdded(ClassRecord a)
            {
                _activitiesAdded.Add(a);
                _currentActivityName = null;
                _modified = false;
            }
        }
    }
}
