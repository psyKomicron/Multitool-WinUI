using Multitool.FileSystem;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultitoolWinUI.Models
{
    public class TestEntry : IFileSystemEntry
    {
        public FileAttributes Attributes => FileAttributes.Normal;

        public FileSystemInfo Info => throw new NotImplementedException();

        public bool IsCompressed => true;

        public bool IsDevice => true;

        public bool IsDirectory => true;

        public bool IsEncrypted => true;

        public bool IsHidden => true;

        public bool IsReadOnly => true;

        public bool IsSystem => true;

        public string Name => "Test file";

        public bool Partial => false;

        public string Path => "Test path";

        public long Size => 0;

        public event EntryAttributesChangedEventHandler AttributesChanged;
        public event EntryChangedEventHandler Deleted;
        public event EntryRenamedEventHandler Renamed;
        public event EntrySizeChangedEventHandler SizedChanged;

        public int CompareTo(object obj)
        {
            return 0;
        }

        public int CompareTo(IFileSystemEntry other)
        {
            return 0;
        }

        public void CopyTo(string newPath)
        {
            
        }

        public void Delete()
        {
            
        }

        public bool Equals(IFileSystemEntry other)
        {
            return false;
        }

        public void Move(string newPath)
        {
            
        }

        public void Rename(string newName)
        {
            
        }
    }
}
