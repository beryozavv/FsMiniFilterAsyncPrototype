// See https://aka.ms/new-console-template for more information

using System.Text;

const string fileNameTemplate = "{0}_{1}.txt";

var existingFiles = new List<string>();

Console.WriteLine("Enter test directory to create/open files:");
var testDir = Console.ReadLine();
Console.WriteLine("Enter driver mode on or off");
var driverMode = Console.ReadLine();

if (string.IsNullOrWhiteSpace(testDir))
{
    throw new ArgumentNullException(nameof(testDir));
}

if (Directory.Exists(testDir))
{
    // Получаем список файлов в указанной папке
    var files = Directory.GetFiles(testDir);

    // Выводим краткие имена файлов
    foreach (var file in files)
    {
        var fileName = Path.GetFileName(file);
        existingFiles.Add(fileName);
    }
}
else
{
    Console.WriteLine("Папка не существует.");
}

for (var i = 0; i < 100; i++)
{
    var fileName = string.Format(fileNameTemplate, DateTimeOffset.Now.ToString().Replace(':', '_'), i);
    await CreateFile(Path.Combine(testDir, fileName), driverMode, i);
    existingFiles.Add(fileName);
}

await Test(testDir, existingFiles, driverMode);


static async Task Test(string testDir, List<string> existingFiles, string? driverMode)
{
    Console.WriteLine(
        $"Start Test DateTime={DateTime.Now:O}; existingFiles count = {existingFiles.Count} driverMode={driverMode}");

    long iteration = 0;
    var count = existingFiles.Count;
    while (true)
    {
        var startIterationTime = DateTimeOffset.Now;
        iteration++;
        try
        {
            var tasks = new List<Task>();
            var createdFiles = new List<string>();
            for (var i = 0; i < count; i++)
            {
                await ReadFile(Path.Combine(testDir, existingFiles[i]));
                if (i % 2 == 0)
                {
                    var fileName = string.Format(fileNameTemplate, DateTimeOffset.Now.ToString().Replace(':', '_'),
                        Guid.NewGuid());
                    var fullFilePath = Path.Combine(testDir, fileName);
                    var currentJ = i;
                    tasks.Add(Task.Run(() => CreateFile(fullFilePath, driverMode, currentJ)));
                    createdFiles.Add(fullFilePath);
                }
                else
                {
                    var existingFile = existingFiles[i];
                    var fullFilePath = Path.Combine(testDir, existingFile);
                    tasks.Add(Task.Run(() => Rewrite(fullFilePath)));
                }
            }

            await Task.WhenAll(tasks);

            foreach (var createdFilePath in createdFiles)
            {
                await ReadFile(createdFilePath);
                DeleteFile(createdFilePath);
            }

            // todo
            // Логирование скорости операций в файл?

            Console.WriteLine(
                $"DateTime={DateTime.Now:O}; Iteration {iteration}; existingFilesCount = {existingFiles.Count}; createdFilesCount={createdFiles.Count}; Delta = {DateTimeOffset.Now - startIterationTime:g}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"DateTime={DateTime.Now:O}; Error in Iteration {iteration}; Exception: {e}");
        }
    }
    // ReSharper disable once FunctionNeverReturns
}

static async Task ReadFile(string fullFilePath)
{
    try
    {
        await File.ReadAllTextAsync(fullFilePath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
    catch (UnauthorizedAccessException)
    {
        
    }
    catch (Exception e)
    {
        Console.WriteLine("ReadFile" + e);
    }
}

static async Task CreateFile(string fullFilePath, string? driverMode, int? i = null)
{
    try
    {
        string text;
        if (i is > 70 and < 100 && driverMode != "on")
        {
            text = "NO for denied testing" +
                   string.Format(fileNameTemplate, DateTimeOffset.Now.ToString().Replace(':', '_'), i);
        }
        else
        {
            text = "YES New Driver test" +
                   string.Format(fileNameTemplate, DateTimeOffset.Now.ToString().Replace(':', '_'), i);
        }

        await File.WriteAllTextAsync(fullFilePath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
    catch (UnauthorizedAccessException)
    {
        
    }
    catch (Exception e)
    {
        Console.WriteLine("CreateFile " + e);
    }
}

static void DeleteFile(string fullFilePath)
{
    try
    {
        File.Delete(fullFilePath);
    }
    catch (UnauthorizedAccessException)
    {
        
    }
    catch (Exception e)
    {
        Console.WriteLine("DeleteFile" + e);
    }
}

static async Task Rewrite(string fullFilePath, int? i = null)
{
    try
    {
        string text;
        if (i is > 60 and < 100)
        {
            text = "NO Modified for denied testing" +
                   string.Format(fileNameTemplate, DateTimeOffset.Now.ToString().Replace(':', '_'), i);
        }
        else
        {
            text = "YES Modified for denied testing" +
                   string.Format(fileNameTemplate, DateTimeOffset.Now.ToString().Replace(':', '_'), i);
        }

        await File.WriteAllTextAsync(fullFilePath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
    catch (UnauthorizedAccessException)
    {
        
    }
    catch (Exception e)
    {
        Console.WriteLine("Rewrite" + e);
    }
}