using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace Linac_QA_Software.Models
{
    /// <summary>
    /// Service class responsible for handling the I/O operations related to 
    /// the application configuration.
    /// </summary>
    public static class ConfigLoader
    {
        private const string DefaultConfigPath = "config.json";

        /// <summary>
        /// Loads the configuration from the specified JSON file.
        /// </summary>
        /// <param name="path">The relative or absolute path to the config file.</param>
        /// <returns>A populated Config object, or a fresh object if loading fails.</returns>
        public static Config Load(string path = DefaultConfigPath)
        {
            try
            {
                if (!File.Exists(path))
                {
                    // If the file is missing in a clinical environment, this is a critical error.
                    // For now, we return an empty config to prevent the app from crashing.
                    return new Config();
                }

                string json = File.ReadAllText(path);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    // Allows the JSON to include comments (useful for physics documentation)
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var config = JsonSerializer.Deserialize<Config>(json, options);

                return config ?? new Config();
            }
            catch (JsonException ex)
            {
                // Handle syntax errors in the JSON file
                MessageBox.Show($"Error parsing configuration: {ex.Message}", "Config Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new Config();
            }
            catch (Exception ex)
            {
                // Handle general I/O or permission errors
                MessageBox.Show($"Could not load config file: {ex.Message}", "I/O Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new Config();
            }
        }

        /// <summary>
        /// Saves the current configuration back to the JSON file.
        /// </summary>
        public static void Save(Config config, string path = DefaultConfigPath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save configuration: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}