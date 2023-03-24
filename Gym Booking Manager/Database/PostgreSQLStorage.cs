using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Gym_Booking_Manager
{
    // PostgreSQL database implementation.
    internal class PostgreSQLStorage : IDatabase
    {
        private NpgsqlConnection connection;

        public PostgreSQLStorage()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;
            connection = new NpgsqlConnection(connectionString);
            connection.Open();
        }
        public List<T> Read<T>(string? field, string? value)
        {
            string tableName = typeof(T).Name.ToLower();
            string query = $"SELECT * FROM {tableName}";

            if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(value))
            {
                string param = $"@{field.ToLower()}";
                query += $" WHERE {field.ToLower()} = {param}";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue(param, value);
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        List<T> results = new List<T>();
                        while (reader.Read())
                        {
                            T result = Activator.CreateInstance<T>();
                            foreach (var prop in typeof(T).GetProperties())
                            {
                                prop.SetValue(result, reader[prop.Name]);
                            }
                            results.Add(result);
                        }
                        return results;
                    }
                }
            }
            else
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                {
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        List<T> results = new List<T>();
                        while (reader.Read())
                        {
                            T result = Activator.CreateInstance<T>();
                            foreach (var prop in typeof(T).GetProperties())
                            {
                                prop.SetValue(result, reader[prop.Name]);
                            }
                            results.Add(result);
                        }
                        return results;
                    }
                }
            }
        }

        public bool Update<T>(T newEntity, T oldEntity)
        {
            string tableName = typeof(T).Name.ToLower();
            string setClause = string.Join(",", typeof(T).GetProperties().Select(p => $"{p.Name.ToLower()}=@{p.Name.ToLower()}"));

            using (NpgsqlCommand cmd = new NpgsqlCommand($"UPDATE {tableName} SET {setClause} WHERE id=@id", connection))
            {
                foreach (var prop in typeof(T).GetProperties())
                {
                    cmd.Parameters.AddWithValue($"@{prop.Name.ToLower()}", prop.GetValue(newEntity));
                }
                cmd.Parameters.AddWithValue("@id", typeof(T).GetProperty("Id").GetValue(oldEntity));

                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected == 1;
            }
        }

        public bool Delete<T>(T entity)
        {
            string tableName = typeof(T).Name.ToLower();
            int id = (int)typeof(T).GetProperty("Id").GetValue(entity);

            using (NpgsqlCommand cmd = new NpgsqlCommand($"DELETE FROM {tableName} WHERE id=@id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected == 1;
            }
        }
        public bool Create<T>(T entity)
        {
            string tableName = typeof(T).Name.ToLower();
            string fieldNames = string.Join(",", typeof(T).GetProperties().Select(p => p.Name.ToLower()));
            string paramNames = string.Join(",", typeof(T).GetProperties().Select(p => $"@{p.Name.ToLower()}"));

            using (NpgsqlCommand cmd = new NpgsqlCommand($"INSERT INTO {tableName} ({fieldNames}) VALUES ({paramNames})", connection))
            {
                foreach (var prop in typeof(T).GetProperties())
                {
                    cmd.Parameters.AddWithValue($"@{prop.Name.ToLower()}", prop.GetValue(entity));
                }

                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected == 1;
            }
        }
    }
}