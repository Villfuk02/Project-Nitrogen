using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Data.Parsers
{
    public static class Parsers
    {
        public delegate T Parse<out T>(ParseStream stream);

        public delegate TTo Transform<out TTo, in TFrom>(ParseStream stream, Parse<TFrom> parse);

        public static char ParseChar(ParseStream stream)
        {
            return stream.Read();
        }

        public static string ParseWord(ParseStream stream)
        {
            StringBuilder sb = new();
            while (stream.TryRead(out char c))
            {
                if (char.IsWhiteSpace(c) || c == '{' || c == '}' || c == '*')
                {
                    stream.ReturnLast();
                    break;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        public static int SkipWhitespace(ParseStream stream, bool keepNewLines = false)
        {
            int count = 0;
            while (stream.TryRead(out char c))
            {
                if ((keepNewLines && c == '\n') || !char.IsWhiteSpace(c))
                {
                    stream.ReturnLast();
                    break;
                }

                count++;
            }

            return count;
        }

        public static void SkipToNextLine(ParseStream stream)
        {
            while (stream.TryRead(out char c) && c != '\n') { }
        }

        public static List<T> ParseList<T>(ParseStream stream, Parse<T> parse)
        {
            SkipWhitespace(stream);
            var res = new List<T>();
            while (stream.TryRead(out char _))
            {
                stream.ReturnLast();
                res.Add(parse(stream));
                SkipWhitespace(stream);
            }

            return res;
        }

        public static int ParseInt(ParseStream stream)
        {
            string w = ParseWord(stream);
            if (!int.TryParse(w, NumberStyles.Any, CultureInfo.InvariantCulture, out int r))
                throw new ParseException(stream, $"\"{w}\" is not a valid integer.");
            return r;
        }

        public static float ParseFloat(ParseStream stream)
        {
            string w = ParseWord(stream);
            if (!float.TryParse(w, NumberStyles.Any, CultureInfo.InvariantCulture, out float r) || !float.IsFinite(r))
                throw new ParseException(stream, $"\"{w}\" is not a valid floating point number.");
            return r;
        }

        public static bool ParseBool(ParseStream stream)
        {
            return ParseWord(stream) switch
            {
                "false" => false,
                "true" => true,
                string w => throw new ParseException(stream, $"\"{w}\" is not a valid bool.")
            };
        }

        public static T ParseLine<T>(ParseStream stream, Parse<T> parse)
        {
            using LineParseStream ls = new(stream);
            SkipWhitespace(ls);
            return parse(ls);
        }

        public static T ParseBlock<T>(ParseStream stream, Parse<T> parse)
        {
            using BlockParseStream bs = new(stream);
            SkipWhitespace(bs);
            return parse(bs);
        }

        public static T ParseAndLoadResource<T>(ParseStream stream) where T : Object
        {
            string path = ParseWord(stream);
            if (string.IsNullOrEmpty(path))
                throw new ParseException(stream, "Missing resource path.");
            return Resources.Load<T>(path) ?? throw new ParseException(stream, $"Could not load prefab at \"{path}\"");
        }

        public static Parse<T1> Chain<T1, T2>(Transform<T1, T2> transform, Parse<T2> parse) => s => transform(s, parse);
        public static Parse<T1> Chain<T1, T2, T3>(Transform<T1, T2> transform1, Transform<T2, T3> transform2, Parse<T3> parse) => Chain(transform1, Chain(transform2, parse));
        public static Parse<T1> Chain<T1, T2, T3, T4>(Transform<T1, T2> transform1, Transform<T2, T3> transform2, Transform<T3, T4> transform3, Parse<T4> parse) => Chain(transform1, Chain(transform2, transform3, parse));
        public static Parse<T1> Chain<T1, T2, T3, T4, T5>(Transform<T1, T2> transform1, Transform<T2, T3> transform2, Transform<T3, T4> transform3, Transform<T4, T5> transform4, Parse<T5> parse) => Chain(transform1, Chain(transform2, transform3, transform4, parse));

        public static bool IsPositive(int value, string valueName, out string err)
        {
            err = $"The value of property \"{valueName}\" ({value}) is not positive.";
            return value > 0;
        }

        public static bool IsNonnegative(int value, string valueName, out string err)
        {
            err = $"The value of property \"{valueName}\" ({value}) cannot be negative.";
            return value >= 0;
        }

        public static bool IsPositive(float value, string valueName, out string err)
        {
            err = $"The value of property \"{valueName}\" ({value}) is not positive.";
            return value > 0;
        }

        public static bool IsNonnegative(float value, string valueName, out string err)
        {
            err = $"The value of property \"{valueName}\" ({value}) cannot be negative.";
            return value >= 0;
        }

        public static bool IsAtLeast(int value, string valueName, int minimum, string minimumName, out string err)
        {
            err = $"The value of property \"{valueName}\" ({value}) is less than \"{minimumName}\" ({minimum}).";
            return value >= minimum;
        }

        public static bool IsAtMost(int value, string valueName, int maximum, string maximumName, out string err)
        {
            err = $"The value of property \"{valueName}\" ({value}) is greater than \"{maximumName}\" ({maximum}).";
            return value <= maximum;
        }

        public static bool IsInRange(int value, string valueName, int minimum, int maximum, out string err)
        {
            err = $"The value of property \"{valueName}\" ({value}) must be in the range {minimum} to {maximum}.";
            return value >= minimum && value <= maximum;
        }
    }
}