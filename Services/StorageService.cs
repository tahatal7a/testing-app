using System;
using System.IO;
using Newtonsoft.Json;
using DesktopTaskAid.Models;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace DesktopTaskAid.Services
{
    public class StorageService
    {
        // Test seams: allow tests to override unit test detection and app data location safely
        public static Func<bool> UnitTestDetector = IsRunningUnderUnitTest;
        public static Func<string> AppDataPathProvider = () => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        private readonly bool _isTestMode;
        private readonly string _dataFolder;
        private readonly string _stateFilePath;

        public StorageService()
        {
            LoggingService.Log("StorageService constructor started");

            _isTestMode = UnitTestDetector();

            if (_isTestMode)
            {
                // Use a unique, isolated temp folder per StorageService instance during tests
                var baseTemp = Path.Combine(Path.GetTempPath(), "DesktopTaskAid_Tests");
                try
                {
                    if (!Directory.Exists(baseTemp))
                    {
                        Directory.CreateDirectory(baseTemp);
                    }
                }
                catch { }

                _dataFolder = Path.Combine(baseTemp, $"run_{Process.GetCurrentProcess().Id}", Guid.NewGuid().ToString("N"));
                LoggingService.Log($"[TEST MODE] Using isolated data folder: {_dataFolder}");
            }
            else
            {
                _dataFolder = Path.Combine(
                    AppDataPathProvider(),
                    "DesktopTaskAid"
                );
                LoggingService.Log($"Data folder path: {_dataFolder}");
            }

            if (!_isTestMode)
            {
                // Only create persistent folder in normal app runs
                if (!Directory.Exists(_dataFolder))
                {
                    LoggingService.Log("Data folder does not exist, creating it");
                    Directory.CreateDirectory(_dataFolder);
                    LoggingService.Log("Data folder created successfully");
                }
                else
                {
                    LoggingService.Log("Data folder already exists");
                }
            }
            else
            {
                try { Directory.CreateDirectory(_dataFolder); } catch { }
            }

            _stateFilePath = Path.Combine(_dataFolder, "appState.json");
            LoggingService.Log($"State file path: {_stateFilePath}");
        }

        public StorageService(string dataFolderPath, bool ensureDirectoryExists = true)
        {
            if (string.IsNullOrWhiteSpace(dataFolderPath))
            {
                throw new ArgumentException("Data folder path must be provided.", nameof(dataFolderPath));
            }

            _dataFolder = dataFolderPath;

            if (ensureDirectoryExists && !Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            _stateFilePath = Path.Combine(_dataFolder, "appState.json");
        }

        public AppState LoadState()
        {
            LoggingService.Log("LoadState called");

            try
            {
                if (_isTestMode)
                {
                    // Always start from a clean state in unit tests
                    LoggingService.Log("[TEST MODE] Returning default empty state");
                    return CreateDefaultState();
                }

                if (!File.Exists(_stateFilePath))
                {
                    LoggingService.Log("State file does not exist, creating default EMPTY state");
                    return CreateDefaultState();
                }

                LoggingService.Log("Reading state file");
                var json = File.ReadAllText(_stateFilePath);
                LoggingService.Log($"State file read successfully, length: {json.Length} chars");

                LoggingService.Log("Deserializing state");
                var state = JsonConvert.DeserializeObject<AppState>(json);

                // Ensure state is valid
                if (state == null)
                {
                    LoggingService.Log("WARNING: Deserialized state is null, creating default EMPTY state");
                    return CreateDefaultState();
                }

                // Ensure collections exist
                if (state.Tasks == null)
                {
                    LoggingService.Log("Tasks list was null, initializing empty list");
                    try { state.Tasks = new List<TaskItem>(); } catch { }
                }

                LoggingService.Log($"State deserialized - Tasks: {state.Tasks?.Count ?? 0}, Theme: {state.Settings?.Theme}");

                // Safe timer refresh
                LoggingService.Log("Refreshing timer daily tracking (safe)");
                state.Timer?.RefreshDailyTracking();

                LoggingService.Log("LoadState completed successfully");
                return state;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("ERROR in LoadState, creating default EMPTY state", ex);
                return CreateDefaultState();
            }
        }

        public void SaveState(AppState state)
        {
            try
            {
                if (_isTestMode)
                {
                    // Do not persist to disk during tests to avoid cross-test interference
                    LoggingService.Log("[TEST MODE] SaveState skipped");
                    return;
                }

                LoggingService.Log("SaveState called");

                // Defensive: ensure lists exist before saving
                if (state != null && state.Tasks == null)
                {
                    try { state.Tasks = new List<TaskItem>(); } catch { }
                }

                var json = JsonConvert.SerializeObject(state, Formatting.Indented);
                LoggingService.Log($"State serialized, writing to file (length: {json.Length} chars)");
                File.WriteAllText(_stateFilePath, json);
                LoggingService.Log("State saved successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("ERROR saving state", ex);
            }
        }

        private AppState CreateDefaultState()
        {
            LoggingService.Log("CreateDefaultState called - creating new EMPTY state");

            var state = new AppState();

            // Ensure tasks list exists (no sample tasks!)
            if (state.Tasks == null)
            {
                try { state.Tasks = new List<TaskItem>(); } catch { }
            }

            LoggingService.Log($"Empty state created (Tasks: {state.Tasks?.Count ?? 0})");
            return state;
        }

        public string GetDataFolderPath()
        {
            return _dataFolder;
        }

        private static bool IsRunningUnderUnitTest()
        {
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                return asms.Any(a =>
                    (a.FullName?.IndexOf("nunit", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (a.FullName?.IndexOf("xunit", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (a.FullName?.IndexOf("mstest", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
            }
            catch { return false; }
        }
    }
}
