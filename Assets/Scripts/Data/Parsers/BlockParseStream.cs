namespace Data.Parsers
{
    public class BlockParseStream : ParseStream
    {
        readonly ParseStream stream_;
        int depth_;

        public BlockParseStream(ParseStream stream) : base("Unexpected end of block.")
        {
            stream_ = stream;
            depth_ = -1;
        }

        protected override bool TryGetNext(out char c) => stream_.TryRead(out c);

        protected override bool TryProcessOutgoing(ref char c)
        {
            switch (c)
            {
                case '{':
                    depth_++;
                    if (depth_ == 0)
                        return TryRead(out c);
                    break;
                case '}':
                    depth_--;
                    break;
            }

            return depth_ >= 0;
        }

        protected override void ProcessReturning(char c)
        {
            switch (c)
            {
                case '{':
                    depth_--;
                    break;
                case '}':
                    depth_++;
                    break;
            }
        }

        public override string GetStatus() => stream_.GetStatus();

        public override void Dispose()
        {
            while (TryRead(out char _)) { }
        }
    }
}