using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace JA.Parsing
{
    public abstract class BinaryExpr : Expr
    {
        static readonly Dictionary<string, Func<double, double, double>> functions = new Dictionary<string, Func<double, double, double>>()
        {
            ["+"] = (x, y) => x + y,
            ["-"] = (x, y) => x - y,
            ["*"] = (x, y) => x * y,
            ["/"] = (x, y) => x / y,
            ["min"] = (x, y) => Math.Min(x, y),
            ["max"] = (x, y) => Math.Max(x, y),
            ["pow"] = (x, y) => Math.Pow(x, y),
            ["sign"] = (x, y) => Math.Abs(x)*Math.Sign(y),
        };

        protected BinaryExpr(string op, Expr left, Expr right)
        {
            this.Key = op;
            try
            {
                Function = functions[op];
            }
            catch(KeyNotFoundException ex)
            {
                Debug.WriteLine(ex.ToString());
                throw new ArgumentException($"Operator {op} not found.", nameof(op));
            }
            this.Left = left;
            this.Right = right;
        }
        protected BinaryExpr(string op, Func<double, double, double> function, Expr left, Expr right)
        {
            this.Key = op;
            this.Function = function;
            this.Left = left;
            this.Right = right;
        }

        public Expr Left { get; }
        public Expr Right { get; }
        protected string Key { get; }
        public Func<double, double, double> Function { get; }

        public override double Eval(params (string sym, double val)[] parameters)
        {
            return Function(Left.Eval(parameters), Right.Eval(parameters));
        }

        public override Expr Partial(VariableExpr variable)
        {
            Expr x = Left, xp = Left.Partial(variable);
            Expr y = Right, yp = Right.Partial(variable);
            switch (Key)
            {
                case "+": return xp+yp;
                case "-": return xp-yp;
                case "*": return y*xp+x*yp;
                case "/": return (y*xp-x*yp)/(y^2);
                case "^": 
                case "pow": return (x^(y-1))*(x*Ln(x)*yp+y*xp);
                case "min": return ((yp-xp)*Sign(x-y)+xp+yp)/2;
                case "max": return ((xp-yp)*Sign(x-y)+xp+yp)/2;
                case "sign": return xp*Sign(x*y);
                default:
                    throw new NotImplementedException($"Operator {Key} does not have slope defined.");
            }
        }
    }

    public sealed class BinaryOperatorExpr : BinaryExpr, IEquatable<BinaryOperatorExpr>
    {
        public BinaryOperatorExpr(string op, Expr left, Expr right)
            : base(op, left, right)
        { }
        public BinaryOperatorExpr(string op, Func<double, double, double> f, Expr left, Expr right)
            : base(op, f, left, right)
        { }
        public string Op { get => Key; }

        public override string ToString(string format, IFormatProvider provider)
            => $"({Left.ToString(format, provider)}{Op}{Right.ToString(format, provider)})";

        public override Expr Substitute(Expr target, Expr expression)
        {
            var left = target.Equals(Left) ? expression : Left;
            var right = target.Equals(Right) ? expression : Right;

            if (left==Left && right==Right) return this;

            return new BinaryOperatorExpr(Op, left, right);
        }


        #region IEquatable Members
        public override bool Equals(object obj) => base.Equals(obj);
        /// <summary>
        /// Equality overrides from <see cref="Expr"/>
        /// </summary>
        /// <param name="other">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(BinaryOperatorExpr)</code></returns>
        public override bool Equals(Expr other)
        {
            if (other is BinaryOperatorExpr item)
            {
                return Equals(item);
            }
            return false;
        }

        /// <summary>
        /// Checks for equality among <see cref="BinaryOperatorExpr"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public bool Equals(BinaryOperatorExpr other)
        {
            return Op.Equals(other.Op)
                && Left.Equals(other.Left)
                && Right.Equals(other.Right);
        }
        /// <summary>
        /// Calculates the hash code for the <see cref="BinaryOperatorExpr"/>
        /// </summary>
        /// <returns>The int hash value</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295)*hc + Op.GetHashCode();
                hc = (-1521134295)*hc + Left.GetHashCode();
                hc = (-1521134295)*hc + Right.GetHashCode();
                return hc;
            }
        }

        #endregion

    }
    public sealed class BinaryFunctionExpr : BinaryExpr, IEquatable<BinaryFunctionExpr>
    {
        public BinaryFunctionExpr(string name, Expr left, Expr right)
            : base(name, left, right)
        { }
        public BinaryFunctionExpr(string name, Func<double, double, double> function, Expr left, Expr right)
            : base(name, function, left, right)
        { }

        public string Name { get => Key; }

        public override string ToString(string format, IFormatProvider provider)
            => $"{Name}({Left.ToString(format, provider)},{Right.ToString(format, provider)})";

        public override Expr Substitute(Expr target, Expr expression)
        {
            var left = target.Equals(Left) ? expression : Left;
            var right = target.Equals(Right) ? expression : Right;

            if (left==Left && right==Right) return this;

            return new BinaryFunctionExpr(Name, left, right);
        }
        #region IEquatable Members
        public override bool Equals(object obj) => base.Equals(obj);
        /// <summary>
        /// Equality overrides from <see cref="Expr"/>
        /// </summary>
        /// <param name="other">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(BinaryOperatorExpr)</code></returns>
        public override bool Equals(Expr other)
        {
            if (other is BinaryFunctionExpr item)
            {
                return Equals(item);
            }
            return false;
        }

        /// <summary>
        /// Checks for equality among <see cref="BinaryFunctionExpr"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public bool Equals(BinaryFunctionExpr other)
        {
            return Name.Equals(other.Name)
                && Left.Equals(other.Left)
                && Right.Equals(other.Right);
        }
        /// <summary>
        /// Calculates the hash code for the <see cref="BinaryFunctionExpr"/>
        /// </summary>
        /// <returns>The int hash value</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295)*hc + Name.GetHashCode();
                hc = (-1521134295)*hc + Left.GetHashCode();
                hc = (-1521134295)*hc + Right.GetHashCode();
                return hc;
            }
        }

        #endregion
    }

}
