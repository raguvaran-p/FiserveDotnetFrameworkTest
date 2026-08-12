using System;
using System.Runtime.InteropServices;

internal class Program
{
    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    private static extern uint MsiOpenDatabase(
        string szDatabasePath,
        int szPersist,
        out IntPtr phDatabase);

    [DllImport("msi.dll")]
    private static extern uint MsiCloseHandle(
        IntPtr hAny);

    private const int MSIDBOPEN_CREATE = 3;

    static int Main(string[] args)
    {
        Console.WriteLine("======================================");
        Console.WriteLine(" Windows Installer API Test");
        Console.WriteLine("======================================");

        Console.WriteLine();
        Console.WriteLine("Windows Installer DLL:");
        Console.WriteLine(@"C:\Windows\System32\msi.dll");

        IntPtr database;

        uint result = MsiOpenDatabase(
            ":memory:",
            MSIDBOPEN_CREATE,
            out database);

        if (result != 0)
        {
            Console.WriteLine(
                $"MsiOpenDatabase failed. Error: {result}");

            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("Windows Installer API is available.");
        Console.WriteLine("MSI database opened successfully.");

        MsiCloseHandle(database);

        Console.WriteLine();
        Console.WriteLine("Stage 3 completed successfully.");

        return 0;
    }
}
