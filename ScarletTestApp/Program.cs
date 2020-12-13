using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Text;

using Scarlet.IO;
using Scarlet.Drawing;
using Scarlet.IO.ImageFormats;
using Scarlet.IO.ContainerFormats;
using Scarlet.IO.CompressionFormats;

namespace ScarletTestApp
{
    class Program
    {
        // "E:\[SSD User Data]\Downloads\__misc-test__"
        // "E:\[SSD User Data]\Desktop\Misc Stuff\PSP-misc\DATA-unpacked"
        // "E:\[SSD User Data]\Downloads\_stex-test"
        // "E:\[SSD User Data]\Desktop\Misc Stuff\ZHP\system"
        // "E:\[SSD User Data]\Downloads\SkullgirlsDDS"

        static char[] directorySeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        static string defaultOutputDir = "(converted)";

        static int indent = 0;
        static bool keepFiles = false;
        static DirectoryInfo globalOutputDir = null;
        static string NewName = null;

        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                var name = (assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false).FirstOrDefault() as AssemblyProductAttribute).Product;
                var version = new Version((assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false).FirstOrDefault() as AssemblyFileVersionAttribute).Version);
                var description = (assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false).FirstOrDefault() as AssemblyDescriptionAttribute).Description;
                var copyright = (assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false).FirstOrDefault() as AssemblyCopyrightAttribute).Copyright;
                var informational = (assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault() as AssemblyInformationalVersionAttribute).InformationalVersion;

                foreach (AssemblyName referencedAssembly in Assembly.GetExecutingAssembly().GetReferencedAssemblies().Where(x => x.Name.StartsWith("Scarlet")).OrderBy(x => x.Name))
                {
                    var loadedAssembly = Assembly.Load(referencedAssembly.Name);
                    var assemblyInformational = (loadedAssembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault() as AssemblyInformationalVersionAttribute).InformationalVersion;
                }

                args = CommandLineTools.CreateArgs(Environment.CommandLine);
                //args = new string[] { "", "C:\\Users\\Acer\\Desktop\\bg_komaroom_obj01.btx", "--output", "C:\\Users\\Acer\\Desktop\\ff", "--PCAE" };

                if (args.Length < 2)
                    throw new CommandLineArgsException("<input ...> [--keep | --output <directory>]");

                List<DirectoryInfo> inputDirs = new List<DirectoryInfo>();
                List<FileInfo> inputFiles = new List<FileInfo>();

                for (int i = 1; i < args.Length; i++)
                {
                    DirectoryInfo directory = new DirectoryInfo(args[i]);
                    if (directory.Exists)
                    {
                        IEnumerable<FileInfo> files = directory.EnumerateFiles("*", SearchOption.AllDirectories).Where(x => x.Extension != ".png");
                        inputDirs.Add(directory);
                        continue;
                    }

                    FileInfo file = new FileInfo(args[i]);
                    if (file.Exists)
                    {
                        inputFiles.Add(file);
                        continue;
                    }

                    if (args[i].StartsWith("-"))
                    {
                        switch (args[i].TrimStart('-'))
                        {
                            case "k":
                            case "keep":
                                keepFiles = true;
                                break;

                            case "o":
                            case "output":
                                globalOutputDir = new DirectoryInfo(args[++i]);
                                break;

                            case "n":
                            case "nameoutput":
                                NewName = args[++i];
                                break;

                            case "PCAE":
                                SHTXFS.PCAE = SHTXFF.PCAE = true;
                                break;

                            default:
                                IndentWriteLine("Unknown argument '{0}'.", args[i]);
                                break;
                        }
                        continue;
                    }
                }

                if (inputDirs.Count > 0)
                {
                    foreach (DirectoryInfo inputDir in inputDirs)
                    {
                        DirectoryInfo outputDir = (globalOutputDir != null ? globalOutputDir : new DirectoryInfo(inputDir.FullName + " " + defaultOutputDir));
                        foreach (FileInfo inputFile in inputDir.EnumerateFiles("*", SearchOption.AllDirectories).Where(x => x.Extension != ".png" && !IsSubdirectory(x.Directory, outputDir)))
                            ProcessInputFile(inputFile, inputDir, outputDir);

                        indent--;
                    }
                }

                if (inputFiles.Count > 0)
                {
                    foreach (FileInfo inputFile in inputFiles)
                    {
                        DirectoryInfo outputDir = (globalOutputDir != null ? globalOutputDir : inputFile.Directory);
                        ProcessInputFile(inputFile, inputFile.Directory, outputDir);
                    }
                }
            }
#if !DEBUG
            catch (CommandLineArgsException claEx)
            {
                IndentWriteLine("Invalid arguments; expected: {0}.", claEx.ExpectedArgs);
            }
            catch (Exception ex)
            {
                IndentWriteLine("Exception occured: {0}.", ex.Message);
            }
