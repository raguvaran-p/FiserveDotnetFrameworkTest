using System;
using System.IO;
using System.Runtime.InteropServices;

internal class Program
{
    private const int MSIDBOPEN_CREATE = 3;

    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    private static extern uint MsiOpenDatabase(
        string szDatabasePath,
        int szPersist,
        out IntPtr phDatabase);

    [DllImport("msi.dll")]
    private static extern uint MsiCloseHandle(
        IntPtr hAny);

    [DllImport("msi.dll")]
    private static extern uint MsiDatabaseCommit(
        IntPtr hDatabase);

    static int Main()
    {
        Console.WriteLine("======================================");
        Console.WriteLine(" Windows Installer API Test");
        Console.WriteLine("======================================");

        string testMsi = Path.Combine(
            Path.GetTempPath(),
            "MsiBuilderTest.msi");

        Console.WriteLine();
        Console.WriteLine($"Test MSI: {testMsi}");

        if (File.Exists(testMsi))
        {
            File.Delete(testMsi);
        }

        IntPtr database = IntPtr.Zero;

        try
        {
            uint result = MsiOpenDatabase(
                testMsi,
                MSIDBOPEN_CREATE,
                out database);

            if (result != 0)
            {
                Console.WriteLine(
                    $"MsiOpenDatabase failed. Error: {result}");

                return 1;
            }

            Console.WriteLine(
                "MSI database opened successfully.");

            result = MsiDatabaseCommit(database);

            if (result != 0)
            {
                Console.WriteLine(
                    $"MsiDatabaseCommit failed. Error: {result}");

                return 1;
            }

            Console.WriteLine(
                "MSI database committed successfully.");

            if (!File.Exists(testMsi))
            {
                Console.WriteLine(
                    "ERROR: MSI database file was not created.");

                return 1;
            }

            FileInfo info = new FileInfo(testMsi);

            Console.WriteLine();
            Console.WriteLine("SUCCESS");
            Console.WriteLine($"File: {info.FullName}");
            Console.WriteLine($"Size: {info.Length} bytes");

            return 0;
        }
        finally
        {
            if (database != IntPtr.Zero)
            {
                MsiCloseHandle(database);
            }
        }
    }
}
