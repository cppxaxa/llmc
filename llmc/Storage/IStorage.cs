using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Storage;

public interface IStorage
{
    IStorage Clone();
    IStorage ApplyConfiguration(StorageConfiguration configuration);
    void DeleteDirectory(string path, bool recursive);
    bool Exists(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    void CreateDirectory(string path);
    void Move(string sourcePath, string destinationPath);
    void MoveDirectory(string sourcePath, string destinationPath);
    void CopyDirectory(string sourcePath, string destinationPath);
    void WriteAllText(string path, string content);
    string ReadAllText(string path);
    void WriteAllLines(string path, IEnumerable<string> lines);
    string[] ReadAllLines(string path);
    void AppendAllLines(string path, IEnumerable<string> lines);
    string ReadAllText(string path, Encoding encoding);
    string[] GetFiles(string path, string wildcard);
    string[] GetFiles(string path);
    IEnumerable<string> EnumerateFiles(string inputFolder, string wildcard);
    IEnumerable<string> EnumerateFiles(string templateFolder);
    void Delete(string redactedFile);
}
