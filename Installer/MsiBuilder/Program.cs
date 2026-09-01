using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

internal static class Program
{
    // ============================================================
    // CONFIGURATION
    // ============================================================

    private const string ProductName = "Fiserv Application";
    private const string Manufacturer = "Fiserv";

    // IMPORTANT:
    // Generate this ONCE for your product and keep it unchanged
    // for all releases of the same product.
    private const string UpgradeCode =
        "{7D3E6A5B-4D92-4A10-9E53-8C7B6F4A2910}";

    // English - United States
    private const string ProductLanguage = "1033";

    // Intel/x86 MSI.
    // For a native x64 package use "x64;1033".
    private const string TemplateSummary = "Intel;1033";

    private const int MSIDBOPEN_CREATE = 3;

    private const uint ERROR_SUCCESS = 0;
    private const uint ERROR_NO_MORE_ITEMS = 259;

    private const int MSIMODIFY_INSERT = 1;

    // Summary Information properties
    private const uint PID_CODEPAGE = 1;
    private const uint PID_TITLE = 2;
    private const uint PID_SUBJECT = 3;
    private const uint PID_AUTHOR = 4;
    private const uint PID_KEYWORDS = 5;
    private const uint PID_COMMENTS = 6;
    private const uint PID_TEMPLATE = 7;
    private const uint PID_REVNUMBER = 9;
    private const uint PID_PAGECOUNT = 14;
    private const uint PID_WORDCOUNT = 15;
    private const uint PID_APPNAME = 18;
    private const uint PID_SECURITY = 19;

    private const uint VT_I2 = 2;
    private const uint VT_I4 = 3;
    private const uint VT_LPSTR = 30;

    private const int MaxFiles = 32767;

    // ============================================================
    // WINDOWS INSTALLER API
    // ============================================================

