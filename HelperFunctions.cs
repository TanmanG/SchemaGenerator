using System.Text.RegularExpressions;

namespace SchemaGenerator
{
    public partial class HelperFunctions
    {
        /// <summary>
        /// Finds and returns the paths to all files with the given extension found within the provided folder and its subdirectories.
        /// </summary>
        /// <param name="extension">Extension, without a period, to search for. (e.g. xsd)</param>
        /// <param name="directories">Directories to folders to search.</param>
        /// <returns>A list of file directories of found items with the given extension.</returns>
        public static List<string> FindFilesByExtension(string extension, List<string> directories)
        {
            List<string> foundFiles = new();

            foreach (string directory in directories)
            {
                if (Directory.Exists(directory))
                {
                    string[] files = Directory.GetFiles(directory, $"*.{extension}", SearchOption.AllDirectories);
                    foundFiles.AddRange(files);
                }
            }

            return foundFiles;
        }
        
        /// <summary>
        /// Moves lines in the given file to the chosen line.
        /// </summary>
        /// <param name="file">The full path to the file to be edited.</param>
        /// <param name="start">The line to start on.</param>
        /// <param name="destination">The line to move the lines to. The line at this index will remain in its current index, with the start line after it.</param>
        /// <param name="end">The line to stop on, -1 will continue until the end of the file.</param>
        public static void MoveDocumentLines(string file, int start, int destination, int end = -1)
        {
            List<string> lines = new();

            // Read all lines from the document
            using (StreamReader reader = new(file))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            // Handle end line if not specified
            if (end == -1 || end > lines.Count)
            {
                end = lines.Count;
            }

            // Perform line movement
            if (start > 0 && start <= lines.Count && destination > 0 && destination <= lines.Count)
            {
                // Extract the lines to be moved
                List<string> movedLines = new();
                for (int i = start - 1; i < end; i++)
                {
                    movedLines.Add(lines[i]);
                }

                // Remove the lines from the original position
                lines.RemoveRange(start - 1, movedLines.Count);

                // Insert the lines at the destination
                lines.InsertRange(destination - 1, movedLines);
            }
            else
            {
                Console.WriteLine("Invalid line numbers.");
                return;
            }

            // Write the modified lines back to the document
            using (StreamWriter writer = new(file))
            {
                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }
            }
        }
        
        /// <summary>
        /// Inserts the given lines of text at the given position in the given document.
        /// </summary>
        /// <param name="file">The desired edited file's full path.</param>
        /// <param name="insertedLines">The lines desired to be inserted into the file.</param>
        /// <param name="insertPosition">The line location to insert the given lines. The line at this index will remain in its current index, with the inserted lines after it.</param>
        public static void InsertDocumentLines(string file, List<string> insertedLines, int insertPosition)
        {
            // Measure and store the length of the document being read into
            int fileLength = 0;
            using (StreamReader reader = new(file))
            {
                while (reader.ReadLine() != null) { fileLength++; }
            }


            // Write code to insert the given lines at the given position in the code
            using var writer = File.AppendText(file);

            // Write all the lines into the file
            foreach (string line in insertedLines)
            {
                writer.WriteLine(line);
            }
            writer.Close();

            // Move the selected lines to the desired location in the file
            MoveDocumentLines(file: file, start: fileLength + 1, destination: insertPosition);
        }
        
        /// <summary>
        /// Inserts the given value into the list at the given key, creating a new list if not present already.
        /// </summary>
        /// <typeparam name="K">Any non-nullable key-valid type.</typeparam>
        /// <typeparam name="T">Any type.</typeparam>
        /// <param name="dictionary">The dictionary to insert into.</param>
        /// <param name="value">The value to insert into the dictionary.</param>
        /// <param name="key">The key to match the dictionary against.</param>
        public static void AddLD<K, T>(Dictionary<K, List<T>> dictionary, T value, K key) where K : notnull
        { // Add a value to the provided dictionary's list if given key is present, otherwise, create a new list and add it
            if (dictionary.TryGetValue(key: key, out var values))
            {
                values.Add(value);
            }
            else
            {
                dictionary.Add(key: key, value: new() { value });
            }
        }

        /// <summary>
        /// Validates whether the given string is valid as a filename.
        /// </summary>
        /// <param name="filename">Filename to validate.</param>
        /// <returns>True if the filename provided is usable.</returns>
        public static bool CheckFilenameIsValid(string filename)
        {
            // Null filename & no filename check
            try
            {
                if (Path.GetFileName(filename).Length == 0)
                    return false;

            }
            catch (ArgumentException)
            {
                return false;
            }

            // Check filename isn't empty
            if (filename.Trim().Length == 0)
            {
                return false;
            }

            // Check the filename isn't breaking any rules (contains invalid characters)
            if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
            {
                return false;
            }

            // Check the target file is an XSD (ends with '.xsd').
            if (!XSDRegex().IsMatch(filename))
                return false;

            // All checks are clear!
            return true;
        }

        [GeneratedRegex("(\\.xsd)$")]
        private static partial Regex XSDRegex();
    }
}