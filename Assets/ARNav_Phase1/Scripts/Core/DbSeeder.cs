using System;
using System.Collections.Generic;
using UnityEngine;

public static class DbSeeder
{
    // ─── Node helpers ───
    public static int Nid(DatabaseManager.NativeConn conn, string name)
    {
        IntPtr stmt;
        var rc = SqliteNative.sqlite3_prepare_v2(conn.Db, "SELECT node_id FROM Navigation_Nodes WHERE node_name = ?;", -1, out stmt, IntPtr.Zero);
        if (rc != SqliteNative.SQLITE_OK) throw new Exception("[SQLite] prepare Nid failed");
        SqliteNative.sqlite3_bind_text(stmt, 1, name, name.Length, (IntPtr)SqliteNative.SQLITE_TRANSIENT);
        int id = -1;
        if (SqliteNative.sqlite3_step(stmt) == SqliteNative.SQLITE_ROW)
            id = SqliteNative.sqlite3_column_int(stmt, 0);
        SqliteNative.sqlite3_finalize(stmt);
        return id;
    }

    public static void Exec(DatabaseManager.NativeConn conn, string sql)
    {
        conn.Exec(sql);
    }

    // ─── Edge helpers ───
    public static void E(DatabaseManager.NativeConn conn, Dictionary<string, int> ids, string a, string b, float d, int st)
    {
        if (!ids.ContainsKey(a) || !ids.ContainsKey(b)) return;
        var sql = $"INSERT INTO Navigation_Edges (start_node_id,end_node_id,distance,is_walkable,is_stairs,is_elevator) VALUES ({ids[a]},{ids[b]},{d.ToString(System.Globalization.CultureInfo.InvariantCulture)},1,{st},0);";
        Exec(conn, sql);
    }
    public static void B(DatabaseManager.NativeConn conn, Dictionary<string, int> ids, string a, string b, float d)
    {
        E(conn, ids, a, b, d, 0);
        E(conn, ids, b, a, d, 0);
    }

    public static void SeedLocations(DatabaseManager.NativeConn conn)
    {
        Exec(conn, "DELETE FROM Location;");
        int count = 0;
        foreach (var loc in DbQueries.StaticLocations)
        {
            int nid = Nid(conn, loc.nodeName);
            if (nid > 0)
            {
                string cleanDesc = loc.description.Replace("'", "''");
                string cleanName = loc.name.Replace("'", "''");
                string cleanCat = loc.category.Replace("'", "''");
                var sql = $"INSERT INTO Location (name,category,description,node_id) VALUES ('{cleanName}','{cleanCat}','{cleanDesc}',{nid});";
                Exec(conn, sql);
                count++;
            }
        }
        Debug.Log($"[DB] Locations synced from DbQueries ({count} entries)");
    }