#endif
            finally
            {
                stopwatch.Stop();

                indent = 0;
            }
        }

        private static void ProcessInputFile(FileInfo inputFile, DirectoryInfo inputDir, DirectoryInfo outputDir)
        {
            string FinalOutputName = null;

            try
            {
                string displayPath = inputFile.FullName.Replace(inputDir.FullName, string.Empty).TrimStart(directorySeparators);

                string relativeDirectory = inputFile.DirectoryName.TrimEnd(directorySeparators).Replace(inputDir.FullName.TrimEnd(directorySeparators), string.Empty).TrimStart(directorySeparators);

                if (keepFiles)
                {
                    string existenceCheckPath = Path.Combine(outputDir.FullName, relativeDirectory);
                    string existenceCheckPattern = inputFile.Name + "*";
                    if (Directory.Exists(existenceCheckPath) && Directory.EnumerateFiles(existenceCheckPath, existenceCheckPattern).Any())
                    {
                        return;
                    }
                }

                using (FileStream inputStream = new FileStream(inputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {

                    var instance = FileFormat.FromFile<FileFormat>(inputStream);
                    if (instance != null)
                    {
                        if (instance is ImageFormat)
                        {
                            var imageInstance = (instance as ImageFormat);

                            int imageCount = imageInstance.GetImageCount();
                            int paletteCount = imageInstance.GetPaletteCount();
                            int blockCount = ((imageInstance is GXT && (imageInstance as GXT).BUVChunk != null) ? (imageInstance as GXT).BUVChunk.Entries.Length : -1);

                            List<string> contentStrings = new List<string>();
                            contentStrings.Add(string.Format("{0} image{1}", imageCount, (imageCount != 1 ? "s" : string.Empty)));
                            if (paletteCount > 0) contentStrings.Add(string.Format("{0} palette{1}", paletteCount, (paletteCount != 1 ? "s" : string.Empty)));
                            if (blockCount > 0) contentStrings.Add(string.Format("{0} block{1}", blockCount, (blockCount != 1 ? "s" : string.Empty)));

                            for (int i = 0; i < imageCount; i++)
                            {
                                string imageName = imageInstance.GetImageName(i);

                                string outputFilename;
                                string NewFileName = null;
                                FileInfo outputFile;

                                if (paletteCount < 2)
                                {
                                    Bitmap image = imageInstance.GetBitmap(i, 0);

                                    if (NewName != null)
                                    {
                                        NewFileName = NewName + ".png";
                                        outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, NewFileName));
                                    }
                                    else if (imageName == null)
                                    {
                                        NewFileName = Path.GetFileName(inputFile.Name).Substring(0, Path.GetFileName(inputFile.Name).IndexOf("."));

                                        if (inputFile.Name.Contains(".gx3dec"))
                                            NewFileName += ".gx3dec";
                                        else if (inputFile.Name.Contains(".dec"))
                                            NewFileName += ".dec";

                                        if (imageInstance is GXT)
                                            NewFileName += ".gxt";
                                        else if (imageInstance is SHTXFS)
                                            NewFileName += ".SHTXFS.btx";
                                        else if (imageInstance is SHTX)
                                            NewFileName += ".SHTX.btx";
                                        else if (imageInstance is SHTXFF)
                                            NewFileName += ".SHTXFF.btx";
                                        else if (imageInstance is SHTXFs)
                                            NewFileName += ".SHTXFs.btx";
                                        else if (imageInstance is DDS)
                                            NewFileName += ".dds";

                                        if (Path.GetExtension(NewFileName) != Path.GetExtension(inputFile.Name))
                                            NewFileName += Path.GetExtension(inputFile.Name);

                                        outputFilename = string.Format("{0}.png", NewFileName, i);
                                        outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, outputFilename));
                                    }
                                    else
                                    {
                                        NewFileName = Path.GetFileName(inputFile.Name).Substring(0, Path.GetFileName(inputFile.Name).IndexOf("."));

                                        if (inputFile.Name.Contains(".gx3dec"))
                                            NewFileName += ".gx3dec";
                                        else if (inputFile.Name.Contains(".dec"))
                                            NewFileName += ".dec";

                                        if (imageInstance is GXT)
                                            NewFileName += ".gxt";
                                        else if (imageInstance is SHTXFS)
                                            NewFileName += ".SHTXFS.btx";
                                        else if (imageInstance is SHTX)
                                            NewFileName += ".SHTX.btx";
                                        else if (imageInstance is SHTXFF)
                                            NewFileName += ".SHTXFF.btx";
                                        else if (imageInstance is SHTXFs)
                                            NewFileName += ".SHTXFs.btx";

                                        if (Path.GetExtension(NewFileName) != Path.GetExtension(inputFile.Name))
                                            NewFileName += Path.GetExtension(inputFile.Name);

                                        outputFilename = string.Format("{0}.png", NewFileName);
                                        outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, inputFile.Name, outputFilename));
                                    }

                                    Directory.CreateDirectory(outputFile.Directory.FullName);
                                    image.Save(outputFile.FullName, System.Drawing.Imaging.ImageFormat.Png);
                                    FinalOutputName = outputFile.FullName;
                                }
                                else
                                {
                                    for (int p = 0; p < paletteCount; p++)
                                    {
                                        Bitmap image = imageInstance.GetBitmap(i, p);

                                        if (NewName != null)
                                        {
                                            NewFileName = NewName + ".png";
                                            outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, NewFileName));
                                        }
                                        else if (imageName == null)
                                        {
                                            outputFilename = string.Format("{0}.png", inputFile.Name, i, p);
                                            outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, outputFilename));
                                        }
                                        else
                                        {
                                            outputFilename = string.Format("{0}.png", imageName, p);
                                            outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, inputFile.Name, outputFilename));
                                        }

                                        Directory.CreateDirectory(outputFile.Directory.FullName);
                                        image.Save(outputFile.FullName, System.Drawing.Imaging.ImageFormat.Png);
                                        FinalOutputName = outputFile.FullName;
                                    }
                                }
                            }

                            if (imageInstance is GXT && (imageInstance as GXT).BUVChunk != null)
                            {
                                var gxtInstance = (imageInstance as GXT);

                                List<Bitmap> buvImages = gxtInstance.GetBUVBitmaps().ToList();
                                for (int b = 0; b < buvImages.Count; b++)
                                {
                                    Bitmap image = buvImages[b];
                                    string outputFilename = string.Format("{0}.png", inputFile.Name, b);
                                    FileInfo outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, outputFilename));

                                    Directory.CreateDirectory(outputFile.Directory.FullName);
                                    image.Save(outputFile.FullName, System.Drawing.Imaging.ImageFormat.Png);
                                }
                            }
                        }
                        else if (instance is ContainerFormat)
                        {
                            var containerInstance = (instance as ContainerFormat);

                            int elementCount = containerInstance.GetElementCount();

                            foreach (var element in containerInstance.GetElements(inputStream))
                            {
                                string outputFilename = element.GetName();
                                FileInfo outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, inputFile.Name, outputFilename));

                                Directory.CreateDirectory(outputFile.Directory.FullName);
                                using (FileStream outputStream = new FileStream(outputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                                {
                                    using (Stream elementStream = element.GetStream(inputStream))
                                    {
                                        elementStream.CopyTo(outputStream);
                                    }
                                }

                                ProcessInputFile(outputFile, outputFile.Directory, new DirectoryInfo(outputFile.Directory.FullName + " " + defaultOutputDir));
                            }
                        }
                        else if (instance is CompressionFormat)
                        {
                            var compressedInstance = (instance as CompressionFormat);

                            // TODO: less naive way of determining target filename; see also CompressionFormat class in Scarlet.IO.CompressionFormats
                            string outputFilename;
                            string nameOrExtension = compressedInstance.GetNameOrExtension();
                            if (nameOrExtension != string.Empty)
                            {
                                bool isFullName = nameOrExtension.Contains('.');
                                outputFilename = (isFullName ? nameOrExtension : inputFile.Name + "." + nameOrExtension).TrimEnd('.');
                            }
                            else
                                outputFilename = Path.GetFileName(inputFile.Name);
                            FileInfo outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, outputFilename));

                            Directory.CreateDirectory(outputFile.Directory.FullName);
                            using (FileStream outputStream = new FileStream(outputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                            {
                                using (Stream decompressedStream = compressedInstance.GetDecompressedStream())
                                {
                                    decompressedStream.CopyTo(outputStream);
                                }
                            }

                            // TODO: make nicer?
                            ProcessInputFile(outputFile, outputFile.Directory, outputFile.Directory);

                            if (File.Exists(outputFile.FullName))
                            {
                                File.Delete(outputFile.FullName);
                                while (File.Exists(outputFile.FullName)) { }
                            }
                        }
                        else
                            Console.WriteLine("unhandled file.");
                    }
                    else
                        Console.WriteLine("unsupported file.");
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                IndentWriteLine("Exception occured: {0}.", ex.Message);
            }
#endif
            finally
            {
                Console.WriteLine(FinalOutputName);
            }
        }

        private static void IndentWrite(string format = "", params object[] param)
        {
            Console.Write(format.Insert(0, new string(' ', indent)), param);
        }

        private static void IndentWriteLine(string format = "", params object[] param)
        {

        }

        /* Slightly modified from https://stackoverflow.com/a/4423615 */
        private static string GetReadableTimespan(TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}{4}",
            span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}, ", span.Seconds, span.Seconds == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Milliseconds > 0 ? string.Format("{0:0} millisecond{1}", span.Milliseconds, span.Milliseconds == 1 ? string.Empty : "s") : string.Empty);
            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);
            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";
            return formatted;
        }

        private static bool IsSubdirectory(DirectoryInfo childDir, DirectoryInfo parentDir)
        {
            if (parentDir.FullName == childDir.FullName) return true;

            DirectoryInfo child = childDir.Parent;
            while (child != null)
            {
                if (child.FullName == parentDir.FullName) return true;
                child = child.Parent;
            }

            return false;
        }
    }
}
