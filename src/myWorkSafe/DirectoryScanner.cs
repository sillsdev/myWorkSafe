using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace myWorkSafe
{
	public class DirectoryScanner
	{      
        public event EventHandler<ScanningErrorArgs> OnError;

		public IEnumerable<FileOrDirectory> Scan(string rootPath)
		{
			yield return new FileOrDirectory() { Type = FileOrDirectory.FileOrDirectoryType.Directory, Path = rootPath };

			string[] directories = new string[0];
			try
			{
                directories = SafeIO.Directory.GetDirectories(rootPath);
			}
			catch (Exception error)
			{
				if (OnError != null)
					OnError.Invoke(this, new ScanningErrorArgs(error, rootPath));
			}

			foreach (var directory in directories)
			{
				foreach (var item in Scan(directory))
				{
					yield return item;
				}
			}

			string[] files = new string[0];
			try
			{
                files = SafeIO.Directory.GetFiles(rootPath);
			}
			catch (Exception error)
			{
				if (OnError != null)
					OnError.Invoke(this, new ScanningErrorArgs(error, rootPath));
			}

			foreach (var file in files)
			{
				yield return new FileOrDirectory() { Type = FileOrDirectory.FileOrDirectoryType.File, Path = file };
			}
		}
	}
		public class ScanningErrorArgs : EventArgs
		{
			public readonly Exception Error;
			public readonly string Path;

			public ScanningErrorArgs(Exception error, string path)
			{
				Error = error;
				Path = path;
			}
		}

		public class FileOrDirectory
		{
			public string Path;
			public enum FileOrDirectoryType { File, Directory }

			public FileOrDirectoryType Type;

		}
	}

