using System;
using System.Collections.Generic;
using static Data.Parsers.Parsers;

namespace Data.Parsers
{
    public class PropertyParser
    {
        protected readonly Dictionary<string, Property> properties = new();

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

            foreach ((string _, var property) in properties)
            {
                if (property.Assigned)
                    property.EnsureValid();
                else
                    property.status = stream.GetStatus();
            }
        }

        protected virtual bool SpecialBehavior(char justRead, ParseStream stream)
        {
            return false;
        }

        public IReadOnlyProperty<T> Register<T>(string name, Parse<T> parse)
        {
            var p = new Property<T>(name, parse);
            properties.Add(name, p);
            return p;
        }

        public IReadOnlyProperty<T> Register<T>(string name, Parse<T> parse, T defaultValue)
        {
            var p = new Property<T>(name, parse, defaultValue);
            properties.Add(name, p);
            return p;
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
        public string Name { get; init; }
        public readonly Type type;
        public bool Assigned { get; protected set; }
        public string status = "default value";

        protected Property(string name, Type type, bool hasDefaultValue)
        {
            Assigned = hasDefaultValue;
            this.type = type;
            Name = name;
        }

        public abstract void Parse(ParseStream stream);

        public abstract void EnsureValid();
    }

    public interface IReadOnlyProperty<T>
    {
        public delegate bool IsValid(T value, out string error);

        public string Name { get; }
        public void SetValidator(IsValid validator);
        public bool TryGetValue(out T value);
        public T GetValue();
    }

    public class Property<T> : Property, IReadOnlyProperty<T>
    {
        T value_;
        readonly Parse<T> parse_;
        IReadOnlyProperty<T>.IsValid? isValid_;

        public Property(string name, Parse<T> parse) : base(name, typeof(T), false)
        {
            value_ = default;
            parse_ = parse;
        }

        public Property(string name, Parse<T> parse, T defaultValue) : base(name, typeof(T), true)
        {
            value_ = defaultValue;
            parse_ = parse;
        }

        public void SetValidator(IReadOnlyProperty<T>.IsValid validator) => isValid_ = validator;

        public override void Parse(ParseStream stream)
        {
            status = stream.GetStatus();
            value_ = parse_(stream);
            Assigned = true;
        }

        public override void EnsureValid()
        {
            if (isValid_ is not null && !isValid_(GetValue(), out var err))
                throw new ParseException(status, err);
        }

        public bool TryGetValue(out T value)
        {
            value = value_;
            return Assigned;
        }

        public T GetValue()
        {
            if (!Assigned)
                throw new ParseException(status, $"The value of property \"{Name}\" was not assigned.");
            return value_;
        }
    }
}