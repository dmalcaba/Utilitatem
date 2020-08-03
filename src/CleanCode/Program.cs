using System;
using System.Collections;
using System.IO;
using System.Text;

namespace CleanCode
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = Directory.GetCurrentDirectory();

            ProcessDirectory(path);
        }

        // Process all files in the directory passed in, recurse on any directories
        // that are found, and process the files they contain.
        public static void ProcessDirectory(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        // Insert logic for processing found files here.
        public static void ProcessFile(string path)
        {
            var fileInfo = Path.GetExtension(path);

            if (fileInfo == ".txt")
            {
                CndRangeRefactor(path);
            }
        }

        /// <summary>
        /// Refactor CndRange; change to use if statement instead
        /// for better code readability
        /// </summary>
        public static void CndRangeRefactor(string path)
        {
            var stringToFind = "Where.Add(CndRange(() =>";

            var fileToProcess = $"{path}.old";
            var workingfile = $"{path}.new";
            File.Move(path, fileToProcess);

            bool fileNeedsCleaning = false;

            using var fileRead = new StreamReader(fileToProcess, Encoding.UTF8);
            using var fileWrite = new StreamWriter(workingfile, false, Encoding.UTF8);

            string line;

            while ((line = fileRead.ReadLine()) != null)
            {
                if (line.Trim().StartsWith(stringToFind))
                {
                    fileNeedsCleaning = true;

                    var codePlacement = line.IndexOf("Where");

                    var padding = line.Substring(0, codePlacement);

                    var getString = line.Split("=> ");

                    if (getString.Length == 2)
                    {
                        var cndRangeParam = getString[1].Split(",");

                        if (cndRangeParam.Length == 2)
                        {
                            var ifCondition = cndRangeParam[0];
                            var whereCondition = cndRangeParam[1].Trim();

                            var newLine = new StringBuilder();

                            newLine.AppendLine($"{padding}if ({ifCondition})");
                            newLine.AppendLine($"{padding}{{");
                            newLine.AppendLine($"{padding}    Where.Add({whereCondition[0..^3]});");
                            newLine.Append($"{padding}}}");

                            fileWrite.WriteLine(newLine.ToString());
                        }
                        else
                        {
                            fileWrite.WriteLine(line);
                        }
                    }
                    else
                    {
                        fileWrite.WriteLine(line);
                    }
                }
                else
                {
                    fileWrite.WriteLine(line);
                }
            }

            fileRead.Close();
            fileWrite.Close();

            if (fileNeedsCleaning)
            {
                File.Move(workingfile, path);
                File.Delete(fileToProcess);
                Console.WriteLine("Processed file '{0}'.", path);
            }
            else
            {
                // revert back to original one
                File.Delete(workingfile);
                File.Move(fileToProcess, path);
                Console.Write(".");
            }
        }



        /// <summary>
        /// For replacing User32 messagebox to standard message box
        /// </summary>
        /// <param name="path"></param>
        public static void ChangeNoRecordsFound(string path)
        { 
            var stringToReplace = "Create<Component_Call_Programs.Functions.I_User32_Messagebox>().Run(\"Information\",";

            var fileToProcess = $"{path}.old";
            var workingfile = $"{path}.new";
            File.Move(path, fileToProcess);

            bool fileNeedsCleaning = false;

            using var fileRead = new StreamReader(fileToProcess, Encoding.UTF8);
            using var fileWrite = new StreamWriter(workingfile, false, Encoding.UTF8);

            string line;

            while ((line = fileRead.ReadLine()) != null)
            {
                if (line.Contains(stringToReplace))
                {
                    fileNeedsCleaning = true;

                    var findInformation = line.IndexOf("Information");

                    var start = line.IndexOf('"', findInformation + 12) + 1;
                    var end = line.IndexOf('"', start);
                    var message = line[start..end];

                    var newCode = $"Message.ShowInfo(\"{message}\");";

                    var startReplace = line.IndexOf("Create<Component_Call_Programs");
                    var endReplace = line.Length;
                    var oldCode = line[startReplace..endReplace];

                    var newLine = line.Replace(oldCode, newCode);

                    fileWrite.WriteLine(newLine);

                }
                else
                {
                    fileWrite.WriteLine(line);
                }
            }

            fileRead.Close();
            fileWrite.Close();

            if (fileNeedsCleaning)
            {
                File.Move(workingfile, path);
                File.Delete(fileToProcess);
                Console.WriteLine("Processed file '{0}'.", path);
            }
            else
            {
                // revert back to original one
                File.Delete(workingfile);
                File.Move(fileToProcess, path);
                Console.Write(".");
            }
        }

        /// <summary>
        /// For removing unreachable code
        /// </summary>
        /// <param name="path"></param>
        public static void RewriteFile(string path)
        {
            var fileToProcess = $"{path}.old";
            var workingfile = $"{path}.new";
            File.Move(path, fileToProcess);

            bool fileNeedsCleaning = false;

            using var fileRead = new StreamReader(fileToProcess, Encoding.UTF8);
            using var fileWrite = new StreamWriter(workingfile, false, Encoding.UTF8);

            string line;

            bool onStartFound = false;
            Stack onStartBracketStack = new Stack();

            bool ifLineFound = false;
            Stack ifLineBracketStack = new Stack();

            while ((line = fileRead.ReadLine()) != null)
            {
                if (line.Trim() == "protected override void OnStart()")
                {
                    onStartFound = true;
                }

                if (onStartFound)
                {
                    if (line.Trim() == "{")
                    {
                        onStartBracketStack.Push("{");
                    }

                    if (line.Trim() == "}")
                    {
                        onStartBracketStack.Pop();

                        if (onStartBracketStack.Count == 0)
                            onStartFound = false;
                    }

                    fileWrite.WriteLine(line);
                    continue;
                }

                if (line.Trim() == "if (false)")
                {
                    fileNeedsCleaning = true;
                    ifLineFound = true;
                    continue;
                }

                if (line.Trim() == "{" && ifLineFound)
                {
                    ifLineBracketStack.Push(true);
                    continue;
                }

                if (line.Trim() == "}" && ifLineFound)
                {
                    ifLineBracketStack.Pop();

                    if (ifLineBracketStack.Count == 0)
                        ifLineFound = false;

                    continue;
                }

                // if (false) is found and the next line doesn't
                // contain a bracket, ignore the line
                if (ifLineFound && ifLineBracketStack.Count == 0)
                {
                    ifLineFound = false;
                    continue;
                }

                if (ifLineFound && ifLineBracketStack.Count != 0)
                {
                    continue;
                }

                fileWrite.WriteLine(line);
            }

            fileRead.Close();
            fileWrite.Close();

            if (fileNeedsCleaning)
            {
                File.Move(workingfile, path);
                File.Delete(fileToProcess);
                Console.WriteLine("Processed file '{0}'.", path);
            }
            else 
            {
                // revert back to original one
                File.Delete(workingfile);
                File.Move(fileToProcess, path);
                Console.Write(".");
            }

        }
    }
}
