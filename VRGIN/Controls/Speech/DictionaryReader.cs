using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VRGIN.Core;

namespace VRGIN.Controls.Speech
{
    public class DictionaryReader
    {
        public Type BaseType { get; private set; }
        Dictionary<string, VoiceCommand> _Dictionary = new Dictionary<string, VoiceCommand>();


        public DictionaryReader(Type type)
        {
            if (IsVoiceCommandType(type))
            {
                BaseType = type;
            } else {
                BaseType = typeof(VoiceCommand);
                VRLog.Error("Invalid VoiceCommand type! {0}", type);
            }

            BuildCommandDictionary();
        }
       
        /// <summary>
        /// Loads the dictionary at <code>path</code> into the VoiceCommand objects.
        /// </summary>
        /// <param name="path"></param>
        public void LoadDictionary(string path)
        {
            if (File.Exists(path)) {
                using (var reader = new StreamReader(File.OpenRead(path), Encoding.UTF8))
                {
                    VoiceCommand context = null;
                    while(!reader.EndOfStream)
                    {
                        string line = reader.ReadLine().Trim().ToLowerInvariant();
                        if(IsCommand(line))
                        {
                            if(_Dictionary.TryGetValue(ExtractCommand(line), out context))
                            {
                                context.Texts.Clear();
                            }
                        } else if(context != null && line.Length > 0) {
                            context.Texts.Add(line);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves the current state of the VoiceCommand objects into <code>path</code>.
        /// </summary>
        /// <param name="path"></param>
        public void SaveDictionary(string path)
        {
            EnsurePath(path);

            using (var writer = new StreamWriter(File.Open(path, FileMode.OpenOrCreate), Encoding.UTF8))
            {
                // Truncate
                writer.BaseStream.SetLength(0);

                foreach(var field in ExtractCommands(BaseType))
                {
                    // Write command
                    writer.WriteLine("[{0}]", field.Name);

                    var command = field.GetValue(null) as VoiceCommand;
                    if(command != null)
                    {
                        foreach (var line in command.Texts) {
                            writer.WriteLine(line);
                        }
                    }
                    writer.WriteLine();
                }
            }
        }
        
        void EnsurePath(string path)
        {
            if (!File.Exists(path))
            {
                // Create file
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
        }
        
        void BuildCommandDictionary()
        {
            foreach (var field in ExtractCommands(BaseType))
            {
                VoiceCommand command = field.GetValue(null) as VoiceCommand;
                if (command != null)
                {
                    _Dictionary[field.Name.ToLowerInvariant()] = command;
                }
            }
        }

        public static IEnumerable<FieldInfo> ExtractCommands(Type type)
        {
            return type
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                .Where(field => IsVoiceCommandType(field.FieldType));
        }

        public static IEnumerable<VoiceCommand> ExtractCommandObjects(Type type)
        {
            return ExtractCommands(type).Select(t => t.GetValue(null) as VoiceCommand).Where(t => t != null);
        }

        static bool IsVoiceCommandType(Type type)
        {
            return typeof(VoiceCommand).IsAssignableFrom(type);
        }

        static bool IsCommand(string line)
        {
            return line.Length > 2 && line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal);
        }
        
        static string ExtractCommand(string line)
        {
            return line.Substring(1, line.Length - 2);
        }

    }
}
