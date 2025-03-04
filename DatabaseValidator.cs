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
        public async Task<(bool success, List<Dictionary<string, object>> records, string errorMessage)> GetAllOutboundRecordsAsync(string requestExecutionID)
        {
            try
            {
                string query = @"
                    SELECT *
                    FROM architect.vrr.outboundfile
                    WHERE RequestExecutionID = @RequestExecutionID
                    ORDER BY RecordNumber";

                var parameters = new Dictionary<string, object>
                {
                    { "@RequestExecutionID", requestExecutionID }
                };

                var dataTable = await _dbHelper.ExecuteQueryAsync(query, parameters);
                if (dataTable.Rows.Count == 0)
                {
                    return (false, null, $"No outbound data found for RequestExecutionID {requestExecutionID}");
                }

                // Convert DataTable to List<Dictionary<string, object>>
                var records = new List<Dictionary<string, object>>();
                foreach (DataRow row in dataTable.Rows)
                {
                    var record = new Dictionary<string, object>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        if (row[col] != DBNull.Value)
                        {
                            record[col.ColumnName] = row[col];
                        }
                    }
                    records.Add(record);
                }
                return (true, records, null);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching outbound records: {ex.Message}");
                return (false, null, $"Error fetching outbound records: {ex.Message}");
            }
        }
        public async Task<(bool success, Dictionary<string, object> data, string errorMessage)> FetchOutboundDataAsync(string requestExecutionID)
        {
            try
            {
                var dataTable = await _dbHelper.GetOutboundDataByRequestExecutionIDAsync(requestExecutionID);
                if (dataTable.Rows.Count == 0)
                {
                    return (false, null, $"No outbound data found for RequestExecutionID {requestExecutionID}");
                }

                DataRow row = dataTable.Rows[0];

                var data = new Dictionary<string, object>
                {
                    { "PID", row["PID"] },
                    { "HID", row["HID"] },
                    { "NDC", row["NDC"] },
                    { "Account_Number", row["Account_Number"] },
                    { "Account_Type", row["ChainID"] },
                    { "ChainID", row["ChainID"] },
                    { "RecordCount", dataTable.Rows.Count }
                };
                string chainAbbrev = await GetChainAbbrevFromChainIDAsync(Convert.ToInt32(row["ChainID"]));
                if (!string.IsNullOrEmpty(chainAbbrev))
                {
                    data.Add("ChainAbbrev", chainAbbrev);
                }
                return (true, data, null);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching outbound data: {ex.Message}");
                return (false, null, $"Error fetching outbound data: {ex.Message}");
            }
        }
        private async Task<string> GetChainAbbrevFromChainIDAsync(int chainID)
        {
            try
            {
                string query = @"
                    SELECT csg.ChainFileAbbrev
                    FROM Architect.vrr.ChainStoreGroups csg
                    INNER JOIN Architect.vrr.ChainStoreGroupAssignments csga ON csga.CSGID = csg.csgid
                    WHERE csga.ChainID = @ChainID";

                var parameters = new Dictionary<string, object>
                {
                    { "@ChainID", chainID }
                };

                var dataTable = await _dbHelper.ExecuteQueryAsync(query, parameters);

                if (dataTable.Rows.Count > 0)
                {
                    return dataTable.Rows[0]["ChainFileAbbrev"].ToString();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting chain abbreiation: {ex.Message}");
                return string.Empty;
            }
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
                        INNER JOIN Architect.vrr.ChainStoreGroupAssignments csga ON csga.ChainID = p.ChainID
                        INNER JOIN Architect.vrr.ChainStoreGroups csg ON csg.CSGID = csga.CSGID
                        WHERE csg.ChainFileAbbrev = @ChainAbbrev
                        AND pa.PID = @PID"; 

                    var parameters = new Dictionary<string, object>
                    {
                        { "@ChainAbbrev", chainAbbrev },
                        { "@PID", pid }
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

                    // Query to get valid NDC for the specified chain
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

                // Check if the NDC is valid (active, not discontinued)
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
        public async Task<ValidationResult> ValidatedUOMAsync(string uom)
        {
            var result = new ValidationResult();

            try
            {
                string cachekey = "UOM";
                if (!_validationCache.ContainsKey(cachekey))
                {
                    _logger.LogInfo("Fetching valid UOMs from database");

                    // Query to get valid UOMs
                    string query = @"
                        SELECT DISTINCT UOM
                        FROM ArchitectMain.dbo.PharmacyAccounts
                        WHERE UOM IS NOT NULL";

                    var dataTable = await _dbHelper.ExecuteQueryAsync(query);

                    var validUOMs = new HashSet<string>();
                    foreach(DataRow row in dataTable.Rows)
                    {
                        validUOMs.Add(row["UOM"].ToString());
                    }

                    _validationCache[cachekey] = validUOMs;
                    _logger.LogInfo($"Caches {validUOMs.Count} UOMs");
                }
                if (_validationCache[cachekey].Contains(uom))
                {
                    result.AddError($"UOM {uom} is not valid");
                    _logger.LogWarning($"UOM Validation failed: {uom} not found in PharmacyAccounts table");
                }
                else
                {
                    result.Value = uom;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating UOM: {ex.Message}");
                result.AddError($"database error validating UOM: {ex.Message}");
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
                        INNER JOIN Architect.vrr.ChainStoreGroupAssignments csga ON csga.ChainID = p.ChainID
                        INNER JOIN Architect.vrr.ChainStoreGroups csg ON csg.CSGID = csga.CSGID
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
                    FROM Architect.vrr.OutboundFile
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
                    INNER JOIN Architect.vrr.ChainStoreGroupAssignments csga ON csga.ChainID = p.ChainID
                    INNER JOIN Architect.vrr.ChainStoreGroups csg ON csg.CSGID = csga.CSGID
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
                //return (false, string.Empty);
            }
            catch (Exception ex)  
            {
                _logger.LogError($"Error getting valid account number: {ex.Message}");
            }
            return (false, string.Empty);
        }
        public async Task<ValidationResult> ValidateUOMAsync(string uom)
        {
            var result = new ValidationResult();

            try
            {
                string cacheKey = "UOM";
                if (!_validationCache.ContainsKey(cacheKey))
                {
                    _logger.LogInfo("Fetching valid UOMs from database");

                    string query = @"
                        SELECT DISTINCT UOM
                        FROM ArchitectMain.dbo.PharmacyAccounts
                        WHERE UOM IS NOT NULL";

                    var dataTable = await _dbHelper.ExecuteQueryAsync(query);

                    var validUOMs = new HashSet<string>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        validUOMs.Add(row["UOM"].ToString());
                    }

                    _validationCache[cacheKey] = validUOMs;
                    _logger.LogInfo($"Cached {validUOMs.Count} UOMs");
                }
                if (!_validationCache[cacheKey].Contains(uom))
                {
                    result.AddError($"UOM {uom} is not valid");
                    _logger.LogWarning($"UOM validation failed: {uom} not found in PharmacyAccounts table");
                }
                else
                {
                    result.Value = uom;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating UOM: {ex.Message}");
                result.AddError($"Database error validating UOM: {ex.Message}");
            }
            return result;
        }
        public async Task<(bool IsValid, string Value)> GetValidUOMAsync(string pid)
        {
            try
            {
                string query = @"
                    SELECT TOP 1 pa.UOM
                    FROM ArchitectMain.dbo.PharmacyAccounts pa
                    WHERE pa.PID = @PID
                    AND pa.UOM IS NOT NULL";

                var parameters = new Dictionary<string, object>
                {
                    {"@PID", pid }
                };
                var dataTable = await _dbHelper.ExecuteQueryAsync(query, parameters);

                if (dataTable.Rows.Count > 0 && dataTable.Rows[0]["UOM"] != DBNull.Value)
                {
                    string uom = dataTable.Rows[0]["UOM"].ToString();
                    return (true, uom);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting valid UOM: {ex.Message}");
            }
            return (false, "EA"); // Default to EA (Each) if not found
        }
        public async Task<(bool IsValid, string Status)> GetNDCStatusAsync(string ndc)
        {
            try
            {
                string cacheKey = $"NDC_Status_{ndc}";
                if (_validationCache.ContainsKey(cacheKey) && _validationCache[cacheKey].Count > 0)
                {
                    return (true, _validationCache[cacheKey].First());
                }

                _logger.LogInfo($"Checking NDC status for {ndc} in database");

                string query = @"
                    SELECT TOP 1
                        CASE
                            WHEN EXISTS (
                                SELECT 1
                                FROM vrr.DiscontinuedNDC
                                WHERE NDC = @NDC
                            ) THEN 'D' 
                            ELSE 'A'
                        END AS Status
                    FROM Architect.dbo.Accumulations
                    WHERE NDC = @NDC";
                var parameters = new Dictionary<string, object>
                {
                    { "@NDC", ndc }
                };
                var dataTable = await _dbHelper.ExecuteQueryAsync(query, parameters );

                if (dataTable.Rows.Count > 0)
                {
                    string status = dataTable.Rows[0]["Status"].ToString();

                    var cacheSet = new HashSet<string> { status };
                    _validationCache[cacheKey] = cacheSet;

                    return (true, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking NDC status: {ex.Message}");
            }

            return (false, "A"); // Default to Active if not found or error
        }
        public async Task<(bool IsValid, (string BillingCode, string BillingCodeSubType) Value)> GetValidBillingCodeAsync()
        {
            try
            {
                string cacheKey = "BillingCode";
                if (_validationCache.ContainsKey(cacheKey) && _validationCache[cacheKey].Count > 0)
                {
                    string[] parts = _validationCache[cacheKey].First().Split('|');
                    if (parts.Length == 2)
                    {
                        return (true, (parts[0], parts[1]));
                    }
                }
                _logger.LogInfo("Fetching valid billing code from database");

                string query = @"
                    SELECT TOP 1
                        BillingCode,
                        BillingCodeSubType
                    FROM Architect.dbo.Purchase
                    WHERE BillingCode IS NOT NULL
                    AND BillingCodeSubType IS NOT NULL";

                var dataTable = await _dbHelper.ExecuteQueryAsync(query);

                if (dataTable.Rows.Count > 0)
                {
                    string billingCode = dataTable.Rows[0]["BillingCode"].ToString();
                    string billingCodeSubType = dataTable.Rows[0]["BillingCodeSubType"].ToString();

                    var cacheSet = new HashSet<string> { $"{billingCode}|{billingCodeSubType}" };
                    _validationCache[cacheKey] = cacheSet;

                    return (true, (billingCode, billingCodeSubType));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting valid billing code: {ex.Message}");
            }
            return (false, ("ZP01", "EA"));
        }
        public async Task<(bool IsValid, string Value)> GetValidHIDAsync(string chainAbbrev)
        {
            try
            {
                string query = @"
                        SELECT TOP 1 p.HID
                        FROM ArchitectMain.dbo.Pharmacy p
                        INNER JOIN Architect.vrr.ChainStoreGroupAssignments csga ON csga.ChainID = p.ChainID
                        INNER JOIN Architect.vrr.ChainStoreGroups csg ON csg.CSGID = csga.CSGID
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
        public async Task<ValidationResult> VerifyDatabaseSchemaAsync()
        {
            var result = new ValidationResult();

            try
            {
                string query = @"
                    SELECT
                        CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'vrr' AND TABLE_NAME = 'ChainStoreGroups' THEN 1 ELSE 0 AS HasChainStoreGroups,
                        CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'vrr' AND TABLE_NAME = 'ChainStoreGroupAssignments' THEN 1 ELSE 0 AS HasChainStoreGroupAssignments,
                        CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'vrr' AND TABLE_NAME = 'OutboundFile' THEN 1 ELSE 0 AS HasOutboundFile,
                        CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'vrr' AND TABLE_NAME = 'InboundFile' THEN 1 ELSE 0 AS HasInboundFile,
                        CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'PharmacyAccounts' THEN 1 ELSE 0 AS HasPharmacyAccounts,
                        CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Pharmacy' THEN 1 ELSE 0 AS HasPharmacy,
                        CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Acccumulations' THEN 1 ELSE 0 AS HasAcccumulations";

                var dataTable = await _dbHelper.ExecuteQueryAsync(query);

                if (dataTable.Rows.Count > 0 )
                {
                    DataRow row = dataTable.Rows[0];

                    if (Convert.ToInt32(row["HasChainStoreGroups"]) == 0)
                        result.AddError("Required table Architect.vrr.ChainStoreGroups not found");

                    if (Convert.ToInt32(row["HasChainStoreGroupAssignments"]) == 0)
                        result.AddError("Required table Architect.vrr.ChainStoreGroupAssignments not found");
                    if (Convert.ToInt32(row["HasOutboundFile"]) == 0)
                        result.AddError("Required table Architect.vrr.OutboundFile not found");
                    if (Convert.ToInt32(row["HasInboundFile"]) == 0)
                        result.AddError("Required table Architect.vrr.InboundFile not found");
                    if (Convert.ToInt32(row["HasPharmacyAccounts"]) == 0)
                        result.AddError("Required table dbo.PharmacyAccounts not found");
                    if (Convert.ToInt32(row["HasPharmacy"]) == 0)
                        result.AddError("Required table dbo.Pharmacy not found");
                    if (Convert.ToInt32(row["HasAcccumulations"]) == 0)
                        result.AddError("Required table dbo.Acccumulations not found");
                }
                else
                {
                    result.AddError("Failed to verify database schema");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying database schema: {ex.Message}");
                result.AddError($"Error verifying database schema: {ex.Message}");
            }
            return result;
        }

        public async Task<ValidationResult> VerifyDatabasePermissionAsync()
        {
            var result = new ValidationResult();

            try
            {
                string[] queries = new string[]
                {
                    "SELECT TOP 1 * FROM Architect.vrr.ChainStoreGroups",
                    "SELECT TOP 1 * FROM Architect.vrr.ChainStoreGroupAssignments",
                    "SELECT TOP 1 * FROM Architect.vrr.OutboundFile",
                    "SELECT TOP 1 * FROM Architect.vrr.InboundFile",
                    "SELECT TOP 1 * FROM ArchitectMain.dbo.PharmacyAccounts",
                    "SELECT TOP 1 * FROM ArchitectMain.dbo.Pharmacy",
                    "SELECT TOP 1 * FROM Architect.dbo.Acccumulations"
                };

                foreach(string query in queries)
                {
                    try
                    {
                        await _dbHelper.ExecuteQueryAsync(query);
                    }
                    catch (Exception ex)
                    {
                        result.AddError($"Permission error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error varifying databasse permission:{ex.Message}");
                result.AddError($"Error varifying databasse permission:{ex.Message}");
            }
            return result;
        }
        public void ClearCache()
        {
            _validationCache.Clear();
            _logger.LogInfo("Validation cche cleared");
        }
    }
}
