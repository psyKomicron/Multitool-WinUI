using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Multitool.DAL.Completion
{
    /// <summary>
    /// Completes filesystem pathes
    /// </summary>
    public class PathCompletor : IPathCompletor
    {
        /// <inheritdoc/>
        public string[] Complete(string input)
        {
            string fileName = GetFileName(input, out int i);
            string directory = GetDirName(input, i);
            string[] entries = GetEntries(directory);
            List<string> list = new();
            if (entries != null)
            {
                PutEntries(fileName, list, entries);
            }
            return list.ToArray();
        }

        private static void PutEntries(string fileName, IList<string> list, string[] joins)
        {
            if (string.IsNullOrEmpty(fileName)) // no file name, list all directory
            {
                for (int j = 0; j < joins.Length; j++)
                {
                    list.Add(joins[j]);
                }
            }
            else // file name, search file name matches in the directory
            {
                string path;
                for (int j = 0; j < joins.Length; j++)
                {
                    path = GetFileName(joins[j]);
                    if (!path.StartsWith(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (list.Contains(joins[j]))
                        {
                            list.Remove(joins[j]);
                        }
                    }
                    else
                    {
                        if (!list.Contains(joins[j]))
                        {
                            list.Add(joins[j]);
                        }
                    }
                }
            }
        }

        private static string GetFileName(string input, out int i)
        {
            if (input.Length > 1)
            {
                string fileName = string.Empty;
                for (i = input.Length - 1; i > 0; i--)
                {
                    if (input[i] == Path.DirectorySeparatorChar || input[i] == Path.AltDirectorySeparatorChar)
                    {
                        i++;
                        break;
                    }
                    fileName += input[i];
                }
                return Reverse(fileName);
            }
            else if (input.Length == 1)
            {
                i = 1;
                return input;
            }
            else
            {
                i = 0;
                return input;
            }
        }

        private static string GetFileName(string input)
        {
            string fileName = string.Empty;
            for (int i = input.Length - 1; i > 0; i--)
            {
                if (input[i] == Path.DirectorySeparatorChar || input[i] == Path.AltDirectorySeparatorChar)
                {
                    break;
                }
                fileName += input[i];
            }
            return Reverse(fileName);
        }

        private static string GetDirName(string input, int i)
        {
            string directory = null;
            if (input.Length < 4)
            {
                switch (input.Length)
                {
                    case 1:
                        directory = input + ':' + Path.DirectorySeparatorChar;
                        break;
                    case 2:
                        directory = input + Path.DirectorySeparatorChar;
                        break;
                    case 3:
                        directory = input;
                        break;
                }
            }
            else
            {
                directory = input[..i];
            }
            return directory;
        }

        private static string[] GetEntries(string path)
        {
            if (Directory.Exists(path))
            {
                string[] directories = Directory.GetDirectories(path);
                string[] files = Directory.GetFiles(path);
                string[] joins = new string[directories.Length + files.Length];

                int j = 0;
                for (; j < files.Length; j++)
                {
                    joins[j] = files[j];
                }
                for (int i = 0; i < directories.Length; i++, j++)
                {
                    joins[j] = directories[i];
                }
                return joins;
            }
            else
            {
                return null;
            }
        }

        private static string Reverse(string input)
        {
            if (input.Length > 100)
            {
                StringBuilder builder = new();
                for (int i = input.Length - 1; i >= 0; i--)
                {
                    builder.Append(input[i]);
                }
                return builder.ToString();
            }
            else
            {
                string reverse = string.Empty;
                for (int i = input.Length - 1; i >= 0; i--)
                {
                    reverse += input[i];
                }
                return reverse;
            }
        }
    }
}
