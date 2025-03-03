using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Diagnostics;
using Microsoft.Extensions.Primitives;
using System.Windows.Forms;

namespace VRR_Inbound_File_Generator
{
    public class FileGenerator
    {
        private const int MAX_RECORDS_PER_FILE = 200000;
        private const int BATCH_SIZE = 5000;

        private readonly string requestExecutionID;
        private readonly string chainAbbrev;
        private readonly int pid;
        private readonly string ndc;
        private readonly string reasonCode;
        private readonly int recordCount;
        private string outputPath;
        private readonly EnhancedProgressTracker _progressTracker;
        private readonly DatabaseValidator _databaseValidator;
        private readonly Random random;

        //private readonly int[] cachedAccountNumbers;
        //private readonly int[] cachedTotalPkgs;
        //private readonly int[] cachedContrctPharmacyAccountNumbers;
        //private readonly string[] cachedCreditMemoNumbers;
        //private readonly string[] cachedCreditRequestLines;

        private readonly ILogger _logger;
        private List<Dictionary<string, object>> _outboundRecords;
        private bool _useOutboundRecords;

        /// <summary>
        /// Initialize a new instance of the FileGenerator class.
        /// </summary>
        /// <param name="requestExecutionID">Unique ID for the entire file batch</param>
        /// <param name="chainAbbrev">Chain Store abbreviation (WMT or CPH)</param>
        /// <param name="pid"></param>
        public FileGenerator(
            string requestExecutionID,
            string chainAbbrev,
            int pid,
            string ndc,
            string reasonCode,
            int recordCount,
            string outputPath,
            EnhancedProgressTracker progressTracker,
            DatabaseValidator databaseValidator,
            ILogger logger,
            List<Dictionary<string, object>> outboundRecords = null)
        {
            this.requestExecutionID = requestExecutionID;
            this.chainAbbrev = chainAbbrev;
            this.pid = pid;
            this.ndc = ndc;
            this.reasonCode = reasonCode;
            this.recordCount = recordCount;
            this.outputPath = outputPath;
            _progressTracker = progressTracker;
            _databaseValidator = databaseValidator;
            _logger = logger;
            _outboundRecords = outboundRecords;
            _useOutboundRecords = outboundRecords != null && outboundRecords.Count > 0;
            this.random = new Random();

        }

