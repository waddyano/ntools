using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Util
{
    class FilePattern
    {
        static IEnumerable<string> RecursiveMatch(string dir, string[] segments, int segmentIndex)
        {
            if (segmentIndex >= segments.Length)
                yield break;
            string pattern = segments[segmentIndex];
            if (pattern.Equals(".."))
            {
                string path = Path.GetFullPath(dir);
                foreach (string o in RecursiveMatch(Path.GetDirectoryName(path), segments, segmentIndex + 1))
                    yield return o;
            }
            else if (segmentIndex != segments.Length - 1)
            {
                if (pattern.Equals("**"))
                {
                    string[] dirs = null;

                    try
                    {
                        dirs = Directory.GetDirectories(dir);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine("No access to: " + dir);
                        yield break;
                    }

                    string[] files = null;
                    
                    try
                    {
                        files = Directory.GetFileSystemEntries(dir, segments[segmentIndex + 1]);
                        files = FilterPreciseExtension(segments[segmentIndex + 1], files);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine("No access to: " + dir);
                        yield break;
                    }

                    int i = 0;
                    int j = 0;
                    while (i < dirs.Length || j < files.Length)
                    {
                        bool isDir = false;
                        bool yieldThis = false;
                        string entry;
                        if (i == dirs.Length)
                        {
                            entry = files[j++];
                            yieldThis = true;
                        }
                        else if (j == files.Length)
                        {
                            isDir = true;
                            entry = dirs[i++];
                        }
                        else
                        {
                            int comp = dirs[i].CompareTo(files[j]);
                            if (comp < 0)
                            {
                                isDir = true;
                                entry = dirs[i++];
                            }
                            else if (comp == 0)
                            {
                                isDir = true;
                                entry = dirs[i++];
                                j++;
                                yieldThis = true;
                            }
                            else
                            {
                                entry = files[j++];
                                yieldThis = true;
                            }
                        }
                        if (yieldThis)
                            yield return entry;
                        if (isDir)
                        {
                            foreach (string o in RecursiveMatch(entry, segments, segmentIndex))
                                yield return o;
                        }
                    }
                }
                else
                {
                    string[] subdirs = Directory.GetDirectories(dir, pattern);
                    foreach (string subdir in subdirs)
                        foreach (string o in RecursiveMatch(subdir, segments, segmentIndex + 1))
                            yield return o;
                }
            }
            else
            {
                string[] files = null;

                try
                {
                    files = Directory.GetFileSystemEntries(dir, pattern);
                    files = FilterPreciseExtension(pattern, files);
                }
                catch (UnauthorizedAccessException)
                {
                    yield break;
                }

                foreach (string file in files)
                {
                    yield return file;
                }
            }
        }

        private static string[] FilterPreciseExtension(string pattern, string[] files)
        {
            int ext = pattern.LastIndexOf('.');
            if (ext >= 0)
            {
                if (pattern.Length == ext + 4 && pattern.IndexOf('*', ext) < 0 && pattern.IndexOf('?', ext) < 0)
                {
                    List<string> filtered = new List<string>();
                    for (int i = 0; i < files.Length; ++i)
                    {
                        string file = files[i];
                        int fileExt = files[i].LastIndexOf('.');
                        if (file.Length == fileExt + 4)
                            filtered.Add(file);
                    }
                    files = filtered.ToArray();
                }
            }

            return files;
        }

        public static IEnumerable<string> Enumerate(string filePattern)
        {
            if (filePattern.Length == 0)
                yield break;
            filePattern = filePattern.Replace('/', '\\');
            string startDir = ".";
            if (filePattern[0] == '\\')
            {
                if (filePattern[1] == '\\')
                {
                    int index = filePattern.IndexOf('\\', 2);
                    if (index > 0)
                    {
                        index = filePattern.IndexOf('\\', index + 1);
                        if (index > 0)
                        {
                            startDir = filePattern.Substring(0, index);
                            filePattern = filePattern.Substring(index + 1);
                        }
                        else
                        {
                            yield break;
                        }
                    }
                    else
                    {
                        yield break;
                    }
                }
                else
                {
                    startDir = "\\";
                    filePattern = filePattern.Substring(1);
                }
            }
            else if (filePattern.Length > 2 && filePattern[1] == ':' && filePattern[2] == '\\')
            {
                startDir = filePattern.Substring(0, 3);
                filePattern = filePattern.Substring(3);
            }
            string[] segments = filePattern.Split(new char[] { '\\' });
            foreach (string file in RecursiveMatch(startDir, segments, 0))
                yield return file;
        }
    }
}
