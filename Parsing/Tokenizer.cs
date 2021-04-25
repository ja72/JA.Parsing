using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace JA.Parsing
{
    public enum Token
    {
        EOF,
        Add,
        Subtract,
        Multiply,
        Divide,
        Power,          // new addition, JA
        OpenParens,
        CloseParens,
        OpenBracket,
        CloseBracket,
        Comma,
        Identifier,
        Number,
    }

    /// <summary>
    /// Code taken from:
    /// https://github.com/toptensoftware/SimpleExpressionEngine
    /// </summary>
    public sealed class Tokenizer : IEnumerator<TokenNode>, IEnumerable<TokenNode>
    {
        public Tokenizer(string expression) 
        {
            _expression = expression;
            Reset();
        }
        readonly string _expression;
        TextReader _reader;
        char _currentChar;

        public TokenNode Current { get; private set; }
        object System.Collections.IEnumerator.Current { get => Current; }

        public IEnumerator<TokenNode> GetEnumerator() => this;
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        // Read the next character from the input strem
        // and store it in _currentChar, or load '\0' if EOF
        void NextChar()
        {
            int ch = _reader.Read();
            _currentChar = ch < 0 ? '\0' : (char)ch;
        }
        public void Reset()
        {
            _reader = new StringReader(_expression);
            NextChar();
            MoveNext();
        }
        // Read the next token from the input stream
        public bool MoveNext()
        {
            // Skip whitespace
            while (char.IsWhiteSpace(_currentChar))
            {
                NextChar();
            }

            // Special characters
            switch (_currentChar)
            {
                case '\0':
                    if (Current.Depth > 0)
                    {
                        Current = new TokenNode(Token.CloseParens, Current.Depth - 1);
                        return true;
                    }
                    Current = new TokenNode(Token.EOF);
                    return false;

                case '+':
                    NextChar();
                    Current = new TokenNode(Token.Add, Current.Depth);
                    return true; 

                case '-':
                    NextChar();
                    Current = new TokenNode(Token.Subtract, Current.Depth);
                    return true;

                case '*':
                    NextChar();
                    Current = new TokenNode(Token.Multiply, Current.Depth);
                    return true;

                case '/':
                    NextChar();
                    Current = new TokenNode(Token.Divide, Current.Depth);
                    return true;

                case '^':
                    NextChar();
                    Current = new TokenNode(Token.Power, Current.Depth);
                    return true;

                case '(':
                    NextChar();
                    Current = new TokenNode(Token.OpenParens, Current.Depth + 1);
                    return true;

                case ')':
                    NextChar();
                    Current = new TokenNode(Token.CloseParens, Current.Depth - 1);
                    return true;

                case ',':
                    NextChar();
                    Current = new TokenNode(Token.Comma, Current.Depth);
                    return true;

                case '[':
                    NextChar();
                    Current = new TokenNode(Token.OpenBracket, Current.Depth+1);
                    return true;

                case ']':
                    NextChar();
                    Current = new TokenNode(Token.CloseBracket, Current.Depth-1);
                    return true;
            }

            // Number?
            if (char.IsDigit(_currentChar) || _currentChar == '.')
            {
                // Capture digits/decimal point
                var sb = new StringBuilder();
                bool haveDecimalPoint = false;
                while (char.IsDigit(_currentChar) || (!haveDecimalPoint && _currentChar == '.'))
                {
                    sb.Append(_currentChar);
                    haveDecimalPoint = _currentChar == '.';
                    NextChar();
                }

                // Parse it
                Current = new TokenNode(double.Parse(sb.ToString(), CultureInfo.InvariantCulture), Current.Depth);
                
                return true;
            }

            // Identifier - starts with letter or underscore
            if (char.IsLetter(_currentChar) || _currentChar == '_')
            {
                var sb = new StringBuilder();

                // Accept letter, digit or underscore
                while (char.IsLetterOrDigit(_currentChar) || _currentChar == '_')
                {
                    sb.Append(_currentChar);
                    NextChar();
                }

                // Setup token
                Current = new TokenNode(sb.ToString(), Current.Depth);
                
                return true;
            }
            return false;
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposed)
        {
            if (disposed)
            {
                _reader.Dispose();
            }
        }
     
    }

    public struct TokenNode
    {
        private TokenNode(TokenNode node, int depth) 
        {
            this = node;
            this.Depth = depth;
        }
        public TokenNode(Token token, int depth=0)
        {
            this.Token = token;
            this.Number = 0;
            this.Identifier = null;
            this.Depth = depth;
        }
        public TokenNode(double number, int depth = 0)
        {
            this.Token = Token.Number;
            this.Number = number;
            this.Identifier = null;
            this.Depth = depth;
        }
        public TokenNode(string identifier, int depth = 0)
        {
            this.Token = Token.Identifier;
            this.Identifier = identifier;
            this.Number = 0;
            this.Depth = depth;
        }
        public static readonly TokenNode Empty = new TokenNode(Token.EOF);

        public static TokenNode operator +(TokenNode node, int level)
            => new TokenNode(node, node.Depth + level);
        public static TokenNode operator -(TokenNode node, int level)
            => new TokenNode(node, node.Depth - level);

        public Token Token { get; }
        public double Number { get; }
        public string Identifier { get;  }
        public int Depth { get; }

        public override string ToString()
        {
            var tab = new string('\t', Depth);
            switch (Token)
            {
                case Token.Identifier:
                    return $"{tab}{Token}({Identifier})";
                case Token.Number:
                    return $"{tab}{Token}({Number})";
                default:
                    return $"{tab}{Token}";
            }
        }
    }
    
}
