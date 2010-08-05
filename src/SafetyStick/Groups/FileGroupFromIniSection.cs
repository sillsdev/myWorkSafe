using System;
using System.Collections.Generic;

namespace myWorkSafe.Groups
{
	public class FileGroupFromIniSection :FileGroup
	{

		public FileGroupFromIniSection()
		{
		}


		public void AddExcludeFilePattern(string pattern)
		{
			Filter.FileNameExcludes.Add(pattern.Trim());
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