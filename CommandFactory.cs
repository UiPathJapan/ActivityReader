using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.ActivityReader
{
    public class CommandFactory
    {
        static public ICommand GetCommand(string key)
        {
            key = key.ToUpperInvariant();
            if (key == "-HELP")
            {
                return new HelpCommand();
            }
            else if (key == "-DIFF")
            {
                return new DiffCommand();
            }
            else if (key == "-PRINT")
            {
                return new PrintCommand();
            }
            else
            {
                return null;
            }
        }
    }
}
