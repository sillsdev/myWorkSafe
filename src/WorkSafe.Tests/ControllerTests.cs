using System.Collections.Generic;
using myWorkSafe;
using myWorkSafe.Groups;
using NUnit.Framework;
using Palaso.TestUtilities;
using System.IO;

namespace WorkSafe.Tests
{
	[TestFixture]
	public class ControllerTests
	{
		[TestAttribute]
		public void Preview_EmptyDest_SingleFileWillBeCopied()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("test1.txt"),"Blah blah");
				System.IO.File.WriteAllText(from.Combine("test2.txt"), "Blah blah blah");
				var source = new RawDirectoryGroup("1",from.Path,null,null);
				var groups = new List<FileGroup>(new[] {source});        
				var sync = new MirrorController(to.Path, groups, 100, new NullProgress());

				sync.GatherPreview();
				Assert.AreEqual(0, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
				Assert.AreEqual(2, source.NewFileCount);
				Assert.AreEqual(23, source.NetChangeInBytes);
			}
		}

		[TestAttribute]
		public void Preview_FileExistsButHasChanged_WillBeReplaced()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				System.IO.File.WriteAllText(from.Combine("test2.txt"), "dee dee dee");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				var groups = new List<FileGroup>(new[] { source });
				var sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();
				sync.DoSynchronization();
				System.IO.File.WriteAllText(from.Combine("test1.txt"), "Blah blah Blah Blah Blah");
				sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();

				Assert.AreEqual(1, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
				Assert.AreEqual(0, source.NewFileCount);
				Assert.AreEqual(15, source.NetChangeInBytes);
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
		public void Preview_FileIncludedInPreviousGroup_WontBeCountedTwice()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				var source1 = new RawDirectoryGroup("1",from.Path,null,null);
				var source2 = new RawDirectoryGroup("2",from.Path,null,null);
				var groups = new List<FileGroup>(new[] { source1, source2 });
				var progress = new StringBuilderProgress(){ShowVerbose=true};
				var sync = new MirrorController(to.Path, groups, 100, progress);
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
		public void Preview_FolderExcluded_WillBeSkipped()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				Directory.CreateDirectory(from.Combine("sub"));
				File.WriteAllText(from.Combine("sub", "one.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				source.Filter.SubdirectoryExcludes.Add("sub");

				var groups = new List<FileGroup>(new[] { source});
				var progress = new StringBuilderProgress() { ShowVerbose = true };
				var sync = new MirrorController(to.Path, groups, 100, progress);
				sync.GatherPreview();

			// we don't get this progress yet	Assert.That(progress.Text.ToLower().Contains("skip"));
				Assert.AreEqual(0, source.NewFileCount);
				Assert.AreEqual(0, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
			}
		}
		[TestAttribute]
		public void Preview_FilteredFile_WontBeCounted()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("text.txt"), "Blah blah");
				File.WriteAllText(from.Combine("info.info"), "deedeedee");
				var source1 = new RawDirectoryGroup("1", from.Path, new []{"*.info"}, null);
				var groups = new List<FileGroup>(new[] { source1 });
				var sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();

				Assert.AreEqual(1, source1.NewFileCount);
				Assert.AreEqual(0, source1.UpdateFileCount);
				Assert.AreEqual(0, source1.DeleteFileCount);
				Assert.AreEqual(9, source1.NetChangeInBytes);
			}
		}
		[TestAttribute]
		public void Run_EmptyDest_SingleFileIsBeCopied()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("1.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				var groups = new List<FileGroup>(new[] { source });
				var progress = new StringBuilderProgress() {ShowVerbose = true};

				var sync = new MirrorController(to.Path, groups, 100, progress);

				sync.DoSynchronization();
				AssertFileExists(sync, source, to, "1.txt");
			}
		}

		[TestAttribute]
		public void Run_FileLocked_OtherFileCopied()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				System.IO.File.WriteAllText(from.Combine("test2.txt"), "Blah blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				var groups = new List<FileGroup>(new[] { source });
				var progress = new StringBuilderProgress(){ShowVerbose=true};
				var sync = new MirrorController(to.Path, groups, 100, progress);

				using(File.OpenWrite(from.Combine("test2.txt")))//lock it up
				{
					sync.GatherPreview();
					sync.DoSynchronization();
				}
				AssertFileExists(sync, source, to, "test1.txt");
				AssertFileDoesNotExist(sync, source, to, "test2.txt");
				Assert.That(progress.ErrorEncountered);
			}
		}

		[Test]
		public void Run_GroupHasDoDeletePolicy_DeletionIsPropagated()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null) {NormallyPropogateDeletions = true};
				var groups = new List<FileGroup>(new[] { source });
				var sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.DoSynchronization();

				//should be there at the destination
				AssertFileExists(sync, source, to, "test1.txt");
				File.Delete(from.Combine("test1.txt"));

				File.WriteAllText(from.Combine("test2.txt"), "Blah blah");
				sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.DoSynchronization();

				AssertFileDoesNotExist(sync, source, to, "test1.txt");
			}
		}
		
		[Test]
		public void Run_FileRemovedAndGroupHasDefaultDeletePolicy_FileIsDeletedFromDest()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				
				//ensure this is the defualt
				Assert.IsFalse(source.NormallyPropogateDeletions);

				var groups = new List<FileGroup>(new[] { source });
				var sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.DoSynchronization();
				string destFile = to.Combine(sync.DestinationRootForThisUser, source.Name, "test1.txt");
				Assert.IsTrue(File.Exists(destFile));
				File.Delete(from.Combine("test1.txt"));

				sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				Assert.IsTrue(File.Exists(destFile));
				sync.DoSynchronization();

				Assert.IsFalse(File.Exists(to.Combine("test1.txt")));
			}
		}

		[Test]
		public void Run_FileInMercurialFolderRemoved_FileGetsDeletedFromDest()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				Directory.CreateDirectory(from.Combine(".hg"));
				File.WriteAllText(from.Combine(".hg","test1.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				//the key here is that even though the group calls for NO deletion,
				//we do it anyways inside of the mercurial folder (.hg)
				source.NormallyPropogateDeletions = false;

				var groups = new List<FileGroup>(new[] { source });
				var sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.DoSynchronization();
				AssertFileExists(sync, source, to, Path.Combine(".hg", "test1.txt"));

				File.Delete(from.Combine(".hg","test1.txt"));
				sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.DoSynchronization();

				AssertFileDoesNotExist(sync,source,to, Path.Combine(".hg", "test1.txt"));
			}
		}


		[TestAttribute]
		public void Run_FolderExcluded_IsSkipped()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				Directory.CreateDirectory(from.Combine("sub"));
				File.WriteAllText(from.Combine("sub", "one.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				source.Filter.SubdirectoryExcludes.Add("sub");

				var groups = new List<FileGroup>(new[] { source });
				var progress = new StringBuilderProgress() { ShowVerbose = true };
				var sync = new MirrorController(to.Path, groups, 100, progress);
				sync.DoSynchronization();

				// we don't get this progress yet Assert.That(progress.Text.ToLower().Contains("skip"));
				Assert.AreEqual(0, source.NewFileCount);
				Assert.AreEqual(0, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
			}
		}


		private void AssertFileDoesNotExist(MirrorController sync, RawDirectoryGroup source, TemporaryFolder destFolder, string fileName)
		{
			string path = destFolder.Combine(sync.DestinationRootForThisUser, source.Name, fileName);
			Assert.IsFalse(File.Exists(path), path);
		}
		private void AssertFileExists(MirrorController sync, RawDirectoryGroup source, TemporaryFolder destFolder, string fileName)
		{
			string path = destFolder.Combine(sync.DestinationRootForThisUser, source.Name, fileName);
			Assert.IsTrue(File.Exists(path), path);
		}

	}
}
