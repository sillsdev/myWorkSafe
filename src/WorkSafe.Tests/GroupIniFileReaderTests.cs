using System.Collections.Generic;
using myWorkSafe;
using myWorkSafe.Groups;
using NUnit.Framework;
using Palaso.IO;
using Palaso.TestUtilities;
using System.IO;
using System.Linq;

namespace WorkSafe.Tests
{
	[TestFixture]
	public class GroupIniFileReaderTests
	{
		[NUnit.Framework.SetUp]
		public void Setup()
		{
			
		}

		[Test]
		public void CreateGroups_GroupsInDistFiles_CanReadAll()
		{
			var path = FileLocator.GetFileDistributedWithApplication("distfiles", "groups.ini");
			var reader = new GroupIniFileReader(path);
			var groups = reader.CreateGroups().ToArray();
			Assert.That(groups.Count(), Is.GreaterThan(5));
		}

		[Test]
		public void CreateGroups_ExplicitName_OverridesSection()
		{
			using (var f = FileFromContents(
							@"
[James]
name= Jim #nickname
			              	"))
			{
				var reader = new GroupIniFileReader(f.Path);
				var groups = reader.CreateGroups().ToArray();
				Assert.That(groups[0].Name, Is.EqualTo("Jim"));
			}
		}

		[Test]
		public void CreateGroups_LiteralRootFolder_Stored()
		{
			using (var f = FileFromContents(
							@"
[James]
rootFolder= c:\foo\bar #blah"))
			{
				var reader = new GroupIniFileReader(f.Path);
				var groups = reader.CreateGroups().ToArray();
				Assert.That(groups[0].RootFolder, Is.EqualTo(@"c:\foo\bar"));
			}
		}

		[Test]
		public void CreateGroups_MyDocumentsBasedRootFolder_Converted()
		{
			using (var f = FileFromContents(
							@"[James]
rootFolder= $MyDocuments$"))
			{
				var reader = new GroupIniFileReader(f.Path);
				var groups = reader.CreateGroups().ToArray();
				Assert.IsFalse(groups[0].RootFolder.Contains("$")); 
				Assert.IsTrue(Directory.Exists(groups[0].RootFolder));
			}
		}


		[Test]
		public void CreateGroups_RegistryKey_Converted()
		{
			using (var f = FileFromContents(
							@"[ARegistryExample]
rootFolder= HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework,InstallRoot"))
			{
				var reader = new GroupIniFileReader(f.Path);
				var groups = reader.CreateGroups().ToArray();
				Assert.IsFalse(groups[0].RootFolder.Contains("$"));
				Assert.IsTrue(Directory.Exists(groups[0].RootFolder));
			}
		}
		
		
		[Test]
		public void CreateGroups_MultipleFileExcludes_GetsAll()
		{
			using (var f = FileFromContents(
			              	@"
[first]
excludeFile=*.one
excludeFile=*.two
			              	"))
			{
				var reader = new GroupIniFileReader(f.Path);
				var groups = reader.CreateGroups().ToArray();
				Assert.That(groups.Count(), Is.EqualTo(1));
				Assert.That(groups[0].Filter.FileNameExcludes.Count(), Is.EqualTo(2));
			}
		}

		[Test]
		public void CreateGroups_MultipleFolderExcludes_GetsAll()
		{
			using (var f = FileFromContents(
							@"
[first]
excludeFolder=one folder #with a comment
#another comment
excludeFolder=  second folder # a last comment
			              	"))
			{
				var reader = new GroupIniFileReader(f.Path);
				var groups = reader.CreateGroups().ToArray();
				Assert.That(groups.Count(), Is.EqualTo(1));
				Assert.That(groups[0].Filter.SubdirectoryExcludes.Count(), Is.EqualTo(2));
				Assert.That(groups[0].Filter.SubdirectoryExcludes.ToArray()[1], Is.EqualTo("second folder"));
			}
		}

		private TempFile FileFromContents(string contents)
		{
			var f = new TempFile();
			File.WriteAllText(f.Path, contents);
			return f;
		}
	}
}
