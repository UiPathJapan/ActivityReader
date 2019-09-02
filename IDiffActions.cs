namespace UiPathTeam.ActivityReader
{
    public interface IDiffActions
    {
        void ActivityOnlyInFirst(ClassRecord a);

        void ActivityOnlyInSecond(ClassRecord a);

        void PropertyMatch(PropertyRecord p);

        void PropertyOnlyInFirst(PropertyRecord p);

        void PropertyOnlyInSecond(PropertyRecord p);
    }
}
