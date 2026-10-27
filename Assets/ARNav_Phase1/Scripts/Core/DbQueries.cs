using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LocationRow
{
    public int id;
    public string name;
    public string category;
    public string description;
    public string nodeName;
}

public static class DbQueries
{
    // Single source of truth for location descriptions and categories
    public static readonly LocationRow[] StaticLocations = new LocationRow[]
    {
        new LocationRow { id = 1,  name = "Room 101", category = "Office",  description = "Office of Prof Udanor", nodeName = "L1_R101" },
        new LocationRow { id = 2,  name = "Room 100", category = "Office",  description = "Ground floor office", nodeName = "L1_R100" },
        new LocationRow { id = 3,  name = "Room 103", category = "Office",  description = "Ground floor office", nodeName = "L1_R103" },
        new LocationRow { id = 4,  name = "Room 104", category = "Office",  description = "Ground floor office", nodeName = "L1_R104" },
        new LocationRow { id = 5,  name = "Room 105", category = "Office",  description = "Ground floor office", nodeName = "L1_R105" },
        new LocationRow { id = 6,  name = "Room 106", category = "Office", description = "Office Dr Ezeora",       nodeName = "L1_R106" },
        new LocationRow { id = 7,  name = "Room 107", category = "Classroom",      description = "Ground floor office",    nodeName = "L1_R107" },
        new LocationRow { id = 8,  name = "Room 108", category = "Classroom",      description = "Ground floor office",    nodeName = "L1_R108" },
        new LocationRow { id = 9,  name = "Room 109", category = "Classroom",      description = "Ground floor office",    nodeName = "L1_R109" },
        new LocationRow { id = 10, name = "Room 200", category = "Office",  description = "Office of the HOD",  nodeName = "L2_R200" },
        new LocationRow { id = 11, name = "Room 202", category = "Office",  description = "Office Dr Ezema",  nodeName = "L2_R202" },
        new LocationRow { id = 12, name = "Room 205", category = "Office", description = "Office Dr Ogbene",        nodeName = "L2_R205" },
        new LocationRow { id = 13, name = "Room 206", category = "Laboratory", description = "Upper floor lab",        nodeName = "L2_R206" },
        new LocationRow { id = 14, name = "Room 207", category = "Classroom",      description = "Seminar room",     nodeName = "L2_R207" },
        new LocationRow { id = 15, name = "Room 208", category = "Laboratory",      description = "Upper floor lab",     nodeName = "L2_R208" }
    };

