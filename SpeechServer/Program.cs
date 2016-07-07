using System;
using System.Collections.Generic;
using System.Text;
using System.Speech.Recognition;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;
using SpeechTransport;

namespace SpeechServer
{
    class Program
    {
        private const string LOCALHOST = "127.0.0.1";
        static void Main(string[] args)
        {
            //args = new string[] { "1337", "hello;next"};

            if (args.Length < 2) return;

            using (var context = new SpeechRecognitionContext(args[1].Split(';')))
            using (var server = new SpeechServer(IPAddress.Parse(LOCALHOST), int.Parse(args[0])))
            {
                server.Listen(context);
            }
        }

    }
}
