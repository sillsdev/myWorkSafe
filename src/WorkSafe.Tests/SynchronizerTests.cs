using System.Collections.Generic;
using myWorkSafe;
using myWorkSafe.Groups;
using NUnit.Framework;
using Palaso.TestUtilities;
using System.IO;

namespace WorkSafe.Tests
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
				var source = new RawDirectoryGroup("1",from.Path,null,null);
				var groups = new List<FileGroup>(new[] {source});        
				var sync = new Synchronizer(to.Path, groups, 100, new NullProgress());

				sync.GatherPreview();
				Assert.AreEqual(0, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
				Assert.AreEqual(2, source.NewFileCount);
				Assert.AreEqual(23, source.NetChangeInBytes);
			}
		}

		[TestAttribute]
		public void Syncronhize_FileLocked_OtherFileCopied()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				//System.IO.File.WriteAllText(from.Combine("test2.txt"), "Blah blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				var groups = new List<FileGroup>(new[] { source });
				var sync = new Synchronizer(to.Path, groups, 100, new NullProgress());

				string path = to.Combine(sync.DestinationRootForThisUser, source.Name, "test2.txt");
				using(var locked = File.OpenWrite(path))
				{
					sync.GatherPreview();
					sync.DoSynchronization();
				}
				AssertFileExists(sync, source, to, "test1.txt");
				//interestingly, the lock actually doesn't keep the sync framework from copying it
				//AssertFileDoesNotExist(sync, source, to, "test2.txt");
			}
		}

		private void AssertFileDoesNotExist(Synchronizer sync, RawDirectoryGroup source, TemporaryFolder destFolder, string fileName)
		{
			string path = destFolder.Combine(sync.DestinationRootForThisUser, source.Name, fileName);
			Assert.IsFalse(File.Exists(path), path);
		}
		private void AssertFileExists(Synchronizer sync, RawDirectoryGroup source, TemporaryFolder destFolder, string fileName)
		{
			string path = destFolder.Combine(sync.DestinationRootForThisUser, source.Name, fileName);
			Assert.IsTrue(File.Exists(path), path);
		}

		[TestAttribute]
		public void GetInfo_FileExistsButHasChanged_WillBeReplaced()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				System.IO.File.WriteAllText(from.Combine("test2.txt"), "dee dee dee");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				var groups = new List<FileGroup>(new[] { source });
				var sync = new Synchronizer(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();
				sync.DoSynchronization();
				System.IO.File.WriteAllText(from.Combine("test1.txt"), "Blah blah Blah Blah Blah");
				sync = new Synchronizer(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();

				Assert.AreEqual(1, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
				Assert.AreEqual(0, source.NewFileCount);
				Assert.AreEqual(15, source.NetChangeInBytes);
			}
		}

		[TestAttribute, Ignore("Deletion works, but is not part of the preview")]
		public void GetInfo_FileRemoved_WillBeDeleted()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				var groups = new List<FileGroup>(new[] { source });
				var sync = new Synchronizer(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();
				sync.DoSynchronization();
				File.Delete(from.Combine("test1.txt"));
				
				//simulate a new run (which will have a new metadata file, else the framework
				//decides not to delete.
				sync = new Synchronizer(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();

				Assert.AreEqual(0, source.NewFileCount);
				Assert.AreEqual(0, source.UpdateFileCount);
				Assert.AreEqual(1, source.DeleteFileCount);
				Assert.AreEqual(-9, source.NetChangeInBytes);
			}
		}


		[Test]
		public void DoSynchronization_FileRemoved_FileGetsDeletedFromDest()
		{

			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				var groups = new List<FileGroup>(new[] { source });
				var sync = new Synchronizer(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();
				sync.DoSynchronization();
				string destFile = to.Combine(sync.DestinationRootForThisUser, source.Name, "test1.txt");
				Assert.IsTrue(File.Exists(destFile));
				File.Delete(from.Combine("test1.txt"));


				sync = new Synchronizer(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();
				Assert.IsTrue(File.Exists(destFile));
				sync.DoSynchronization();

				Assert.IsFalse(File.Exists(to.Combine("test1.txt")));
			}
		}

		/// <summary>
		/// NB: the result of this logic is that a file which is first selected by a "wesay" group,
		/// and if found in documents/wesay, won't
		/// appear in the backup under "documents" folder produced by a subsequent "all documents" group.
		/// If the order of the two groups is reversed, then the whole "wesay" group could be empty, as
		/// all the files were already accounted for.
		/// </summary>
		[TestAttribute]
		public void GetInfo_FileIncludedInPreviousGroup_WontBeCountedTwice()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				var source1 = new RawDirectoryGroup("1",from.Path,null,null);
				var source2 = new RawDirectoryGroup("2",from.Path,null,null);
				var groups = new List<FileGroup>(new[] { source1, source2 });
				var sync = new Synchronizer(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();

				Assert.AreEqual(1, source1.NewFileCount);
				Assert.AreEqual(0, source1.UpdateFileCount);
				Assert.AreEqual(0, source1.DeleteFileCount);
				Assert.AreEqual(9, source1.NetChangeInBytes);

				Assert.AreEqual(0, source2.NewFileCount);
				Assert.AreEqual(0, source2.UpdateFileCount);
				Assert.AreEqual(0, source2.DeleteFileCount);
				Assert.AreEqual(0, source2.NetChangeInBytes);
			}
		}

		[TestAttribute]
		public void GetInfo_FilteredFile_WontBeCounted()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("text.txt"), "Blah blah");
				File.WriteAllText(from.Combine("info.info"), "deedeedee");
				var source1 = new RawDirectoryGroup("1", from.Path, new []{"*.info"}, null);
				var groups = new List<FileGroup>(new[] { source1 });
				var sync = new Synchronizer(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();

				Assert.AreEqual(1, source1.NewFileCount);
				Assert.AreEqual(0, source1.UpdateFileCount);
				Assert.AreEqual(0, source1.DeleteFileCount);
				Assert.AreEqual(9, source1.NetChangeInBytes);
			}
		}
	}
}
