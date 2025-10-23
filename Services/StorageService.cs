using System;
using System.IO;
using Newtonsoft.Json;
using DesktopTaskAid.Models;
using System.Collections.Generic;

namespace DesktopTaskAid.Services
{
    public class StorageService
    {
        private readonly string _dataFolder;
        private readonly string _stateFilePath;

        public StorageService()
        {
            LoggingService.Log("StorageService constructor started");

            _dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DesktopTaskAid"
            );

            LoggingService.Log($"Data folder path: {_dataFolder}");

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

            _stateFilePath = Path.Combine(_dataFolder, "appState.json");
            LoggingService.Log($"State file path: {_stateFilePath}");
        }

        public AppState LoadState()
        {
            LoggingService.Log("LoadState called");

            try
            {
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
                    try { state.Tasks = new List<TaskItem>(); } catch { /* if setter not available, ignore */ }
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
    }
}
