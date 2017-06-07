using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.CommandLineUtils;

namespace AccentKiller
{
    public class Program
    {
        static void Main(params string[] args)
        {            
            var app = new CommandLineApplication();            

            var clean = app.Command("clean", config =>
            {
                config.Description = "Clean directories/files.";
                config.HelpOption("-? | -h | --help");

                var dir = config.Argument("directory", "Full directory");

                config.OnExecute(() =>
                {
                    if (string.IsNullOrWhiteSpace(dir.Value))
                    {
                        config.ShowHelp("clean");
                        return 1;
                    }

                    CleanUp(dir.Value);
                    return 0;
                });
            });

            clean.Command("help", config => {
                config.Description = "get help";
                config.OnExecute(() => {
                    Console.WriteLine("Cleans up directories/files.");
                    return 1;
                });
            });

            app.HelpOption("-? | -h | --help");

            var result = app.Execute(args);
            Environment.Exit(result);
            
        }

        private static void CleanUp(string dir)
        {
            Console.WriteLine("* " + dir);

            var di = new DirectoryInfo(dir);
            var newDir = CleanCharacters(di.Name);

            if (newDir != di.Name)
            {
                Console.WriteLine(di.Name + " -> " + newDir);
                
                dir = Path.Combine(di.Parent.FullName, newDir);

                try
                {
                    di.MoveTo(dir);
                }
                catch
                {
                    Console.WriteLine("! error renaming directory");
                }
            }

            foreach (var file in Directory.GetFiles(dir))
            {
                var fi = new FileInfo(file);
                var newFile = CleanCharacters(fi.Name);

                if (fi.Name != newFile)
                {
                    Console.WriteLine(fi.Name + " -> " + newFile);                    

                    try
                    {
                        fi.MoveTo(Path.Combine(fi.DirectoryName, newFile));
                    }
                    catch
                    {
                        Console.WriteLine("! error renaming file");
                    }
                }
            }

            foreach (var directory in Directory.GetDirectories(dir))
            {
                CleanUp(directory);
            }
        }

        private static bool ContainsUnicodeCharacter(string input)
        {
            const int maxAnsiCode = 255;

            return input.Any(c => c > maxAnsiCode);
        }

        private static string CleanCharacters(string input)
        {
            if (!ContainsUnicodeCharacter(input))
                return input;

            var newStringBuilder = new StringBuilder();
            newStringBuilder.Append(input.Normalize(NormalizationForm.FormKD)
                                            .Where(x => x < 128)
                                            .ToArray());
            return newStringBuilder.ToString();
        }        
    }
}