using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal class Program
{
    private const int MSIDBOPEN_CREATE = 3;
    private const uint ERROR_NO_MORE_ITEMS = 259;

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
    private static extern uint MsiViewFetch(
        IntPtr hView,
        out IntPtr hRecord);

    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    private static extern uint MsiRecordGetString(
        IntPtr hRecord,
        uint iField,
        StringBuilder szValue,
        ref uint pcchValue);

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
        Console.WriteLine(" Step 2 - MSI Property Table");
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

            CheckResult(
                result,
                "MsiOpenDatabase");

            Console.WriteLine(
                "MSI database opened successfully.");

            // Create the standard MSI Property table.
            string createPropertyTableSql =
                "CREATE TABLE `Property` (" +
                "`Property` CHAR(72) NOT NULL, " +
                "`Value` CHAR(0) NOT NULL LOCALIZABLE PRIMARY KEY `Property`)";

            Console.WriteLine();
            Console.WriteLine(
                "Creating MSI Property table...");

            ExecuteSql(
                database,
                createPropertyTableSql);

            Console.WriteLine(
                "Property table created successfully.");

            // Add MSI package properties.
            string[] propertySql =
            {
                "INSERT INTO `Property` (`Property`, `Value`) VALUES ('ProductName', 'Fiserv Application')",

                "INSERT INTO `Property` (`Property`, `Value`) VALUES ('ProductVersion', '1.0.0')",

                "INSERT INTO `Property` (`Property`, `Value`) VALUES ('Manufacturer', 'Fiserv')",

                "INSERT INTO `Property` (`Property`, `Value`) VALUES ('ProductCode', '{11111111-1111-1111-1111-111111111111}')",

                "INSERT INTO `Property` (`Property`, `Value`) VALUES ('UpgradeCode', '{22222222-2222-2222-2222-222222222222}')"
            };

            foreach (string sql in propertySql)
            {
                Console.WriteLine();
                Console.WriteLine("Executing:");
                Console.WriteLine(sql);

                ExecuteSql(
                    database,
                    sql);
            }

            Console.WriteLine();
            Console.WriteLine(
                "MSI properties inserted successfully.");

            // Read the properties back before committing.
            VerifyProperties(database);

            result = MsiDatabaseCommit(database);

            CheckResult(
                result,
                "MsiDatabaseCommit");

            Console.WriteLine();
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

    private static void VerifyProperties(
        IntPtr database)
    {
        IntPtr view = IntPtr.Zero;

        try
        {
            string sql =
                "SELECT `Property`, `Value` FROM `Property`";

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine(" Verifying MSI Property table");
            Console.WriteLine("======================================");

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

            while (true)
            {
                IntPtr record = IntPtr.Zero;

                result = MsiViewFetch(
                    view,
                    out record);

                if (result == ERROR_NO_MORE_ITEMS)
                {
                    break;
                }

                CheckResult(
                    result,
                    "MsiViewFetch");

                try
                {
                    string property =
                        GetRecordString(record, 1);

                    string value =
                        GetRecordString(record, 2);

                    Console.WriteLine(
                        $"{property} = {value}");
                }
                finally
                {
                    if (record != IntPtr.Zero)
                    {
                        MsiCloseHandle(record);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine(
                "Property verification successful.");
        }
        finally
        {
            if (view != IntPtr.Zero)
            {
                MsiViewClose(view);
            }
        }
    }

    private static string GetRecordString(
        IntPtr record,
        uint field)
    {
        uint capacity = 256;

        StringBuilder value =
            new StringBuilder((int)capacity);

        uint result = MsiRecordGetString(
            record,
            field,
            value,
            ref capacity);

        CheckResult(
            result,
            "MsiRecordGetString");

        return value.ToString();
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