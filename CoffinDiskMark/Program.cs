using System.Diagnostics;
using System.Text;

namespace CoffinDiskMark
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string firstLine;
            if (args.Length < 1)
            {
                firstLine = "default";
            }
            else
            {
                firstLine = args[0].ToLower();
            }
            switch (firstLine)
            {
                case "decrypt":
                    var watch = Stopwatch.StartNew();
                    await Decrypt(args);
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    Console.WriteLine("Decryption finished in " + elapsedMs + " ms.");
                    break;
                case "bench":
                    await RunBenchmark(args);
                    break;
                default:
                    Console.WriteLine("CoffinDiskMark: A TCAL-Based Benchmarking Tool");
                    Console.WriteLine("Usage: CoffinDiskMark <mode> <arguments>");
                    Console.WriteLine("Modes: decrypt, bench");
                    Console.WriteLine("Decrypt: CoffinDiskMark decrypt <inputFileOrDir> <outputDir> (optional)");
                    Console.WriteLine("Bench: CoffinDiskMark bench <seconds> (default: 10)");
                    Console.WriteLine("CoffinDiskMark should be placed inside of the game directory that contains game.exe.");
                    break;
            }
            Console.WriteLine("All tasks finished.");
        }

        static async Task RunBenchmark(string[] args)
        {
            int durationSeconds = 10;
            if (args.Length == 2 && int.TryParse(args[1], out int parsedSeconds))
            {
                durationSeconds = parsedSeconds;
            }

            Console.WriteLine($"Starting benchmark for {durationSeconds} seconds...");

            string currentDir = Directory.GetCurrentDirectory();
            if (!CheckGameDirectory(currentDir))
            {
                Console.WriteLine("Error: Game not found in the current directory.");
                return;
            }

            string[] dirInputs = { Path.Combine("www", "img"), Path.Combine("www", "audio"), Path.Combine("www", "data") };
            string dirOutput = "decrypted_benchmark";

            int totalFilesDecrypted = 0;
            long totalBytesDecrypted = 0;
            var benchmarkWatch = Stopwatch.StartNew();

            try
            {
                while (benchmarkWatch.Elapsed.TotalSeconds < durationSeconds)
                {
                    var decryptTasks = dirInputs.Select(async folder =>
                    {
                        string fullFolderPath = Path.Combine(currentDir, folder);
                        if (Directory.Exists(fullFolderPath))
                        {
                            //Console.WriteLine($"Decrypting files in directory: {folder}");

                            string[] fileList = Directory.GetFiles(fullFolderPath, "*.k9a", SearchOption.AllDirectories);

                            // Use Parallel.ForEachAsync for concurrent decryption of files.
                            await Parallel.ForEachAsync(fileList, async (f, cancellationToken) =>
                            {
                                try
                                {
                                    // Read the file asynchronously.
                                    byte[] rawData = await File.ReadAllBytesAsync(f, cancellationToken);

                                    // Determine the file extension and decrypt the file.
                                    string fileExtension = GetFileExtension(rawData);
                                    byte[] decryptedFile = DecryptFile(rawData, f);

                                    // If decryption fails, handle the failure.
                                    if (decryptedFile.Length == 1)
                                    {
                                        DecryptionFailure(f);
                                    }
                                    else
                                    {
                                        // Construct the output path for the decrypted file.
                                        string decryptedFilename = Path.Combine(dirOutput, Path.GetRelativePath(Directory.GetCurrentDirectory(), f));
                                        string? directoryToCreate = Path.GetDirectoryName(decryptedFilename);

                                        // Create the output directory if it doesn't exist.
                                        if (directoryToCreate != null)
                                        {
                                            Directory.CreateDirectory(directoryToCreate);
                                        }

                                        // Set the new filename with the correct extension.
                                        string newFilename = Path.ChangeExtension(decryptedFilename, fileExtension);

                                        // Write the decrypted file asynchronously.
                                        await File.WriteAllBytesAsync(newFilename, decryptedFile, cancellationToken);

                                        // Update counters.
                                        Interlocked.Increment(ref totalFilesDecrypted);
                                        Interlocked.Add(ref totalBytesDecrypted, decryptedFile.Length);

                                        //Console.WriteLine("File decrypted and saved to: " + newFilename);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Handle any exceptions that occur during file processing.
                                    Console.WriteLine($"Error processing file {f}: {ex.Message}");
                                }
                            });
                        }
                    });

                    // Wait for the current set of decryption tasks to complete.
                    await Task.WhenAll(decryptTasks);
                }
            }
            finally
            {
                benchmarkWatch.Stop();
            }

            double elapsedSeconds = benchmarkWatch.Elapsed.TotalSeconds;
            double totalMBDecrypted = totalBytesDecrypted / (1024.0 * 1024.0);
            double mbPerSecond = elapsedSeconds > 0 ? totalMBDecrypted / elapsedSeconds : 0;
            double elapsedMs = benchmarkWatch.ElapsedMilliseconds;

            Console.WriteLine($"Benchmark complete. Results:");
            Console.WriteLine($"Total files decrypted: {totalFilesDecrypted}");
            Console.WriteLine($"Total size decrypted: {totalMBDecrypted:F2} MB");
            Console.WriteLine($"Decryption speed: {mbPerSecond:F2} MB/s");
            Console.WriteLine($"Benchmark duration: {elapsedMs} ms");
        }


        static bool CheckGameDirectory(string folder)
        {
            string game = Path.Combine(folder, "Game.exe");
            string nw = Path.Combine(folder, "nw.exe");
            bool inGameDir = (File.Exists(game) || File.Exists(nw));
            return inGameDir;
        }


        static void DecryptionFailure(string userIn)
        {
            Console.WriteLine("Decryption failed. File was NOT decrypted: " + userIn);
        }
        static async Task Decrypt(string[] args)
        {
            if (args.Length == 3)
            {
                string userIn = args[1];
                string dirOutput = args[2];
                if (File.Exists(userIn))
                {
                    byte[] rawData = File.ReadAllBytes(userIn);
                    string fileExtension = GetFileExtension(rawData);
                    byte[] decryptedFile = DecryptFile(rawData, userIn);
                    if (decryptedFile.Length == 1)
                    {
                        DecryptionFailure(userIn);
                    }
                    else
                    {
                        string decryptedFilename = Path.Combine(dirOutput, Path.GetFileName(userIn));
                        string? directoryToCreate = Path.GetDirectoryName(decryptedFilename);
                        if (directoryToCreate != null)
                        {
                            Directory.CreateDirectory(directoryToCreate);
                        }
                        string newFilename = Path.ChangeExtension(decryptedFilename, fileExtension);
                        File.WriteAllBytes(newFilename, decryptedFile);
                        Console.WriteLine("Single file decrypted and saved to: " + newFilename);
                    }
                }
                else if (Directory.Exists(userIn))
                {
                    Console.WriteLine("Processing directory: " + userIn);
                    await DecryptFolder(userIn, dirOutput);
                }
                else
                {
                    Console.WriteLine("Error: File or directory does not exist: " + userIn);
                }
            }
            else
            {
                string currentDir = Directory.GetCurrentDirectory();
                if (CheckGameDirectory(currentDir))
                {
                    string[] dirInputs = [Path.Combine("www", "img"), Path.Combine("www", "audio"), Path.Combine("www", "data")];
                    string dirOutput = "decrypted";
                    IList<Task> folderTaskList = new List<Task>();
                    foreach (string folder in dirInputs)
                    {
                        string fullFolderPath = Path.Combine(Directory.GetCurrentDirectory(), folder);
                        if (Directory.Exists(fullFolderPath))
                        {
                            Console.WriteLine("Processing directory: " + folder);
                            folderTaskList.Add(DecryptFolder(fullFolderPath, dirOutput));
                        }
                        else
                        {
                            Console.WriteLine("Error: Directory does not exist: " + folder);
                        }
                    }
                    await Task.WhenAll(folderTaskList);
                }
                else
                {
                    Console.WriteLine("Error: Game not found!");
                }
            }
        }


        static async Task DecryptFolder(string dirInput, string dirOutput)
        {
            // Get all the files with .k9a extension in the input directory.
            string[] fileList = Directory.GetFiles(dirInput, "*.k9a", SearchOption.AllDirectories);

            // Use Parallel.ForEachAsync for concurrent processing.
            await Parallel.ForEachAsync(fileList, async (f, cancellationToken) =>
            {
                try
                {
                    // Read the file asynchronously.
                    byte[] rawData = await File.ReadAllBytesAsync(f, cancellationToken);

                    // Determine the file extension and decrypt the file.
                    string fileExtension = GetFileExtension(rawData);
                    byte[] decryptedFile = DecryptFile(rawData, f);

                    // If decryption fails, handle the failure.
                    if (decryptedFile.Length == 1)
                    {
                        DecryptionFailure(f);
                    }
                    else
                    {
                        // Construct the output path for the decrypted file.
                        string decryptedFilename = Path.Combine(dirOutput, Path.GetRelativePath(Directory.GetCurrentDirectory(), f));
                        string? directoryToCreate = Path.GetDirectoryName(decryptedFilename);

                        // Create the output directory if it doesn't exist.
                        if (directoryToCreate != null)
                        {
                            Directory.CreateDirectory(directoryToCreate);
                        }

                        // Set the new filename with the correct extension.
                        string newFilename = Path.ChangeExtension(decryptedFilename, fileExtension);

                        // Write the decrypted file asynchronously.
                        await File.WriteAllBytesAsync(newFilename, decryptedFile, cancellationToken);

                        Console.WriteLine("File decrypted and saved to: " + newFilename);
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that occur during file processing.
                    Console.WriteLine($"Error processing file {f}: {ex.Message}");
                }
            });
        }

        static string GetFileExtension(byte[] data)
        {
            int headerLength = data[0];
            return Encoding.ASCII.GetString(data, 1, headerLength);
        }
        static int Mask(string inputString)
        {
            int maskValue = 0;
            string decodedFilename = Path.GetFileNameWithoutExtension(inputString).ToUpper();
            foreach (char c in decodedFilename)
            {
                maskValue = (maskValue << 1) ^ c;
            }
            return maskValue;
        }
        static byte[] DecryptFile(byte[] data, string url)
        {
            if (!url.EndsWith(".k9a"))
            {
                Console.WriteLine("Not a .k9a file, skipping...");
                byte[] empty = { 0 };
                return empty;
            }

            int headerLength = data[0];
            int dataLength = data[1 + headerLength];
            byte[] encryptedData = new byte[data.Length - 2 - headerLength];
            Array.Copy(data, 2 + headerLength, encryptedData, 0, encryptedData.Length);
            int newMask = Mask(url);

            if (dataLength == 0)
            {
                dataLength = encryptedData.Length;
            }

            byte[] decryptedData = encryptedData;

            for (int i = 0; i < dataLength; i++)
            {
                byte encryptedByte = encryptedData[i];
                decryptedData[i] = (byte)((encryptedByte ^ newMask) % 256);
                newMask = newMask << 1 ^ encryptedByte;
            }

            return decryptedData;
        }
        static string[] GetFiles(string sourceFolder, string filters, SearchOption searchOption)
        {
            return filters.Split('|').SelectMany(filter => Directory.GetFiles(sourceFolder, filter, searchOption)).ToArray();
        }
    }
}
