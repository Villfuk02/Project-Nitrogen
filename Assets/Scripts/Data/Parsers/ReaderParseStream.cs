using System.IO;

namespace Data.Parsers
{
    public class ReaderParseStream : ParseStream
    {
        public static readonly string EOF = "Unexpected end of file.";
        readonly StreamReader reader_;
        public string Path { get; init; }
        public int Line { get; private set; }

        public ReaderParseStream(string path) : base(EOF)
        {
            Path = path;
            reader_ = new(path);
        }

        protected override void ReadPreprocess()
        {
            if (Line == 0)
                Line++;
        }

        protected override bool TryGetNext(out char c)
        {
            int r = reader_.Read();
            c = (char)r;
            return r != -1;
        }

        protected override bool ProcessOutgoing(ref char c)
        {
            if (c == '\n')
                Line++;
            return false;
        }

        protected override void ProcessReturning(char c)
        {
            if (c == '\n')
                Line--;
        }

        public override string GetStatus() => $"in \"{Path}\" on line {Line}";

        public override void Dispose() => reader_.Dispose();
    }
}
