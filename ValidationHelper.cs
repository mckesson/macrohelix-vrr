using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VRR_Inbound_File_Generator
{
    public class ValidationHelper
    {
        private readonly Dictionary<string, ChainConfiguration> _chainConfigs;
        private readonly DatabaseValidator _databaseValidator;
        // Constructor without database validator for backward compatibility
        public ValidationHelper()
        {
            _chainConfigs = LoadChainConfiguration();
            _databaseValidator = null;
        }

        public ValidationHelper(DatabaseValidator databaseValidator = null)
        {
            _databaseValidator = databaseValidator;
            _chainConfigs = LoadChainConfiguration();
        }
        /// <summary>
        /// Validate the Request Execution ID
        /// </summary>
        /// <param name="requestExecutionID">Request Execution ID to validate</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateRequestExecutionID(string requestExecutionID)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(requestExecutionID))
            {
                result.AddError("Request Execution ID cannot be empty");
                return result;
            }
            else if(requestExecutionID.Length < 5 || requestExecutionID.Length > 500)
            {
                result.AddError($"Request Execution ID {requestExecutionID} must be between 5 and 500 characters");
            }
            return result;

        }
        
       
        /// <summary>
        /// Validate the Chain Abbreviation
        /// </summary>
        /// <param name="chainAbbrev">Chain abbreviation to validate</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateChainAbbrev(string chainAbbrev)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(chainAbbrev))
            {
                result.AddError("Chain Abbreviation cannot be empty");
                return result;
            }

            chainAbbrev = chainAbbrev.Trim().ToUpper();

            if (chainAbbrev != "WMT" && chainAbbrev != "CPH")
            {
                result.AddError("Chain Abbreviation must be either WMT or CPH");
            }
            return result;
        }
        /// <summary>
        /// Validate the PID
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="chainAbbrev"></param>
        /// <returns></returns>
        public ValidationResult ValidatePID(string pid, string chainAbbrev)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(pid))
            {
                result.AddError("PID cannont be empty");
                return result;
            }

            if (!int.TryParse(pid, out _))
            {
                result.AddError("PID must be a number");
                return result;
            }

            var chainConfig = GetChainConfig(chainAbbrev);
            if (pid.Length >= chainConfig.MaxPIDLength)
            {
                result.AddError($"{chainAbbrev} PIDs must be less than {chainConfig.MaxPIDLength} characters");
            }
            return result;
        }

        /// <summary>
        /// Validate the National Drug Code (NDC).
        /// </summary>
        /// <param name="ndc">NDC to validate</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateNDC(string ndc)
        {
            var result = new ValidationResult();
            // Add validation logic here
            if (string.IsNullOrWhiteSpace(ndc))
            {
                result.AddError("NDC cannot be empty");
                return result;
            }

            //ndc = ndc.Replace("-", "").Trim();

            //if (!System.Text.RegularExpressions.Regex.IsMatch(ndc, @"^\d{10, 11}$"))
            //{
            //    result.AddError("NDC must be 10 or 11 digits");
            //}
            return result;
        }

        public ValidationResult ValidateRecordCount(string recordCountStr)
        {
            var result = new ValidationResult();

            if (!int.TryParse(recordCountStr, out int recordCount))
            {
                result.AddError("Record count must be a valid number");
                return result;
            }

            if (recordCount < 0)
            {
                result.AddError("Record count cannot be negative");
            }
            return result;
        }
        public ValidationResult ValidateOutputPath(string outputPath)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                result.AddError("Output path cannot be empty");
                return result;
            }
            return result ;
        }
        public ValidationResult ValidateReasonCode(string reasonCode)
        {
            var result = new ValidationResult() ;
            if (string.IsNullOrWhiteSpace(reasonCode))
            {
                result.AddError($"{reasonCode} is not valid.");
                return result;
            }
            return result ;
        }

