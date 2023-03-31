using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace HiguSaveConverter
{
    public class GlobalData
    {
        public Dictionary<int, int> flags;
        public List<string> cgflags;
        public Dictionary<string, List<int>> readText;
        public Dictionary<string, int> graphicsPresetState;
    }

    public class GlobalDataExtractor
    {
        public static string ExtractAsFormattedJSON(string path)
        {
            GlobalData globalData = Extract(path);

            var serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            StringWriter sw = new StringWriter();
            serializer.Serialize(sw, globalData);

            return sw.ToString();
        }

        // The following is a modified version of the "LoadGlobals()" function in
        // Assets.Scripts.Core.Buriko/BurikoMemory.cs from the Modded Higurashi DLL
        // It will also load old modded or vanilla global.dat, but the "graphicsPresetState" dictionary wiil be empty and a warning will be printed.
        public static GlobalData Extract(string path)
        {
            byte[] array = File.ReadAllBytes(path);
            MGHelper.KeyEncode(array);
            byte[] buffer = CLZF2.Decompress(array);

            GlobalData globalData = new GlobalData();

            JsonSerializer jsonSerializer = new JsonSerializer();
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BsonDataReader reader = new BsonDataReader(stream) { CloseInput = false })
                {
                    // was: globalFlags = jsonSerializer.Deserialize<Dictionary<int, int>>(reader);
                    // if global.dat exists but a new build introduced a new global with a default value, then the default value would be overwritten.
                    // Replace each key-val pair instead
                    // globalFlags.MergeOverwrite();
                    globalData.flags = jsonSerializer.Deserialize<Dictionary<int, int>>(reader);
                }
                using (BsonDataReader reader = new BsonDataReader(stream) { CloseInput = false, ReadRootValueAsArray = true })
                {
                    globalData.cgflags = jsonSerializer.Deserialize<List<string>>(reader);
                }
                using (BsonDataReader reader = new BsonDataReader(stream) { CloseInput = false })
                {
                    globalData.readText = jsonSerializer.Deserialize<Dictionary<string, List<int>>>(reader);
                }
                try
                {
                    using (BsonDataReader reader = new BsonDataReader(stream) { CloseInput = false })
                    {
                        globalData.graphicsPresetState = jsonSerializer.Deserialize<Dictionary<string, int>>(reader);
                        if (globalData.graphicsPresetState == null)
                        {
                            Console.Error.WriteLine("WARNING: Failed to load graphics preset state (serializer returned null)! Probably is old or vanilla global.dat file missing this data.");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error: Failed to load graphics preset state! Exception: " + e);
                }
            }

            return globalData;
        }
    }
}