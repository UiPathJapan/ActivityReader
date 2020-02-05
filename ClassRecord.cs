using System.Collections.Generic;
using System.Linq;

namespace UiPathTeam.ActivityReader
{
    public class ClassRecord
    {
        static public readonly string[] DEFAULT_ACTIVITY_CLASS_NAMES =
        {
            "System.Activities.Activity",
            "System.Activities.CodeActivity",
            "System.Activities.AsyncCodeActivity",
            "System.Activities.NativeActivity",
        };

        static public string[] ActivityClassNames
        {
            get;
            set;
        }

        static ClassRecord()
        {
            ActivityClassNames = DEFAULT_ACTIVITY_CLASS_NAMES;
        }

        private Mono.Cecil.TypeDefinition _td;

        private Mono.Cecil.ModuleDefinition _md;

        private ClassDatabase _db;

        public string Name
        {
            get
            {
                return GetSimpleName(_td);
            }
        }

        public string FullName
        {
            get
            {
                return _td.FullName;
            }
        }

        private string _namespace = null;

        public string Namespace
        {
            get
            {
                if (_namespace == null)
                {
                    int pos = Name.LastIndexOf('.');
                    if (pos > -1)
                    {
                        _namespace = Name.Substring(0, pos + 1);
                    }
                    else
                    {
                        _namespace = ".";
                    }
                }
                return _namespace;
            }
        }


        public string SuperClassName
        {
            get
            {
                return _td.BaseType != null ? GetSimpleName(_td.BaseType.FullName) : "";
            }
        }

        public string SuperClassFullName
        {
            get
            {
                return _td.BaseType != null ? _td.BaseType.FullName : string.Empty;
            }
        }

        public string PackageName
        {
            get
            {
                return _md.Name;
            }
        }

        public bool IsPublic
        {
            get
            {
                return _td.IsPublic;
            }
        }

        public bool IsAbstract
        {
            get
            {
                return _td.IsAbstract;
            }
        }

        private Dictionary<string, PropertyRecord> properties = null;

        public IEnumerable<PropertyRecord> Properties
        {
            get
            {
                if (properties == null)
                {
                    properties = new Dictionary<string, PropertyRecord>();
                    foreach (var pd in _td.Properties)
                    {
                        if (IsActivityProperty(pd))
                        {
                            AddProperty(pd);
                        }
                    }
                    PopulateProperties(SuperClassName, 0);
                }
                return properties.Values.OrderBy(x => x.Name);
            }
        }

        public ClassRecord(Mono.Cecil.TypeDefinition td, Mono.Cecil.ModuleDefinition md, ClassDatabase db)
        {
            _td = td;
            _md = md;
            _db = db;
        }

        public bool IsActivity
        {
            get
            {
                return CheckActivity(SuperClassName, 0);
            }
        }

        private bool CheckActivity(string name, int depth)
        {
            if (name.Length > 0 && depth < 30)
            {
                if (ActivityClassNames.Contains(name))
                {
                    return true;
                }
                if (_db.FindClass(name, out ClassRecord cr))
                {
                    if (CheckActivity(cr.SuperClassName, depth + 1))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void PopulateProperties(string className, int depth)
        {
            if (className.Length > 0 && depth < 30 && !ActivityClassNames.Contains(className))
            {
                if (_db.FindClass(className, out ClassRecord cr))
                {
                    foreach (var pd in cr._td.Properties)
                    {
                        if (IsInheritedProperty(pd))
                        {
                            AddProperty(DecoratePropertyName(pd, cr), pd);
                        }
                    }
                    PopulateProperties(cr.SuperClassName, depth + 1);
                }
            }
        }

        private bool IsActivityProperty(Mono.Cecil.PropertyDefinition pd)
        {
            return pd.HasThis
                && pd.SetMethod != null
                && pd.GetMethod != null
                && pd.GetMethod.IsPublic;
        }

        private bool IsInheritedProperty(Mono.Cecil.PropertyDefinition pd)
        {
            return IsActivityProperty(pd)
                && !(pd.GetMethod.IsVirtual && properties.TryGetValue(pd.Name, out PropertyRecord pr));
        }

        private PropertyRecord AddProperty(Mono.Cecil.PropertyDefinition pd)
        {
            return AddProperty(pd.Name, pd);
        }

        private PropertyRecord AddProperty(string name, Mono.Cecil.PropertyDefinition pd)
        {
            if (!properties.TryGetValue(name, out PropertyRecord pr))
            {
                pr = new PropertyRecord(name, pd, this);
                properties.Add(pr.Name, pr);
            }
            return pr;
        }

        private string DecoratePropertyName(Mono.Cecil.PropertyDefinition pd, ClassRecord cr)
        {
            if (cr.Name.StartsWith(Namespace))
            {
                return cr.Name.Substring(Namespace.Length) + "::" + pd.Name;
            }
            else
            {
                return cr.Name + "::" + pd.Name;
            }
        }

        static public string GetSimpleName(Mono.Cecil.TypeDefinition td)
        {
            return GetSimpleName(td.FullName);
        }

        static private string GetSimpleName(string name)
        {
            int pos = name.IndexOf('`');
            return pos > 0 ? name.Substring(0, pos) : name;
        }
    }
}
