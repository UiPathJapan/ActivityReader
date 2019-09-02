namespace UiPathTeam.ActivityReader
{
    public class PropertyRecord
    {
        public string Name
        {
            get;
            private set;
        }

        public string Type
        {
            get;
            private set;
        }

        public ClassRecord Activity
        {
            get;
            private set;
        }

        public PropertyRecord(string name, Mono.Cecil.PropertyDefinition pd, ClassRecord activity)
        {
            Name = name;
            Type = pd.PropertyType.FullName;
            Activity = activity;
        }
    }
}
