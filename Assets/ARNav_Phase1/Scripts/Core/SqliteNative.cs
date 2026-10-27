using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Minimal P/Invoke wrapper over the sqlite3 C API.
/// Windows Editor: loads sqlite3.dll from Assets/Plugins/x86_64/ (already staged).
/// Android: uses the system libsqlite.so (present on every Android device).
/// iOS: statically links against system libsqlite3.dylib via __Internal.
/// No Mono.Data.Sqlite / System.Data assembly needed — no IL2CPP stripping issues.
/// </summary>
public static class SqliteNative
{
    public const int SQLITE_OK = 0;
    public const int SQLITE_ROW = 100;
    public const int SQLITE_DONE = 101;
    public const int SQLITE_TRANSIENT = -1;

#if UNITY_IOS && !UNITY_EDITOR
    private const string LibName = "__Internal";
#else
    private const string LibName = "sqlite3";
#endif

    [DllImport(LibName)]
    public static extern int sqlite3_open(string filename, out IntPtr db);

    [DllImport(LibName)]
    public static extern int sqlite3_close(IntPtr db);

    [DllImport(LibName)]
    public static extern int sqlite3_exec(IntPtr db, string sql, IntPtr callback, IntPtr arg, out IntPtr errMsg);

    [DllImport(LibName)]
    public static extern int sqlite3_prepare_v2(IntPtr db, string sql, int nByte, out IntPtr stmt, IntPtr tail);

    [DllImport(LibName)]
    public static extern int sqlite3_step(IntPtr stmt);

    [DllImport(LibName)]
    public static extern int sqlite3_column_count(IntPtr stmt);

    [DllImport(LibName)]
    public static extern IntPtr sqlite3_column_text(IntPtr stmt, int col);

    [DllImport(LibName)]
    public static extern int sqlite3_column_int(IntPtr stmt, int col);

    [DllImport(LibName)]
    public static extern double sqlite3_column_double(IntPtr stmt, int col);

    [DllImport(LibName)]
    public static extern IntPtr sqlite3_column_name(IntPtr stmt, int col);

    [DllImport(LibName)]
    public static extern int sqlite3_finalize(IntPtr stmt);

    [DllImport(LibName)]
    public static extern IntPtr sqlite3_errmsg(IntPtr db);

    [DllImport(LibName)]
    public static extern long sqlite3_last_insert_rowid(IntPtr db);

    [DllImport(LibName)]
    public static extern int sqlite3_bind_text(IntPtr stmt, int index, string text, int n, IntPtr destructor);

    [DllImport(LibName)]
    public static extern int sqlite3_bind_int(IntPtr stmt, int index, int value);

    [DllImport(LibName)]
    public static extern int sqlite3_bind_double(IntPtr stmt, int index, double value);

    [DllImport(LibName)]
    public static extern void sqlite3_free(IntPtr ptr);

    public static string GetColumnText(IntPtr stmt, int col)
    {
        var ptr = sqlite3_column_text(stmt, col);
        return ptr == IntPtr.Zero ? "" : Marshal.PtrToStringAnsi(ptr);
    }

    public static string GetColumnName(IntPtr stmt, int col)
    {
        var ptr = sqlite3_column_name(stmt, col);
        return ptr == IntPtr.Zero ? "" : Marshal.PtrToStringAnsi(ptr);
    }

    public static void Check(int rc, IntPtr db, string context)
    {
        if (rc != SQLITE_OK && rc != SQLITE_ROW && rc != SQLITE_DONE)
            throw new Exception($"[SQLite] {context} failed (rc={rc}): {Marshal.PtrToStringAnsi(sqlite3_errmsg(db))}");
    }
}
