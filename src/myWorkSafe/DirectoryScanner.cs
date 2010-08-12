using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace myWorkSafe
{
	public class DirectoryScanner
	{
		public IEnumerable<FileOrDirectory> Scan(string rootPath)
		{
			yield return new FileOrDirectory() {Type = FileOrDirectory.FileOrDirectoryType.Directory, Path = rootPath};

			foreach (var directory in Directory.GetDirectories(rootPath))
			{
				foreach(var item in Scan(directory))
				{
					yield return item; 
				}
			}
			foreach (var file in Directory.GetFiles(rootPath))
			{
				yield return new FileOrDirectory() {Type = FileOrDirectory.FileOrDirectoryType.File, Path = file};
			}
		}
	}

	public struct FileOrDirectory
	{
		public string Path;
		public enum FileOrDirectoryType {File, Directory}

		public FileOrDirectoryType Type;

	}
}
