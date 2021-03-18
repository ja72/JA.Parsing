using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Math;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.Serialization;

namespace JA.Parsing
{
    public class Parser
    {

        // Constructor - just store the tokenizer
        public Parser(Tokenizer tokenizer)
        {
            _tokenizer = tokenizer;
        }

        readonly Tokenizer _tokenizer;

        // Parse an entire expression and check EOF was reached
        public Expr ParseExpression()
        {
            // For the moment, all we understand is add and subtract
            var expr = ParseAddSubtract();

            // Check everything was consumed
            if (_tokenizer.Current.Token != Token.EOF)
                throw new SyntaxException("Unexpected characters at end of expression");

            return expr;
        }

        // Parse an sequence of add/subtract operators
        Expr ParseAddSubtract()
        {
            // Parse the left hand side
            var lhs = ParseMultiplyDivide();

            while (true)
            {
                // Work out the operator
                string op = null;
                switch (_tokenizer.Current.Token)
                {
                    case Token.Add:
                        op = "+";
                        break;
                    case Token.Subtract:
                        op = "-";
                        break;
                    default:
                        // Binary operator not found.
                        return lhs;
                }

                // Skip the operator
                _tokenizer.MoveNext();

                // Parse the right hand side of the expression
                var rhs = ParseMultiplyDivide();

                // Create a binary node and use it as the left-hand side from now on
                switch (op)
                {
                    case "+":
                        lhs = Expr.Add(lhs, rhs);
                        break;
                    case "-":
                        lhs = Expr.Subtract(lhs, rhs);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        // Parse an sequence of add/subtract operators
        Expr ParseMultiplyDivide()
        {
            // Parse the left hand side
            var lhs = ParsePowers();

            while (true)
            {
                // Work out the operator
                string op = null;
                switch (_tokenizer.Current.Token)
                {
                    case Token.Multiply:
                        op = "*";
                        break;
                    case Token.Divide:
                        op = "/";
                        break;
                    default:
                        // Binary operator not found.
                        return lhs;
                }

                // Skip the operator
                _tokenizer.MoveNext();

                // Parse the right hand side of the expression
                var rhs = ParsePowers();

                // Create a binary node and use it as the left-hand side from now on
                switch (op)
                {
                    case "*":
                        lhs = Expr.Multiply(lhs, rhs);
                        break;
                    case "/":
                        lhs = Expr.Divide(lhs, rhs);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        Expr ParsePowers()
        {
            // Parse the left hand side
            var lhs = ParseUnary();
            while (true)
            {
                // Work out the operator
                string op = null;
                switch (_tokenizer.Current.Token)
                {
                    case Token.Power:
                        op = "^";
                        break;
                    default:
                        // Binary operator not found.
                        return lhs;
                }

                // Skip the operator
                _tokenizer.MoveNext();

                // Parse the right hand side of the expression
                var rhs = ParseUnary();

                // Create a binary node and use it as the left-hand side from now on
                switch (op)
                {
                    case "^":
                        lhs = Expr.Power(lhs, rhs);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

        }

        // Parse a unary operator (eg: negative/positive)
        Expr ParseUnary()
        {
            while (true)
            {
                // Positive operator is a no-op so just skip it
                if (_tokenizer.Current.Token == Token.Add)
                {
                    // Skip
                    _tokenizer.MoveNext();
                    continue;
                }

                // Negative operator
                if (_tokenizer.Current.Token == Token.Subtract)
                {
                    // Skip
                    _tokenizer.MoveNext();

                    // Parse RHS 
                    // Note this recurses to self to support negative of a negative
                    var rhs = ParseUnary();

                    // Create unary node
                    return new UnaryOperatorExpr("-", rhs);
                }

                // No positive/negative operator so parse a leaf node
                return ParseLeaf();
            }
        }

        // Parse a leaf node
        // (For the moment this is just a number)
        Expr ParseLeaf()
        {
            // Is it a number?
            if (_tokenizer.Current.Token == Token.Number)
            {
                var node = Expr.Const(_tokenizer.Current.Number);
                _tokenizer.MoveNext();
                return node;
            }

            // Parenthesis?
            if (_tokenizer.Current.Token == Token.OpenParens)
            {
                // Skip '('
                _tokenizer.MoveNext();

                // Parse a top-level expression
                var node = ParseAddSubtract();

                // Check and skip ')'
                if (_tokenizer.Current.Token != Token.CloseParens)
                    throw new SyntaxException("Missing close parenthesis");
                _tokenizer.MoveNext();

                // Return
                return node;
            }

            // Variable
            if (_tokenizer.Current.Token == Token.Identifier)
            {
                // Capture the name and skip it
                var name = _tokenizer.Current.Identifier;
                _tokenizer.MoveNext();

                // Parens indicate a function call, otherwise just a variable
                if (_tokenizer.Current.Token != Token.OpenParens)
                {
                    return Expr.VariableOrConst(name);
                }
                else
                {
                    // Function call

                    // Skip parens
                    _tokenizer.MoveNext();

                    // Parse arguments
                    var arguments = new List<Expr>();
                    while (true)
                    {
                        // Parse argument and add to list
                        arguments.Add(ParseAddSubtract());

                        // Is there another argument?
                        if (_tokenizer.Current.Token == Token.Comma)
                        {
                            _tokenizer.MoveNext();
                            continue;
                        }

                        // Get out
                        break;
                    }

                    // Check and skip ')'
                    if (_tokenizer.Current.Token != Token.CloseParens)
                        throw new SyntaxException("Missing close parenthesis");
                    _tokenizer.MoveNext();

                    // Create the function call node
                    switch (arguments.Count)
                    {
                        case 1:
                            return new UnaryFunctionExpr(name, arguments[0]);
                        case 2:
                            return new BinaryFunctionExpr(name, arguments[0], arguments[1]);
                        default:
                            throw new SyntaxException("Invalid number of arguments");
                    }
                }
            }

            // Don't Understand
            throw new SyntaxException($"Unexpected token: {_tokenizer.Current.Token}");
        }

    }

    // Exception for syntax errors
    [Serializable]
    public class SyntaxException : Exception
    {
        public SyntaxException() { }
        public SyntaxException(string message) : base(message) { }
        public SyntaxException(string message, Exception inner) : base(message, inner) { }
        protected SyntaxException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
