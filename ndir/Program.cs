using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ndir
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                List<string> filePatterns = new List<string>();
                bool longFormat = false;
                bool dateOrder = false;

                foreach (string arg in args)
                {
                    string larg = arg.ToLower();

                    if (arg == "-l")
                    {
                        longFormat = true;
                    }
                    else if (larg == "/od" || larg == "-od")
                    {
                        dateOrder = true;
                    }
                    else
                    {
                        if (Directory.Exists(arg))
                            filePatterns.Add(arg + "\\*");
                        else
                            filePatterns.Add(arg);
                    }
                }

                if (filePatterns.Count == 0)
                    filePatterns.Add("*");

                long totalLength = 0;
                long fileCount = 0;
                long dirCount = 0;

                foreach (string fp in filePatterns)
                {
                    foreach (string file in Util.FilePattern.Enumerate(fp))
                    {
                        string f = file;
                        if (f.StartsWith(".\\"))
                            f = f.Substring(2);
                        if (longFormat)
                        {
                            FileInfo info = new FileInfo(f);
                            string len = "";
                            if ((info.Attributes & FileAttributes.Directory) == 0)
                            {
                                len = info.Length.ToString("#,#");
                                totalLength += info.Length;
                                ++fileCount;
                            }
                            else
                            {
                                ++dirCount;
                            }

                            Console.WriteLine("{0,-64} {1,10} {2:MM/dd/yy hh:mm tt}", f, len, info.LastWriteTime);
                        }
                        else
                        {
                            Console.WriteLine(f);
                        }
                    }
                }

                if (longFormat)
                    Console.WriteLine(" {0} files, {1} directories, Total size {2}", fileCount, dirCount, totalLength.ToString("#,#"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}
