using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace HiguSaveConverter {
	public static class Utility {
		public static void WriteNameValue(this JsonWriter self, string name, bool value)
		{
			self.WritePropertyName(name);
			self.WriteValue(value);
		}

		public static void WriteNameValue(this JsonWriter self, string name, int value)
		{
			self.WritePropertyName(name);
			self.WriteValue(value);
		}

		public static void WriteNameValue(this JsonWriter self, string name, string value)
		{
			self.WritePropertyName(name);
			self.WriteValue(value);
		}

		public static void WriteNameValue(this JsonWriter self, string name, float value)
		{
			self.WritePropertyName(name);
			self.WriteValue(value);
		}
	}

	class Program {
		static void CopyBSONToJSON(JsonWriter writer, BinaryReader reader)
		{
			using (var bson = new BsonDataReader(reader) { CloseInput = false }) {
				writer.WriteToken(bson);
			}
		}

		static void MemoryToJSON(JsonWriter writer, MemoryStream input)
		{
			BinaryReader reader = new BinaryReader(input);
			int count = reader.ReadInt32();
			writer.WritePropertyName("MemoryList");
			writer.WriteStartArray();
			for (int i = 0; i < count; i++) {
				writer.WriteStartObject();
				writer.WriteNameValue("Key", reader.ReadString());
				writer.WriteNameValue("Scope", reader.ReadInt32());
				string type = reader.ReadString();
				writer.WriteNameValue("Type", type);

				using (var bson = new BsonDataReader(reader) { CloseInput = false, ReadRootValueAsArray = true }) {
					writer.WritePropertyName("Value");
					writer.WriteToken(bson);
				}
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			NamedObjectToJSON(CopyBSONToJSON, writer, reader, "VariableReference");
			NamedObjectToJSON(CopyBSONToJSON, writer, reader, "Flags");
		}

		static void CurrentAudioToJSON(JsonWriter writer, MemoryStream input)
		{
			NamedObjectToJSON(CopyBSONToJSON, writer, new BinaryReader(input), "CurrentAudio");
		}

		static void ColorToJSON(JsonWriter writer, BinaryReader reader)
		{
			writer.WriteStartObject();
			writer.WriteNameValue("r", reader.ReadSingle());
			writer.WriteNameValue("g", reader.ReadSingle());
			writer.WriteNameValue("b", reader.ReadSingle());
			writer.WriteNameValue("a", reader.ReadSingle());
			writer.WriteEndObject();
		}

		static void Vector3ToJSON(JsonWriter writer, BinaryReader reader)
		{
			writer.WriteStartObject();
			writer.WriteNameValue("x", reader.ReadSingle());
			writer.WriteNameValue("y", reader.ReadSingle());
			writer.WriteNameValue("z", reader.ReadSingle());
			writer.WriteEndObject();
		}

		static void Vector2ToJSON(JsonWriter writer, BinaryReader reader)
		{
			writer.WriteStartObject();
			writer.WriteNameValue("x", reader.ReadSingle());
			writer.WriteNameValue("y", reader.ReadSingle());
			writer.WriteEndObject();
		}

		static JSONCopier LayerToJSON(int gameVersion)
		{
			return (writer, reader) => {
				writer.WriteStartObject();
				NamedObjectToJSON(Vector3ToJSON, writer, reader, "Position");
				NamedObjectToJSON(Vector3ToJSON, writer, reader, "Scale");
				writer.WriteNameValue("Filename", reader.ReadString());
				writer.WriteNameValue("Alpha", reader.ReadSingle());
				writer.WriteNameValue("Alignment", reader.ReadInt32());
				if (gameVersion >= 7) {
					NamedObjectToJSON(OptionalToJSON(Vector2ToJSON), writer, reader, "Origin");
					NamedObjectToJSON(OptionalToJSON(Vector2ToJSON), writer, reader, "ForceSize");
				}
				writer.WriteNameValue("ShaderType", reader.ReadInt32());
				writer.WriteEndObject();
			};
		}

		static void StackEntryToJSON(JsonWriter writer, BinaryReader reader)
		{
			writer.WriteStartObject();
			writer.WriteNameValue("Filename", reader.ReadString());
			writer.WriteNameValue("LineNum", reader.ReadInt32());
			writer.WriteEndObject();
		}

		public delegate void JSONCopier(JsonWriter writer, BinaryReader reader);

		static JSONCopier OptionalToJSON(JSONCopier copier)
		{
			return (writer, reader) => {
				bool exists = reader.ReadBoolean();
				if (exists) {
					copier(writer, reader);
				} else {
					writer.WriteNull();
				}
			};
		}

		static void NamedObjectToJSON(JSONCopier copier, JsonWriter writer, BinaryReader reader, string name)
		{
			writer.WritePropertyName(name);
			copier(writer, reader);
		}

		static void SceneToJSON(JsonWriter writer, MemoryStream input, int gameVersion)
		{
			BinaryReader reader = new BinaryReader(input);
			writer.WritePropertyName("Scene");
			writer.WriteStartObject();
			foreach (var name in new string[] { "FaceToUpperLayer", "UseFilm", "UseBlur", "UseHorizontalBlur" }) {
				writer.WriteNameValue(name, reader.ReadBoolean());
			}
			foreach (var name in new string[] { "FilmPower", "FilmType", "FilmStyle" }) {
				writer.WriteNameValue(name, reader.ReadInt32());
			}
			NamedObjectToJSON(ColorToJSON, writer, reader, "FilmColor");
			NamedObjectToJSON(LayerToJSON(gameVersion), writer, reader, "Background");
			NamedObjectToJSON(OptionalToJSON(LayerToJSON(gameVersion)), writer, reader, "FaceLayer");

			writer.WritePropertyName("Layers");
			writer.WriteStartObject();
			for (int i = 0; i < 64; i++) {
				if (reader.ReadBoolean()) {
					NamedObjectToJSON(LayerToJSON(gameVersion), writer, reader, i.ToString());
				}
			}
			writer.WriteEndObject();

			if (gameVersion >= 7) {
				NamedObjectToJSON(OptionalToJSON((writer, reader) => {
					writer.WriteStartObject();
					writer.WriteNameValue("CubemapName", reader.ReadString());
					writer.WriteNameValue("FragmentPrefab", reader.ReadString());
					writer.WriteEndObject();
				}), writer, reader, "FragmentController");
			}

			writer.WriteEndObject();
		}

		static string SaveFileToJSON(string path, int gameVersion)
		{
			byte[] array = File.ReadAllBytes(path);
			MGHelper.KeyEncode(array);
			byte[] buffer = CLZF2.Decompress(array);

			StringBuilder output = new StringBuilder();
			using (JsonTextWriter writer = new JsonTextWriter(new StringWriter(output)) { Formatting = Formatting.Indented }) {
				writer.DateFormatHandling = DateFormatHandling.IsoDateFormat;
				writer.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
				writer.WriteStartObject();
				using (MemoryStream input = new MemoryStream(buffer)) {
					using (BinaryReader reader = new BinaryReader(input)) {
						string magic = new string(reader.ReadChars(4));
						if (magic != "MGSV") {
							throw new FileLoadException("Save file does not appear to be valid! Invalid header.");
						}
						int num = reader.ReadInt32();
						if (num != 1) {
							throw new FileLoadException("Save file does not appear to be valid! Invalid version number.");
						}
						writer.WritePropertyName("Time");
						writer.WriteValue(DateTime.FromBinary(reader.ReadInt64()));
						foreach (var name in new string[] { "TextJP", "TextEN", "PrevTextJP", "PrevTextEN" }) {
							writer.WriteNameValue(name, reader.ReadString());
						}
						writer.WriteNameValue("PrevAppendState", reader.ReadBoolean());
						int stackSize = reader.ReadInt32();
						writer.WritePropertyName("CallStack");
						writer.WriteStartArray();
						for (int i = 0; i < stackSize; i++) {
							StackEntryToJSON(writer, reader);
						}
						writer.WriteEndArray();
						NamedObjectToJSON(StackEntryToJSON, writer, reader, "CurrentScript");
						MemoryToJSON(writer, input);
						CurrentAudioToJSON(writer, input);
						SceneToJSON(writer, input, gameVersion);
					}
				}
				writer.WriteEndObject();
			}

			return output.ToString();
		}

		static void Main(string[] args)
		{
			if (args.Length < 2) {
				Console.Error.WriteLine("Please supply the game version as a number (1 for Onikakushi, 5 for Meakashi, etc) and the save file as arguments");
				Console.Error.WriteLine("Or if extracting a global.dat, put 'global' instead of the number (like `dotnet run global global.dat`)");
				return;
			}

			string path = args[1];

			// global.dat processing
			if(args[0].ToLower() == "global")
			{
				Console.WriteLine(GlobalDataExtractor.ExtractAsFormattedJSON(path));
				return;
			}

			// normal save file processing
			int gameVersion = Int32.Parse(args[0]);
			string save = SaveFileToJSON(path, gameVersion);
			Console.WriteLine(save);
		}
	}
}
