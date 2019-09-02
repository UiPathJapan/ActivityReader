namespace UiPathTeam.ActivityReader
{
    public interface ICommand
    {
        bool Parse(string[] args);

        void Run();
    }
}
