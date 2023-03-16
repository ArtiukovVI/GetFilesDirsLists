

namespace GetFilesList
{
    public class FullFilesDirsList
    {
        public List<FileInfo> Files { get; private set; } = new List<FileInfo>();
        public List<DirectoryInfo> Folders { get; private set; } = new List<DirectoryInfo>();
        public List<(DirectoryInfo, Exception)> NotAccessedFolders { get; private set; } = new List<(DirectoryInfo, Exception)>();

        public static FullFilesDirsList Clone()
        {
            return new FullFilesDirsList();
        }

        public void GetFilesDirsList(string dirPath, string searchPattern)
        {
            DirectoryInfo dirInfo = new(dirPath);
            GetFilesDirsList(dirInfo, searchPattern);
        }

        public void GetFilesDirsList(DirectoryInfo dirInfo, string searchPattern)
        {
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
                Folders.Add(d);
                GetFilesDirsList(d, searchPattern);
            }
        }

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

            result.Folders.AddRange(subDirs);
            result.Files.AddRange(dirInfo.GetFiles(searchPattern));

            return result;
        }
    }
}