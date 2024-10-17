# CoffinDiskMark

![hdd](/img/hdd.jpg)

CoffinDiskMark is a TCAL-based benchmarking tool for decrypting `.k9a` files. It is designed to assess the decryption speed of large collections of `.k9a` files. The tool reports performance metrics such as total files decrypted, total data size, and decryption speed in MB/s.

Based on LlamaToolkit, but is significantly faster.

## Features

- Decrypt individual `.k9a` files or entire directories containing `.k9a` files.
- Repeatedly decrypt files for a specified duration to measure performance.
- Uses asynchronous and parallel threading to decrypt files quickly.

## Usage

```
./CoffinDiskMark <mode> <arguments>
```

## Modes

**Decrypt**

Decrypts `.k9a` files or directories containing `.k9a` files.

```
./CoffinDiskMark decrypt <inputFileOrDir> <outputDir>
```
- `<inputFileOrDir>`: The file or directory to decrypt.  
- `<outputDir>`: The directory where decrypted files will be saved.
- Passing these arguments is optional.

**Bench**

Runs a decryption benchmark for a specified number of seconds.

```
./CoffinDiskMark bench <seconds>
```

- `<seconds>`: Duration of the benchmark in seconds (default is 10 seconds).  
- Continuously decrypts files until the specified time limit is reached and provides a summary of the performance.

## Examples

1. **Run a 20-second benchmark**:
```
./CoffinDiskMark bench 20
```
2. **Decrypt the entire game**:
```
./CoffinDiskMark decrypt
```
3. **Decrypt a single file**:
```
./CoffinDiskMark decrypt encryptedFile.k9a decrypted/
```
4. **Decrypt an entire directory**:
```
./CoffinDiskMark decrypt encryptedFolder/ decrypted/
```

## Notes

- The tool assumes it is running in the game directory containing `game.exe`. It will automatically look for assets in `www/img`, `www/audio`, and `www/data`.
- Native AOT binaries are provided for Windows and Linux.
- You may want to run the tool with `sudo chrt 99` to give it the highest priority, which increases benchmarking speed. (This may cause your system to hang while the benchmark is in progress.)
- Depending on your system, the decryption speed may be limited by your CPU speed or your disk read/write speed. The tool cannot differentiate between these cases. Please keep this in mind.
- You can use tools such as `atop` during the benchmark to determine the bottleneck.

## License

This project is licensed under the Tumbolia Public License.