    [DllImport(
        "msi.dll",
        CharSet = CharSet.Unicode,
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiOpenDatabase(
        string szDatabasePath,
        int szPersist,
        out IntPtr phDatabase);

    [DllImport(
        "msi.dll",
        CharSet = CharSet.Unicode,
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiDatabaseOpenView(
        IntPtr hDatabase,
        string szQuery,
        out IntPtr phView);

    [DllImport(
        "msi.dll",
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiViewExecute(
        IntPtr hView,
        IntPtr hRecord);

    [DllImport(
        "msi.dll",
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiViewFetch(
        IntPtr hView,
        out IntPtr hRecord);

    [DllImport(
        "msi.dll",
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiViewModify(
        IntPtr hView,
        int eModifyMode,
        IntPtr hRecord);

    [DllImport(
        "msi.dll",
        CharSet = CharSet.Unicode,
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiRecordSetString(
        IntPtr hRecord,
        uint iField,
        string szValue);

    [DllImport(
        "msi.dll",
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiRecordSetInteger(
        IntPtr hRecord,
        uint iField,
        int iValue);

    [DllImport(
        "msi.dll",
        CharSet = CharSet.Unicode,
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiRecordSetStream(
        IntPtr hRecord,
        uint iField,
        string szFilePath);

    [DllImport(
        "msi.dll",
        CallingConvention = CallingConvention.Winapi)]
    private static extern IntPtr MsiCreateRecord(
        uint cParams);

    [DllImport(
        "msi.dll",
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiViewClose(
        IntPtr hView);

    [DllImport(
        "msi.dll",
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiCloseHandle(
        IntPtr hAny);

    [DllImport(
        "msi.dll",
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiDatabaseCommit(
        IntPtr hDatabase);

    [DllImport(
        "msi.dll",
        CharSet = CharSet.Unicode,
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiGetSummaryInformation(
        IntPtr hDatabase,
        string? szDatabasePath,
        uint uiUpdateCount,
        out IntPtr phSummaryInfo);

    [DllImport(
        "msi.dll",
        EntryPoint = "MsiSummaryInfoSetPropertyA",
        CharSet = CharSet.Ansi,
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiSummaryInfoSetPropertyA(
        IntPtr hSummaryInfo,
        uint uiProperty,
        uint uiDataType,
        int iValue,
        IntPtr pftValue,
        IntPtr szValue);

    [DllImport(
        "msi.dll",
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiSummaryInfoPersist(
        IntPtr hSummaryInfo);

    [DllImport(
        "msi.dll",
        CharSet = CharSet.Unicode,
        CallingConvention = CallingConvention.Winapi)]
    private static extern uint MsiDatabaseIsTablePersistent(
        IntPtr hDatabase,
        string szTable);

    // ============================================================
    // MAIN
    // ============================================================

    private static int Main(string[] args)
    {
        PrintHeader();

        if (args.Length < 2)
        {
            PrintUsage();
            return 1;
        }

        string sourceDirectory;
        string outputMsi;
        string productVersion;

        try
        {
            sourceDirectory = Path.GetFullPath(args[0]);
            outputMsi = Path.GetFullPath(args[1]);

            productVersion =
                args.Length >= 3
                    ? NormalizeProductVersion(args[2])
                    : "1.0.0";

            ValidateSourceDirectory(sourceDirectory);

            string? outputDirectory =
                Path.GetDirectoryName(outputMsi);

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new InvalidOperationException(
                    "Invalid output MSI path.");
            }

            Directory.CreateDirectory(outputDirectory);
        }
        catch (Exception ex)
        {
            PrintError(ex);
            return 1;
        }

        Console.WriteLine($"Source directory : {sourceDirectory}");
        Console.WriteLine($"Output MSI       : {outputMsi}");
        Console.WriteLine($"Product name     : {ProductName}");
        Console.WriteLine($"Manufacturer     : {Manufacturer}");
        Console.WriteLine($"Product version  : {productVersion}");
        Console.WriteLine($"Upgrade code     : {UpgradeCode}");
        Console.WriteLine();

        string workingDirectory =
            Path.Combine(
                Path.GetTempPath(),
                "FiservMsiBuilder",
                Guid.NewGuid().ToString("N"));

        string cabDirectory =
            Path.Combine(
                workingDirectory,
                "cab");

        string ddfFile =
            Path.Combine(
                workingDirectory,
                "FiservApplication.ddf");

        string cabFile =
            Path.Combine(
                cabDirectory,
                "FiservApplication.cab");

        IntPtr database = IntPtr.Zero;

        try
        {
            Directory.CreateDirectory(cabDirectory);

            // ====================================================
            // STEP 1
            // ====================================================

            Console.WriteLine("===============================================");
            Console.WriteLine(" STEP 1 - Collect application files");
            Console.WriteLine("===============================================");

            List<ApplicationFile> files =
                CollectApplicationFiles(sourceDirectory);

            if (files.Count == 0)
            {
                throw new InvalidOperationException(
                    "No application files were found.");
            }

            if (files.Count > MaxFiles)
            {
                throw new InvalidOperationException(
                    $"Too many files: {files.Count}. " +
                    $"Maximum supported by this builder is {MaxFiles}.");
            }

            Console.WriteLine(
                $"Files found: {files.Count}");
            Console.WriteLine();

            // ====================================================
            // STEP 2
            // ====================================================

            Console.WriteLine("===============================================");
            Console.WriteLine(" STEP 2 - Create MSI database");
            Console.WriteLine("===============================================");

            if (File.Exists(outputMsi))
            {
                Console.WriteLine(
                    "Deleting previous MSI...");

                File.Delete(outputMsi);
            }

            uint result =
                MsiOpenDatabase(
                    outputMsi,
                    MSIDBOPEN_CREATE,
                    out database);

            CheckResult(
                result,
                "MsiOpenDatabase");

            Console.WriteLine(
                "MSI database created.");
            Console.WriteLine();

            // ====================================================
            // STEP 3
            // ====================================================

            Console.WriteLine("===============================================");
            Console.WriteLine(" STEP 3 - Create MSI tables");
            Console.WriteLine("===============================================");

            string productCode =
                CreateProductCode(
                    UpgradeCode,
                    productVersion);

            Console.WriteLine(
                $"ProductCode: {productCode}");

            CreatePropertyTable(
                database,
                productCode,
                productVersion);

            Dictionary<string, string> directoryIds =
                CreateDirectoryTable(
                    database,
                    files);

            CreateComponentTable(
                database,
                files,
                directoryIds);

            CreateFileTable(
                database,
                files);

            CreateFeatureTable(
                database);

            CreateFeatureComponentsTable(
                database,
                files);

            CreateMediaTable(
                database,
                files.Count);

            CreateInstallExecuteSequenceTable(
                database);

            Console.WriteLine(
                "MSI tables created successfully.");
            Console.WriteLine();

            // ====================================================
            // STEP 4
            // ====================================================

            Console.WriteLine("===============================================");
            Console.WriteLine(" STEP 4 - Create CAB");
            Console.WriteLine("===============================================");

            CreateDdfFile(
                ddfFile,
                cabDirectory,
                files);

            RunMakeCab(
                ddfFile,
                cabFile);

            if (!File.Exists(cabFile))
            {
                throw new InvalidOperationException(
                    $"CAB file was not created: {cabFile}");
            }

            Console.WriteLine(
                $"CAB created: {cabFile}");

            Console.WriteLine(
                $"CAB size: {new FileInfo(cabFile).Length:N0} bytes");

            Console.WriteLine();

            // ====================================================
            // STEP 5
            // ====================================================

            Console.WriteLine("===============================================");
            Console.WriteLine(" STEP 5 - Embed CAB into MSI");
            Console.WriteLine("===============================================");

            EmbedCabinet(
                database,
                cabFile);

            Console.WriteLine(
                "CAB embedded successfully.");
            Console.WriteLine();

            // ====================================================
            // STEP 6
            // ====================================================

            Console.WriteLine("===============================================");
            Console.WriteLine(" STEP 6 - Set Summary Information");
            Console.WriteLine("===============================================");

            string packageCode =
                Guid.NewGuid()
                    .ToString()
                    .ToUpperInvariant();

            SetSummaryInformation(
                database,
                packageCode);

            Console.WriteLine(
                "Summary information created.");
            Console.WriteLine();

            // ====================================================
            // STEP 7
            // ====================================================

            Console.WriteLine("===============================================");
            Console.WriteLine(" STEP 7 - Commit MSI");
            Console.WriteLine("===============================================");

            result =
                MsiDatabaseCommit(
                    database);

            CheckResult(
                result,
                "MsiDatabaseCommit");

            Console.WriteLine(
                "MSI database committed successfully.");
            Console.WriteLine();

            // Close before validation.
            MsiCloseHandle(database);
            database = IntPtr.Zero;

            // ====================================================
            // STEP 8
            // ====================================================

            Console.WriteLine("===============================================");
            Console.WriteLine(" STEP 8 - Verify MSI");
            Console.WriteLine("===============================================");

            VerifyMsiFile(outputMsi);

            Console.WriteLine();

            Console.WriteLine("===============================================");
            Console.WriteLine(" SUCCESS");
            Console.WriteLine("===============================================");

            Console.WriteLine(
                $"MSI              : {outputMsi}");

            Console.WriteLine(
                $"Size             : {new FileInfo(outputMsi).Length:N0} bytes");

            Console.WriteLine(
                $"ProductCode      : {productCode}");

            Console.WriteLine(
                $"PackageCode      : {packageCode}");

            Console.WriteLine(
                $"Files            : {files.Count}");

            Console.WriteLine();
            Console.WriteLine(
                "MSI creation completed successfully.");

            return 0;
        }
        catch (Exception ex)
        {
            PrintError(ex);
            return 1;
        }
        finally
        {
            if (database != IntPtr.Zero)
            {
                MsiCloseHandle(database);
            }

            try
            {
                if (Directory.Exists(workingDirectory))
                {
                    Directory.Delete(
                        workingDirectory,
                        true);
                }
            }
            catch
            {
                // Do not hide the original build error.
            }
        }
    }

    // ============================================================
    // HEADER
    // ============================================================

    private static void PrintHeader()
    {
        Console.WriteLine("===============================================");
        Console.WriteLine(" Fiserv MSI Builder");
        Console.WriteLine(" C# / Windows Installer API / makecab.exe");
        Console.WriteLine("===============================================");
        Console.WriteLine();
    }

    private static void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine();
        Console.WriteLine(
            "MsiBuilder.exe <SourceDirectory> <OutputMsi> [Version]");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine(
            @"MsiBuilder.exe C:\publish C:\output\FiservApplication.msi 1.0.0");
        Console.WriteLine();
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static void ValidateSourceDirectory(
        string sourceDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Source directory does not exist: {sourceDirectory}");
        }
    }

    private static string NormalizeProductVersion(
        string version)
    {
        if (!Version.TryParse(
                version,
                out Version? parsed))
        {
            throw new ArgumentException(
                $"Invalid MSI version: {version}");
        }

        int major = parsed.Major;
        int minor = parsed.Minor;
        int build =
            parsed.Build < 0
                ? 0
                : parsed.Build;

        if (major > 255)
        {
            throw new ArgumentException(
                "MSI major version cannot exceed 255.");
        }

        if (minor > 255)
        {
            throw new ArgumentException(
                "MSI minor version cannot exceed 255.");
        }

        if (build > 65535)
        {
            throw new ArgumentException(
                "MSI build version cannot exceed 65535.");
        }

        return $"{major}.{minor}.{build}";
    }

    private static void VerifyMsiFile(
        string msiPath)
    {
        if (!File.Exists(msiPath))
        {
            throw new InvalidOperationException(
                "MSI file was not created.");
        }

        FileInfo info =
            new FileInfo(msiPath);

        if (info.Length == 0)
        {
            throw new InvalidOperationException(
                "MSI file is empty.");
        }

        Console.WriteLine(
            $"MSI exists: {msiPath}");

        Console.WriteLine(
            $"MSI size: {info.Length:N0} bytes");
    }

    // ============================================================
    // COLLECT FILES
    // ============================================================

    private static List<ApplicationFile>
        CollectApplicationFiles(
            string sourceDirectory)
    {
        var result =
            new List<ApplicationFile>();

        string[] physicalFiles =
            Directory.GetFiles(
                sourceDirectory,
                "*",
                SearchOption.AllDirectories);

        Array.Sort(
            physicalFiles,
            StringComparer.OrdinalIgnoreCase);

        int sequence = 1;

        foreach (string physicalFile in physicalFiles)
        {
            string relativePath =
                Path.GetRelativePath(
                    sourceDirectory,
                    physicalFile);

            relativePath =
                relativePath.Replace(
                    '/',
                    '\\');

            string relativeDirectory =
                Path.GetDirectoryName(
                    relativePath)
                ?? string.Empty;

            string fileName =
                Path.GetFileName(
                    relativePath);

            FileInfo info =
                new FileInfo(
                    physicalFile);

            if (info.Length > int.MaxValue)
            {
                throw new InvalidOperationException(
                    $"File is too large for this MSI builder: {physicalFile}");
            }

            string fileId =
                $"F{sequence:D6}";

            string componentId =
                $"C{sequence:D6}";

            string componentGuid =
                CreateStableGuid(
                    "COMPONENT:" +
                    relativePath);

            result.Add(
                new ApplicationFile
                {
                    PhysicalPath = physicalFile,
                    RelativePath = relativePath,
                    RelativeDirectory = relativeDirectory,
                    FileName = fileName,
                    FileId = fileId,
                    ComponentId = componentId,
                    ComponentGuid = componentGuid,
                    Sequence = sequence,
                    FileSize = info.Length
                });

            sequence++;
        }

        return result;
    }

    // ============================================================
    // PROPERTY TABLE
    // ============================================================

    private static void CreatePropertyTable(
        IntPtr database,
        string productCode,
        string productVersion)
    {
        ExecuteSql(
            database,
            "CREATE TABLE `Property` (" +
            "`Property` CHAR(72) NOT NULL, " +
            "`Value` CHAR(0) NOT NULL LOCALIZABLE " +
            "PRIMARY KEY `Property`)");

        InsertProperty(
            database,
            "ProductName",
            ProductName);

        InsertProperty(
            database,
            "ProductVersion",
            productVersion);

        InsertProperty(
            database,
            "Manufacturer",
            Manufacturer);

        InsertProperty(
            database,
            "ProductCode",
            productCode);

        InsertProperty(
            database,
            "UpgradeCode",
            UpgradeCode);

        InsertProperty(
            database,
            "ProductLanguage",
            ProductLanguage);

        InsertProperty(
            database,
            "ALLUSERS",
            "1");

        InsertProperty(
            database,
            "ARPNOMODIFY",
            "1");

        InsertProperty(
            database,
            "ARPNOREPAIR",
            "1");

        Console.WriteLine(
            "Property table created.");
    }

    private static void InsertProperty(
        IntPtr database,
        string property,
        string value)
    {
        ExecuteSql(
            database,
            "INSERT INTO `Property` " +
            "(`Property`, `Value`) VALUES (" +
            SqlQuote(property) +
            ", " +
            SqlQuote(value) +
            ")");
    }

    // ============================================================
    // DIRECTORY TABLE
    // ============================================================

    private static Dictionary<string, string>
        CreateDirectoryTable(
            IntPtr database,
            List<ApplicationFile> files)
    {
        ExecuteSql(
            database,
            "CREATE TABLE `Directory` (" +
            "`Directory` CHAR(72) NOT NULL, " +
            "`Directory_Parent` CHAR(72), " +
            "`DefaultDir` CHAR(255) NOT NULL " +
            "PRIMARY KEY `Directory`)");

        ExecuteSql(
            database,
            "INSERT INTO `Directory` " +
            "(`Directory`, `Directory_Parent`, `DefaultDir`) " +
            "VALUES " +
            "('TARGETDIR', NULL, 'SourceDir')");

        ExecuteSql(
            database,
            "INSERT INTO `Directory` " +
            "(`Directory`, `Directory_Parent`, `DefaultDir`) " +
            "VALUES " +
            "('ProgramFilesFolder', 'TARGETDIR', '.')");

        ExecuteSql(
            database,
            "INSERT INTO `Directory` " +
            "(`Directory`, `Directory_Parent`, `DefaultDir`) " +
            "VALUES " +
            "('INSTALLDIR', 'ProgramFilesFolder', " +
            SqlQuote(ProductName) +
            ")");

        var directoryIds =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        directoryIds[string.Empty] =
            "INSTALLDIR";

        var directories =
            files
                .Select(
                    f => f.RelativeDirectory)
                .Where(
                    d => !string.IsNullOrWhiteSpace(d))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    d => d.Count(
                        c => c == '\\'))
                .ThenBy(
                    d => d,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        foreach (string directory in directories)
        {
            string[] parts =
                directory.Split(
                    '\\',
                    StringSplitOptions.RemoveEmptyEntries);

            string currentPath =
                string.Empty;

            foreach (string part in parts)
            {
                string nextPath =
                    string.IsNullOrEmpty(currentPath)
                        ? part
                        : currentPath + "\\" + part;

                if (directoryIds.ContainsKey(
                        nextPath))
                {
                    currentPath = nextPath;
                    continue;
                }

                string parentDirectory =
                    string.IsNullOrEmpty(currentPath)
                        ? "INSTALLDIR"
                        : directoryIds[currentPath];

                string directoryId =
                    CreateDirectoryId(
                        nextPath);

                directoryIds[nextPath] =
                    directoryId;

                ExecuteSql(
                    database,
                    "INSERT INTO `Directory` " +
                    "(`Directory`, `Directory_Parent`, `DefaultDir`) " +
                    "VALUES (" +
                    SqlQuote(directoryId) +
                    ", " +
                    SqlQuote(parentDirectory) +
                    ", " +
                    SqlQuote(part) +
                    ")");

                currentPath = nextPath;
            }
        }

        Console.WriteLine(
            $"Directory entries: {directoryIds.Count}");

        return directoryIds;
    }

    // ============================================================
    // COMPONENT TABLE
    // ============================================================

    private static void CreateComponentTable(
        IntPtr database,
        List<ApplicationFile> files,
        Dictionary<string, string> directoryIds)
    {
        ExecuteSql(
            database,
            "CREATE TABLE `Component` (" +
            "`Component` CHAR(72) NOT NULL, " +
            "`ComponentId` CHAR(38), " +
            "`Directory_` CHAR(72) NOT NULL, " +
            "`Attributes` SHORT NOT NULL, " +
            "`Condition` CHAR(255), " +
            "`KeyPath` CHAR(72) " +
            "PRIMARY KEY `Component`)");

        foreach (ApplicationFile file in files)
        {
            string directoryId =
                directoryIds[
                    file.RelativeDirectory];

            ExecuteSql(
                database,
                "INSERT INTO `Component` " +
                "(`Component`, `ComponentId`, `Directory_`, " +
                "`Attributes`, `Condition`, `KeyPath`) VALUES (" +
                SqlQuote(file.ComponentId) +
                ", " +
                SqlQuote(
                    file.ComponentGuid) +
                ", " +
                SqlQuote(directoryId) +
                ", " +
                "0, " +
                "NULL, " +
                SqlQuote(file.FileId) +
                ")");
        }

        Console.WriteLine(
            $"Components: {files.Count}");
    }

    // ============================================================
    // FILE TABLE
    // ============================================================

    private static void CreateFileTable(
        IntPtr database,
        List<ApplicationFile> files)
    {
        ExecuteSql(
            database,
            "CREATE TABLE `File` (" +
            "`File` CHAR(72) NOT NULL, " +
            "`Component_` CHAR(72) NOT NULL, " +
            "`FileName` CHAR(255) NOT NULL, " +
            "`FileSize` LONG NOT NULL, " +
            "`Version` CHAR(72), " +
            "`Language` CHAR(20), " +
            "`Attributes` SHORT, " +
            "`Sequence` SHORT NOT NULL " +
            "PRIMARY KEY `File`)");

        foreach (ApplicationFile file in files)
        {
            string fileName =
                GetMsiFileName(
                    file.FileName);

            ExecuteSql(
                database,
                "INSERT INTO `File` " +
                "(`File`, `Component_`, `FileName`, `FileSize`, " +
                "`Version`, `Language`, `Attributes`, `Sequence`) VALUES (" +
                SqlQuote(file.FileId) +
                ", " +
                SqlQuote(file.ComponentId) +
                ", " +
                SqlQuote(fileName) +
                ", " +
                file.FileSize.ToString(
                    CultureInfo.InvariantCulture) +
                ", " +
                "NULL, " +
                "NULL, " +
                "0, " +
                file.Sequence.ToString(
                    CultureInfo.InvariantCulture) +
                ")");
        }

        Console.WriteLine(
            $"Files in File table: {files.Count}");
    }

    private static string GetMsiFileName(
        string fileName)
    {
        if (fileName.Contains(
                '\\'))
        {
            throw new InvalidOperationException(
                $"Invalid file name: {fileName}");
        }

        return fileName;
    }

    // ============================================================
    // FEATURE TABLE
    // ============================================================

    private static void CreateFeatureTable(
        IntPtr database)
    {
        ExecuteSql(
            database,
            "CREATE TABLE `Feature` (" +
            "`Feature` CHAR(38) NOT NULL, " +
            "`Feature_Parent` CHAR(38), " +
            "`Title` CHAR(64), " +
            "`Description` CHAR(255), " +
            "`Display` SHORT, " +
            "`Level` SHORT NOT NULL, " +
            "`Directory_` CHAR(72), " +
            "`Attributes` SHORT NOT NULL " +
            "PRIMARY KEY `Feature`)");

        ExecuteSql(
            database,
            "INSERT INTO `Feature` " +
            "(`Feature`, `Feature_Parent`, `Title`, " +
            "`Description`, `Display`, `Level`, " +
            "`Directory_`, `Attributes`) VALUES (" +
            "'MainFeature', " +
            "NULL, " +
            SqlQuote(ProductName) +
            ", " +
            SqlQuote(
                "Main application feature") +
            ", " +
            "1, " +
            "1, " +
            "'INSTALLDIR', " +
            "0)");

        Console.WriteLine(
            "MainFeature created.");
    }

    // ============================================================
    // FEATURE COMPONENTS
    // ============================================================

    private static void CreateFeatureComponentsTable(
        IntPtr database,
        List<ApplicationFile> files)
    {
        ExecuteSql(
            database,
            "CREATE TABLE `FeatureComponents` (" +
            "`Feature_` CHAR(38) NOT NULL, " +
            "`Component_` CHAR(72) NOT NULL " +
            "PRIMARY KEY `Feature_`, `Component_`)");

        foreach (ApplicationFile file in files)
        {
            ExecuteSql(
                database,
                "INSERT INTO `FeatureComponents` " +
                "(`Feature_`, `Component_`) VALUES (" +
                "'MainFeature', " +
                SqlQuote(file.ComponentId) +
                ")");
        }

        Console.WriteLine(
            $"FeatureComponents: {files.Count}");
    }

    // ============================================================
    // MEDIA TABLE
    // ============================================================

    private static void CreateMediaTable(
        IntPtr database,
        int fileCount)
    {
        ExecuteSql(
            database,
            "CREATE TABLE `Media` (" +
            "`DiskId` SHORT NOT NULL, " +
            "`LastSequence` SHORT NOT NULL, " +
            "`DiskPrompt` CHAR(64), " +
            "`Cabinet` CHAR(255), " +
            "`VolumeLabel` CHAR(32), " +
            "`Source` CHAR(255) " +
            "PRIMARY KEY `DiskId`)");

        ExecuteSql(
            database,
            "INSERT INTO `Media` " +
            "(`DiskId`, `LastSequence`, `DiskPrompt`, " +
            "`Cabinet`, `VolumeLabel`, `Source`) VALUES (" +
            "1, " +
            fileCount.ToString(
                CultureInfo.InvariantCulture) +
            ", " +
            SqlQuote(ProductName) +
            ", " +
            "'#FiservApplication.cab', " +
            "'FISERV', " +
            "NULL)");

        Console.WriteLine(
            "Media table created.");
    }

    // ============================================================
    // INSTALL EXECUTE SEQUENCE
    // ============================================================

    private static void CreateInstallExecuteSequenceTable(
        IntPtr database)
    {
        ExecuteSql(
            database,
            "CREATE TABLE `InstallExecuteSequence` (" +
            "`Action` CHAR(72) NOT NULL, " +
            "`Condition` CHAR(255), " +
            "`Sequence` SHORT " +
            "PRIMARY KEY `Action`)");

        AddSequence(
            database,
            "FindRelatedProducts",
            null,
            200);

        AddSequence(
            database,
            "CostInitialize",
            null,
            800);

        AddSequence(
            database,
            "FileCost",
            null,
            900);

        AddSequence(
            database,
            "CostFinalize",
            null,
            1000);

        AddSequence(
            database,
            "InstallValidate",
            null,
            1400);

        AddSequence(
            database,
            "InstallInitialize",
            null,
            1500);

        AddSequence(
            database,
            "ProcessComponents",
            null,
            1600);

        AddSequence(
            database,
            "CreateFolders",
            null,
            3700);

        AddSequence(
            database,
            "InstallFiles",
            null,
            4000);

        AddSequence(
            database,
            "RegisterUser",
            null,
            6000);

        AddSequence(
            database,
            "RegisterProduct",
            null,
            6100);

        AddSequence(
            database,
            "PublishComponents",
            null,
            6200);

        AddSequence(
            database,
            "PublishFeatures",
            null,
            6300);

        AddSequence(
            database,
            "PublishProduct",
            null,
            6400);

        AddSequence(
            database,
            "RemoveExistingProducts",
            null,
            6500);

        AddSequence(
            database,
            "InstallFinalize",
            null,
            6600);

        Console.WriteLine(
            "InstallExecuteSequence created.");
    }

    private static void AddSequence(
        IntPtr database,
        string action,
        string? condition,
        int sequence)
    {
        string conditionSql =
            condition == null
                ? "NULL"
                : SqlQuote(condition);

        ExecuteSql(
            database,
            "INSERT INTO `InstallExecuteSequence` " +
            "(`Action`, `Condition`, `Sequence`) VALUES (" +
            SqlQuote(action) +
            ", " +
            conditionSql +
            ", " +
            sequence.ToString(
                CultureInfo.InvariantCulture) +
            ")");
    }

    // ============================================================
    // DDF
    // ============================================================

    private static void CreateDdfFile(
        string ddfFile,
        string cabDirectory,
        List<ApplicationFile> files)
    {
        var lines =
            new List<string>();

        lines.Add(
            ".OPTION EXPLICIT");

        lines.Add(
            ".Set Cabinet=ON");

        lines.Add(
            ".Set Compress=ON");

        lines.Add(
            ".Set CompressionType=MSZIP");

        lines.Add(
            ".Set UniqueFiles=ON");

        lines.Add(
            ".Set MaxDiskSize=0");

        lines.Add(
            ".Set CabinetNameTemplate=FiservApplication.cab");

        lines.Add(
            $".Set DiskDirectory1={QuoteDdfValue(cabDirectory)}");

        lines.Add(
            ".Set RptFileName=NUL");

        lines.Add(
            ".Set InfFileName=NUL");

        foreach (ApplicationFile file in files)
        {
            string source =
                file.PhysicalPath
                    .Replace(
                        "\"",
                        "\"\"");

            lines.Add(
                $"\"{source}\" {file.FileId}");
        }

        File.WriteAllLines(
            ddfFile,
            lines,
            Encoding.ASCII);

        Console.WriteLine(
            $"DDF: {ddfFile}");
    }

    private static string QuoteDdfValue(
        string value)
    {
        return value.Contains(' ')
            ? $"\"{value}\""
            : value;
    }

    // ============================================================
    // MAKECAB
    // ============================================================

    private static void RunMakeCab(
        string ddfFile,
        string expectedCabFile)
    {
        string makeCab =
            FindMakeCab();

        Console.WriteLine(
            $"makecab.exe: {makeCab}");

        var startInfo =
            new ProcessStartInfo
            {
                FileName = makeCab,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

        startInfo.ArgumentList.Add(
            "/F");

        startInfo.ArgumentList.Add(
            ddfFile);

        using Process process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Could not start makecab.exe.");

        string stdout =
            process.StandardOutput.ReadToEnd();

        string stderr =
            process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            Console.WriteLine(stdout);
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            Console.WriteLine(stderr);
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"makecab.exe failed with exit code " +
                $"{process.ExitCode}.");
        }

        if (!File.Exists(expectedCabFile))
        {
            throw new InvalidOperationException(
                $"Expected CAB was not found: {expectedCabFile}");
        }
    }

    private static string FindMakeCab()
    {
        string? environmentPath =
            Environment.GetEnvironmentVariable(
                "MAKECAB_PATH");

        if (!string.IsNullOrWhiteSpace(
                environmentPath) &&
            File.Exists(environmentPath))
        {
            return environmentPath;
        }

        string systemDirectory =
            Environment.GetFolderPath(
                Environment.SpecialFolder.System);

        string system32 =
            Path.Combine(
                systemDirectory,
                "makecab.exe");

        if (File.Exists(system32))
        {
            return system32;
        }

        string windows =
            Environment.GetFolderPath(
                Environment.SpecialFolder.Windows);

        string sysWow64 =
            Path.Combine(
                windows,
                "SysWOW64",
                "makecab.exe");

        if (File.Exists(sysWow64))
        {
            return sysWow64;
        }

        return "makecab.exe";
    }

    // ============================================================
    // EMBED CAB
    // ============================================================

    private static void EmbedCabinet(
        IntPtr database,
        string cabFile)
    {
        IntPtr view = IntPtr.Zero;
        IntPtr record = IntPtr.Zero;

        try
        {
            uint result =
                MsiDatabaseOpenView(
                    database,
                    "INSERT INTO `_Streams` " +
                    "(`Name`, `Data`) VALUES (?, ?)",
                    out view);

            CheckResult(
                result,
                "MsiDatabaseOpenView(_Streams)");

            record =
                MsiCreateRecord(2);

            if (record == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "MsiCreateRecord failed.");
            }

            result =
                MsiRecordSetString(
                    record,
                    1,
                    "#FiservApplication.cab");

            CheckResult(
                result,
                "MsiRecordSetString(_Streams.Name)");

            result =
                MsiRecordSetStream(
                    record,
                    2,
                    cabFile);

            CheckResult(
                result,
                "MsiRecordSetStream(_Streams.Data)");

            result =
                MsiViewExecute(
                    view,
                    record);

            CheckResult(
                result,
                "MsiViewExecute(_Streams)");
        }
        finally
        {
            if (record != IntPtr.Zero)
            {
                MsiCloseHandle(record);
            }

            if (view != IntPtr.Zero)
            {
                MsiViewClose(view);
            }
        }
    }

    // ============================================================
    // SUMMARY INFORMATION
    // ============================================================

    private static void SetSummaryInformation(
        IntPtr database,
        string packageCode)
    {
        IntPtr summaryInfo = IntPtr.Zero;

        try
        {
            uint result =
                MsiGetSummaryInformation(
                    database,
                    null,
                    20,
                    out summaryInfo);

            CheckResult(
                result,
                "MsiGetSummaryInformation");

            if (summaryInfo == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "Invalid Summary Information handle.");
            }

            SetSummaryInteger(
                summaryInfo,
                PID_CODEPAGE,
                VT_I2,
                1252);

            SetSummaryString(
                summaryInfo,
                PID_TITLE,
                "Installation Database");

            SetSummaryString(
                summaryInfo,
                PID_SUBJECT,
                ProductName);

            SetSummaryString(
                summaryInfo,
                PID_AUTHOR,
                Manufacturer);

            SetSummaryString(
                summaryInfo,
                PID_KEYWORDS,
                "Installer, MSI, Fiserv");

            SetSummaryString(
                summaryInfo,
                PID_COMMENTS,
                "Fiserv Application Installer");

            SetSummaryString(
                summaryInfo,
                PID_TEMPLATE,
                TemplateSummary);

            SetSummaryString(
                summaryInfo,
                PID_REVNUMBER,
                packageCode);

            SetSummaryInteger(
                summaryInfo,
                PID_PAGECOUNT,
                VT_I4,
                200);

            SetSummaryInteger(
                summaryInfo,
                PID_WORDCOUNT,
                VT_I4,
                0);

            SetSummaryString(
                summaryInfo,
                PID_APPNAME,
                "Fiserv MSI Builder");

            SetSummaryInteger(
                summaryInfo,
                PID_SECURITY,
                VT_I4,
                2);

            result =
                MsiSummaryInfoPersist(
                    summaryInfo);

            CheckResult(
                result,
                "MsiSummaryInfoPersist");
        }
        finally
        {
            if (summaryInfo != IntPtr.Zero)
            {
                MsiCloseHandle(summaryInfo);
            }
        }
    }

    private static void SetSummaryString(
        IntPtr summaryInfo,
        uint property,
        string value)
    {
        if (summaryInfo == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Summary information handle is invalid.");
        }

        IntPtr valuePtr = IntPtr.Zero;

        try
        {
            valuePtr =
                Marshal.StringToCoTaskMemAnsi(
                    value);

            uint result =
                MsiSummaryInfoSetPropertyA(
                    summaryInfo,
                    property,
                    VT_LPSTR,
                    0,
                    IntPtr.Zero,
                    valuePtr);

            CheckResult(
                result,
                $"MsiSummaryInfoSetPropertyA({property})");
        }
        finally
        {
            if (valuePtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(
                    valuePtr);
            }
        }
    }

    private static void SetSummaryInteger(
        IntPtr summaryInfo,
        uint property,
        uint dataType,
        int value)
    {
        if (summaryInfo == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Summary information handle is invalid.");
        }

        uint result =
            MsiSummaryInfoSetPropertyA(
                summaryInfo,
                property,
                dataType,
                value,
                IntPtr.Zero,
                IntPtr.Zero);

        CheckResult(
            result,
            $"MsiSummaryInfoSetPropertyA({property})");
    }

    // ============================================================
    // SQL
    // ============================================================

    private static void ExecuteSql(
        IntPtr database,
        string sql)
    {
        IntPtr view = IntPtr.Zero;

        try
        {
            uint result =
                MsiDatabaseOpenView(
                    database,
                    sql,
                    out view);

            CheckResult(
                result,
                "MsiDatabaseOpenView");

            result =
                MsiViewExecute(
                    view,
                    IntPtr.Zero);

            CheckResult(
                result,
                "MsiViewExecute");
        }
        finally
        {
            if (view != IntPtr.Zero)
            {
                MsiViewClose(view);
            }
        }
    }

    // ============================================================
    // SQL ESCAPING
    // ============================================================

    private static string SqlQuote(
        string value)
    {
        return "'" +
               value.Replace(
                   "'",
                   "''") +
               "'";
    }

    // ============================================================
    // GUIDS
    // ============================================================

    private static string CreateProductCode(
        string upgradeCode,
        string version)
    {
        return CreateStableGuid(
            "PRODUCT:" +
            upgradeCode +
            ":" +
            version);
    }

    private static string CreateStableGuid(
        string value)
    {
        using SHA1 sha1 =
            SHA1.Create();

        byte[] hash =
            sha1.ComputeHash(
                Encoding.UTF8.GetBytes(
                    value));

        hash[6] =
            (byte)(
                (hash[6] & 0x0F) |
                0x50);

        hash[8] =
            (byte)(
                (hash[8] & 0x3F) |
                0x80);

        byte[] guidBytes =
            new byte[16];

        Array.Copy(
            hash,
            guidBytes,
            16);

        return
            new Guid(
                guidBytes)
            .ToString()
            .ToUpperInvariant();
    }

    private static string CreateDirectoryId(
        string relativeDirectory)
    {
        string guid =
            CreateStableGuid(
                "DIRECTORY:" +
                relativeDirectory);

        return
            "D" +
            guid
                .Replace(
                    "-",
                    string.Empty)
                .Substring(
                    0,
                    30);
    }

    // ============================================================
    // ERROR HANDLING
    // ============================================================

    private static void CheckResult(
        uint result,
        string operation)
    {
        if (result != ERROR_SUCCESS)
        {
            throw new InvalidOperationException(
                $"{operation} failed. " +
                $"Windows Installer error code: {result}.");
        }
    }

    private static void PrintError(
        Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine("===============================================");
        Console.WriteLine(" ERROR");
        Console.WriteLine("===============================================");
        Console.WriteLine();
        Console.WriteLine(ex.ToString());
        Console.WriteLine();
    }

    // ============================================================
    // MODEL
    // ============================================================

    private sealed class ApplicationFile
    {
        public string PhysicalPath { get; set; }
            = string.Empty;

        public string RelativePath { get; set; }
            = string.Empty;

        public string RelativeDirectory { get; set; }
            = string.Empty;

        public string FileName { get; set; }
            = string.Empty;

        public string FileId { get; set; }
            = string.Empty;

        public string ComponentId { get; set; }
            = string.Empty;

        public string ComponentGuid { get; set; }
            = string.Empty;

        public int Sequence { get; set; }

        public long FileSize { get; set; }
    }
}
