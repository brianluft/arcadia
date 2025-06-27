namespace ComputerUse;

public class StorageFolder
{
    private readonly string _path;
    private readonly object _lock = new object();
    private int _counter = 0;

    public StorageFolder(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public FileInfo GenerateFilename(string extension)
    {
        if (extension == null)
            throw new ArgumentNullException(nameof(extension));

        if (!extension.StartsWith("."))
            extension = "." + extension;

        int counter;
        lock (_lock)
        {
            counter = ++_counter;
        }

        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string filename = $"{timestamp}_{counter}{extension}";
        string fullPath = Path.Combine(_path, filename);

        return new FileInfo(fullPath);
    }
}
