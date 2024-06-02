namespace Data.Parsers
{
    public class LineParseStream : ParseStream
    {
        readonly ParseStream stream_;

        public LineParseStream(ParseStream stream) : base("Unexpected end of line.")
        {
            stream_ = stream;
        }

        protected override bool TryGetNext(out char c) => stream_.TryRead(out c);
        protected override bool TryProcessOutgoing(ref char c) => c != '\n';
        protected override void ProcessReturning(char c) { }
        public override string GetStatus() => stream_.GetStatus();

        public override void Dispose()
        {
            while (TryRead(out char _)) { }
        }
    }
}