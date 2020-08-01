using System;
using System.IO;

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
                RewriteFile(path);
                Console.WriteLine("Processed file '{0}'.", path);
            }
        }

        public static void RewriteFile(string path)
        {
            var fileToProcess = $"{path}.old";
            File.Move(path, fileToProcess);

            using var fileRead = new StreamReader(fileToProcess);
            using var fileWrite = new StreamWriter(path);

            string line;

            bool ifLineFound = false;
            bool openBracketFound = false;

            while ((line = fileRead.ReadLine()) != null)
            {
                if (line.Trim() == "if (false)")
                {
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

                fileWrite.WriteLine(line.TrimEnd());
            }

            fileRead.Close();
            File.Delete(fileToProcess);
        }
    }
}
