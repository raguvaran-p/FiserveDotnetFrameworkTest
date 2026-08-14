using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal class Program
{
    private const int MSIDBOPEN_CREATE = 3;
    private const uint ERROR_NO_MORE_ITEMS = 259;

    // ============================================================
    // Windows Installer API
    // ============================================================

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

    // ============================================================
    // Main
    // ============================================================

    static int Main()
    {
        Console.WriteLine("======================================");
        Console.WriteLine(" C# Windows Installer MSI Builder");
        Console.WriteLine(" Step 1 + Step 2 + Step 3 + Step 4");
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
            // ====================================================
            // STEP 1 - Create MSI database
            // ====================================================

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine(" Step 1 - Create MSI Database");
            Console.WriteLine("======================================");

            uint result = MsiOpenDatabase(
                testMsi,
                MSIDBOPEN_CREATE,
                out database);

            CheckResult(
                result,
                "MsiOpenDatabase");

            Console.WriteLine(
                "MSI database opened successfully.");

            // ====================================================
            // STEP 2 - Property table
            // ====================================================

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine(" Step 2 - MSI Property Table");
            Console.WriteLine("======================================");

            CreatePropertyTable(database);

            VerifyProperties(database);

            // ====================================================
            // STEP 3 - Directory table
            // ====================================================

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine(" Step 3 - MSI Directory Table");
            Console.WriteLine("======================================");

            CreateDirectoryTable(database);

            VerifyDirectoryTable(database);

            // ====================================================
            // STEP 4 - Component table
            // ====================================================

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine(" Step 4 - MSI Component Table");
            Console.WriteLine("======================================");

            CreateComponentTable(database);

            VerifyComponentTable(database);

            // ====================================================
            // Commit MSI database
            // ====================================================

            Console.WriteLine();
            Console.WriteLine("Committing MSI database...");

            result = MsiDatabaseCommit(database);

            CheckResult(
                result,
                "MsiDatabaseCommit");

            Console.WriteLine(
                "MSI database committed successfully.");

            // ====================================================
            // Verify file
            // ====================================================

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
            Console.WriteLine("======================================");
            Console.WriteLine(" ERROR");
            Console.WriteLine("======================================");
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

    // ============================================================
    // STEP 2 - Create Property table
    // ============================================================

    private static void CreatePropertyTable(
        IntPtr database)
    {
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
    }

    // ============================================================
    // STEP 2 - Verify Property table
    // ============================================================

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

    // ============================================================
    // STEP 3 - Create Directory table
    // ============================================================

    private static void CreateDirectoryTable(
        IntPtr database)
    {
        string createDirectoryTableSql =
            "CREATE TABLE `Directory` (" +
            "`Directory` CHAR(72) NOT NULL, " +
            "`Directory_Parent` CHAR(72), " +
            "`DefaultDir` CHAR(255) NOT NULL " +
            "PRIMARY KEY `Directory`)";

        Console.WriteLine();
        Console.WriteLine(
            "Creating MSI Directory table...");

        ExecuteSql(
            database,
            createDirectoryTableSql);

        Console.WriteLine(
            "Directory table created successfully.");

        string[] directorySql =
        {
            "INSERT INTO `Directory` " +
            "(`Directory`, `Directory_Parent`, `DefaultDir`) " +
            "VALUES ('TARGETDIR', NULL, 'SourceDir')",

            "INSERT INTO `Directory` " +
            "(`Directory`, `Directory_Parent`, `DefaultDir`) " +
            "VALUES ('INSTALLDIR', 'TARGETDIR', 'Fiserv Application')"
        };

        foreach (string sql in directorySql)
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
            "Directory entries inserted successfully.");
    }

    // ============================================================
    // STEP 3 - Verify Directory table
    // ============================================================

    private static void VerifyDirectoryTable(
        IntPtr database)
    {
        IntPtr view = IntPtr.Zero;

        try
        {
            string sql =
                "SELECT `Directory`, `Directory_Parent`, `DefaultDir` " +
                "FROM `Directory`";

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine(" Verifying MSI Directory table");
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
                    string directory =
                        GetRecordString(record, 1);

                    string parent =
                        GetRecordString(record, 2);

                    string defaultDir =
                        GetRecordString(record, 3);

                    Console.WriteLine(
                        $"{directory} | Parent: {parent} | DefaultDir: {defaultDir}");
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
                "Directory verification successful.");
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
    // STEP 4 - Create Component table
    // ============================================================

    private static void CreateComponentTable(
        IntPtr database)
    {
        string createComponentTableSql =
            "CREATE TABLE `Component` (" +
            "`Component` CHAR(72) NOT NULL, " +
            "`ComponentId` CHAR(38), " +
            "`Directory_` CHAR(72) NOT NULL, " +
            "`Attributes` SHORT NOT NULL " +
            "PRIMARY KEY `Component`)";

        Console.WriteLine();
        Console.WriteLine(
            "Creating MSI Component table...");

        ExecuteSql(
            database,
            createComponentTableSql);

        Console.WriteLine(
            "Component table created successfully.");

        string[] componentSql =
        {
            "INSERT INTO `Component` " +
            "(`Component`, `ComponentId`, `Directory_`, `Attributes`) " +
            "VALUES " +
            "('ApplicationComponent', " +
            "'{33333333-3333-3333-3333-333333333333}', " +
            "'INSTALLDIR', 0)"
        };

        foreach (string sql in componentSql)
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
            "Component entries inserted successfully.");
    }

    // ============================================================
    // STEP 4 - Verify Component table
    // ============================================================

    private static void VerifyComponentTable(
        IntPtr database)
    {
        IntPtr view = IntPtr.Zero;

        try
        {
            string sql =
                "SELECT `Component`, `ComponentId`, " +
                "`Directory_`, `Attributes` " +
                "FROM `Component`";

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine(" Verifying MSI Component table");
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
                    string component =
                        GetRecordString(record, 1);

                    string componentId =
                        GetRecordString(record, 2);

                    string directory =
                        GetRecordString(record, 3);

                    string attributes =
                        GetRecordString(record, 4);

                    Console.WriteLine(
                        $"{component} | " +
                        $"ComponentId: {componentId} | " +
                        $"Directory: {directory} | " +
                        $"Attributes: {attributes}");
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
                "Component verification successful.");
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
    // Execute MSI SQL
    // ============================================================

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

    // ============================================================
    // Read MSI record field
    // ============================================================

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

    // ============================================================
    // Error handling
    // ============================================================

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
