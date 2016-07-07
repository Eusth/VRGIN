using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace SpeechServer
{
    public class SpeechRecognitionContext : IDisposable
    {
        public List<string> Words = new List<string>();
        private Grammar _WordGrammar;
        private CultureInfo _Culture;

        public SpeechRecognitionEngine Engine { get; private set; }
        //public SpeechRecognizer Engine { get; private set; }

        public SpeechRecognitionContext(IEnumerable<string> wordList = null)
        {
            if(wordList != null)
            {
                Words.AddRange(wordList);
            }

            //new SpeechRecognizer().
            _Culture = new CultureInfo("en-US");
            Engine = new SpeechRecognitionEngine(_Culture);
            //Engine = new SpeechRecognizer();
            Engine.SetInputToDefaultAudioDevice();

            DictationGrammar dg = new DictationGrammar("grammar:dictation#pronunciation");
            dg.Name = "random";
            Engine.LoadGrammar(dg);
            
            RefreshGrammar();

            Engine.UpdateRecognizerSetting("ResponseSpeed", 20);
        }

        /// <summary>
        /// Updates the loaded grammar. Call this whenever you make changes to <see cref="Words"/>.
        /// </summary>
        public void RefreshGrammar()
        {
            if (_WordGrammar != null)
            {
                // Unload old grammad
                Engine.UnloadGrammar(_WordGrammar);
                _WordGrammar = null;
            }

            if (Words.Count > 0)
            {

                // Create grammar
                Choices words = new Choices();
                words.Add(Words.ToArray());

                GrammarBuilder gb = new GrammarBuilder();
                gb.Culture = _Culture;
                gb.Append(words);

                // Create the Grammar instance.
                _WordGrammar = new Grammar(gb);
                _WordGrammar.Name = "words";

                Engine.LoadGrammar(_WordGrammar);
            }

        }

        public void Dispose()
        {
            Engine.Dispose();
        }
    }
}
