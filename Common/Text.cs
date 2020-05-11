using System;
using System.Collections.Generic;

namespace Common
{
    public class Text
    {
        private readonly string text;
        public int End { get; private set; }
        public List<string> Lines { get; private set; }

        private Text(string text)
        {
            this.text = text;
            End = text.Length;

            Lines = new List<string>(text.Split('\n'));
        }

        public static Text Of(string text)
        {
            return new Text(text);
        }

        public int Pos { get; private set; }
        public int Line { get; private set; }
        public int LinePos { get; private set; }
        public bool IsExhausted => Pos >= End;
        public char Current => Pos < End ? text[Pos] : '\0';
        public char Peek => Pos + 1 < End ? text[Pos + 1] : '\0';
        private string NextTwo => $"{Current}{Peek}";
        public string GetLine(int i) => Lines[i];
        
        public string Range(int start, int len)
        {
            return text.Substring(start, len);
        }

        public void Advance(int n = 1)
        {
            if (n < 0) throw new ArgumentException();
            var startPos = Pos;
            while (Pos < startPos + n && !IsExhausted)
            {
                if ("\n\r".IndexOf(Current) >= 0)
                {
                    Line++;
                    LinePos = 0;
                }
                else
                {
                    LinePos++;
                }

                Pos++;
            }
        }

        public char Next()
        {
            var p = Peek;

            if (p != '\0') Advance();

            return p;
        }

        public void SkipSpacesAndComments()
        {
            SkipSpaces();
            var done = false;
            var startPos = Pos;
            var startLine = Line;
            var startLinePos = LinePos;

            while (!done && !IsExhausted)
            {
                done = true;
                switch (NextTwo)
                {
                    // block comment, advance until end marker or EOF
                    case "{*":
                    {
                        Advance(2);
                        while (!IsExhausted && NextTwo != "*}")
                        {
                            done = false;
                            Advance();
                            if (!IsExhausted) continue;

                            // TODO: runaway comment error
                        }

                        Advance(2);
                        break;
                    }
                }

                SkipSpaces();
            }
        }

        public void SkipSpaces()
        {
            var curr = Current;

            while ("\r\n\t ".IndexOf(curr) >= 0 && !IsExhausted) curr = Next();
        }

        public void SkipLine()
        {
            while ("\r\n\v\xA".IndexOf(Current) < 0 && !IsExhausted) Advance();
            Advance();
        }

        public static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }
    }
}