#region Database Validation Methods

        public async Task<ValidationResult> ValidatePIDWithDatabaseAsync(string pid, string chainAbbrev)
        {
            // Basic validation first
            var basicResult = ValidatePID(pid, chainAbbrev);
            if (!basicResult.IsValid)
            {
                return basicResult;
            }

            // Database validation if available
            if (_databaseValidator != null)
            {
                return await _databaseValidator.ValidatePIDAsync(pid, chainAbbrev);
            }
            return basicResult;
        }

        public async Task<ValidationResult> ValidateNDCWithDatabaseAsync(string ndc)
        {
            var basicResult = ValidateNDC(ndc);
            if (!basicResult.IsValid)
            {
                return basicResult;
            }
            if (_databaseValidator != null)
            {
                return await _databaseValidator.ValidateNDCAsync(ndc);
            }
            return basicResult;
        }

        public async Task<ValidationResult> ValidateRequestExecutionIDWithDatabaseAsync(string requestExecutionID)
        {
            var basicResult = ValidateRequestExecutionID(requestExecutionID);
            if (!basicResult.IsValid)
            {
                return basicResult;
            }

            if (_databaseValidator != null)
            {
                return await _databaseValidator.ValidateRequestExecutionIDAsync(requestExecutionID);
            }
            return basicResult;
        }

        public async Task<ValidationResult> ValidateHIDDatabaseAsync(string hid, string chainAbbrev)
        {
            var basicResult = new ValidationResult();

            if (string.IsNullOrWhiteSpace(hid))
            {
                basicResult.AddError("HID cannot be empty");
                return basicResult;
            }
            if (_databaseValidator != null)
            {
                return await _databaseValidator.ValidateHIDAsync(hid, chainAbbrev);
            }
            return basicResult;
        }

        public async Task<ValidationResult> ValidateUOMWithDatabaseAsync(string uom)
        {
            var basicResult = new ValidationResult();
            if (string.IsNullOrWhiteSpace(uom))
            {
                basicResult.AddError("UOM cannot be empty");
                return basicResult; 
            }
            if (_databaseValidator != null)
            {
                return await _databaseValidator.ValidateUOMAsync(uom);
            }
            return basicResult;
        }

        public async Task<(bool IsValid, string Value)> GetValidAccountNumberAsync(string chainAbbrev)
        {
            if (_databaseValidator == null)
            {
                return await _databaseValidator.GetValidAccountNumberAsync(chainAbbrev);
            }
            return (false, string.Empty);
        }
        public async Task<(bool IsValid, string Value)> GetValidUOMAsync(string pid)
        {
            if (_databaseValidator != null)
            {
                return await _databaseValidator.GetValidUOMAsync(pid);
            }
            return (false, "EA");
        }
        public async Task<(bool IsValid, string Status)> GetNDCStatusAsync(string ndc)
        {
            if (_databaseValidator != null)
            {
                return await _databaseValidator.GetNDCStatusAsync(ndc);
            }
            return (false, "A"); // Default to Active if not available
        }
        #endregion
        #region Helper Methods
        public ChainConfiguration GetChainConfig(string chainAbbrev)
        {
            if (string.IsNullOrWhiteSpace(chainAbbrev))
            {
                return GetDefaultChainConfig();
            }
            chainAbbrev = chainAbbrev.Trim().ToUpper();
            if (_chainConfigs.TryGetValue(chainAbbrev, out var chainConfig))
            {
                return chainConfig;
            }
            return GetDefaultChainConfig();
        }
        private ChainConfiguration GetDefaultChainConfig()
        {
            return new ChainConfiguration
            {
                MaxPIDLength = 10,
                FileNameFormat = "MH340BVRR_Recon_Daily_Data_NN_Default_{0}.txt",
                DataFilePrefix = "MHOutboundAccumulations_Daily_Data",
                TriggerFilePrefix = "MHOutboundAccumulations_Daily_Trigger"
            };
        }
        private Dictionary<string, ChainConfiguration> LoadChainConfiguration()
        {
            return new Dictionary<string, ChainConfiguration>
            {
                {
                    "WMT",
                    new ChainConfiguration
                    {
                        MaxPIDLength = 10,
                        FileNameFormat = "MH340BVRR_Recon_Daily_Data_NN_WMT_{0}.txt",
                        DataFilePrefix = "MHOutboundAccumulations_Daily_Data",
                        TriggerFilePrefix = "MHOutboundAccumulations_Daily_Trigger"
                    }
                },
                {
                    "CPH",
                    new ChainConfiguration
                    {
                        MaxPIDLength = 10,
                        FileNameFormat = "MH340BVRR_Recon_Daily_Data_NN_CPH_{0}.txt",
                        DataFilePrefix = "MHOutboundAccumulations_Daily_Data",
                        TriggerFilePrefix = "MHOutboundAccumulations_Daily_Trigger"
                    }
                }
            };
        }
        #endregion
    }
}
