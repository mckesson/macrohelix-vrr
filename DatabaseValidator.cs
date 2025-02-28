using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VRR_Inbound_File_Generator
{
    public class DatabaseValidator
    {
        private readonly DBHelper _dbHelper;
        private readonly ILogger _logger;
        private readonly Dictionary<string, HashSet<string>> _validationCache;

        public DatabaseValidator(DBHelper dbHelper, ILogger logger)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validationCache = new Dictionary<string, HashSet<string>>();
        }

        public async Task<ValidationResult> ValidatePIDAsync(string pid, string chainAbbrev)
        {
            var result = new ValidationResult();

            try
            {
                // Check cache first for performance
                string cacheKey = $"PID_{chainAbbrev}";
                if (!_validationCache.ContainsKey(cacheKey))
                {
                    _logger.LogInfo($"Fetching valid PIDs for chain {chainAbbrev} from database");

                    // Query to get valid PIDs for the specified chain
                    string query = @"
                        SELECT pa.PID
                        FROM ArchitectMain.dbo.PharmacyAccounts pa
                        INNER JOIN ArchitectMain.dbo.Pharmacy p ON p.VID = pa.VID
                        INNER JOIN vrr.ChainStoreGroupAssignments csga ON csga.ChainID = p.ChainID
                        INNER JOIN vrr.ChainStoreGroups csg ON csg.CSGID = csga.CSGID
                        WHERE csg.ChainFileAbbrev = @ChainAbbrev"; 

                    var parameters = new Dictionary<string, object>
                    {
                        { "@ChainAbbrev", chainAbbrev }
                    };

                    var dataTable = await _dbHelper.ExecuteQueryAsync(query, parameters);

                    // Cache the results for future validations
                    var validPIDs = new HashSet<string>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        validPIDs.Add(row["PID"].ToString());
                    }

                    _validationCache[cacheKey] = validPIDs;
                    _logger.LogInfo($"Cached {validPIDs.Count} PIDs for chain {chainAbbrev}");
                }

                // Check if the PID is valid for this chain
                if (!_validationCache[cacheKey].Contains(pid))
                {
                    result.AddError($"PID {pid} is not valid for chain {chainAbbrev}");
                    _logger.LogWarning($"PID validation failed: {pid} is not valid for chain {chainAbbrev}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating PID: {ex.Message}");
                result.AddError($"Database error validating PID: {ex.Message}");
            }
            return result;
        }
        public async Task<ValidationResult> ValidateNDCAsync(string ndc)
        {
            var result = new ValidationResult();

            try
            {
                // Check cache first for performance
                string cacheKey ="NDC_Valid";
                if (!_validationCache.ContainsKey(cacheKey))
                {
                    _logger.LogInfo($"Fetching valid NDCs from database");

                    // Query to get valid PIDs for the specified chain
                    string query = @"
                        SELECT DISTINCT a.NDC
                        FROM Architect.dbo.Accumulations a
                        WHERE a.AccountType = '340B'
                        AND a.NDC IS NOT NULL";

                    var dataTable = await _dbHelper.ExecuteQueryAsync(query);

                    // Cache the results for future validations
                    var validNDCs = new HashSet<string>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        validNDCs.Add(row["NDC"].ToString());
                    }

                    _validationCache[cacheKey] = validNDCs;
                    _logger.LogInfo($"Cached {validNDCs.Count} valid NDCs");
                }

                // Check if the PID is valid (active, not discontinued)
                if (!_validationCache[cacheKey].Contains(ndc))
                {
                    result.AddError($"NDC {ndc} is not valid NDCs or discontinued");
                    _logger.LogWarning($"NDC validation failed: {ndc} is not valid or discontinued");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating NDC: {ex.Message}");
                result.AddError($"Database error validating NDC: {ex.Message}");
            }
            return result;
        }

        public async Task<ValidationResult> ValidateHIDAsync(string hid, string chainAbbrev)
        {
            var result = new ValidationResult();

            try
            {
                // Check cache first for performance
                string cacheKey = $"HID_{chainAbbrev}";
                if (!_validationCache.ContainsKey(cacheKey))
                {
                    _logger.LogInfo($"Fetching valid HIDs for chain {chainAbbrev} from database");

                    // Query to get valid PIDs for the specified chain
                    string query = @"
                        SELECT DISTINCT p.HID
                        FROM ArchitectMain.dbo.Pharmacy p
                        INNER JOIN vrr.ChainStoreGroupAssignments csga ON csga.ChainID = p.ChainID
                        INNER JOIN vrr.ChainStoreGroups csg ON csg.CSGID = csga.CSGID
                        WHERE csg.ChainFileAbbrev = @ChainAbbrev
                        AND p.HID IS NOT NULL";

                    var parameters = new Dictionary<string, object>
                    {
                        { "@ChainAbbrev", chainAbbrev }
                    };

                    var dataTable = await _dbHelper.ExecuteQueryAsync(query, parameters);

                    // Cache the results for future validations
                    var validHIDs = new HashSet<string>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        validHIDs.Add(row["HID"].ToString());
                    }

                    _validationCache[cacheKey] = validHIDs;
                    _logger.LogInfo($"Cached {validHIDs.Count} HIDs for chain {chainAbbrev}");
                }

                // Check if the PID is valid for this chain
                if (!_validationCache[cacheKey].Contains(hid))
                {
                    result.AddError($"HID {hid} is not valid for chain {chainAbbrev}");
                    _logger.LogWarning($"HID validation failed: {hid} is not valid for chain {chainAbbrev}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating HID: {ex.Message}");
                result.AddError($"Database error validating HID: {ex.Message}");
            }
            return result;
        }

        public async Task<ValidationResult> ValidateRequestExecutionIDAsync(string requestExecutionID)
        {
            var result = new ValidationResult();

            try
            {
                _logger.LogInfo($"Validating RequestExecutionID {requestExecutionID}");

                string query = @"
                    SELECT COUNT(*) AS Count
                    FROM vrr.OutboundFile
                    WHERE RequestExecutionID = @RequestExecutionID";

                var parameters = new Dictionary<string, object>
                {
                    { "@RequestExecutionID", requestExecutionID }
                };

                var dataTable = await _dbHelper.ExecuteQueryAsync(query, parameters);

                if (dataTable.Rows.Count > 0)
                {
                    int count = Convert.ToInt32(dataTable.Rows[0]["Count"]);
                    if (count == 0)
                    {
                        result.AddError($"RequestExecutionID {requestExecutionID} does not exist in the OutbounFile table");
                        _logger.LogWarning($"RequestExecutionID validation failed: {requestExecutionID} does not exist");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating RequestExecutionID: {ex.Message}");
                result.AddError($"Database error validating RequestExecutionID: {ex.Message}");
            }   
            return result;
        }

        public async Task<(bool IsValid, String Value)> GetValidAccountNumberAsync(string chainAbbrev)
        {
            try
            {
                string query = @"
                    SELECT TOP 1 p.AccountNumber
                    FROM ArchitectMain.dbo.Pharmacy p
                    INNER JOIN vrr.ChainStoreGroupAssignments csga ON csga.ChainID = p.ChainID
                    INNER JOIN vrr.ChainStoreGroups csg ON csg.CSGID = csga.CSGID
                    WHERE csg.ChainFileAbbrev = @ChainAbbrev
                    AND p.AccountNumber IS NOT NULL";
                var parameters = new Dictionary<string, object>
                {
                    { "@ChainAbbrev", chainAbbrev }
                };

                var dataTable = await _dbHelper.ExecuteQueryAsync(query, parameters);

                if (dataTable.Rows.Count > 0)
                {
                    return (true, dataTable.Rows[0]["AccountNumber"].ToString());
                }
                return (false, string.Empty);
            }
            catch (Exception ex)  
            {
                _logger.LogError($"Error getting valid account number: {ex.Message}");
                return (false, string.Empty);
            }
        }

        public async Task<(bool IsValid, string Value)> GetValidHIDAsync(string chainAbbrev)
        {
            try
            {
                string query = @"
                        SELECT TOP 1 p.HID
                        FROM ArchitectMain.dbo.Pharmacy p
                        INNER JOIN vrr.ChainStoreGroupAssignments csga ON csga.ChainID = p.ChainID
                        INNER JOIN vrr.ChainStoreGroups csg ON csg.CSGID = csga.CSGID
                        WHERE csg.ChainFileAbbrev = @ChainAbbrev
                        AND p.HID IS NOT NULL";

                var parameters = new Dictionary<string, object>
                {
                    { "@ChainAbbrev", chainAbbrev }
                };

                var dataTable = await _dbHelper.ExecuteQueryAsync(query, parameters);

                if (dataTable.Rows.Count > 0)
                {
                    return (true, dataTable.Rows[0]["HID"].ToString());
                }
                return (false, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting valid HID: {ex.Message}");
                return (false, string.Empty);
            }
        }
    }
}