    public static Dictionary<string, int> SeedAll(DatabaseManager.NativeConn conn)
    {
        var ids = new Dictionary<string, int>();
        // Nodes
        for (int i = 0; i < DbSeedData.Names.Length; i++)
        {
            var sql = $"INSERT INTO Navigation_Nodes (node_name,x_coordinate,y_coordinate,z_coordinate,node_type,floor,authoritative_area,is_transition) VALUES ('{DbSeedData.Names[i]}',{DbSeedData.Xs[i].ToString(System.Globalization.CultureInfo.InvariantCulture)},{DbSeedData.Ys[i].ToString(System.Globalization.CultureInfo.InvariantCulture)},{DbSeedData.Zs[i].ToString(System.Globalization.CultureInfo.InvariantCulture)},'{DbSeedData.Types[i]}',{DbSeedData.Floors[i]},'{DbSeedData.Areas[i]}',{DbSeedData.Trans[i]}); SELECT last_insert_rowid();";
            Exec(conn, sql);
            IntPtr stmt;
            var rc = SqliteNative.sqlite3_prepare_v2(conn.Db, "SELECT last_insert_rowid();", -1, out stmt, IntPtr.Zero);
            int id = -1;
            if (rc == SqliteNative.SQLITE_OK && SqliteNative.sqlite3_step(stmt) == SqliteNative.SQLITE_ROW)
                id = SqliteNative.sqlite3_column_int(stmt, 0);
            SqliteNative.sqlite3_finalize(stmt);
            ids[DbSeedData.Names[i]] = id;
        }
        Debug.Log("[DB] nodes=" + DbSeedData.Names.Length);

        // Ground Floor Edges (Synced with DuplexGraph.asset)
        B(conn, ids, "L1_AT01_EXIT", "L1_AT02_ENTRY", 0.59f);
        B(conn, ids, "L1_AT01_EXIT", "L1_CorA", 2.77f);
        B(conn, ids, "L1_AT01_EXIT", "L1_CorC", 1.62f);
        B(conn, ids, "L1_AT01_EXIT", "L1_CorE", 6.21f);
        B(conn, ids, "L1_AT01_EXIT", "L1_T1", 1.13f);
        B(conn, ids, "L1_AT01_EXIT", "L1_T2", 2.21f);
        B(conn, ids, "L1_AT02_ENTRY", "STAIR_L1_BOT", 0.48f);
        B(conn, ids, "L1_CorA", "L1_CorB", 14.01f);
        B(conn, ids, "L1_CorA", "L1_CorC", 3.18f);
        B(conn, ids, "L1_CorA", "L1_CorE", 4.51f);
        B(conn, ids, "L1_CorA", "L1_R107", 3.82f);
        B(conn, ids, "L1_CorA", "L1_R108", 9.75f);
        B(conn, ids, "L1_CorA", "L1_R109", 4.42f);
        B(conn, ids, "L1_CorA", "L1_T2", 1.70f);
        B(conn, ids, "L1_CorB", "L1_R107", 10.23f);
        B(conn, ids, "L1_CorB", "L1_R108", 4.31f);
        B(conn, ids, "L1_CorB", "L1_R109", 9.82f);
        B(conn, ids, "L1_CorC", "L1_CorD", 8.79f);
        B(conn, ids, "L1_CorC", "L1_CorE", 5.26f);
        B(conn, ids, "L1_CorC", "L1_R103", 4.56f);
        B(conn, ids, "L1_CorC", "L1_R104", 4.56f);
        B(conn, ids, "L1_CorC", "L1_R105", 4.56f);
        B(conn, ids, "L1_CorC", "L1_R106", 1.13f);
        B(conn, ids, "L1_CorC", "L1_T1", 1.74f);
        B(conn, ids, "L1_CorD", "L1_R100", 1.55f);
        B(conn, ids, "L1_CorD", "L1_R101", 1.55f);
        B(conn, ids, "L1_CorD", "L1_R103", 4.38f);
        B(conn, ids, "L1_CorD", "L1_R104", 4.38f);
        B(conn, ids, "L1_CorD", "L1_R105", 4.38f);
        B(conn, ids, "L1_CorD", "L1_R106", 7.93f);
        B(conn, ids, "L1_R103", "L1_R106", 3.59f);
        B(conn, ids, "L1_R104", "L1_R106", 3.59f);
        B(conn, ids, "L1_R105", "L1_R106", 3.59f);
        B(conn, ids, "L1_R107", "L1_R108", 5.94f);
        B(conn, ids, "L1_R107", "L1_R109", 1.67f);
        B(conn, ids, "L1_R108", "L1_R109", 5.74f);

        // Stairs
        E(conn, ids, "STAIR_L1_BOT", "STAIR_L2_MID", 5.07f, 1);
        E(conn, ids, "STAIR_L2_MID", "STAIR_L1_BOT", 5.07f, 1);
        E(conn, ids, "STAIR_L2_MID", "STAIR_L2_TOP", 4.59f, 1);
        E(conn, ids, "STAIR_L2_TOP", "STAIR_L2_MID", 4.59f, 1);

        // Upper Floor Edges
        B(conn, ids, "L2_AT02_EXIT", "L2_AT03_ENTRY", 0.66f);
        B(conn, ids, "L2_AT02_EXIT", "STAIR_L2_TOP", 0.47f);
        B(conn, ids, "L2_AT03_ENTRY", "L2_AT03_EXIT", 2.01f);
        B(conn, ids, "L2_AT03_ENTRY", "L2_AT04_ENTRY", 0.78f);
        B(conn, ids, "L2_AT03_ENTRY", "L2_T3", 0.89f);
        B(conn, ids, "L2_AT03_EXIT", "L2_AT04_EXIT", 1.05f);
        B(conn, ids, "L2_AT03_EXIT", "L2_CorA", 0.77f);
        B(conn, ids, "L2_AT03_EXIT", "L2_T3", 1.75f);
        B(conn, ids, "L2_AT04_ENTRY", "L2_AT04_EXIT", 1.97f);
        B(conn, ids, "L2_AT04_ENTRY", "L2_T3", 1.58f);
        B(conn, ids, "L2_AT04_ENTRY", "L2_T4", 1.46f);
        B(conn, ids, "L2_AT04_EXIT", "L2_CorC", 1.37f);
        B(conn, ids, "L2_AT04_EXIT", "L2_T4", 2.20f);
        B(conn, ids, "L2_CorA", "L2_CorB", 14.01f);
        B(conn, ids, "L2_CorA", "L2_R206", 3.82f);
        B(conn, ids, "L2_CorA", "L2_R208", 4.07f);
        B(conn, ids, "L2_CorA", "L2_T3", 1.51f);
        B(conn, ids, "L2_CorB", "L2_R206", 10.23f);
        B(conn, ids, "L2_CorB", "L2_R207", 4.31f);
        B(conn, ids, "L2_CorB", "L2_R208", 10.05f);
        B(conn, ids, "L2_CorC", "L2_CorD", 8.79f);
        B(conn, ids, "L2_CorC", "L2_R202", 8.10f);
        B(conn, ids, "L2_CorC", "L2_R205", 2.72f);
        B(conn, ids, "L2_CorC", "L2_T4", 1.70f);
        B(conn, ids, "L2_CorD", "L2_R200", 1.55f);
        B(conn, ids, "L2_CorD", "L2_R202", 0.86f);
        B(conn, ids, "L2_CorD", "L2_R205", 6.15f);
        B(conn, ids, "L2_R202", "L2_R205", 5.42f);
        B(conn, ids, "L2_R206", "L2_R207", 5.94f);
        B(conn, ids, "L2_R206", "L2_R208", 1.23f);
        B(conn, ids, "L2_R207", "L2_R208", 5.89f);

        Debug.Log("[DB] edges seeded (138 directed edges / 69 pairs)");

        // Locations (Synced directly from DbQueries.StaticLocations)
        SeedLocations(conn);

        return ids;
    }
}
