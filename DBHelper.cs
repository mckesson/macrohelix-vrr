using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRR_Inbound_File_Generator
{
    public class DBHelper : IDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;
        private bool _disposed;

        public DBHelper(string connectionString, ILogger logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<DataTable> GetOutboundDataByRequestExecutionIDAsync(string requestExecutionID)
        {
            string query = @"
                SELECT TOP 100 * 
                FROM architect.vrr.outboundfile
                WHERE RequestExecutionID = @RequestExecutionID
                ORDER BY datecreated DESC";

            var parameters = new Dictionary<string, object>
            {
                { "@RequestExecutionID", requestExecutionID }
            };
            return await ExecuteQueryAsync(query, parameters);
        }
        public async Task<(bool IsSuccessful, string Message)> TestConnectionAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // If we get here, connection was successful
                    _logger.LogInfo("Database connection test successful");
                    return (true, "Connection successful");
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = $"SQL Error: {ex.Number} - {ex.Message}";
                _logger.LogError(errorMessage);

                // Provide specific error message based on SQL error code
                switch (ex.Number)
                {
                    case 4060:
                        return (false, "Invalid Databse specified in connection string");
                    case 18456:
                        return (false, "Authentication failed - check credientials");
                    case 53:
                        return (false, "Server not found - check server name and network connectivity");
                    case 40:
                        return (false, "Connection timeout - Server may be busy or unrechable");
                    default:
                        return (false, $"Database connection error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error testing database connection: {ex.Message}";
                _logger.LogError(errorMessage);
                return (false, errorMessage);
            }
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInfo("Database connection opened successfully");

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                            }
                        }
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database error: {ex.Message}");
                throw;
            }

            return dataTable;
        }

        public async Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInfo("Database connection opened successfully");

                    using (var command = new SqlCommand(query, connection))
                    {
                        AddParameters(command, parameters);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database error: {ex.Message}");
                throw;
            }
        }

        public async Task<object> ExecuteScalarAsync(string query, Dictionary<string, object> parameters = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInfo("Database connection opened successfully");

                    using (var command = new SqlCommand(query, connection))
                    {
                        AddParameters(command, parameters);
                        return await command.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database error: {ex.Message}");
                throw;
            }
        }

        private void AddParameters(SqlCommand command, Dictionary<string, object> parameters)
        {
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(new SqlParameter(parameter.Key, parameter.Value ?? DBNull.Value));
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                }

                // Dispose unmanaged resources here
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
