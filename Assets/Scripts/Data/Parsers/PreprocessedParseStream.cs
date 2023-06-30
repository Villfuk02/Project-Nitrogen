namespace Data.Parsers
{
    public class PreprocessedParseStream : ParseStream
    {
        readonly CommentErasedParseStream stream_;
        bool skipWhitespace_;

        public PreprocessedParseStream(string path) : base(ReaderParseStream.EOF)
        {
            stream_ = new(path);
            skipWhitespace_ = true;
        }

        protected override void ReadPreprocess()
        {
            if (!skipWhitespace_)
                return;

            Parsers.SkipWhitespace(stream_);
            skipWhitespace_ = false;
        }

        protected override bool TryGetNext(out char c) => stream_.TryRead(out c);

        protected override bool ProcessOutgoing(ref char c)
        {
            if (c == '\n')
                skipWhitespace_ = true;
            return false;
        }

        protected override void ProcessReturning(char c) { }
        public override string GetStatus() => stream_.GetStatus();
        public override void Dispose() => stream_.Dispose();
    }

    internal class CommentErasedParseStream : ParseStream
    {
        readonly ReaderParseStream stream_;

        public CommentErasedParseStream(string path) : base(ReaderParseStream.EOF)
        {
            stream_ = new(path);
        }

        protected override bool TryGetNext(out char c) => stream_.TryRead(out c);

        protected override bool ProcessOutgoing(ref char c)
        {
            switch (c)
            {
                case '#':
                    Parsers.SkipToNextLine(stream_);
                    c = '\n';
                    break;
                case '%':
                    while (stream_.Read() != '%') { }
                    c = '\n';
                    break;
            }
            return false;
        }

        protected override void ProcessReturning(char c) { }
        public override string GetStatus() => stream_.GetStatus();
        public override void Dispose() => stream_.Dispose();
    }
}