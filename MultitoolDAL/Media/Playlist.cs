using Multitool.Data.Win32;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using Windows.Storage;

namespace Multitool.Data.Media
{
    public class Playlist : IList<StorageFile>
    {
        private static readonly Regex audioRegex = CreateAudioRegex();
        private readonly XmlDocument document = new();
        private readonly XmlNode root;
        private readonly List<StorageFile> songs = new();
        private int position;

        public Playlist()
        {
            root = document.CreateElement(nameof(Playlist));
            document.AppendChild(root);
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public bool Loop { get; set; }

        #region IList
        public int Count => songs.Count;
        public bool IsReadOnly => false;

        public StorageFile this[int index]
        {
            get => songs[index];
            set => songs[index] = value;
        }

        public void Add(StorageFile newSong)
        {
        }

        public int IndexOf(StorageFile item) => songs.IndexOf(item);

        public void Insert(int index, StorageFile item)
        {
            songs.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            songs.RemoveAt(index);
        }

        public void Clear()
        {
            songs.Clear();
        }

        public bool Contains(StorageFile item) => songs.Contains(item);

        public void CopyTo(StorageFile[] array, int arrayIndex) => songs.CopyTo(array, arrayIndex);

        public bool Remove(StorageFile item)
        {
            return songs.Remove(item);
        }

        public IEnumerator<StorageFile> GetEnumerator() => songs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        } 
        #endregion

        public async Task Add(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
        }

#nullable enable
        public StorageFile? Next()
        {
            StorageFile? storageFile = null;
            if (position + 1 < songs.Count)
            {
                storageFile = songs[position++];
            }
            else if (Loop)
            {
                position = (position + 1) % songs.Count;
                storageFile = songs[position];
            }
            return storageFile;
        }

        public StorageFile? Back()
        {
            throw new NotImplementedException();
        }
#nullable disable

        private static Regex CreateAudioRegex()
        {
            string[] extensions = RegistryHelper.GetExtensionsForMime(new(@"^audio"));
            StringBuilder sb = new();
            for (int i = 0; i < extensions.Length; i++)
            {
                sb.Append(Regex.Escape(extensions[i])).Append('|');
            }
            return new(sb.ToString());
        }
    }
}
