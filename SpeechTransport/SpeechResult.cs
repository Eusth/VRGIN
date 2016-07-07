using System;

namespace SpeechTransport
{
    /// <summary>
    /// Simple struct that can be used to transport speech results over sockets.
    /// </summary>
    public struct SpeechResult
    {
        public string Text;
        public double Confidence;
        public bool Final;
        public int ID;

        public override string ToString()
        {
            return String.Format("{0}\t{1:0.00}\t{2}\t{3}", Text, Confidence, Final, ID);
        }


        public static SpeechResult Deserialize(string str)
        {
            var parts = str.Split('\t');
            if (parts.Length != 4) throw new Exception("Invalid format.");

            var result = new SpeechResult();
            result.Text = parts[0];
            result.Confidence = Convert.ToDouble(parts[1]);
            result.Final = Convert.ToBoolean(parts[2]);
            result.ID = Convert.ToInt32(parts[3]);

            return result;
        }
    }
}
