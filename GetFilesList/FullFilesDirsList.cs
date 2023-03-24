

using System.Data;

namespace GetFilesList
{
    public class FullFilesDirsList
    {
        /// <summary> 
        /// Get the <see cref="List"/> of <see cref="FileInfo"/> that belong to this instance.
        /// </summary>
        public List<FileInfo> Files { get; private set; } = new List<FileInfo>();

        /// <summary> 
        /// Get the <see cref="List"/> of <see cref="DirectoryInfo"/> that belong to this instance.
        /// </summary>

        public List<DirectoryInfo> Folders { get; private set; } = new List<DirectoryInfo>();
        
        /// <summary> 
        /// Get the List of tuples which consisting of <see cref="DirectoryInfo"/> and <see cref="Exception"/> that occurred when trying to access it.
        /// </summary>
        public List<(DirectoryInfo, Exception)> NotAccessedFolders { get; private set; } = new List<(DirectoryInfo, Exception)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FullFilesDirsList"/> class.
        /// </summary>
        public FullFilesDirsList()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FullFilesDirsList"/> class.
        /// </summary>
        public FullFilesDirsList Clone()
        {
            return new FullFilesDirsList();
        }

        /// <summary>
        /// Clear all properties of current instance.
        /// </summary>
        public void Clear()
        {
            Files.Clear();
            Folders.Clear();
            NotAccessedFolders.Clear();
        }

        /// <summary>
        /// Recursively walk through all folders and fill properties.
        /// </summary>
        /// <param name="dirPath">Directory from which files and folders are taken.</param>
        /// <param name="searchPattern">The search string to match against the names of files.</param>
        public void GetFilesDirsList(string dirPath, string searchPattern)
        {
            DirectoryInfo dirInfo = new(dirPath);
            GetFilesDirsList(dirInfo, searchPattern);
        }

        /// <summary>
        /// Recursively walk through all folders and fill properties.
        /// </summary>
        /// <param name="dirInfo">Directory from which files and folders are taken.</param>
        /// <param name="searchPattern">The search string to match against the names of files.</param>
        public void GetFilesDirsList(DirectoryInfo dirInfo, string searchPattern)
        {
            Folders.Add(dirInfo);
            try
            {
                foreach (FileInfo f in dirInfo.GetFiles(searchPattern))
                {
                    Files.Add(f);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Directory {0}  \n could not be accessed!!!!", dirInfo.FullName);
                NotAccessedFolders.Add((dirInfo, e));
                return;
            }

            foreach (DirectoryInfo d in dirInfo.GetDirectories())
            {
                GetFilesDirsList(d, searchPattern);
            }
        }

        /// <summary>
        /// Recursively in parallel walk through all folders.
        /// </summary>
        /// <param name="dirPath">Directory from which files and folders are taken.</param>
        /// <param name="searchPattern">The search string to match against the names of files.</param>
        /// <returns>Returns a new instance of the <see cref="FullFilesDirsList"/> with filled properties.</returns>
        public static FullFilesDirsList GetFilesDirsListParallel(string dirPath, string searchPattern)
        {
            DirectoryInfo dirInfo = new(dirPath);
            return GetFilesDirsListParallel(dirInfo, searchPattern);
        }

        /// <summary>
        /// Recursively in parallel walk through all folders.
        /// </summary>
        /// <param name="dirInfo">Directory from which files and folders are taken.</param>
        /// <param name="searchPattern">The search string to match against the names of files.</param>
        /// <returns>Returns a new instance of the <see cref="FullFilesDirsList"/> with filled properties.</returns>
        public static FullFilesDirsList GetFilesDirsListParallel(DirectoryInfo dirInfo, string searchPattern)
        {
            FullFilesDirsList result = new();
            DirectoryInfo[] subDirs;
            try
            {
                subDirs = dirInfo.GetDirectories();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Directory {0}  \n could not be accessed!!!!", dirInfo.FullName);
                result.Folders.Add(dirInfo);
                result.NotAccessedFolders.Add((dirInfo, ex));
                return result;
            }

            List<Task<FullFilesDirsList>> allTasks = new();
            foreach (DirectoryInfo d in subDirs)
            {
                Task<FullFilesDirsList> oneTask = Task<FullFilesDirsList>.Factory.StartNew(() =>
                {
                    return GetFilesDirsListParallel(d, searchPattern);
                });
                allTasks.Add(oneTask);
            }
            Task.WaitAll(allTasks.ToArray());

            static FullFilesDirsList SelectMany(IEnumerable<FullFilesDirsList> fullFilesDirsList)
            {
                FullFilesDirsList result = new()
                {
                    Folders = fullFilesDirsList.SelectMany(q => q.Folders).ToList(),
                    Files = fullFilesDirsList.SelectMany(q => q.Files).ToList(),
                    NotAccessedFolders = fullFilesDirsList.SelectMany(q => q.NotAccessedFolders).ToList()
                };
                return result;
            }

            result = SelectMany(allTasks.Select(t => t.Result));

            result.Folders.Add(dirInfo);
            result.Files.AddRange(dirInfo.GetFiles(searchPattern));

            return result;
        }
    }
}