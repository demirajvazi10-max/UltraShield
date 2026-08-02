using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace UltraShield.Services
{
    /// <summary>
    /// Persists checklist completion state (by item title) so progress
    /// survives an app restart. Keyed by title rather than index, so
    /// reordering or adding items later doesn't scramble existing state.
    /// </summary>
    public class ChecklistPersistenceService
    {
        private static readonly string StatePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UltraShield", "checklist-state.json");

        public async Task<Dictionary<string, bool>> LoadAsync()
        {
            try
            {
                if (!File.Exists(StatePath))
                    return new Dictionary<string, bool>();

                var json = await File.ReadAllTextAsync(StatePath);
                return JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new Dictionary<string, bool>();
            }
            catch
            {
                return new Dictionary<string, bool>();
            }
        }

        public async Task SaveAsync(Dictionary<string, bool> state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(StatePath, json);
        }
    }
}