    public static List<Node> LoadAllNodes(DatabaseManager db)
    {
        if (db != null && db.HasNativeSqlite)
        {
            try
            {
                var list = new List<Node>();
                using (var conn = db.OpenConn())
                {
                    if (conn != null)
                    {
                        IntPtr stmt;
                        var rc = SqliteNative.sqlite3_prepare_v2(conn.Db,
                            "SELECT node_name,x_coordinate,y_coordinate,z_coordinate,floor,authoritative_area,is_transition FROM Navigation_Nodes;",
                            -1, out stmt, IntPtr.Zero);
                        if (rc == SqliteNative.SQLITE_OK)
                        {
                            while (SqliteNative.sqlite3_step(stmt) == SqliteNative.SQLITE_ROW)
                            {
                                string name = SqliteNative.GetColumnText(stmt, 0);
                                float x = (float)SqliteNative.sqlite3_column_double(stmt, 1);
                                float y = (float)SqliteNative.sqlite3_column_double(stmt, 2);
                                float z = (float)SqliteNative.sqlite3_column_double(stmt, 3);
                                int floor = SqliteNative.sqlite3_column_int(stmt, 4);
                                string area = SqliteNative.GetColumnText(stmt, 5);
                                int trans = SqliteNative.sqlite3_column_int(stmt, 6);
                                list.Add(new Node(name, new Vector3(x, y, z), floor, area, trans == 1));
                            }
                            SqliteNative.sqlite3_finalize(stmt);
                            if (list.Count > 0) return list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DbQueries] Native node load failed: {ex.Message}. Falling back to seed data.");
            }
        }

        // Fallback directly from DbSeedData arrays
        var fallbackNodes = new List<Node>();
        for (int i = 0; i < DbSeedData.Names.Length; i++)
        {
            fallbackNodes.Add(new Node(
                DbSeedData.Names[i],
                new Vector3(DbSeedData.Xs[i], DbSeedData.Ys[i], DbSeedData.Zs[i]),
                DbSeedData.Floors[i],
                DbSeedData.Areas[i],
                DbSeedData.Trans[i] == 1
            ));
        }
        return fallbackNodes;
    }

    public static List<Edge> LoadAllEdges(DatabaseManager db)
    {
        if (db != null && db.HasNativeSqlite)
        {
            try
            {
                var list = new List<Edge>();
                using (var conn = db.OpenConn())
                {
                    if (conn != null)
                    {
                        IntPtr stmt;
                        var rc = SqliteNative.sqlite3_prepare_v2(conn.Db,
                            "SELECT s.node_name, e.node_name, j.distance, j.is_stairs, j.is_elevator FROM Navigation_Edges j " +
                            "JOIN Navigation_Nodes s ON s.node_id=j.start_node_id JOIN Navigation_Nodes e ON e.node_id=j.end_node_id WHERE j.is_walkable=1;",
                            -1, out stmt, IntPtr.Zero);
                        if (rc == SqliteNative.SQLITE_OK)
                        {
                            while (SqliteNative.sqlite3_step(stmt) == SqliteNative.SQLITE_ROW)
                            {
                                string from = SqliteNative.GetColumnText(stmt, 0);
                                string to = SqliteNative.GetColumnText(stmt, 1);
                                float dist = (float)SqliteNative.sqlite3_column_double(stmt, 2);
                                int stairs = SqliteNative.sqlite3_column_int(stmt, 3);
                                int elev = SqliteNative.sqlite3_column_int(stmt, 4);
                                list.Add(new Edge(from, to, dist, stairs == 1, elev == 1));
                            }
                            SqliteNative.sqlite3_finalize(stmt);
                            if (list.Count > 0) return list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DbQueries] Native edge load failed: {ex.Message}. Falling back.");
            }
        }

        return new List<Edge>();
    }

    public static List<LocationRow> Search(DatabaseManager db, string qq, string cat)
    {
        if (qq == null) qq = "";
        if (cat == null) cat = "";

        if (db != null && db.HasNativeSqlite)
        {
            try
            {
                var list = new List<LocationRow>();
                string likePattern = "%" + qq + "%";
                using (var conn = db.OpenConn())
                {
                    if (conn != null)
                    {
                        IntPtr stmt;
                        var sql = "SELECT l.location_id, l.name, l.category, l.description, n.node_name FROM Location l " +
                                  "JOIN Navigation_Nodes n ON n.node_id=l.node_id WHERE l.name LIKE ? AND (l.category=? OR ?='') ORDER BY l.name LIMIT 50;";
                        var rc = SqliteNative.sqlite3_prepare_v2(conn.Db, sql, -1, out stmt, IntPtr.Zero);
                        if (rc == SqliteNative.SQLITE_OK)
                        {
                            SqliteNative.sqlite3_bind_text(stmt, 1, likePattern, likePattern.Length, (IntPtr)SqliteNative.SQLITE_TRANSIENT);
                            SqliteNative.sqlite3_bind_text(stmt, 2, cat, cat.Length, (IntPtr)SqliteNative.SQLITE_TRANSIENT);
                            SqliteNative.sqlite3_bind_text(stmt, 3, cat, cat.Length, (IntPtr)SqliteNative.SQLITE_TRANSIENT);
                            while (SqliteNative.sqlite3_step(stmt) == SqliteNative.SQLITE_ROW)
                            {
                                int id = SqliteNative.sqlite3_column_int(stmt, 0);
                                string name = SqliteNative.GetColumnText(stmt, 1);
                                string category = SqliteNative.GetColumnText(stmt, 2);
                                string desc = SqliteNative.GetColumnText(stmt, 3);
                                string nodeName = SqliteNative.GetColumnText(stmt, 4);
                                list.Add(new LocationRow { id = id, name = name, category = category, description = desc, nodeName = nodeName });
                            }
                            SqliteNative.sqlite3_finalize(stmt);
                            if (list.Count > 0) return list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DbQueries] Native search failed ({ex.Message}), falling back to in-memory dataset.");
            }
        }

        // In-memory fallback
        var results = new List<LocationRow>();
        string qLower = qq.Trim().ToLowerInvariant();
        string catLower = cat.Trim().ToLowerInvariant();

        foreach (var loc in StaticLocations)
        {
            bool matchesQuery = string.IsNullOrEmpty(qLower) ||
                                loc.name.ToLowerInvariant().Contains(qLower) ||
                                loc.description.ToLowerInvariant().Contains(qLower);

            bool matchesCat = string.IsNullOrEmpty(catLower) ||
                              loc.category.ToLowerInvariant().Equals(catLower);

            if (matchesQuery && matchesCat)
            {
                results.Add(loc);
            }
        }
        return results;
    }
}
