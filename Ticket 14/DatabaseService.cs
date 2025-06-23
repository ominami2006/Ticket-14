using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Ticket_14 {
    public class DatabaseService : IDisposable {
        private SqlConnection _connection;
        private string _connectionString;
        public DatabaseService(string connectionString) {
            _connectionString = connectionString;
            _connection = new SqlConnection(_connectionString);
        }
        public void Connect() {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }
        public List<string> GetTableNames() {
            Connect();
            var tables = new List<string>();
            DataTable dt = _connection.GetSchema("Tables", new[] { null, null, null, "BASE TABLE" });
            foreach (DataRow row in dt.Rows) {
                string schema = row["TABLE_SCHEMA"].ToString();
                string name = row["TABLE_NAME"].ToString();
                if (schema != "sys" && schema != "INFORMATION_SCHEMA")
                    tables.Add($"[{schema}].[{name}]");
            }
            return tables.OrderBy(t => t).ToList();
        }
        public DataTable GetTableData(string tableName) {
            var dataTable = new DataTable();
            if (string.IsNullOrEmpty(tableName)) return dataTable;
            Connect();
            using (var adapter = new SqlDataAdapter($"SELECT * FROM {tableName}", _connection)) {
                adapter.Fill(dataTable);
            }
            return dataTable;
        }
        public List<string> GetPrimaryKeys(string tableName) {
            Connect();
            var keys = new List<string>();
            var parts = tableName.Replace("[", "").Replace("]", "").Split('.');
            var schemaName = parts[0];
            var pureTableName = parts[1];
            using (var command = _connection.CreateCommand()) {
                command.CommandText = @"
                    SELECT kcu.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
                      ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                      AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                      AND tc.TABLE_NAME = kcu.TABLE_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                      AND tc.TABLE_SCHEMA = @schemaName
                      AND tc.TABLE_NAME = @tableName;";
                command.Parameters.AddWithValue("@schemaName", schemaName);
                command.Parameters.AddWithValue("@tableName", pureTableName);
                using (var reader = command.ExecuteReader()) {
                    while (reader.Read())
                        keys.Add(reader.GetString(0));
                }
            }
            return keys;
        }
        public int UpdateRow(string tableName, Dictionary<string, object> values, Dictionary<string, object> pkValues) {
            if (values.Count == 0)
                return 0;

            if (pkValues.Count == 0)
                throw new InvalidOperationException("Cannot update a row without a primary key.");

            Connect();
            using (var command = _connection.CreateCommand()) {
                var setClauses = values.Keys.Select(k => $"[{k}] = @set_{k}");
                var whereClauses = pkValues.Keys.Select(k => $"[{k}] = @where_{k}");
                command.CommandText = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {string.Join(" AND ", whereClauses)}";
                foreach (var kvp in values)
                    command.Parameters.AddWithValue($"@set_{kvp.Key}", kvp.Value ?? DBNull.Value);
                foreach (var kvp in pkValues)
                    command.Parameters.AddWithValue($"@where_{kvp.Key}", kvp.Value ?? DBNull.Value);
                return command.ExecuteNonQuery();
            }
        }
        public int InsertRow(string tableName, Dictionary<string, object> values) {
            Connect();
            using (var command = _connection.CreateCommand()) {
                var columns = values.Keys.Select(k => $"[{k}]");
                var parameters = values.Keys.Select(k => $"@{k}");
                command.CommandText = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)})";
                foreach (var kvp in values)
                    command.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                return command.ExecuteNonQuery();
            }
        }
        public int DeleteRow(string tableName, Dictionary<string, object> pkValues) {
            if (pkValues.Count == 0)
                throw new InvalidOperationException("Cannot delete a row without a primary key.");
            Connect();
            using (var command = _connection.CreateCommand())  {
                var whereClauses = pkValues.Keys.Select(k => $"[{k}] = @{k}");
                command.CommandText = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereClauses)}";
                foreach (var kvp in pkValues)
                    command.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                return command.ExecuteNonQuery();
            }
        }
        public void Dispose() {
            if (_connection != null) {
                if (_connection.State != ConnectionState.Closed)
                    _connection.Close();
                _connection.Dispose();
            }
        }
    }
}
