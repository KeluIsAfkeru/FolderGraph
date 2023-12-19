using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace FolderGraph
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("请输入文件夹的绝对路径：");
			string folderPath = Console.ReadLine();

			Console.WriteLine("是否要遍历隐藏的文件和文件夹？(y/n):");
			bool includeHidden = Console.ReadLine()?.ToLower() == "y";

			Console.WriteLine("请输入需要忽略的文件夹名称，使用英文分号';'分隔（不需要则直接回车）:");
			string ignoreFoldersInput = Console.ReadLine();
			HashSet<string> ignoreFolderNames = ignoreFoldersInput?.Split(';', StringSplitOptions.RemoveEmptyEntries)
				.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

			Console.WriteLine("正在分析中，请稍后...");

			if (!Directory.Exists(folderPath))
			{
				Console.WriteLine("指定的路径不存在，请输入有效的文件夹路径。");
				return;
			}

			var stopwatch = Stopwatch.StartNew();

			await DisplayFolderTreeAsync(folderPath, "", includeHidden, ignoreFolderNames);

			stopwatch.Stop(); // 停止计时
			Console.WriteLine($"遍历完成，耗时：{stopwatch.ElapsedMilliseconds}ms");

			Console.ReadKey();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static async Task DisplayFolderTreeAsync(string folderPath, string indent, bool includeHidden, HashSet<string> ignoreFolderNames)
		{
			EnumerationOptions enumerationOptions = new EnumerationOptions
			{
				IgnoreInaccessible = true,
				MatchCasing = MatchCasing.CaseInsensitive,
				MatchType = MatchType.Simple,
				RecurseSubdirectories = false,
				ReturnSpecialDirectories = false,
				AttributesToSkip = includeHidden ? FileAttributes.Normal : FileAttributes.Hidden
			};

			try
			{
				string[] directories = Directory.GetDirectories(folderPath, "*", enumerationOptions);

				await Task.WhenAll(directories.Select(async directory =>
				{
					DirectoryInfo dirInfo = new DirectoryInfo(directory);
					if (ignoreFolderNames.Contains(dirInfo.Name))
						return;

					Console.WriteLine($"{indent}└── {dirInfo.Name}");
					await DisplayFolderTreeAsync(directory, indent + "    ", includeHidden, ignoreFolderNames);
				}));

				string[] files = Directory.GetFiles(folderPath, "*", enumerationOptions);
				foreach (string file in files)
					Console.WriteLine($"{indent}└── {Path.GetFileName(file)}");
			}
			catch (UnauthorizedAccessException)
			{
				Console.WriteLine($"{indent}└── [访问被拒绝]");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"{indent}发生错误：{ex.Message}");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EnsureDirExists(params string[] paths)
		{
			if (paths == null || paths.Length == 0)
				throw new ArgumentException("目标路径不能为空", nameof(paths));

			string combinedPath = Path.Combine(paths);

			if (string.IsNullOrWhiteSpace(combinedPath))
				throw new ArgumentException("目标路径不能为空或仅包含空白字符", nameof(combinedPath));

			try
			{
				DirectoryInfo directoryInfo = new(combinedPath);
				if (!directoryInfo.Exists)
				{
					directoryInfo.Create();
					return true;
				}

				return false;
			}
			catch (IOException ioEx)
			{
				// 处理文件I/O错误
				throw new InvalidOperationException("创建目录时遇到I/O错误", ioEx);
			}
			catch (UnauthorizedAccessException unauthorizedEx)
			{
				// 处理权限错误
				throw new InvalidOperationException("没有权限创建目录", unauthorizedEx);
			}
			catch (Exception ex)
			{
				// 处理其他潜在异常
				throw new InvalidOperationException("创建目录时发生未知错误", ex);
			}
		}
	}
}