        public async Task GenerateFilesAsync()
        {
            try
            {
                _progressTracker?.Start(recordCount);
                _progressTracker?.UpdateStatus("Starting files generation...");

                string debugMessage = $"Output path: '{outputPath}'";
                MessageBox.Show(debugMessage, "Debug Info");
                try
                {
                    if (!Directory.Exists(outputPath))
                    {
                        _logger?.LogInfo($"Creating output directory: {outputPath}");
                        Directory.CreateDirectory(outputPath);
                    }

                    string testFilePath = Path.Combine(outputPath, $"test_access_3478.tmp");
                    if (IsFileInUse(testFilePath))
                    {
                        throw new IOException($"File {testFilePath} is in use by another process.");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    string altPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VRROutput");
                    _logger?.LogWarning($"Access denied to {outputPath} unauthorized {ex.Message}, Using alternative path: {altPath}");
                    if (!Directory.Exists(altPath))
                    {
                        Directory.CreateDirectory(altPath);
                    }
                    outputPath = altPath;
                }
                catch (IOException ex)
                {
                    _logger?.LogError($"File in use: {ex.Message}");
                    throw;
                }

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    throw new ArgumentException("Output path is empty or null");
                }
                if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
                {
                    _logger?.LogInfo($"Creating output directory: {outputPath}");
                    Directory.CreateDirectory(outputPath);
                }

                // generate data files  
                _progressTracker?.UpdateStatus("Generating data file...");
                List<string> generatedFiles = await GenerateDataFileAsync(outputPath);

                // Generate trigger file
                _progressTracker?.UpdateStatus("Generating trigger file...");
                await GenerateTriggerFileAsync(generatedFiles);

                _progressTracker?.Complete();
                _logger?.LogInfo("File generation completed successfully");
            }
            catch (Exception ex)
            {
                _progressTracker?.Error($"Error generating files: {ex.Message}");
                _logger?.LogError($"Exception in GeneratedFilesAsync: {ex.Message}");
                _logger?.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Generate a data file with records.
        /// </summary>
        /// <returns>The file name of the generated data file</returns>
        public async Task<List<string>> GenerateDataFileAsync(string outputPath)
        {
            outputPath = outputPath.Replace("'", "").Replace("\"", "").Trim();
            List<string> generatedFiles = new List<string>();
            var date = DateTime.Now.ToString("yyyyMMdd");

            string outputDirectory = outputPath;

            // Calculate how many files we'll need 
            int totalFiles = (int)Math.Ceiling(recordCount / (double)MAX_RECORDS_PER_FILE);

            // Create file names upfront
            for (int fileIndex = 0; fileIndex < totalFiles; fileIndex++)
            {
                // Calculate records for this file
                int startRecord = fileIndex * MAX_RECORDS_PER_FILE;
                int recordsForThisFile = Math.Min(MAX_RECORDS_PER_FILE, recordCount - startRecord);

                string fileNumber = (fileIndex +1).ToString("00");
                var dataFileName = $"MH340BVRR_Recon_Daily_Data_{fileNumber}_{chainAbbrev}_{date}.txt";
                string fullFilePath = Path.Combine(outputDirectory, dataFileName);
                generatedFiles.Add(dataFileName);

                _progressTracker.UpdateStatus($"Generating file {fileIndex + 1} of {totalFiles}: {dataFileName}");

                using (FileStream fileStream = new FileStream(fullFilePath, FileMode.Create))
                using (var bufferesStream = new BufferedStream(fileStream, 262144)) // 256KB buffer
                using (var writer = new StreamWriter(bufferesStream, Encoding.UTF8, 262144))
                {
                    writer.WriteLine("RequestExecutionID    340B_ID    PID    Account_Number    Account_Type    NDC    Total_Pkgs    Contract_Pharmacy_Account_Number    " +
                                            "Credit_Request_Type    Credit_Request_Number    Credit_Request_Line    Credit_Memo_Type    Credit_Memo_PO    Credit_Memo_Number    " +
                                            "Credit_Memo_Line    Material_Number    Material_Description    Retail_Price    Credit_Qty    Reference_Invoice_Number    Reference_Invoice_Line    " +
                                            "Reference_Invoice_Date    Covered_Entity_Account_Number    Debit_Request_Type    Debit_Request_Number    Debit_Request_Line    Debit_Memo_Type    " +
                                            "Debit_Memo_PO    Debit_Memo_Number    Debit_Memo_Line    Debit_Qty    Billing_Date    340B_Price    Department_Code    Package_UOM    Alternate_UOM    " +
                                            "Alternate_UOM_Qty    UPC_Number    Retail_Warehouse    Reason_Code    VRR_Message");

                    // use batching for faster writes
                    StringBuilder lineBuilder = new StringBuilder(500);
                    StringBuilder batchBuilder = new StringBuilder(BATCH_SIZE * 500);
                    int updateFrequency = Math.Max(1, recordsForThisFile / 100);

                    for (int batchStart = 0; batchStart < recordsForThisFile; batchStart += BATCH_SIZE)
                    {
                        batchBuilder.Clear();
                        int batchSize = Math.Min(BATCH_SIZE, recordsForThisFile - batchStart);

                        for (int i = 0; i < batchSize; i++)
                        {
                            int recordIndex = startRecord + batchStart + i;
                            string line = await GenerateDataLineAsync(recordIndex);
                            batchBuilder.Append(line);
                        }

                        writer.Write(batchBuilder.ToString());

                        if (batchStart % updateFrequency == 0 || batchStart + batchSize >= recordsForThisFile)
                        {
                            lock (_progressTracker)
                            {
                                _progressTracker?.UpdateProgress(startRecord + batchSize);
                            }
                        }
                    }
                }
            }
            return generatedFiles;
        }

        /// <summary>
        /// Generates a trigger file for the batch.
        /// </summary>
        /// <param name="dataFileName">The name of the data file</param>
        private async Task GenerateTriggerFileAsync(List<string> dataFileNames)
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            string triggerFileName = $"MH340BVRR_Recon_Daily_Trigger_{chainAbbrev}_{date}.txt";
            string filePath = Path.Combine(outputPath, triggerFileName);
            string zipFileName = $"MH340BVRR_Recon_Daily_{chainAbbrev}_{date}.zip";

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                await writer.WriteLineAsync("Data_Filename|Total_Record_Count|ChainFileAbbrev|ZipFileName");
                int remainingRecords = recordCount;

                
                foreach (var dataFileName in dataFileNames)
                {
                    int recordsInThisFile = Math.Min(MAX_RECORDS_PER_FILE, remainingRecords);
                    await writer.WriteLineAsync($"{dataFileName}    {recordsInThisFile}    {chainAbbrev}    {zipFileName}");
                    remainingRecords -= recordsInThisFile;
                }
                
            }
            _progressTracker.UpdateStatus($"Generated trigger file: {triggerFileName}");
        }

