using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Remoting.Messaging;

namespace VRR_Inbound_File_Generator
{
    /// <summary>
    /// Provides a way to track the progress of an operation and display it in a ProgressBar and Label.
    /// </summary>
    public class EnhancedProgressTracker : IDisposable
    {
        private readonly System.Windows.Forms.Timer _updateTimer; // Corrected variable name
        private readonly ProgressBar _progressBar;
        private readonly Label _statusLabel;
        private readonly Stopwatch _stopwatch;
        private Color _originalStatusColor;
        private bool _isOperationInProgress;
        private int _totalItems;
        private int _currentItem;

        /// <summary>
        /// Initializes a new instance of the EnhancedProgressTracker class.
        /// </summary>
        /// <param name="progressBar">Progress bar control.</param>
        /// <param name="statusLabel">Status label control</param>
        /// <exception cref="ArgumentNullException"></exception>
        public EnhancedProgressTracker(ProgressBar progressBar, Label statusLabel)
        {
            _progressBar = progressBar ?? throw new ArgumentNullException(nameof(progressBar));
            _statusLabel = statusLabel ?? throw new ArgumentNullException(nameof(statusLabel));
            _stopwatch = new Stopwatch();
            _originalStatusColor = statusLabel.ForeColor;

            _updateTimer = new System.Windows.Forms.Timer(); // Corrected variable name
            _updateTimer.Interval = 250; // Corrected variable name
            _updateTimer.Tick += UpdateTimer_Tick; // Corrected variable name
        }
        /// <summary>
        /// Starts the operation and resets the progress tracker.
        /// </summary>
        /// <param name="totalItems"></param>
        public void Start(int totalItems = 100)
        {
            _totalItems = totalItems;
            _currentItem = 0;
            _isOperationInProgress = true;
            _progressBar.Value = 0;
            _progressBar.Minimum = 0;
            _progressBar.Maximum = 100;
            _progressBar.Style = ProgressBarStyle.Continuous;
            _stopwatch.Reset();
            _stopwatch.Start();
            _updateTimer.Start(); // Corrected variable name
            UpdateStatus("Starting operation...");
        }
        /// <summary>
        /// Updates the progress of the operation.
        /// </summary>
        /// <param name="currentItem"></param>
        public void UpdateProgress(int currentItem)
        {
            if (!_isOperationInProgress)
            {
                return;
            }
            _currentItem = Math.Min(currentItem, _totalItems);

            // Ensure we don't exceed the maximum
            if (_currentItem > _totalItems)
                _currentItem = _totalItems;

            var percentage = (int)((double)_currentItem / _totalItems * 100);

            if (_progressBar.InvokeRequired)
            {
                _progressBar.Invoke(new Action(() => UpdateUI(percentage)));
            }
            else
            {
                UpdateUI(percentage);
            }
        }
        /// <summary>
        /// Updates the UI with the current progress.
        /// </summary>
        /// <param name="percentage"></param>
        private void UpdateUI(int percentage)
        {
            percentage = Math.Max(0, Math.Min(100, percentage));
            if (_progressBar.Value < percentage)
            {
                _progressBar.Value = Math.Min(_progressBar.Value + 1, percentage);

                if (_progressBar.Value < percentage)
                {
                    System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer();
                    animationTimer.Interval = 15;
                    animationTimer.Tick += (s, e) =>
                    {
                        if (_progressBar.Value < percentage)
                        {
                            _progressBar.Value++;
                        }
                        else
                        {
                            ((System.Windows.Forms.Timer)s).Stop();
                            ((System.Windows.Forms.Timer)s).Dispose();
                        }
                    };
                    animationTimer.Start();
                }
            }
            else
            {
                _progressBar.Value = percentage;
            }
            UpdateStatus(GetProgressMessage());
        }
        /// <summary>
        /// Gets the progress message with time estimates.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!_isOperationInProgress) return;
            UpdateStatus(GetProgressMessage());
        }
        /// <summary>
        /// Gets the progress message with time estimates.
        /// </summary>
        /// <returns></returns>
        private string GetProgressMessage()
        {
            var elapsedTime = _stopwatch.Elapsed;
            var itemsPerSecond = _currentItem / Math.Max(1, elapsedTime.TotalSeconds);
            var remainingItems = _totalItems - _currentItem;
            var estimatedRemainingSeconds = itemsPerSecond > 0 ? remainingItems / itemsPerSecond : 0;

            return $"Processing {_currentItem} of {_totalItems:N0}" +
                $"({(_currentItem / (double)_totalItems):P0}) " +
                $"Elapsed: {FormatTimeSpan(elapsedTime)}, " +
                $"Remaining: {FormatTimeSpan(TimeSpan.FromSeconds(estimatedRemainingSeconds))}";
        }
        /// <summary>
        /// Formats a TimeSpan into a string.
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
            {
                return $"{timeSpan:h\\:mm\\:ss}";
            }
            return $"{timeSpan:mm\\:ss}";

        }
        /// <summary>
        /// Updates the status label with a message.
        /// </summary>
        /// <param name="message"></param>
        public void UpdateStatus(string message)
        {
            if (_statusLabel.InvokeRequired)
            {
                _statusLabel.Invoke(new Action(() => _statusLabel.Text = message));
            }
            else
            {
                _statusLabel.Text = message;
            }
            //_statusLabel.Text = message;
        }
        /// <summary>
        /// Sets the color of the status label.
        /// </summary>
        /// <param name="color"></param>
        public void SetStatusColor(Color color)
        {
            if (_statusLabel.InvokeRequired)
            {
                _statusLabel.Invoke(new Action(() => _statusLabel.ForeColor = color));
            }
            else
            {
                _statusLabel.ForeColor = color;
            }
        }
        /// <summary>
        /// Completes the operation and stops the progress tracker.
        /// </summary>
        public void Complete()
        {
            _isOperationInProgress = false;
            _stopwatch.Stop();
            _updateTimer.Stop(); // Corrected variable name
            if (_progressBar.InvokeRequired)
            {
                _progressBar.Invoke(new Action(() =>
                {
                    _progressBar.Value = _progressBar.Maximum;
                    UpdateStatus("Operation completed successfully");
                    SetStatusColor(Color.Green);
                }));
            }
            else
            {
                _progressBar.Value = _progressBar.Maximum;
                UpdateStatus("Operation completed successfully");
                SetStatusColor(Color.Green);
            }

        }
        /// <summary>
        /// Handles an error that occurred during the operation.
        /// </summary>
        /// <param name="ex"></param>
        public void Error(string errorMessage)
        {
            _isOperationInProgress = false;
            _stopwatch.Stop();
            _updateTimer.Stop(); // Corrected variable name
            UpdateStatus($"Error: {errorMessage}");
            SetStatusColor(Color.Red);
        }
        /// <summary>
        /// Resets the progress tracker.
        /// </summary>
        public void Reset()
        {
            _isOperationInProgress = false;
            _stopwatch.Reset();
            _updateTimer.Stop(); 

            if (_progressBar.InvokeRequired)
            {
                _progressBar.Invoke(new Action(() => {
                    _progressBar.Value = 0;
                    UpdateStatus("Ready");
                    SetStatusColor(_originalStatusColor);
                }));   
            }
            else
            {
                _progressBar.Value = 0;
                UpdateStatus("Ready");
                SetStatusColor(_originalStatusColor);
            }

        }
        /// <summary>
        /// Disposes of the progress tracker.
        /// </summary>
        public void Dispose()
        {
            _updateTimer?.Stop(); // Corrected variable name
            _updateTimer?.Dispose(); // Corrected variable name
            _stopwatch.Stop();
        }
    }
}
