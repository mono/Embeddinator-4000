using System;

namespace MonoManagedToNative
{
    public class Binder
    {
        static string Language;
        static string MonoIncDir;
        static string OutputDir;

        static void ParseCommandLineArgs(string[] args)
        {
            var showHelp = args.Length == 0;

            var optionSet = new Mono.Options.OptionSet() {
                { "language=", "output language", v => Language = v },
                { "o|out=", "output directory", v => OutputDir = v },
                { "mono=", "Mono include directory", v => MonoIncDir = v },
                { "h|help",  "show this message and exit",  v => showHelp = v != null },
            };

            try
            {
                optionSet.Parse(args);
            }
            catch (Mono.Options.OptionException e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(0);
            }

            if (showHelp)
            {
                // Print usage and exit.
                Console.WriteLine("{0} [--gen=c/c++] [--out=dir] "
                    + "[--mono=dir]",
                    AppDomain.CurrentDomain.FriendlyName);
                Environment.Exit(0);
            }
        }

        static void Main(string[] args)
        {
            ParseCommandLineArgs(args);

            var options = new Options();
            var driver = new Driver(options);

            driver.Run();
        }
    }
}
