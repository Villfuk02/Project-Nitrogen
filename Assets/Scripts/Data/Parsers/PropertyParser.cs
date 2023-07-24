using System;
using System.Collections.Generic;
using static Data.Parsers.Parsers;

namespace Data.Parsers
{
    public class PropertyParser
    {
        protected readonly Dictionary<string, Property> properties = new();
        protected string endStatus;

        public void Parse(ParseStream stream)
        {
            while (true)
            {
                SkipWhitespace(stream);
                if (!stream.TryRead(out char c))
                    break;
                if (SpecialBehavior(c, stream))
                    continue;
                stream.ReturnLast();
                string w = ParseWord(stream);
                if (string.IsNullOrEmpty(w))
                {
                    if (!stream.TryRead(out char n))
                        break;
                    throw new ParseException(stream, $"Unexpected character \'{n}\'.");
                }
                if (!properties.TryGetValue(w, out Property p))
                    throw new ParseException(stream, $"Unknown property \"{w}\".");
                SkipWhitespace(stream);
                p.Parse(stream);
            }
            endStatus = stream.GetStatus();
        }

        protected virtual bool SpecialBehavior(char justRead, ParseStream stream)
        {
            return false;
        }

        public Func<T> Register<T>(string name, Parse<T> parse)
        {
            var p = new Property<T>(parse);
            properties.Add(name, p);
            return () => GetProperty<T>(name);
        }
        public Func<T> Register<T>(string name, Parse<T> parse, T defaultValue)
        {
            var p = new Property<T>(parse, defaultValue);
            properties.Add(name, p);
            return () => GetProperty<T>(name);
        }

        TP GetProperty<TP>(string name)
        {
            var p = (Property<TP>)properties[name];
            if (!p.TryGetValue(out TP value))
                throw new ParseException(endStatus, $"The value of property \"{name}\" was not assigned.");
            return value;
        }
    }

    public class PropertyParserWithNamedExtra<T> : PropertyParser
    {
        public delegate T ExtraParse(string name, ParseStream stream);
        ExtraParse extraParse_;
        public List<T> ParsedExtra { get; init; } = new();

        public void RegisterExtraParser(ExtraParse extraParse)
        {
            extraParse_ = extraParse;
        }

        protected override bool SpecialBehavior(char justRead, ParseStream stream)
        {
            if (justRead != '*')
                return false;

            SkipWhitespace(stream);
            string w = ParseWord(stream);
            if (string.IsNullOrEmpty(w))
                throw new ParseException(stream, "Missing identifier.");
            SkipWhitespace(stream);
            ParsedExtra.Add(extraParse_(w, stream));
            return true;
        }
    }

    public abstract class Property
    {
        public bool Assigned { get; protected set; }
        public readonly Type type;

        protected Property(bool assigned, Type type)
        {
            Assigned = assigned;
            this.type = type;
        }

        public abstract void Parse(ParseStream stream);
    }

    public class Property<T> : Property
    {
        T value_;
        readonly Parse<T> parse_;

        public Property(Parse<T> parse) : base(false, typeof(T))
        {
            value_ = default;
            parse_ = parse;
        }
        public Property(Parse<T> parse, T defaultValue) : base(true, typeof(T))
        {
            value_ = defaultValue;
            parse_ = parse;
        }

        public override void Parse(ParseStream stream)
        {
            Assigned = true;
            value_ = parse_(stream);
        }

        public bool TryGetValue(out T value)
        {
            value = value_;
            return Assigned;
        }
    }
}