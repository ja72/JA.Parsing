using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace JA.Parsing
{
    public abstract class UnaryExpr : Expr
    {
        static readonly Dictionary<string, Func<double, double>> functions = new Dictionary<string, Func<double, double>>()
        {
            ["+"]       = (x) => x,
            ["-"]       = (x) => -x,
            ["pi"]      = (x) => Math.PI * x,
            ["inv"]     = (x) => 1/x,
            ["abs"]     = (x) => Math.Abs(x),
            ["sign"]    = (x) => Math.Sign(x),
            ["exp"]     = (x) => Math.Exp(x),
            ["ln"]      = (x) => Math.Log(x),
            ["sqr"]     = (x) => x * x,
            ["cub"]     = (x) => x * x * x,
            ["sqrt"]    = (x) => Math.Sqrt(x),
            ["cbrt"]    = (x) => Math.Pow(x, 1/3.0),
            ["floor"]   = (x) => Math.Floor(x),
            ["ceil"]    = (x) => Math.Ceiling(x),
            ["round"]   = (x) => Math.Round(x),
            ["sin"]     = (x) => Math.Sin(x),
            ["cos"]     = (x) => Math.Cos(x),
            ["tan"]     = (x) => Math.Tan(x),
            ["sinh"]    = (x) => Math.Sinh(x),
            ["cosh"]    = (x) => Math.Cosh(x),
            ["tanh"]    = (x) => Math.Tanh(x),
            ["asin"]    = (x) => Math.Asin(x),
            ["acos"]    = (x) => Math.Acos(x),
            ["atan"]    = (x) => Math.Atan(x),
            ["asinh"]   = (x) => Math.Log(x + Math.Sqrt(x*x+1)),
            ["acosh"]   = (x) => Math.Log(x + Math.Sqrt(x*x-1)),
            ["atanh"]   = (x) => -Math.Log((1-x)/(1+x))/2,
        };

        protected UnaryExpr(string op, Expr arg)
        {
            this.Key = op;
            try
            {
                this.Function = functions[op];
            }
            catch (KeyNotFoundException ex)
            {
                Debug.WriteLine(ex.ToString());
                throw new ArgumentException($"Operator {op} not found.", nameof(op));
            }
            this.Argument = arg;
        }
        protected UnaryExpr(string op, Func<double, double> function, Expr arg)
        {
            this.Key = op;
            this.Function = function;
            this.Argument = arg;
        }

        public Func<double, double> Function { get; }
        public Expr Argument { get; }
        protected string Key { get; }

        public override double Eval(params (string sym, double val)[] parameters)
        {
            return Function(Argument.Eval(parameters));
        }

        public override Expr Partial(VariableExpr variable)
        {
            Expr x = Argument, xp = Argument.Partial(variable);
            switch (Key)
            {
                case "+": return xp;
                case "-": return -xp;
                case "pi": return Math.PI*xp;
                case "inv": return -xp/(x^2);
                case "abs": return Sign(x)*xp;
                case "sign": return 0;
                case "exp": return Exp(x)*xp;
                case "ln": return xp/x;
                case "sqr": return 2*x*xp;
                case "cub": return 3*(x^2)*xp;
                case "sqrt": return xp/(2*Sqrt(x));
                case "cbrt": return xp/(3*(x^(2.0/3)));
                case "floor": return 0;
                case "ceil": return 0;
                case "round": return 0;
                case "sin":  return xp*Cos(x);
                case "cos":  return -xp*Sin(x);
                case "tan":  return 2*xp/(1+Cos(2*x));
                case "sinh": return xp*Cosh(x);
                case "cosh": return xp*Sinh(x);
                case "tanh": return xp/(0.5*(1+Cosh(2*x)));
                case "asin":  return xp/Sqrt(1-(x^2));
                case "acos":  return -xp/Sqrt(1-(x^2));
                case "atan":  return xp/(1+(x^2));
                case "asinh": return xp/Sqrt((x^2)+1);
                case "acosh": return xp/Sqrt((x^2)-1);
                case "atanh": return xp/(1-(x^2));
                default:
                    throw new NotImplementedException($"Operator {Key} does not have slope defined.");
            }
        }
    }

    public sealed class UnaryOperatorExpr : UnaryExpr, IEquatable<UnaryOperatorExpr>
    {
        public UnaryOperatorExpr(string op, Expr arg)
            : base(op, arg)
        { }
        public UnaryOperatorExpr(string op, Func<double, double> function, Expr arg)
            : base(op, function, arg)
        { }
        public string Op { get => Key; }

        public override string ToString(string format, IFormatProvider provider)
            => $"({Op}{Argument.ToString(format, provider)})";

        public override Expr Substitute(Expr target, Expr expression)
        {
            if (target.Equals(Argument))
            {
                return new UnaryOperatorExpr(Op, expression);
            }
            return this;
        }

        #region IEquatable Members
        public override bool Equals(object obj) => base.Equals(obj);
        /// <summary>
        /// Equality overrides from <see cref="Expr"/>
        /// </summary>
        /// <param name="other">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(UnaryOperatorExpr)</code></returns>
        public override bool Equals(Expr other)
        {
            if (other is UnaryOperatorExpr item)
            {
                return Equals(item);
            }
            return false;
        }

        /// <summary>
        /// Checks for equality among <see cref="UnaryOperatorExpr"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public bool Equals(UnaryOperatorExpr other)
        {
            return Op.Equals(other.Op)
                && Argument.Equals(other.Argument);
        }
        /// <summary>
        /// Calculates the hash code for the <see cref="UnaryOperatorExpr"/>
        /// </summary>
        /// <returns>The int hash value</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295)*hc + Op.GetHashCode();
                hc = (-1521134295)*hc + Argument.GetHashCode();
                return hc;
            }
        }

        #endregion

    }
    public sealed class UnaryFunctionExpr : UnaryExpr, IEquatable<UnaryFunctionExpr>
    {
        public UnaryFunctionExpr(string name, Expr argument)
            : base(name, argument)
        { }
        public UnaryFunctionExpr(string name, Func<double, double> function, Expr argument)
            : base(name, function, argument)
        { }

        public string Name { get => Key; }

        public override string ToString(string format, IFormatProvider provider)
            => $"{Name}({Argument.ToString(format, provider)})";

        public override Expr Substitute(Expr target, Expr expression)
        {
            if (target.Equals(Argument))
            {
                return new UnaryFunctionExpr(Name, expression);
            }
            return this;
        }


        #region IEquatable Members
        public override bool Equals(object obj) => base.Equals(obj);
        /// <summary>
        /// Equality overrides from <see cref="Expr"/>
        /// </summary>
        /// <param name="other">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(UnaryFunctionExpr)</code></returns>
        public override bool Equals(Expr other)
        {
            if (other is UnaryFunctionExpr item)
            {
                return Equals(item);
            }
            return false;
        }

        /// <summary>
        /// Checks for equality among <see cref="UnaryFunctionExpr"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public bool Equals(UnaryFunctionExpr other)
        {
            return Name.Equals(other.Name)
                && Argument.Equals(other.Argument);
        }
        /// <summary>
        /// Calculates the hash code for the <see cref="UnaryFunctionExpr"/>
        /// </summary>
        /// <returns>The int hash value</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295)*hc + Name.GetHashCode();
                hc = (-1521134295)*hc + Argument.GetHashCode();
                return hc;
            }
        }

        #endregion

    }


}
