using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Core {
	public partial class SettingsManager : Node {
		public static SettingsManager Instance { get; private set; }
		private const string ENCRYPTION_KEY = "DefaultKey";
		private const string SAVE_PATH = "user://user_settings.dat";

		private readonly Dictionary<string, object> DefaultSettings = new() {
			{ "ExampleSettings", new Dictionary<string, object> {
					{ "master_volume", 1.0f },
					{ "graphics", 3 },
					{ "someboolean", true },
					{ "somestring", "example" }
				}
			},
			{ "You can add more categories,", new Dictionary<string, object> {
					{ "and it'll automatically", "adapt to them" },
					{ "you just need to run SaveSettings()", "to apply changes" }
				}
			}
		};

		private Dictionary<string, object> CurrentSettings;

		public override void _Notification(int what) {
			if (what == NotificationPredelete) {
				SaveSettings();
			}
		}

		public async override void _Ready() {
			Instance ??= this;
			CurrentSettings = new Dictionary<string, object>(DefaultSettings);
			await LoadSettings();
		}

		public async Task SetSetting(string path, object value) {
			var keys = path.Split(".");
			if (keys.Length < 2) {
				GD.PrintErr($"Invalid settings path: {path}. Must have at least two levels.");
				return;
			}
			try {
				var current = CurrentSettings;
				for (int i = 0; i < keys.Length - 1; i++) {
					if (!current.TryGetValue(keys[i], out var next) || next is not Dictionary<string, object> nextDict) {
						nextDict = [];
						current[keys[i]] = nextDict;
					}
					current = nextDict;
				}
				current[keys[^1]] = value;
				await SaveSettings();
				GD.Print($"[CORE] Setting updated: {path} = {value}");
			} catch (Exception ex) {
				GD.PrintErr($"Error setting value for path '{path}': {ex.Message}");
			}
		}

		public object GetSetting(string path, object Default = null) {
			if (CurrentSettings == null) {
				GD.PrintErr("CurrentSettings is null; returning default value.");
				return Default;
			}
			var keys = path.Split(".");
			if (keys.Length == 0) return Default;
			try {
				// Traverse the dictionary using the keys
				object current = CurrentSettings;
				foreach (var key in keys) {
					if (current is Dictionary<string, object> dict && dict.TryGetValue(key, out var next)) {
						current = next;
					} else {
						GD.PrintErr($"Key '{key}' not found; returning default value.");
						return Default;
					}
				}
				return current;
			} catch (Exception ex) {
				GD.PrintErr($"Error retrieving setting for path '{path}': {ex.Message}");
				return Default;
			}
		}

		public Task SaveSettings() {
			var saveData = ConvertToGodotDictionary(CurrentSettings);
			string jsonString = Json.Stringify(saveData);
			string encodedData = EncodeData(jsonString);
			using (var saveFile = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Write | FileAccess.ModeFlags.WriteRead)) {
				saveFile?.StoreString(encodedData);
			}
			GD.Print("[CORE] Successfully saved settings.");
			return Task.CompletedTask;
		}

		public async Task LoadSettings() {
			GD.Print("[CORE] Loading settings...");
			if (!FileAccess.FileExists(SAVE_PATH)) {
				GD.PushWarning("Settings file does not exist, saving default settings...");
				await SaveSettings();
				await LoadSettings();
				return;
			}
			var SaveFile = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Read);
			if (SaveFile == null) {
				GD.PushError("Cannot open settings file as it doesn't exist.");
				return;
			}
			var EncodedData = SaveFile.GetAsText();
			SaveFile.Close();
			var JSONString = DecodeData(EncodedData);
			var ParseResult = Json.ParseString(JSONString);
			MergeSettings(ParseResult);
			// Add post load logic here
		}

		private void MergeSettings(Variant parseResult) {
			if (parseResult.VariantType != Variant.Type.Dictionary) {
				GD.PushError("Failed to parse settings file");
				return;
			}
			var loadedSettings = parseResult.AsGodotDictionary();
			var mergedSettings = new Dictionary<string, object>();
			foreach (var categoryKey in CurrentSettings.Keys) {
				if (CurrentSettings[categoryKey] is Dictionary<string, object> categoryDict) {
					var copyCategoryDict = new Dictionary<string, object>();
					foreach (var key in categoryDict.Keys) {
						copyCategoryDict[key] = categoryDict[key];
					}
					mergedSettings[categoryKey] = copyCategoryDict;
				}
			}
			foreach (var categoryKey in loadedSettings.Keys) {
				string category = categoryKey.ToString();
				if (!mergedSettings.TryGetValue(category, out object value)) {
					value = new Dictionary<string, object>();
					mergedSettings[category] = value;
				}
				if (loadedSettings[categoryKey].VariantType == Variant.Type.Dictionary) {
					var loadedCategoryDict = loadedSettings[categoryKey].AsGodotDictionary();
					if (value is not Dictionary<string, object> mergedCategoryDict) {
						mergedCategoryDict = [];
						mergedSettings[category] = mergedCategoryDict;
					}
					foreach (var keyVariant in loadedCategoryDict.Keys) {
						string key = keyVariant.ToString();
						mergedCategoryDict[key] = ConvertVariantToObject(loadedCategoryDict[keyVariant]);
					}
				}
			}
			CurrentSettings = mergedSettings;
			GD.Print("[CORE] Successfully merged settings.");
		}

		private static Godot.Collections.Dictionary ConvertToGodotDictionary(Dictionary<string, object> dict) {
			var godotDict = new Godot.Collections.Dictionary();
			foreach (var pair in dict) {
				godotDict[pair.Key] = pair.Value switch {
					Dictionary<string, object> nestedDict => ConvertToGodotDictionary(nestedDict),
					string strValue => strValue,
					int intValue => intValue,
					float floatValue => floatValue,
					double doubleValue => (float)doubleValue,
					bool boolValue => boolValue,
					null => new Variant(),
					Variant variant => variant,
					// Add more type handlers as needed
					_ => pair.Value.ToString() // Fallback case
				};
			}
			return godotDict;
		}

		private static Dictionary<string, object> ConvertToDotNetDictionary(Godot.Collections.Dictionary<string, Variant> godotDict) {
			var dotNetDict = new Dictionary<string, object>();
			foreach (var pair in godotDict) {
				dotNetDict[pair.Key] = pair.Value;
			}
			return dotNetDict;
		}

		public static object ConvertVariantToObject(Variant variant) {
			switch (variant.VariantType) {
				case Variant.Type.Bool:
					return variant.AsBool();
				case Variant.Type.Int:
					return variant.AsInt32();
				case Variant.Type.Float:
					return variant.AsSingle();
				case Variant.Type.String:
					return variant.AsString();
				case Variant.Type.Vector2:
					return variant.AsVector2();
				case Variant.Type.Dictionary:
					var dict = new Dictionary<string, object>();
					var godotDict = variant.AsGodotDictionary();
					foreach (var key in godotDict.Keys)
					{
						dict[key.ToString()] = ConvertVariantToObject(godotDict[key]);
					}
					return dict;
				default:
					return null;
			}
		}

		private static string EncodeData(string data) {
			if (string.IsNullOrEmpty(ENCRYPTION_KEY)) {
				return data;
			}
			byte[] keyBytes = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);
			byte[] hashedKey = SHA256.HashData(keyBytes);
			byte[] iv = new byte[16];
			Array.Copy(hashedKey, 0, iv, 0, 16);
			using var aes = Aes.Create();
			aes.Key = hashedKey;
			aes.IV = iv;
			using var encryptor = aes.CreateEncryptor();
			byte[] dataBytes = Encoding.UTF8.GetBytes(data);
			byte[] encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
			return Convert.ToBase64String(encryptedBytes);
		}

		private static string DecodeData(string encodedData) {
			if (string.IsNullOrEmpty(ENCRYPTION_KEY)) {
				return encodedData;
			}
			try {
				byte[] keyBytes = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);
				byte[] hashedKey = SHA256.HashData(keyBytes);
				byte[] iv = new byte[16];
				Array.Copy(hashedKey, 0, iv, 0, 16);
				using var aes = Aes.Create();
				aes.Key = hashedKey;
				aes.IV = iv;
				using var decryptor = aes.CreateDecryptor();
				byte[] encryptedBytes = Convert.FromBase64String(encodedData);
				byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
				return Encoding.UTF8.GetString(decryptedBytes);
			} catch (Exception ex) {
				GD.PushError("Failed to decrypt settings file: " + ex);
				return "{}";
			}
		}
	}
}