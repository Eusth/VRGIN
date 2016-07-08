using SpeechTransport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using VRGIN.Core;
using System.Diagnostics;

namespace VRGIN.Controls.Speech
{
    public class SpeechRecognizedEventArgs : EventArgs
    {
        public SpeechResult Result { get; private set; }

        public SpeechRecognizedEventArgs(SpeechResult result)
        {
            Result = result;
        }
    }

    public class SpeechManager : ProtectedBehaviour
    {
        Thread receiveThread;
        UdpClient client;
        SpeechResult? result;

        const string LOCALHOST = "127.0.0.1";
        const string CAMEL_CASE_REGEX = @"(\B[A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))";
        const string DICT_PATH = @"UserData\dictionaries";
        string _ServerPath;
        object LOCK = new object();

        static System.Diagnostics.Process server;

        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized = delegate { };


        // Use this for initialization
        protected override void OnStart()
        {

            base.OnStart();
            InitializeDictionary();
            StartServer();
        }

        private void StartServer()
        {
            _ServerPath = Application.dataPath + "/../Plugins/VR/SpeechServer.exe";

            if (!File.Exists(_ServerPath))
            {
                VRLog.Error("Could not find SpeechServer at {0}", _ServerPath);
                this.enabled = false;
                return;
            }


            var serverBin = new FileInfo(_ServerPath);

            //receiveThread = new Thread(new ThreadStart(ReceiveData));
            //receiveThread.IsBackground = true;
            //receiveThread.Start();

            if (server == null)
            {
                VRLog.Info(serverBin.FullName);
                server = new System.Diagnostics.Process();
                server.StartInfo.FileName = serverBin.FullName;
                server.StartInfo.UseShellExecute = false;
                server.StartInfo.CreateNoWindow = true;
                server.StartInfo.Arguments = String.Format("--words \"{0}\" --locale {1}", GetVoiceCommands(), VR.Settings.Locale);

                server.StartInfo.RedirectStandardOutput = true;
                server.StartInfo.RedirectStandardError = true;
                server.StartInfo.RedirectStandardInput = true; // This makes sure that the child process will exit along with this application
                server.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                server.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                server.OutputDataReceived += OnOutputReceived;
                server.ErrorDataReceived += OnErrorReceived;
                
                VRLog.Info("Starting speech server: {0}", server.StartInfo.Arguments);

                server.Start();
                server.BeginOutputReadLine();
                server.BeginErrorReadLine();
                VRLog.Info("Started!");
            }
        }

        private void OnErrorReceived(object sender, DataReceivedEventArgs e)
        {
            VRLog.Error(e.Data);
        }

        private void InitializeDictionary()
        {
            var path = CombinePath(Application.dataPath, "..", DICT_PATH, VR.Settings.Locale + ".txt");
            var reader = new DictionaryReader(VR.Context.VoiceCommandType);

            // Load dictionary and save immediately
            VRLog.Info("Loading dictionary at {0}...", path);
            reader.LoadDictionary(path);
            VRLog.Info("Saving dictionary at {0}...", path);
            reader.SaveDictionary(path);
        }


        private string CombinePath(params string[] paths)
        {
            string res = paths[0];
            for(int i = 1; i < paths.Length; i++)
            {
                res = Path.Combine(res, paths[i]);
            }

            return res;
        }

        private void OnOutputReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                lock (LOCK)
                {
                    result = SpeechResult.Deserialize(e.Data);
                    VRLog.Info("RECEIVED MESSAGE: " + e.Data);
                }
            }
            catch (Exception err)
            {
                VRLog.Error(err);
            }
        }

        private String GetVoiceCommands()
        {
            return string.Join(";", DictionaryReader.ExtractCommandObjects(VR.Context.VoiceCommandType).SelectMany(command => command.Texts).ToArray());
        }

       
        void OnDisable()
        {
            if (receiveThread != null)
            {
                receiveThread.Abort();
                receiveThread = null;
            }

            client.Close();
        }

        // Update is called once per frame
        protected override void OnUpdate()
        {
            lock (LOCK)
            {
                if (result != null)
                {
                    SendMessage("OnSpeech", result.Value);
                    SpeechRecognized(this, new SpeechRecognizedEventArgs(result.Value));
                    result = null;
                }
            }
        }
    }
}
