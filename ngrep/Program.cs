using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace grep
{
    class Program
    {
        static void SearchFile(Regex regex, string file, Options options)
        {
            int lineNo = -1;
            int lastPrintedLine = -1;
            int before = options.BeforeContext;
            int after = options.AfterContext;
            string[] buffer = new string[before + 1];
            int afterLeft = 0;
            int count = 0;
            try
            {
                using (StreamReader sr = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (regex.Matches(line).Count > 0)
                        {
                            if (before > 0)
                            {
                                int firstLine = lineNo - before + 1;
                                if (firstLine < 0)
                                    firstLine = 0;
                                if (firstLine < lastPrintedLine)
                                    firstLine = lastPrintedLine + 1;
                                for (int i = firstLine; i <= lineNo; ++i)
                                    Console.WriteLine("{0}- {1}", file, buffer[i % buffer.Length]);
                            }

                            if (options.CountOnly)
                                ++count;
                            else
                                Console.WriteLine("{0}: {1}", file, line);
                            lastPrintedLine = lineNo;
                            afterLeft = after;
                            if (before > 0 && after == 0)
                                Console.WriteLine("--");
                        }
                        else if (afterLeft > 0)
                        {
                            --afterLeft;
                            Console.WriteLine("{0}- {1}", file, line);
                            lastPrintedLine = lineNo + 1;
                            if (afterLeft == 0)
                                Console.WriteLine("--");
                        }
                        ++lineNo;
                        buffer[lineNo % buffer.Length] = line;
                    }

                    if (options.CountOnly)
                        Console.WriteLine("{0}: {1}", file, count);
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                Console.WriteLine("Can not open: " + file);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        class Options
        {
            public int BeforeContext;
            public int AfterContext;
            public bool CountOnly;
        }

        static void Main(string[] args)
        {
            try
            {
                string pattern = null;
                List<string> filePatterns = new List<string>();
                RegexOptions regexOptions = RegexOptions.None;
                Options options = new Options();
                foreach (string arg in args)
                {
                    if (arg[0] == '-')
                    {
                        if (arg.ToLower().Equals("-i"))
                            regexOptions |= RegexOptions.IgnoreCase;
                        else if (arg.ToLower().Equals("-c"))
                            options.CountOnly = true;
                        else if (arg.ToLower().StartsWith("-b"))
                            options.BeforeContext = Int32.Parse(arg.Substring(2));
                        else if (arg.ToLower().StartsWith("-a"))
                            options.AfterContext = Int32.Parse(arg.Substring(2));
                    }
                    else if (pattern == null)
                        pattern = arg;
                    else
                        filePatterns.Add(arg);
                }
                if (pattern == null)
                {
                    Console.WriteLine("no pattern");
                    return;
                }

                Regex regex = new Regex(pattern, regexOptions);

                if (options.CountOnly)
                {
                    options.BeforeContext = 0;
                    options.AfterContext = 0;
                }

                foreach (string fp in filePatterns)
                {
                    foreach (string file in Util.FilePattern.Enumerate(fp))
                    {
                        try
                        {
                            if ((File.GetAttributes(file) & FileAttributes.Directory) == 0)
                                SearchFile(regex, file, options);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("can not search {0} due to exception {1}", file, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}
