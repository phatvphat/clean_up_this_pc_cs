namespace clean_up_this_pc_cs
{
    internal static class Program
    {
        private sealed record FolderInfo(string Name, string Path, Version Version);

        private static readonly string AppDataLocal = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Local");

        private static readonly List<string> BrowserDirs = new()
        {
            "Google\\Chrome",
            "CocCoc\\Browser"
        };

        private static readonly List<string> AppAsarDirs = new()
        {
            "Discord",
            "DiscordPTB",
            "GithubDesktop",
            "MongoDBCompass",
            "Postman"
        };

        private static readonly List<string> DirsToDelete = new()
        {
            "opgg-electron-app-updater"
        };

        private static readonly string CapcutDir = Path.Combine(AppDataLocal, "CapCut\\Apps");

        private static void CleanBrowserDirectories()
        {
            foreach (string browserDir in BrowserDirs)
            {
                string fullPath = Path.Combine("C:\\Program Files", browserDir, "Application");
                CleanVersionedDirectory(fullPath, browserDir, delete7zFiles: true);
            }
        }

        private static void CleanAppAsarDirectories()
        {
            foreach (string appDir in AppAsarDirs)
            {
                string fullPath = Path.Combine(AppDataLocal, appDir);
                CleanVersionedDirectory(fullPath, appDir, "app-*", delete7zFiles: false);
            }
        }

        private static void DeleteSpecificDirectories()
        {
            foreach (string dir in DirsToDelete)
            {
                string fullPath = Path.Combine(AppDataLocal, dir);
                DeleteDirectory(fullPath, dir);
            }
        }

        private static void CleanCapcutDirectory()
        {
            CleanVersionedDirectory(CapcutDir, "CapCut", delete7zFiles: false);
        }

        private static void CleanVersionedDirectory(string directoryPath, string dirName, string searchPattern = "*", bool delete7zFiles = false)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"{dirName}: Directory not found at {directoryPath}.");
                return;
            }

            try
            {
                var folders = Directory.GetDirectories(directoryPath, searchPattern)
                    .Select(folder =>
                    {
                        string name = Path.GetFileName(folder);
                        string versionString = name.StartsWith("app-") ? name.Replace("app-", "") : name;
                        try
                        {
                            Version version = Version.Parse(versionString);
                            return new FolderInfo(name, folder, version);
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(folder => folder != null)
                    .ToList();

                if (folders == null || folders.Count == 0)
                {
                    Console.WriteLine($"{dirName}: No versioned folders found.");
                    return;
                }

                var latestFolder = folders.OrderByDescending(f => f?.Version).First();
                if (latestFolder == null) return;
                Console.WriteLine($"{dirName} - Latest version: {latestFolder.Version}, deleting old versions...");

                if (folders.Count < 2)
                {
                    Console.WriteLine($"{dirName}: No old versions to delete.");
                }
                else
                {
                    foreach (var folder in folders)
                    {
                        if (folder != null && folder.Version < latestFolder.Version)
                        {
                            DeleteDirectory(folder.Path, folder.Name);
                        }
                    }
                }

                if (delete7zFiles && latestFolder != null)
                {
                    string installerPath = Path.Combine(latestFolder.Path, "Installer");
                    Delete7zFiles(installerPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {dirName}: {ex.Message}");
            }
        }

        private static void DeleteDirectory(string path, string name)
        {
            try
            {
                Directory.Delete(path, true);
                Console.WriteLine($"{name}: Deleted successfully at {path}.");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"{name}: Cannot delete (please run as Administrator): {path}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{name}: Cannot delete (possibly in use): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{name}: Error deleting directory: {ex.Message}");
            }
        }

        private static void Delete7zFiles(string installerPath)
        {
            if (!Directory.Exists(installerPath))
            {
                return;
            }

            try
            {
                string[] files = Directory.GetFiles(installerPath, "*.7z");
                if (files.Length == 0) return;

                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted: {file}");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine($"Cannot delete (please run as Administrator): {file}");
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Cannot delete (possibly in use): {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing Installer directory: {ex.Message}");
            }
        }

        private static void PrintSeparator()
        {
            Console.WriteLine(new string('-', 40));
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Starting cleanup process...\n");

            // Browser
            CleanBrowserDirectories();
            PrintSeparator();

            // AppAsar
            CleanAppAsarDirectories();
            PrintSeparator();

            // CapCut
            CleanCapcutDirectory();
            PrintSeparator();

            DeleteSpecificDirectories();
            PrintSeparator();

            Console.WriteLine("\nCleanup completed! Press any key to exit.");
            Console.ReadLine();
        }
    }
}
