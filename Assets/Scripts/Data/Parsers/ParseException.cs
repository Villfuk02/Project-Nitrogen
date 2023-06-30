using System;

namespace Data.Parsers
{
    public class ParseException : Exception
    {
        public ParseException(ParseStream s, string message) : base($"Parsing error {s.GetStatus()}: {message}") { }
        public ParseException(string status, string message) : base($"Parsing error {status}: {message}") { }
    }
}
