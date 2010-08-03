using System;
using System.Collections.Generic;

namespace myWorkSafe.Groups
{
	public class FileGroupFromIniSection :FileGroup
	{

		public FileGroupFromIniSection()
		{
		}

		public override string RootFolder
		{
			get; set;
		}

		public void AddExcludeFilePattern(string pattern)
		{
		}
		public void AddExcludeFolder(string pattern)
		{
			Filter.SubdirectoryExcludes.Add(pattern.Trim());
		}
		public IEnumerable<string> ExcludeFolderPatterns
		{
			set
			{
				foreach (var pattern in value)
				{
					Filter.FileNameExcludes.Add(pattern);
				}
			}
		}


	}
}