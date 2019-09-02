using System;
using System.IO;

namespace UiPathTeam.ActivityReader
{
    class Program
    {
        static public readonly string TRY_HELP = "Try -help for usage.";
        static public readonly string ERROR_BAD_CMDLINE_SYNTAX = "ERROR: Bad command line syntax. " + TRY_HELP;

        static public string name = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

        static public int errors = 0;

        static public bool debug = false;

        static void Main(string[] args)
        {
            Configuration.GetInstance().Load();

            if (args.Length == 0)
            {
                Console.WriteLine(TRY_HELP);
                Environment.Exit(0);
            }

            ICommand command = CommandFactory.GetCommand(args[0]);

            if (command == null)
            {
                Console.Error.WriteLine(ERROR_BAD_CMDLINE_SYNTAX);
                Environment.Exit(1);
            }

            if (!command.Parse(args))
            {
                Console.Error.WriteLine(Program.ERROR_BAD_CMDLINE_SYNTAX);
                Environment.Exit(127);
            }

            command.Run();

            Environment.Exit(Program.errors == 0 ? 0 : 1);
        }

        static public void PrintError(string message, string filename)
        {
            errors++;
            Console.Error.WriteLine("ERROR: {0}", message);
            Console.Error.WriteLine("  {0}", filename);
        }

        static public void PrintError(Exception ex, string filename)
        {
            PrintError(ex, filename, null);
        }

        static public void PrintError(Exception ex, string filename1, string filename2)
        {
            errors++;
            Console.Error.WriteLine("ERROR: {0}", ex.Message);
            Console.Error.WriteLine("  {0}", filename1);
            if (filename2 != null)
            {
                Console.Error.WriteLine("    {0}", filename2);
            }
        }
    }
}
