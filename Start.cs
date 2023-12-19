using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FolderGraph
{
	class Start
	{
		internal static class NativeMethods
		{
			[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
			public static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

			[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
			public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

			[DllImport("kernel32.dll")]
			public static extern bool FindClose(IntPtr hFindFile);

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
			public struct WIN32_FIND_DATA
			{
				public FileAttributes dwFileAttributes;
				public FILETIME ftCreationTime;
				public FILETIME ftLastAccessTime;
				public FILETIME ftLastWriteTime;
				public uint nFileSizeHigh;
				public uint nFileSizeLow;
				public uint dwReserved0;
				public uint dwReserved1;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
				public string cFileName;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
				public string cAlternateFileName;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct FILETIME
			{
				public uint dwLowDateTime;
				public uint dwHighDateTime;
			}
		}

		static readonly EnumerationOptions enumerationOptions = new()
		{
			IgnoreInaccessible = true,
			MatchCasing = MatchCasing.CaseInsensitive,
			MatchType = MatchType.Simple,
			RecurseSubdirectories = false,
			ReturnSpecialDirectories = false
		};

		static async Task Main()
		{
			Console.WriteLine("请输入文件夹的绝对路径：");
			string? folderPath = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
			{
				Console.WriteLine("指定的路径不存在，请输入有效的文件夹路径。");
				return;
			}

			Console.WriteLine("是否要遍历隐藏的文件和文件夹？(y/n):");
			bool includeHidden = Console.ReadLine()?.ToLower() == "y";

			enumerationOptions.AttributesToSkip = includeHidden ? FileAttributes.Normal : FileAttributes.Hidden | FileAttributes.System;

			Console.WriteLine("请输入需要忽略的文件夹名称，使用英文分号';'分隔（不需要则直接回车）:");
			string? ignoreFoldersInput = Console.ReadLine();
			var ignoreFolderNames = ignoreFoldersInput?.Split(';', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

			Console.WriteLine("正在分析中，请稍后...");

			Stopwatch stopwatch = Stopwatch.StartNew();

			await foreach (var line in EnumerateFolderTreeAsync(folderPath, includeHidden, ignoreFolderNames))
			{
				Console.WriteLine(line);
			}

			stopwatch.Stop();
			Console.WriteLine($"遍历完成，耗时：{stopwatch.ElapsedMilliseconds}ms");

			Console.ReadKey();
		}

		static async IAsyncEnumerable<string> EnumerateFolderTreeAsync(string folderPath, bool includeHidden, HashSet<string> ignoreFolderNames, string indent = "")
		{
			IntPtr INVALID_HANDLE_VALUE = new(-1);
			NativeMethods.WIN32_FIND_DATA findData;

			IntPtr findHandle = NativeMethods.FindFirstFile(folderPath + Path.DirectorySeparatorChar + "*", out findData);

			if (findHandle != INVALID_HANDLE_VALUE)
			{
				do
				{
					string currentFileName = findData.cFileName;

					if (currentFileName == "." || currentFileName == ".." || ignoreFolderNames.Contains(currentFileName))
						continue;

					string fullPath = Path.Combine(folderPath, currentFileName);
					FileAttributes attributes = findData.dwFileAttributes;

					if (!includeHidden && attributes.HasFlag(FileAttributes.Hidden))
						continue;

					yield return $"{indent}└── {currentFileName}";

					if (attributes.HasFlag(FileAttributes.Directory))
					{
						await foreach (var subline in EnumerateFolderTreeAsync(fullPath, includeHidden, ignoreFolderNames, indent + "    "))
						{
							yield return subline;
						}
					}
				} while (NativeMethods.FindNextFile(findHandle, out findData));

				NativeMethods.FindClose(findHandle);
			}
		}
	}
}