        /// <summary>
        /// Genrates a data line for a record with consistent RequestExecutionID and other fields.
        /// </summary>
        /// <returns>Pipe-delimited data line</returns>
        private async Task<string> GenerateDataLineAsync(int index)
        {
            // Constant
            // var accountNumber = GenerateRandomNumber(100000, 1000000).ToString();
            //var totalPkgs = GenerateRandomNumber(1, 10).ToString();
            var contractPharmacyAccountNumber = GenerateRandomNumber(500000, 600000).ToString();
            var threeFourtyBID = "340B";
            var materialStatus = "";


            // Credit related fields
            var creditRequestType = "EA";
            var creditRequestNumber = "1";
            var creditRequestLine = $"{GenerateRandomNumber(1000000000, 2000000000)}";
            var creditMemoType = "R";
            var creditMemoPO = "0";
            var creditMemoNumber = GenerateRandomNumber(10000, 20000).ToString();
            var creditMemoLine = "1";

            // Material related fields
            var materialNumber = "CM23219-GM";
            var materialDescription = "VRR Test batch";
            var retailPrice = "133.75";
            var creditQty = "1";

            // Reference and contract fields
            var refInvoiceDate = DateTime.Now.ToString("yyyyMMdd");
            var contractEntityAcctNum = "3377";

            // Debit related fields
            var debitRequestType = "ZPD2";
            var debitRequestNumber = "1";
            var debitRequestLine = "1";
            var debitMemoType = "R";
            var debitMemoPO = "0";
            var debitMemoNumber = "1";
            var debitMemoLine = "1";
            var debitQty = "1";

            var price340B = "0";
            var DepartmentCode = "TESTDEPT";
            var packageUOM = "0";
            var alternateUOM = "1";
            var alternateUOMQty = "1";
            var upcNumber = "20231026";
            var retailWarehouse = "9.3";
            var vrrMessage = reasonCode == "00" ? "Success" : $"Error code {reasonCode}";
            string pid = GenerateRandomNumber(100000, 1000000).ToString();
            string ndc = GenerateRandomNumber(100000, 1000000).ToString();
            string accountNumber = GenerateRandomNumber(100000, 1000000).ToString();
            string accountType = "340B";
            string totalPkgs = GenerateRandomNumber(1, 10).ToString();
            string hid = "HID";
            if (_useOutboundRecords && index < _outboundRecords.Count)
            {
                var record = _outboundRecords[index];

                //Extract values from the record
                pid = record.ContainsKey("PID") ? record["PID"].ToString() : this.pid.ToString();
                ndc = record.ContainsKey("NDC") ? record["NDC"].ToString() : this.ndc;
                hid = record.ContainsKey("HID") ? record["HID"].ToString() : "";
                accountNumber = record.ContainsKey("Account_Number") ? record["Account_Number"].ToString() : "123456";
                accountType = record.ContainsKey("Account_Type") ? record["Account_Type"].ToString() : "340B";
                totalPkgs = record.ContainsKey("Total_Pkgs") ? record["Total_Pkgs"].ToString() : GenerateRandomNumber(1, 10).ToString();


                

                return $"{requestExecutionID}    {threeFourtyBID}    {pid}    {accountNumber}    {accountType}    {ndc}    {totalPkgs}    {contractPharmacyAccountNumber}" +
                    $"    {creditRequestType}    {creditRequestNumber}    {creditRequestLine}    {creditMemoType}    {creditMemoPO}    {creditMemoNumber}    {creditMemoLine}    " +
                    $"{materialNumber}    {materialDescription}    {materialStatus}    {retailPrice}    {creditQty}    {refInvoiceDate}    {contractEntityAcctNum}" +
                    $"    {debitRequestType}    {debitRequestNumber}    {debitRequestLine}    {debitMemoType}    {debitMemoPO}    {debitMemoNumber}    {debitMemoLine}    {debitQty}    " +
                    $"{price340B}    {DepartmentCode}    {packageUOM}    {alternateUOM}    {alternateUOMQty}    {upcNumber}    {retailWarehouse}    {reasonCode}    {vrrMessage}";

            }
            else
            {
                return $"{requestExecutionID}    {threeFourtyBID}    {pid}    {accountNumber}    {accountType}    {ndc}    {totalPkgs}    {contractPharmacyAccountNumber}" +
                    $"    {creditRequestType}    {creditRequestNumber}    {creditRequestLine}    {creditMemoType}    {creditMemoPO}    {creditMemoNumber}    {creditMemoLine}    " +
                    $"{materialNumber}    {materialDescription}    {materialStatus}    {retailPrice}    {creditQty}    {refInvoiceDate}    {contractEntityAcctNum}" +
                    $"    {debitRequestType}    {debitRequestNumber}    {debitRequestLine}    {debitMemoType}    {debitMemoPO}    {debitMemoNumber}    {debitMemoLine}    {debitQty}    " +
                    $"{price340B}    {DepartmentCode}    {packageUOM}    {alternateUOM}    {alternateUOMQty}    {upcNumber}    {retailWarehouse}    {reasonCode}    {vrrMessage}";
            }
        }

        /// <summary>
        /// Generate a random number between min and max values.
        /// Handles cases where min is greater than max.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>Random integer between min and max</returns>
        private int GenerateRandomNumber(int min, int max)
        {
            if (min >= max)
            {
                //Debug.WriteLine($"Min {min} must be less than max {max}");
                if (min == max)
                {
                    return min;
                }
                if (min > max)
                {
                    int temp = min;
                    min = max;
                    max = temp;
                }
            }

            return random.Next(min, max);
        }
        public bool IsFileInUse(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // If we can open the file with exclusive access, it is not in use
                    return false;
                }
            }
            catch (IOException)
            {
                // If an IOException is thrown, the file is in use
                return true;
            }
        }
    }
}
