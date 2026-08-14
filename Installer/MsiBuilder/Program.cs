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

    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    private static extern uint MsiDatabaseOpenView(
        IntPtr hDatabase,
        string szQuery,
        out IntPtr phView);

    [DllImport("msi.dll")]
    private static extern uint MsiViewExecute(
        IntPtr hView,
        IntPtr hRecord);

    [DllImport("msi.dll")]
    private static extern uint MsiViewClose(
        IntPtr hView);

    [DllImport("msi.dll")]
    private static extern uint MsiCloseHandle(
        IntPtr hAny);

    [DllImport("msi.dll")]
    private static extern uint MsiDatabaseCommit(
        IntPtr hDatabase);

    static int Main()
    {
        Console.WriteLine("======================================");
        Console.WriteLine(" C# Windows Installer MSI Builder");
        Console.WriteLine(" Step 1 - SQL Test");
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

            CheckResult(result, "MsiOpenDatabase");

            Console.WriteLine(
                "MSI database opened successfully.");

            // First SQL test.
            string sql =
                "CREATE TABLE `TestTable` (" +
                "`TestKey` CHAR(72) NOT NULL PRIMARY KEY `TestKey`)";

            Console.WriteLine();
            Console.WriteLine("Executing SQL:");
            Console.WriteLine(sql);

            ExecuteSql(database, sql);

            Console.WriteLine(
                "SQL executed successfully.");

            result = MsiDatabaseCommit(database);

            CheckResult(result, "MsiDatabaseCommit");

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
            Console.WriteLine("======================================");
            Console.WriteLine(" SUCCESS");
            Console.WriteLine("======================================");
            Console.WriteLine($"File: {info.FullName}");
            Console.WriteLine($"Size: {info.Length} bytes");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("ERROR:");
            Console.WriteLine(ex.Message);

            return 1;
        }
        finally
        {
            if (database != IntPtr.Zero)
            {
                MsiCloseHandle(database);
            }
        }
    }

    private static void ExecuteSql(
        IntPtr database,
        string sql)
    {
        IntPtr view = IntPtr.Zero;

        try
        {
            uint result = MsiDatabaseOpenView(
                database,
                sql,
                out view);

            CheckResult(
                result,
                "MsiDatabaseOpenView");

            result = MsiViewExecute(
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

    private static void CheckResult(
        uint result,
        string operation)
    {
        if (result != 0)
        {
            throw new InvalidOperationException(
                $"{operation} failed. Windows Installer error: {result}");
        }
    }

}

}

