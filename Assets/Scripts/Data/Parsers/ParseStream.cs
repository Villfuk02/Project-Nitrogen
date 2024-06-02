using System;

namespace Data.Parsers
{
    public abstract class ParseStream : IDisposable
    {
        char? current_;
        char? buffer_;
        bool ended_;
        readonly string unexpectedEndMessage_;

        protected ParseStream(string unexpectedEndMessage)
        {
            unexpectedEndMessage_ = unexpectedEndMessage;
        }

        public bool TryRead(out char c)
        {
            if (ended_)
            {
                c = default;
                return false;
            }

            if (buffer_ is char b)
            {
                c = b;
                buffer_ = null;
            }
            else
            {
                ReadPreprocess();
                if (!TryGetNext(out c))
                {
                    ended_ = true;
                    return false;
                }
            }

            if (!TryProcessOutgoing(ref c))
            {
                ended_ = true;
                return false;
            }

            current_ = c;
            return true;
        }

        public char Read()
        {
            if (!TryRead(out char c))
                throw new ParseException(this, unexpectedEndMessage_);
            return c;
        }

        protected abstract bool TryGetNext(out char c);

        public void ReturnLast()
        {
            if (current_ is not char c)
                throw new InvalidOperationException("Trying to return last character when at the start of the stream.");
            ProcessReturning(c);
            buffer_ = c;
            current_ = null;
        }

        protected virtual void ReadPreprocess() { }
        protected abstract bool TryProcessOutgoing(ref char c);
        protected abstract void ProcessReturning(char c);
        public abstract string GetStatus();
        public abstract void Dispose();
    }
}