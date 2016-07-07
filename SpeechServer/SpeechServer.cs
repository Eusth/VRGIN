using SpeechTransport;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Speech.Recognition;

namespace SpeechServer
{
    class SpeechServer : IDisposable
    {
        public int Port { get; private set; }
        public IPAddress Host { get; private set; }

        private Socket _Socket;
        private IPEndPoint _IPEndPoint;
        private ConcurrentQueue<SpeechResult> _Queue = new ConcurrentQueue<SpeechResult>();

        private object _Lock = new object();
        private bool _IsListening = false;
        private SpeechRecognitionContext _CurrentContext;

        private int _IdCounter = 0;


        public SpeechServer(IPAddress host, int port)
        {
            Port = port;
            Host = host;

            Connect();
        }

        private void Connect()
        {
            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _IPEndPoint = new IPEndPoint(Host, Port);
        }

        private void Send(ref SpeechResult result)
        {
            Console.WriteLine(result);

            var data = Encoding.UTF8.GetBytes(result.ToString());
            _Socket.SendTo(data, _IPEndPoint);
        }

        public void Listen(SpeechRecognitionContext context)
        {
            // Only one listener at a time!
            if (_IsListening) return;

            _IsListening = true;
            LoadContext(context);

            while (_IsListening)
            {
                lock (_Lock)
                {
                    Monitor.Wait(_Lock);
                }

                while(_IsListening && !_Queue.IsEmpty)
                {
                    SpeechResult payload;
                    while (_Queue.TryDequeue(out payload))
                    {
                        Send(ref payload);
                    }
                }
            }
        }

        public async Task ListenAsync(SpeechRecognitionContext context)
        {
            await Task.Factory.StartNew(delegate
            {
                Listen(context);
            });
        }

        public void Stop()
        {
            _IsListening = false;
            UnloadContext();

            // Wake up for the listener to die
            lock (_Lock)
            {
                Monitor.PulseAll(_Lock);
            }
        }

        public void Dispose()
        {
            _Socket.Dispose();
            UnloadContext();
        }

        private void LoadContext(SpeechRecognitionContext context)
        {
            _CurrentContext = context;

            _CurrentContext.Engine.SpeechRecognized += OnSpeechRecognized;
            _CurrentContext.Engine.SpeechHypothesized += OnSpeechHypothesized;

            //_CurrentContext.Engine.Enabled = true;
            _CurrentContext.Engine.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void UnloadContext()
        {
            if (_CurrentContext == null) return;


            _CurrentContext.Engine.SpeechRecognized -= OnSpeechRecognized;
            _CurrentContext.Engine.SpeechHypothesized -= OnSpeechHypothesized;

            //_CurrentContext.Engine.Enabled = false;
            _CurrentContext.Engine.RecognizeAsyncStop();

            _CurrentContext = null;
        }

        private void OnSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (e.Result.Grammar.Name != "random")
            {
                _Queue.Enqueue(
                    new SpeechResult()
                    {
                        Text = e.Result.Text,
                        Confidence = e.Result.Confidence,
                        Final = false,
                        ID = _IdCounter
                    }
                );

                lock(_Lock)
                {
                    Monitor.PulseAll(_Lock);
                }
            }
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Grammar.Name != "random")
            {
                _Queue.Enqueue(
                    new SpeechResult()
                    {
                        Text = e.Result.Text,
                        Confidence = e.Result.Confidence,
                        Final = true,
                        ID = ++_IdCounter
                    }
                );

                lock (_Lock)
                {
                    Monitor.PulseAll(_Lock);
                }
            }
        }





    }
}
