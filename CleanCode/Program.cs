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

            if (fileInfo == ".cs")
            {
                RewriteFile(path);
            }
        }

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
            bool openBracketFound = false;

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
                    openBracketFound = true;
                    continue;
                }

                if (line.Trim() == "}" && ifLineFound && openBracketFound)
                {
                    ifLineFound = false;
                    openBracketFound = false;
                    continue;
                }

                if (ifLineFound && !openBracketFound)
                {
                    ifLineFound = false;
                    continue;
                }

                if (ifLineFound && openBracketFound)
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
            }

        }
    }
}
