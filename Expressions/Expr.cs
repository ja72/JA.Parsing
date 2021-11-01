using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace JA.Expressions
{
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Xml.Linq;
    using JA.Expressions.Parsing;

    public abstract partial record Expr : IFormattable
    {
        #region State
        static readonly Dictionary<string, double> parameters = new();
        public static IReadOnlyDictionary<string, double> Parameters => parameters; 
        public static void ClearParameters() => parameters.Clear();
        #endregion

        public abstract int Rank { get; }
        protected internal abstract Expr Substitute(Expr variable, Expr value);
        public abstract IQuantity Eval(params (string sym, double val)[] parameters);
        protected internal abstract void Compile(ILGenerator gen, Dictionary<string, int> env);
        protected internal abstract void FillSymbols(ref List<string> variables);
        protected internal abstract void FillValues(ref List<double> values);

        public static Expr Assign(Expr Left, Expr Right)
        {
            // TODO: Handle special cases
            if (IsVectorizable(Left, Right, out var leftArray, out var rightArray))
            {
                Expr[] array = new Expr[leftArray.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = Assign(leftArray[i], rightArray[i]);
                }
                return Array(array);
            }

            if (Left.IsSymbol(out string sym) && Right.IsConstant(out double val, true))
            {
                return Const(sym, val);
            }

            return new AssignExpr(Left, Right);
        }

        #region Calculus
        /// <summary>
        /// Get the partial derivative of the expression with respect to a variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// </example>
        public abstract Expr PartialDerivative(VariableExpr variable);
        public Expr PartialDerivative(string symbol) => PartialDerivative(Variable(symbol));

        public Expr PartialDerivative(Expr expr)
        {
            if (expr.IsSymbol(out string sym))
            {
                return PartialDerivative(sym);
            }
            throw new NotSupportedException("Cannot take partial derivative with expression.");
        }
        public Expr Jacobian() => Jacobian(GetSymbols());
        public Expr Jacobian(params string[] symbols)
        {
            var array = new Expr[symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
            {
                array[i] = PartialDerivative(symbols[i]);
            }
            return Array(array);
        }

        public bool ExtractLinearSystem(string[] symbols, out Matrix A, out Vector b)
        {            
            b = null;
            if (IsAssign(out var lhs, out var rhs))
            {
                return (lhs-rhs).ExtractLinearSystem(symbols, out A, out b);
            }
            var Jt = Transpose(Jacobian(symbols));
            if (Jt.IsConstMatrix(out A))
            {
                var zeros = CreateVector(symbols.Length);
                var r = -Substitute(symbols, zeros);
                if (r.IsConstVector(out b))
                {
                    return true;
                }
            }
            return false;
        }
        internal Expr TotalDerivative(ref List<string> paramsAndDots)
        {
            var @params = paramsAndDots.Select(
                (item)=> ( item, Variable(item).Derivative() )).ToArray();
            return TotalDerivative(@params, ref paramsAndDots);
        }
        internal Expr TotalDerivative((string sym, Expr expr)[] parameters, ref List<string> paramsAndDots)
        {
            Expr body = 0;
            foreach (var (sym, expr) in parameters)
            {
                var q_dot = expr;
                if (q_dot.IsSymbol(out var sym_dot))
                {
                    paramsAndDots.Add(sym_dot);
                }
                body += this.PartialDerivative(sym) * q_dot;
            }
            return body;
        }
        #endregion

        #region Methods
        public Function GetFunction(string name, bool alphabetically = false)
        {
            return GetFunction(name, GetSymbols(alphabetically));
        }
        public Function GetFunction(string name, params string[] arguments)
        {
            return new Function(name, this, arguments);
        }
        public Function GetFunction(string name, string[] arguments, (string sym, double val)[] knownValues)
        {
            var expr = Substitute(knownValues);
            return new Function(name, expr, arguments);
        }
        public Expr Substitute(params (string sym, double val)[] knownValues)
        {
            Expr result = this;
            foreach (var (sym, val) in knownValues)
            {
                result = result.Substitute(sym, val);
            }
            return result;
        }
        public Expr Substitute(params (string sym, Expr expr)[] subExpressions)
        {
            Expr result = this;
            foreach (var (sym, expr) in subExpressions)
            {
                result = result.Substitute(sym, expr);
            }
            return result;
        }
        public Expr Substitute(string[] variables, Expr[] values)
        {
            return Substitute(variables.Zip(values, (x, y) => (x, y)).ToArray());
        }
        public string[] GetSymbols(bool alphabetically = true)
        {
            var all = new List<string>();
            FillSymbols(ref all);
            var list = all.Distinct();
            if (alphabetically)
            {
                list = list.OrderBy((x) => x);
            }
            return list.ToArray();
        }
        public double[] GetValues()
        {
            var list = new List<double>();
            FillValues(ref list);
            return list.ToArray();
        }
        #endregion

        #region Transformations
        public Expr Simplify()
        {
            Expr prev;
            Expr result = this;
            do
            {
                prev = result;

                switch (this)
                {
                    case UnaryExpr unary:
                        switch (unary.Op.Identifier)
                        {
                            case "+": result = unary.Argument; break;
                            case "-": result = -unary.Argument; break;
                        }
                        break;
                    case BinaryExpr binary:
                        switch (binary.Op.Identifier)
                        {
                            case "+": result = binary.Left + binary.Right; break;
                            case "-": result = binary.Left - binary.Right; break;
                            case "*": result = binary.Left * binary.Right; break;
                            case "/": result = binary.Left / binary.Right; break;
                        }
                        break;
                }

            } while (!result.Equals(prev));
            return result;
        }

        public Expr Substitute(params (Expr variable, Expr value)[] arguments)
        {
            Expr result = this;
            foreach (var (variable, value) in arguments)
            {
                result = result.Substitute(variable, value);
            }
            return result;
        }
        public Expr[] ToArray()
        {
            if (this.IsArray(out var array))
            {
                return array;
            }
            return new[] { this };
        }

        public Expr[][] ToMatrix()
        {
            if (IsMatrix(out var matrix))
            {
                return matrix;
            }
            return new[] { new[] { this } };
        }
        #endregion

        #region Parsing
        public static Expr Parse(string expression) => Parse(new Tokenizer(expression));
        static Expr Parse(Tokenizer tokenizer)
        {
            var parser = new ExprParser(tokenizer);
            return parser.ParseExpression();
        }
        #endregion

        #region Formatting
        public override string ToString()
            => ToString("g");
        public string ToString(string formatting)
            => ToString(formatting, null);
        public abstract string ToString(string formatting, IFormatProvider provider);
        #endregion                
    }

}
