using llmc.Project;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Storage;

/// <summary>
/// TODO Bug: If we move a directory/ copy a directory, and given some files exists in the memory,
/// the extra existing files won't get deleted. Overwrite and create will work fine.
/// </summary>
public class SwitchableStorage(
    StorageConfiguration storageConfiguration,
    Dictionary<string, string>? inMemoryParam = null) : IStorage
{
    private readonly Dictionary<string, string> inMemory = inMemoryParam ?? [];

    private static string GetPath(string path)
    {
        return Path.GetFullPath(path);
    }

    public IStorage Clone()
    {
        return new SwitchableStorage(storageConfiguration, inMemory);
    }

    public IStorage ApplyConfiguration(StorageConfiguration configuration)
    {
        storageConfiguration = configuration;
        return this;
    }

    public void AppendAllText(string path, string content)
    {
        path = GetPath(path);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            if (!inMemory.ContainsKey(path))
            {
                inMemory[path] = string.Empty;
            }

            inMemory[path] = inMemory[path] + content;
        }
        else
        {
            File.AppendAllText(path, content);
        }
    }

    public void AppendAllLines(string path, IEnumerable<string> lines)
    {
        path = GetPath(path);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            string joined = string.Join(Environment.NewLine, lines);

            if (!inMemory.ContainsKey(path))
            {
                inMemory[path] = string.Empty;
            }

            inMemory[path] = inMemory[path] + joined;
        }
        else
        {
            File.AppendAllLines(path, lines);
        }
    }

    public void CreateDirectory(string path)
    {
        path = GetPath(path);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            // Do nothing
        }
        else
        {
            Directory.CreateDirectory(path);
        }
    }

    public void Delete(string path)
    {
        path = GetPath(path);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            inMemory.Remove(path);
        }
        else
        {
            File.Delete(path);
        }
    }

    public void DeleteDirectory(string path, bool recursive)
    {
        path = GetPath(path);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            foreach (var key in inMemory.Keys.Where(k => k.StartsWith(path)).ToArray())
            {
                inMemory.Remove(key);
            }
        }
        else
        {
            Directory.Delete(path, recursive);
        }
    }

    public IEnumerable<string> EnumerateFiles(
        string path, string wildcard, SearchOption searchOption)
    {
        path = GetPath(path);
        char pathSeparator = GetPathSeparator(path);

        IEnumerable<string> localFiles = [];

        if (Path.Exists(path))
        {
            localFiles = Directory.EnumerateFiles(path, wildcard, searchOption);
        }

        if (storageConfiguration.EnableInMemoryStorage)
        {
            if (searchOption == SearchOption.AllDirectories)
            {
                return inMemory.Keys.Where(k => k.StartsWith(path + pathSeparator) &&
                    Common.IsWildcardMatch(Path.GetFileName(k), wildcard))
                    .Union(localFiles).ToList();
            }
            else if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return inMemory.Keys.Where(k =>
                    k == Path.Join(path, Path.GetFileName(k)) &&
                    Common.IsWildcardMatch(Path.GetFileName(k), wildcard))
                    .Union(localFiles).ToList();
            }
            else
            {
                throw new NotImplementedException("SwitchableStorage:EnumerateFiles:SearchOption");
            }
        }
        else
        {
            return localFiles;
        }
    }

    private static char GetPathSeparator(string path)
    {
        string dummyDirectoryName = "dummy";
        string p = GetPath(Path.Join(path, dummyDirectoryName));
        return p.Substring(path.Length)[0];
    }

    public IEnumerable<string> EnumerateFiles(string path, string wildcard)
    {
        path = GetPath(path);

        IEnumerable<string> localFiles = [];

        if (Path.Exists(path))
        {
            localFiles = Directory.EnumerateFiles(path, wildcard);
        }

        if (storageConfiguration.EnableInMemoryStorage)
        {
            return inMemory.Keys.Where(k =>
                k == Path.Join(path, Path.GetFileName(k)) &&
                Common.IsWildcardMatch(Path.GetFileName(k), wildcard)).Union(localFiles)
                .ToList();
        }
        else
        {
            return localFiles;
        }
    }

    public IEnumerable<string> EnumerateFiles(string path)
    {
        path = GetPath(path);

        IEnumerable<string> localFiles = [];

        if (Path.Exists(path))
        {
            localFiles = Directory.EnumerateFiles(path);
        }

        if (storageConfiguration.EnableInMemoryStorage)
        {
            return inMemory.Keys.Where(k =>
                k == Path.Join(path, Path.GetFileName(k))).Union(localFiles)
                .ToList();
        }
        else
        {
            return localFiles;
        }
    }

    public bool Exists(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        path = GetPath(path);

        bool localPathExists = File.Exists(path) || Directory.Exists(path);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            return inMemory.ContainsKey(path) || localPathExists;
        }
        else
        {
            return localPathExists;
        }
    }

    public string[] GetFiles(string path, string wildcard)
    {
        path = GetPath(path);

        string[] localFiles = [];

        if (Path.Exists(path))
        {
            localFiles = Directory.GetFiles(path, wildcard);
        }

        if (storageConfiguration.EnableInMemoryStorage)
        {
            return inMemory.Keys
                .Where(k => k == Path.Join(path, Path.GetFileName(k)) &&
                    Common.IsWildcardMatch(Path.GetFileName(k), wildcard))
                .Union(localFiles).ToArray();
        }
        else
        {
            return localFiles;
        }
    }

    public string[] GetFiles(string path)
    {
        path = GetPath(path);

        string[] localFiles = [];

        if (Path.Exists(path))
        {
            localFiles = Directory.GetFiles(path);
        }

        if (storageConfiguration.EnableInMemoryStorage)
        {
            return inMemory.Keys.Where(k =>
                k == Path.Join(path, Path.GetFileName(k))).Union(localFiles).ToArray();
        }
        else
        {
            return localFiles;
        }
    }

    public void Move(string sourcePath, string destinationPath)
    {
        sourcePath = GetPath(sourcePath);
        destinationPath = GetPath(destinationPath);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            if (inMemory.ContainsKey(sourcePath))
            {
                inMemory[destinationPath] = inMemory[sourcePath];
                inMemory.Remove(sourcePath);
            }
            else
            {
                inMemory[destinationPath] = File.ReadAllText(sourcePath);
            }
        }
        else
        {
            File.Move(sourcePath, destinationPath);
        }
    }

    public void MoveDirectory(string sourcePath, string destinationPath)
    {
        sourcePath = GetPath(sourcePath);
        destinationPath = GetPath(destinationPath);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            foreach (var key in inMemory.Keys.Where(k => k.StartsWith(sourcePath)).ToArray())
            {
                string newKey = key.Replace(sourcePath, destinationPath);
                inMemory[newKey] = inMemory[key];
                inMemory.Remove(key);
            }

            // Put any missing files from local source.
            foreach (var filename in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                if (!inMemory.ContainsKey(filename))
                {
                    inMemory[filename.Replace(sourcePath, destinationPath)] = File
                        .ReadAllText(filename);
                }
            }
        }
        else
        {
            Directory.Move(sourcePath, destinationPath);
        }
    }

    public string[] ReadAllLines(string path)
    {
        path = GetPath(path);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            if (inMemory.ContainsKey(path))
            {
                string separator = Common.FindLineSeparator(inMemory[path]);
                return inMemory[path].Split(separator);
            }
            else
            {
                return File.ReadAllLines(path);
            }
        }
        else
        {
            return File.ReadAllLines(path);
        }
    }

    public string ReadAllText(string path)
    {
        path = GetPath(path);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            if (inMemory.ContainsKey(path))
            {
                return inMemory[path];
            }
            else
            {
                return File.ReadAllText(path);
            }
        }
        else
        {
            return File.ReadAllText(path);
        }
    }

    public string ReadAllText(string path, Encoding encoding)
    {
        path = GetPath(path);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            if (inMemory.ContainsKey(path))
            {
                return encoding.GetString(Encoding.UTF8.GetBytes(inMemory[path]));
            }
            else
            {
                return File.ReadAllText(path, encoding);
            }
        }
        else
        {
            return File.ReadAllText(path, encoding);
        }
    }

    public void WriteAllLines(string path, IEnumerable<string> lines)
    {
        path = GetPath(path);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            inMemory[path] = string.Join(Environment.NewLine, lines);
        }
        else
        {
            File.WriteAllLines(path, lines);
        }
    }

    public void WriteAllText(string path, string content)
    {
        path = GetPath(path);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            inMemory[path] = content;
        }
        else
        {
            File.WriteAllText(path, content);
        }
    }

    public void CopyDirectory(string sourcePath, string destinationPath)
    {
        sourcePath = GetPath(sourcePath);
        destinationPath = GetPath(destinationPath);

        if (storageConfiguration.EnableInMemoryStorage)
        {
            foreach (var key in inMemory.Keys.Where(k => k.StartsWith(sourcePath)).ToArray())
            {
                string newKey = key.Replace(sourcePath, destinationPath);
                inMemory[newKey] = inMemory[key];
            }

            // Put any missing files from local source.
            foreach (var filename in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                string newKey = filename.Replace(sourcePath, destinationPath);
                
                if (!inMemory.ContainsKey(newKey))
                {
                    inMemory[newKey] = File.ReadAllText(filename);
                }
            }
        }
        else
        {
            // If windows use robocopy, otherwise use rsync.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "robocopy",
                    Arguments = $"\"{sourcePath}\" \"{destinationPath}\" /E /Z /R:5 /W:5",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process proc = Process.Start(psi)!;
                proc.WaitForExit();
            }
            else
            {
                // Untested.
                ProcessStartInfo psi = new()
                {
                    FileName = "rsync",
                    Arguments = $"-a \"{sourcePath}\" \"{destinationPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process proc = Process.Start(psi)!;
                proc.WaitForExit();
            }
        }
    }
}
