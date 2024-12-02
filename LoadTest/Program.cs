﻿// See https://aka.ms/new-console-template for more information

using System.Text;

const string fileNameTemplate = "{0}_{1}.txt";
var existingFiles = new List<string>();

Console.WriteLine("Enter test directory to create/open files:");
var testDir = Console.ReadLine();

if (string.IsNullOrWhiteSpace(testDir))
{
    throw new ArgumentNullException(nameof(testDir));
}

for (int i = 0; i < 20; i++)
{
    var fileName = string.Format(fileNameTemplate, DateTimeOffset.Now.ToString().Replace(':', '_'), i);
    await CreateFile(Path.Combine(testDir, fileName));
    existingFiles.Add(fileName);
}

await Test(testDir, existingFiles);


static async Task Test(string testDir, List<string> existingFiles)
{
    var random = new Random();
    for (int i = 0; i < 20; i++)
    {
        var tasks = new List<Task>();
        for (int j = 0; j < 10; j++)
        {
            if (j % 2 == 0)
            {
                var fileName = string.Format(fileNameTemplate, DateTimeOffset.Now.ToString().Replace(':', '_'), Guid.NewGuid());
                var fullFilePath = Path.Combine(testDir, fileName);
                tasks.Add(Task.Run(() => CreateFile(fullFilePath)));
            }
            else
            {
                var existingFile = existingFiles[random.Next(0, existingFiles.Count)];
                tasks.Add(Task.Run(() => Rewrite(existingFile)));
            }
        }

        await Task.WhenAll(tasks);
    }

}

static async Task CreateFile(string fullFilePath)
{
    await File.WriteAllTextAsync(fullFilePath, "New Driver test", Encoding.UTF8);
}

static async Task Rewrite(string fullFilePath)
{
    try
    {
        await File.WriteAllTextAsync(fullFilePath, "NO test", Encoding.UTF8);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}