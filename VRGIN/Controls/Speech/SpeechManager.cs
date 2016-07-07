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

namespace VRGIN.Controls.Speech
{
    public static class VoiceCommandsExtensions
    {
        public static bool IsMatch(this VoiceCommands command, String text)
        {
            return text.Replace(" ", "").Equals(Enum.GetName(typeof(VoiceCommands), command), StringComparison.OrdinalIgnoreCase);
        }
    }
    public enum VoiceCommands
    {
        Next,
        Previous,
        Start,
        Faster,
        Slower,
        ToggleMenu,
        Impersonate,
        Larger,
        Smaller,
        Save,
        SaveSettings,
        LoadSettings,
        ResetSettings
    }

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

        private const string LOCALHOST = "127.0.0.1";
        private const string CAMEL_CASE_REGEX = @"(\B[A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))";

        private string _ServerPath;
        private object LOCK = new object();

        private static System.Diagnostics.Process server;

        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized = delegate { };


        // Use this for initialization
        protected override void OnStart()
        {
            base.OnStart();

            _ServerPath = Application.dataPath + "/../Plugins/VR/SpeechServer.exe";

            if(!File.Exists(_ServerPath))
            {
                VRLog.Error("Could not find SpeechServer at {0}", _ServerPath);
                this.enabled = false;
                return;
            }

            var serverBin = new FileInfo(_ServerPath);

            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            if (server == null)
            {
                Debug.Log(serverBin.FullName);
                server = new System.Diagnostics.Process();
                server.StartInfo.FileName = serverBin.FullName;
                server.StartInfo.UseShellExecute = false;
                server.StartInfo.CreateNoWindow = true;
                server.StartInfo.Arguments = String.Format("{0} \"{1}\"", VR.Settings.SpeechRecognitionPort, GetVoiceCommands());
                
                server.Start();
            }
        }

        private String GetVoiceCommands()
        {
            return string.Join(";", Enum.GetNames(typeof(VoiceCommands)).Select( command => Regex.Replace(command, CAMEL_CASE_REGEX, " $1")).ToArray());
        }

        private void ReceiveData()
        {
            client = new UdpClient(VR.Settings.SpeechRecognitionPort);
            IPEndPoint myIP = new IPEndPoint(IPAddress.Parse(LOCALHOST), VR.Settings.SpeechRecognitionPort);

            while (this)
            {
                try
                {
                    string message;
                    byte[] data = client.Receive(ref myIP);
                    message = Encoding.UTF8.GetString(data);

                    lock (LOCK)
                    {
                        result = SpeechResult.Deserialize(message);
                    }
                }
                catch (Exception err)
                {
                    VRLog.Error(err);
                }
            }
        }
        
        void OnDisable()
        {
            if (server != null)
            {
                try
                {
                    server.Kill();
                }
                catch (Exception e) { }
                server.Dispose();
                server = null;
            }

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
