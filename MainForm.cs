using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using VRR_Inbound_File_Generator;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Forms.Button;
using ProgressBar = System.Windows.Forms.ProgressBar;
using ComboBox = System.Windows.Forms.ComboBox;
using TextBox = System.Windows.Forms.TextBox;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;


namespace VRR_Inbound_File_Generator
{
    
    public partial class MainForm : Form
    {
        private EnhancedProgressTracker _progressTracker;
        private ValidationHelper _validationHelper;
        private FileGenerator _fileGenerator;

        // New fields for database validation
        private DBHelper _dbHelper;
        private DatabaseValidator _databaseValidator;
        private ILogger _logger;
        private bool _useDatabaseValidation = true;
        private bool _recordCountManuallySet = false;
        public MainForm()
        {
            InitializeComponent();
            txtRecordCount.TextChanged += txtRecordCount_TextChanged;
            InitializeComponents();
            _logger = new FileLogger();
            _logger.LogInfo("Application started");

            // Initialize database components
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["VRRDatabase"]?.ConnectionString;
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogInfo("Initializing database connection");
                    _dbHelper = new DBHelper(connectionString, _logger);

                    // Don't try to connect yet, just initialize the helper
                    lblConnectionStatus.Text = "Database configured (not tested)";
                    btnTestConnection.Enabled = true;

                    // We'll initialize the validators but won't enable validation until connection is tested
                    _databaseValidator = null;
                    _validationHelper = new ValidationHelper();
                    chkUseDatabaseValidation.Checked = true;
                    chkUseDatabaseValidation.Enabled = true;
                }
                else
                {
                    _logger.LogWarning("Connection string not found in App.config");
                    lblConnectionStatus.Text = "No connection string found";
                    lblConnectionStatus.ForeColor = Color.Red;
                    btnTestConnection.Enabled = false;

                    // Fall back to basic validation
                    _databaseValidator = null;
                    _validationHelper = new ValidationHelper();
                    chkUseDatabaseValidation.Checked = false;
                    chkUseDatabaseValidation.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Eror initializing database components: {ex.Message}");
                lblConnectionStatus.Text = "Error initializing database";
                lblConnectionStatus.ForeColor = Color.Red;
                btnTestConnection.Enabled = false;

                // Fall back to basic validation
                _databaseValidator = null;
                _validationHelper = new ValidationHelper();
                chkUseDatabaseValidation.Checked = false;
                chkUseDatabaseValidation.Enabled = false;
            }
        }
        private void InitializeComponents()
        { 
            _logger = new FileLogger();

            try
            {
                // Initialize progress tracker
                _progressTracker = new EnhancedProgressTracker(progressBar, lblStatus);
                //Initialize database components if database validation is enabled
                string connectionString = ConfigurationManager.ConnectionStrings["VRRDatabase"]?.ConnectionString;
                if (!string.IsNullOrEmpty(connectionString) && _useDatabaseValidation)
                {
                    _logger.LogInfo("Initializing database conection");
                    _dbHelper = new DBHelper(connectionString, _logger);
                    _databaseValidator = new DatabaseValidator(_dbHelper, _logger);
                    _validationHelper = new ValidationHelper(_databaseValidator);
                    lblStatus.Text = "Ready (Database Validation Active)";
                }
                else
                {
                    _logger.LogWarning("Database validation disabled - using basic validation only");
                    _validationHelper = new ValidationHelper();
                    lblStatus.Text = "Ready (Basic Validation Only)";
                }
                PopulateReasonCodes();

                // Set default values
                txtChainStore.Text = "WMT";
                txtRecordCount.Text = "1000";

                //Setup checkbox
                chkUseDatabaseValidation.Checked = true; 
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing database components: {ex.Message}");
                MessageBox.Show($"Error initializing database component: {ex.Message}\n\nFailing back to basic validation.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                _validationHelper = new ValidationHelper();
                lblStatus.Text = "Ready (Basic Validation Only - Error)";
                chkUseDatabaseValidation.Checked = false;
                chkUseDatabaseValidation.Enabled = false;
            }
        }

        private void PopulateReasonCodes()
        {
            cboReasonCode.Items.Clear();

            cboReasonCode.Items.Add("00 - 340B qty successfully consumed");
            cboReasonCode.Items.Add("01 - Covered Entity account does not exist in Master data");
            cboReasonCode.Items.Add("02 - Covered Entity account exists but not active");
            cboReasonCode.Items.Add("03 - Covered Entity DEA License expired");
            cboReasonCode.Items.Add("04 - Covered Entity Account has illing block");

            cboReasonCode.SelectedIndex = 0;
        }
        private async void BtnFetchOutboundData_Click(object sender, EventArgs e)
        {
            if (!_useDatabaseValidation || _databaseValidator == null)
            {
                MessageBox.Show("Database validation must be enabled to fetch outbound data.",
                    "Validation Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string requestExecutionID = txtRequestExecutionID.Text.Trim();
            if (string.IsNullOrWhiteSpace(requestExecutionID))
            {
                MessageBox.Show("Please enter a RequestExecutionID.",
                    "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                btnFetchOutboundData.Enabled = false;
                btnGenerate.Enabled = false;
                lblStatus.Text = "Fetching outbound data...";
                Application.DoEvents();

                var result = await _databaseValidator.FetchOutboundDataAsync(requestExecutionID);

                if (result.success)
                {
                    if (result.data.ContainsKey("PID"))
                    {
                        txtPID.Text = result.data["PID"].ToString();
                        txtPID.Enabled = false;
                    }
                    if (result.data.ContainsKey("NDC"))
                    {
                        txtNDC.Text = result.data["NDC"].ToString();
                        txtNDC.Enabled = false;
                    }
                    if (result.data.ContainsKey("ChainAbbrev"))
                    {
                        txtChainStore.Text = result.data["ChainAbbrev"].ToString();
                    }
                    if (result.data.ContainsKey("RecordCount"))
                    {
                        int recordCount = Convert.ToInt32(result.data["RecordCount"]);
                        txtRecordCount.Text = recordCount.ToString();
                    }

                    lblStatus.Text = "Outbound data loaded successfully.";
                    MessageBox.Show("Outbound data loaded successfully.", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblStatus.Text = "Failed to fetch outbound data.";
                    MessageBox.Show(result.errorMessage, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    txtPID.Enabled = true;
                    txtNDC.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching outbound data: {ex.Message}");
                lblStatus.Text = "Error fetching outbound data.";
                MessageBox.Show($"Error fetching outbound data: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                txtPID.Enabled = true;
                txtNDC.Enabled = true;
            }
            finally
            {
                btnFetchOutboundData.Enabled = true;
                btnGenerate.Enabled = true;
            }
        }
        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutputPath.Text = folderDialog.SelectedPath;
                }
            }
        }
        private async void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs())
                return;

            if (_databaseValidator != null && _useDatabaseValidation && !await ValidateWithDatabaseAsync())
                return;

            try
            {
                //Debug.WriteLine($"Progress: {progressBar.Value}");
                btnGenerate.Enabled = false;
                btnBrowse.Enabled = false;
                btnFetchOutboundData.Enabled= false;
                progressBar.Value = 0;
                lblStatus.Text = "Generating file...";

                // get values from form controls
                string requestExecutionID = txtRequestExecutionID.Text.Trim();
                string chainAbbrev = txtChainStore.Text.Trim();
                string pid = txtPID.Text.Trim();
                string ndc = txtNDC.Text.Replace("_", "").Trim();
                string reasonCode = cboReasonCode.SelectedItem != null
                    ? cboReasonCode.SelectedItem.ToString()
                    : "00"; // Default reason code
                int recordCount = int.Parse(txtRecordCount.Text);
                string outputPath = txtOutputPath.Text;
                List<Dictionary<string, object>> outboundRecords = null;
                if (_databaseValidator != null && _useDatabaseValidation)
                {
                    var outboundDAtaResult = await _databaseValidator.GetAllOutboundRecordsAsync(requestExecutionID);
                    if (outboundDAtaResult.success)
                    {
                        outboundRecords = outboundDAtaResult.records;

                        // if record count wasn't explicitly changed, use the outbound count
                        if (outboundRecords.Count > 0 && !_recordCountManuallySet)
                        {
                            recordCount = outboundRecords.Count;
                            txtRecordCount.Text = recordCount.ToString();
                        }
                    }
                }
                LogMessage($"Initializing FileGenerator with values: ChainAbbrev: {chainAbbrev}, PID: {pid}, NDC: {ndc}, ReasonCode: {reasonCode}, RecordCount: {recordCount}, OutputPath: {outputPath}, RequestExecutionID: {requestExecutionID}");
                //Generate files
                _fileGenerator = new FileGenerator(
                    requestExecutionID,
                    chainAbbrev,
                    int.Parse(pid), // Convert pid to int
                    ndc,
                    reasonCode,
                    recordCount,
                    outputPath,
                    _progressTracker,
                    _useDatabaseValidation ? _databaseValidator : null,
                    _logger,
                    outboundRecords);

                await _fileGenerator.GenerateFilesAsync();
                lblStatus.Text = "Files generated successfully";
                MessageBox.Show("File generated successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating files: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error generating files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
            }
            finally
            {
                btnGenerate.Enabled = true;
                btnBrowse.Enabled = true;
                btnFetchOutboundData.Enabled = _useDatabaseValidation;
                //lblStatus.Text = "Ready";
                if(!_useDatabaseValidation)
                {
                    txtPID.Enabled = true;
                    txtNDC.Enabled = true;
                }
            }
        }
        private bool ValidateInputs()
        {
            var reqExecIDValidation = _validationHelper.ValidateRequestExecutionID(txtRequestExecutionID.Text);
            if (!reqExecIDValidation.IsValid)
            {
                MessageBox.Show(reqExecIDValidation.GetErrorMessage(), "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
            {
                MessageBox.Show("Please select an output path.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var chainAbbrev = txtChainStore.Text.Trim().ToUpper();
            if (chainAbbrev != "WMT" && chainAbbrev != "CPH")
            {
                MessageBox.Show("Invalid chain store.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var pidValidaion = _validationHelper.ValidatePID(txtPID.Text.Trim(), chainAbbrev);

            if (!pidValidaion.IsValid)
            {
                MessageBox.Show($"Invalid PID for {txtChainStore.Text}.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            var ndcValidation = _validationHelper.ValidateNDC(txtNDC.Text.Replace("-", ""));
            if (!ndcValidation.IsValid)
            {
                //MessageBox.Show("Invalid NDC.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }

            if (!int.TryParse(txtRecordCount.Text, out int recordCount) || recordCount <= 0 || recordCount > 2000000)
            {
                MessageBox.Show("Invalid record count.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
            {
                MessageBox.Show("Output Path cannnot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }
        private async Task<bool> ValidateWithDatabaseAsync()
        {
            // Skip if database validation is not available
            if (_databaseValidator == null || !_useDatabaseValidation)
            {
                return true;
            }
            try
            {
                _logger.LogInfo("Starting database validation");

                string chainAbbrev = txtChainStore.Text.Trim();
                string pid = txtPID.Text.Trim();
                string ndc = txtNDC.Text.Replace("-", "").Trim();
                string requestExecutionID = txtRequestExecutionID.Text.Trim();

                // Validate PID
                var pidResult = await _validationHelper.ValidatePIDWithDatabaseAsync(pid, chainAbbrev);
                if (!pidResult.IsValid)
                {
                    MessageBox.Show(pidResult.GetErrorMessage(), "Database Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Validate NDC
                var ndcResult = await _validationHelper.ValidateNDCWithDatabaseAsync(ndc);
                if (!ndcResult.IsValid)
                {
                    MessageBox.Show(ndcResult.GetErrorMessage(), "Database Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Validate RequestExecutionID
                var reqIdResult = await _validationHelper.ValidateRequestExecutionIDWithDatabaseAsync(requestExecutionID);
                if (!reqIdResult.IsValid)
                {
                    MessageBox.Show(reqIdResult.GetErrorMessage(), "Database Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                _logger.LogInfo("Database validation completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database validation error: {ex.Message}");
                MessageBox.Show($"Database validation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async void BtnTestConnection_Click(object sender, EventArgs e)
        {
            try
            {
                // Disable the button during test
                btnTestConnection.Enabled = false;
                string connectionString = ConfigurationManager.ConnectionStrings["VRRDatabase"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    MessageBox.Show("No connection string found in App.config.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                var dbHelper = new DBHelper(connectionString, _logger);
                var (isSuccessful, message) = await dbHelper.TestConnectionAsync();

                if (isSuccessful)
                {
                    MessageBox.Show("Database connection successful", "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Database connection failed: {message}", "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error testing connection: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblConnectionStatus.Text = $"Error: {ex.Message}";
                lblConnectionStatus.ForeColor = Color.Red;
                _logger.LogError($"Error during connection test: {ex.Message}");
            }
            finally
            {
                btnTestConnection.Enabled = true;
            }
        }
        private async void ChkUseDatabaseValidation_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                _useDatabaseValidation = chkUseDatabaseValidation.Checked;

                if (_useDatabaseValidation)
                {
                    if (_dbHelper == null)
                    {
                        try
                        {
                            if (_logger == null)
                            {
                                _logger = new FileLogger();
                            }

                            // Re-initiating database components
                            string connectionString = ConfigurationManager.ConnectionStrings["VRRDatabase"]?.ConnectionString;
                            if (!string.IsNullOrEmpty(connectionString))
                            {
                                //_logger.LogInfo("Initializing database connection");
                                _dbHelper = new DBHelper(connectionString, _logger);
                                _databaseValidator = new DatabaseValidator(_dbHelper, _logger);
                                
                                var (isSuccessful, message) = await _dbHelper.TestConnectionAsync();

                                if (isSuccessful)
                                {
                                    _databaseValidator = new DatabaseValidator(_dbHelper, _logger);
                                    _validationHelper = new ValidationHelper(_databaseValidator);
                                    lblStatus.Text = "Ready (Database Validation Active)";
                                    lblConnectionStatus.Text = "Connection successful";
                                    lblConnectionStatus.ForeColor = Color.Green;
                                }
                                else
                                {
                                    // Connection failed, disable the checkbox
                                    chkUseDatabaseValidation.Checked = false;
                                    _useDatabaseValidation = false;
                                    lblStatus.Text = "Ready (Basic Validation Only)";
                                    lblConnectionStatus.ForeColor = Color.Red;

                                    MessageBox.Show($"Database connection failed: {message}\nFalling back to basic validation.",
                                        "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }

                            }
                            else
                            {
                                chkUseDatabaseValidation.Checked = false;
                                _useDatabaseValidation = false;
                                lblStatus.Text = "Ready (Basic Validation Only)";
                                lblConnectionStatus.Text = "No connection string found";
                                lblConnectionStatus.ForeColor = Color.Red;
                                MessageBox.Show("Connection string not found in config file. Falling back to basic validation.",
                                    "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error initializing database connection: {ex.Message}");
                            chkUseDatabaseValidation.Checked = false;
                            _useDatabaseValidation = false;
                            lblStatus.Text = "Ready (Basic Validation Only - Error)";
                            lblConnectionStatus.Text = $"Error: {ex.Message}";
                            lblConnectionStatus.ForeColor = Color.Red;

                            MessageBox.Show($"Error initializing database connection: {ex.Message}\nFalling back to basic validation.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else if (_databaseValidator == null)
                    {
                        // Disable database validation
                        _databaseValidator = new DatabaseValidator(_dbHelper, _logger);
                        _validationHelper = new ValidationHelper(_databaseValidator);
                        lblStatus.Text = "Ready (Basic Validation Only)";
                    }
                    else
                    {
                        lblStatus.Text = "Ready (Databse Validation Active)";
                    }
                    // Enable the fetch button
                    btnFetchOutboundData.Enabled = true;
                }
                else
                {
                    _databaseValidator = null;
                    _validationHelper = new ValidationHelper();
                    lblStatus.Text = "Ready (Basic Validation Only)";

                    // Disable the fetch button and re-enable input fields
                    btnFetchOutboundData.Enabled = false;
                    txtPID.Enabled = true;
                    txtNDC.Enabled= true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing validation mode: {ex.Message}");
                MessageBox.Show($"Error changing validation mode: {ex.Message}", "Error", MessageBoxButtons.OK,MessageBoxIcon.Error);

                _validationHelper = new ValidationHelper();
                lblStatus.Text = "Ready (Basic Validation Only - Error)";
                chkUseDatabaseValidation.Checked = false;
            }
        }

        private void LogMessage(string message)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logPath);
                string logFile = Path.Combine(logPath, $"vrr_generator_{DateTime.Now:yyyyMMdd}.log");
                using (StreamWriter writer = File.AppendText(logFile))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                }
            }
            catch
            {
                Debug.WriteLine($"Fsiled to log message: {message}");
            }
        }
        private void txtRecordCount_TextChanged(object sender, EventArgs e)
        {
            // Handle the text changed event here
            _recordCountManuallySet = true;
        }
    }
}

