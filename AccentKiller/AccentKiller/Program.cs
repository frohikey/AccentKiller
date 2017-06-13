using System;
using System.IO;
using System.Linq;
using Force.Crc32;
using Microsoft.Extensions.CommandLineUtils;
using Sushi2;

namespace AccentKiller
{
    public class Program
    {
        private static bool _deleteAllDuplicateDirs;
        private static bool _deleteAllDuplicateFiles;

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
            var newDir = di.Name.ToStringWithoutDiacritics();

            if (newDir != di.Name)
            {
                Console.WriteLine(di.Name + " -> " + newDir);
                
                dir = Path.Combine(di.Parent.FullName, newDir);

                try
                {
                    var dir2 = new DirectoryInfo(dir);

                    if (dir2.Exists)
                    {
                        if (_deleteAllDuplicateDirs)
                        {
                            di.Delete(true);
                        }
                        else
                        {
                            var r = Ask("Wanna remove original directory?");

                            if (r == null)
                            {
                                r = true;
                                _deleteAllDuplicateDirs = true;
                            }

                            if (r == true)
                                di.Delete(true);
                        }

                        return;
                    }

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
                var newFile = fi.Name.ToStringWithoutDiacritics();

                if (fi.Name != newFile)
                {
                    Console.WriteLine(fi.Name + " -> " + newFile);                    

                    try
                    {
                        var newFile2 = new FileInfo(Path.Combine(fi.DirectoryName, newFile));

                        if (newFile2.Exists)
                        {
                            var crco = GetFileCrc32(fi);
                            var crcd = GetFileCrc32(newFile2);

                            if (crco == crcd)
                            {
                                if (_deleteAllDuplicateFiles)
                                {
                                    fi.Delete();
                                }
                                else
                                {
                                    var r = Ask("Files are same. Want to delete the original?");

                                    if (r == null)
                                    {
                                        r = true;
                                        _deleteAllDuplicateFiles = true;
                                    }

                                    if (r == true)
                                        fi.Delete();
                                }
                            }
                            else
                            {
                                //Ask("Files are different!");
                            }
                        }
                        else
                        {
                            fi.MoveTo(Path.Combine(fi.DirectoryName, newFile));
                        }
                    }
                    catch
                    {
                        Console.WriteLine("! error renaming file");
                        Console.ReadLine();
                    }
                }
            }

            foreach (var directory in Directory.GetDirectories(dir))
            {
                CleanUp(directory);
            }
        }

       private static uint GetFileCrc32(FileInfo fi)
        {
            if (!fi.Exists)
                return 0;

            var bytes = File.ReadAllBytes(fi.FullName);
            var crc1 = new Crc32Algorithm().ComputeHash(bytes);

            if (BitConverter.IsLittleEndian)
                crc1 = crc1.Reverse().ToArray();

            var crc2 = BitConverter.ToUInt32(crc1, 0);

            return crc2;
        }

        private static bool? Ask(string question)
        {
            Console.Write(question + " [Y/n/a]? ");
            var a = Console.ReadLine();

            if (a.Equals("y", StringComparison.OrdinalIgnoreCase))
                return true;

            if (a.Equals("n", StringComparison.OrdinalIgnoreCase))
                return false;

            return null;
        }
    }
}