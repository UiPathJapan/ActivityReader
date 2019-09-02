using System;

namespace UiPathTeam.ActivityReader
{
    public class HelpCommand : ICommand
    {
        public HelpCommand()
        {
        }

        bool ICommand.Parse(string[] args)
        {
            return true;
        }

        void ICommand.Run()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  {0} -diff [-type=none|full|simple] FILE1.nupkg FILE2.nupkg", Program.name);
            Console.WriteLine("  {0} -diff [-type=none|full|simple] FILE1.nupkg FILE2A.nupkg FILE2B.nupkg", Program.name);
            Console.WriteLine("  {0} -diff [-type=none|full|simple] DIRECTORY1 DIRECTORY2", Program.name);
            Console.WriteLine("  {0} -print [DIRECTORY|FILE.nupkg|FILE.dll]...", Program.name);
        }
    }
}
