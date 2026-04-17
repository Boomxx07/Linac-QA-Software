using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Linac_QA_Software.Models
{
    public static class ConfigLoader
    {
        public static Config Load(string path = "config.json")
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Config>(json, options);
        }
    }
}
