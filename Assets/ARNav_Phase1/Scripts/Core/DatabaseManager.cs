using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager I;
    public string dbFileName = "nexpath.db";
    public bool reseedOnStart = false;
    public string DbPath { get; private set; }
    public bool IsReady { get; private set; }
    public bool HasNativeSqlite { get; private set; }

    // Simple native connection handle wrapper
    public class NativeConn : IDisposable
    {
        public IntPtr Db;
        public bool Opened;
        public NativeConn(string path)
        {
            var rc = SqliteNative.sqlite3_open(path, out Db);
            Opened = rc == SqliteNative.SQLITE_OK;
            if (!Opened)
                throw new Exception($"[SQLite] open failed (rc={rc}): {MarshalErrmsg(Db)}");
        }
        static string MarshalErrmsg(IntPtr db)
        {
            var ptr = SqliteNative.sqlite3_errmsg(db);
            return ptr == IntPtr.Zero ? "(unknown)" : Marshal.PtrToStringAnsi(ptr);
        }
        public void Exec(string sql)
        {
            var rc = SqliteNative.sqlite3_exec(Db, sql, IntPtr.Zero, IntPtr.Zero, out IntPtr errMsg);
            if (rc != SqliteNative.SQLITE_OK)
            {
                string msg = errMsg == IntPtr.Zero ? "(unknown)" : Marshal.PtrToStringAnsi(errMsg);
                SqliteNative.sqlite3_free(errMsg);
                throw new Exception($"[SQLite] exec failed: {msg}");
            }
        }
        public void Dispose()
        {
            if (Opened) { SqliteNative.sqlite3_close(Db); Opened = false; }
        }
    }

    void Awake() 
    { 
        I = this; 
        DbPath = Path.Combine(Application.persistentDataPath, dbFileName); 
    }

    void Start()
    {
        try
        {
            HasNativeSqlite = true;
            EnsureDatabase();
            IsReady = true;
            Debug.Log("[DB] Native SQLite ready. nodes=" + Count("Navigation_Nodes") + " edges=" + Count("Navigation_Edges") + " locs=" + Count("Location") + " path=" + DbPath);
        }
        catch (Exception ex)
        {
            HasNativeSqlite = false;
            IsReady = true;
            Debug.LogWarning($"[DB] Native SQLite not available on this platform ({ex.GetType().Name}: {ex.Message}). Operating in seamless in-memory fallback mode.");
        }
    }

    void EnsureDatabase()
    {
        if (!File.Exists(DbPath)) CreateAndSeed();
        else if (reseedOnStart) { File.Delete(DbPath); CreateAndSeed(); }
        else 
        {
            Exec(SchemaSQL);
            using (var conn = OpenConn())
            {
                if (conn != null) DbSeeder.SeedLocations(conn);
            }
        }
    }

    public NativeConn OpenConn() 
    { 
        if (!HasNativeSqlite) return null;
        return new NativeConn(DbPath); 
    }

    void Exec(string sql)
    {
        var parts = sql.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        using (var conn = OpenConn())
        {
            if (conn == null) return;
            foreach (var p in parts)
            {
                var s = p.Trim();
                if (s.Length == 0) continue;
                conn.Exec(s);
            }
        }
    }

    int Count(string t)
    {
        try
        {
            using (var conn = OpenConn())
            {
                if (conn == null) return -1;
                IntPtr stmt;
                var rc = SqliteNative.sqlite3_prepare_v2(conn.Db, "SELECT COUNT(*) FROM " + t + ";", -1, out stmt, IntPtr.Zero);
                if (rc != SqliteNative.SQLITE_OK) return -1;
                int result = -1;
                if (SqliteNative.sqlite3_step(stmt) == SqliteNative.SQLITE_ROW)
                    result = SqliteNative.sqlite3_column_int(stmt, 0);
                SqliteNative.sqlite3_finalize(stmt);
                return result;
            }
        }
        catch { return -1; }
    }

    public static string SchemaSQL => @"
CREATE TABLE IF NOT EXISTS Navigation_Nodes (
  node_id INTEGER PRIMARY KEY AUTOINCREMENT,
  node_name VARCHAR(100) NOT NULL UNIQUE,
  x_coordinate DECIMAL(9,6) NOT NULL,
  y_coordinate DECIMAL(9,6) NOT NULL,
  z_coordinate DECIMAL(9,6) NOT NULL,
  node_type VARCHAR(50) NOT NULL DEFAULT 'junction',
  floor INTEGER NOT NULL DEFAULT 1,
  authoritative_area VARCHAR(16) NOT NULL DEFAULT 'AT_01',
  is_transition INTEGER NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS Navigation_Edges (
  edge_id INTEGER PRIMARY KEY AUTOINCREMENT,
  start_node_id INTEGER NOT NULL,
  end_node_id INTEGER NOT NULL,
  distance DECIMAL(9,6) NOT NULL,
  is_walkable INTEGER NOT NULL DEFAULT 1,
  is_stairs INTEGER NOT NULL DEFAULT 0,
  is_elevator INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS idx_edges_start ON Navigation_Edges(start_node_id);
CREATE TABLE IF NOT EXISTS Location (
  location_id INTEGER PRIMARY KEY AUTOINCREMENT,
  name VARCHAR(255) NOT NULL,
  category VARCHAR(50) NOT NULL,
  description TEXT,
  node_id INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_location_cat ON Location(category);
CREATE INDEX IF NOT EXISTS idx_location_name ON Location(name);
";
    void CreateAndSeed()
    {
        File.WriteAllBytes(DbPath, new byte[0]); // ensure empty file exists
        Exec(SchemaSQL);
        using (var conn = OpenConn())
        {
            if (conn != null) DbSeeder.SeedAll(conn);
        }
        Debug.Log("[DB] Seeded OK");
    }
}
