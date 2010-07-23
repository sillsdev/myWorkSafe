using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Palaso.TestUtilities;
using SafetyStick;
using System.IO;

namespace SafetypeStickTests
{
	[TestFixture]
	public class SynchronizerTests
	{
		[TestAttribute]
		public void GetInfo_EmptyDest_SingleFileWillBeCopied()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("test1.txt"),"Blah blah");
				System.IO.File.WriteAllText(from.Combine("test2.txt"), "Blah blah blah");
				var source = new RawDirectorySource(from.Path);
				var groups = new List<FileSource>(new[] {source});        
				var sync = new Synchronizer(to.Path, groups, 100);

				sync.GatherInformation();
				Assert.AreEqual(0, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
				Assert.AreEqual(2, source.NewFileCount);
				Assert.AreEqual(23, source.NetChangeInBytes);
			}
		}

		[TestAttribute]
		public void GetInfo_FileExistsButHasChanged_WillBeReplaced()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				System.IO.File.WriteAllText(from.Combine("test2.txt"), "dee dee dee");
				var source = new RawDirectorySource(from.Path);
				var groups = new List<FileSource>(new[] { source });
				var sync = new Synchronizer(to.Path, groups, 100);
				sync.GatherInformation();
				sync.DoSynchronization();
				System.IO.File.WriteAllText(from.Combine("test1.txt"), "Blah blah Blah Blah Blah");
				sync.GatherInformation();

				Assert.AreEqual(1, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
				Assert.AreEqual(0, source.NewFileCount);
				Assert.AreEqual(15, source.NetChangeInBytes);
			}
		}

		[TestAttribute, Ignore("Deletion just doesn't work")]
		public void GetInfo_FileRemoved_WillBeDeleted()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				var source = new RawDirectorySource(from.Path);
				var groups = new List<FileSource>(new[] { source });
				var sync = new Synchronizer(to.Path, groups, 100);
				sync.GatherInformation();
				sync.DoSynchronization();
				File.Delete(from.Combine("test1.txt"));
				sync.GatherInformation();

				Assert.AreEqual(0, source.NewFileCount);
				Assert.AreEqual(0, source.UpdateFileCount);
				Assert.AreEqual(1, source.DeleteFileCount);
				Assert.AreEqual(-9, source.NetChangeInBytes);
			}
		}

		[Test, Ignore("Deletion just doesn't work")]
		public void DoSynchronization_FileRemoved_FileGetsDeletedFromDest()
		{

			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				var source = new RawDirectorySource(from.Path);
				var groups = new List<FileSource>(new[] { source });
				var sync = new Synchronizer(to.Path, groups, 100);
				sync.GatherInformation();
				sync.DoSynchronization();
				File.Delete(from.Combine("test1.txt"));
				sync.GatherInformation();
				Assert.IsTrue(File.Exists(to.Combine("test1.txt"))); 
				sync.DoSynchronization();

				Assert.IsFalse(File.Exists(to.Combine("test1.txt")));
			}
		}
	}
}
