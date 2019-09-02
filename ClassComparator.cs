namespace UiPathTeam.ActivityReader
{
    public class ClassComparator
    {
        static private ClassComparator _singleton;

        static ClassComparator()
        {
            _singleton = new ClassComparator();
        }

        static public ClassComparator GetInstance()
        {
            return _singleton;
        }

        public TypeFormat TypeFormat
        {
            get;
            set;
        }

        private ClassComparator()
        {
            TypeFormat = TypeFormat.SIMPLE;
        }

        public int Run(ClassDatabase db1, ClassDatabase db2, IDiffActions actions)
        {
            int differences = 0;
            var e1 = db1.Classes.GetEnumerator();
            var e2 = db2.Classes.GetEnumerator();
            var b1 = e1.MoveNext();
            var b2 = e2.MoveNext();
            while (b1 && b2)
            {
                var c1 = e1.Current;
                var c2 = e2.Current;
                if (!IsAccessibleActivity(c1))
                {
                    b1 = e1.MoveNext();
                }
                else if (!IsAccessibleActivity(c2))
                {
                    b2 = e2.MoveNext();
                }
                else
                {
                    var rc = c1.Name.CompareTo(c2.Name);
                    if (rc < 0)
                    {
                        actions.ActivityOnlyInFirst(c1);
                        differences++;
                        b1 = e1.MoveNext();
                    }
                    else if (rc > 0)
                    {
                        actions.ActivityOnlyInSecond(c2);
                        differences++;
                        b2 = e2.MoveNext();
                    }
                    else
                    {
                        differences += Run(c1, c2, actions);
                        b1 = e1.MoveNext();
                        b2 = e2.MoveNext();
                    }
                }
            }
            while (b1)
            {
                var c1 = e1.Current;
                if (IsAccessibleActivity(c1))
                {
                    actions.ActivityOnlyInFirst(c1);
                    differences++;
                }
                b1 = e1.MoveNext();
            }
            while (b2)
            {
                var c2 = e2.Current;
                if (IsAccessibleActivity(c2))
                {
                    actions.ActivityOnlyInSecond(c2);
                    differences++;
                }
                b2 = e2.MoveNext();
            }
            return differences;
        }

        static private bool IsAccessibleActivity(ClassRecord c)
        {
            return c.IsPublic && !c.IsAbstract && c.IsActivity;
        }

        /// <summary>
        /// Finds the differences in properties.
        /// </summary>
        /// <returns>Number of differences found</returns>
        public int Run(ClassRecord c1, ClassRecord c2, IDiffActions actions)
        {
            int differences = 0;
            var e1 = c1.Properties.GetEnumerator();
            var e2 = c2.Properties.GetEnumerator();
            var b1 = e1.MoveNext();
            var b2 = e2.MoveNext();
            while (b1 && b2)
            {
                var p1 = e1.Current;
                var p2 = e2.Current;
                var rc = p1.Name.CompareTo(p2.Name);
                if (rc < 0)
                {
                    actions.PropertyOnlyInFirst(p1);
                    differences++;
                    b1 = e1.MoveNext();
                }
                else if (rc > 0)
                {
                    actions.PropertyOnlyInSecond(p2);
                    differences++;
                    b2 = e2.MoveNext();
                }
                else if (TypeFormat != TypeFormat.NONE && p1.Type != p2.Type)
                {
                    actions.PropertyOnlyInFirst(p1);
                    actions.PropertyOnlyInSecond(p2);
                    differences++;
                    b1 = e1.MoveNext();
                    b2 = e2.MoveNext();
                }
                else
                {
                    actions.PropertyMatch(p1);
                    b1 = e1.MoveNext();
                    b2 = e2.MoveNext();
                }
            }
            while (b1)
            {
                actions.PropertyOnlyInFirst(e1.Current);
                differences++;
                b1 = e1.MoveNext();
            }
            while (b2)
            {
                actions.PropertyOnlyInSecond(e2.Current);
                differences++;
                b2 = e2.MoveNext();
            }
            return differences;
        }
    }
}
