using System;
using System.Collections.Generic;

namespace JA.Parsing
{
    public class ConstExpr : Expr, IEquatable<ConstExpr>
    {
        public ConstExpr(double value)
        {
            this.Value = value;
        }
        public static explicit operator double(ConstExpr e) => e.Value;
        public double Value { get; }
        public override double Eval(params (string sym, double val)[] parameters)
        {
            return Value;
        }
        public override string ToString(string format, IFormatProvider provider)
        {
            return Value.ToString(format, provider);
        }

        public override Expr Substitute(Expr target, Expr expression)
        {
            return this;
        }

        public override Expr Partial(VariableExpr variable)
        {
            return 0;
        }

        #region IEquatable Members
        public override bool Equals(object obj) => base.Equals(obj);

        /// <summary>
        /// Equality overrides from <see cref="Expr"/>
        /// </summary>
        /// <param name="obj">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(ConstExpr)</code></returns>
        public override bool Equals(Expr other)
        {
            if (other is ConstExpr item)
            {
                return Equals(item);
            }
            return false;
        }

        /// <summary>
        /// Checks for equality among <see cref="ConstExpr"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public virtual bool Equals(ConstExpr other)
        {
            return Value.Equals(other.Value);
        }
        /// <summary>
        /// Calculates the hash code for the <see cref="ConstExpr"/>
        /// </summary>
        /// <returns>The int hash value</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295)*hc + Value.GetHashCode();
                return hc;
            }
        }

        #endregion

    }

    public sealed class NamedConstantExpr : ConstExpr, IEquatable<NamedConstantExpr>
    {
        static readonly Dictionary<string, NamedConstantExpr> defined = new Dictionary<string, NamedConstantExpr>();
        public static IReadOnlyDictionary<string, NamedConstantExpr> Defined => defined;

        public NamedConstantExpr(string symbol, double value)
            : base(value)
        {
            this.Symbol = symbol;
            defined[symbol] = this;
        }
        public static implicit operator NamedConstantExpr((string symbol, double value) assign)
            => new NamedConstantExpr(assign.symbol, assign.value);
        public static explicit operator (string symbol, double value) (NamedConstantExpr e)
            => (e.Symbol, e.Value);

        public string Symbol { get; }

        public override string ToString(string format, IFormatProvider provider)
        {
            return Symbol.ToString(provider);
        }

        #region IEquatable Members
        public override bool Equals(object obj) => base.Equals(obj);
        /// <summary>
        /// Equality overrides from <see cref="Expr"/>
        /// </summary>
        /// <param name="obj">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(ConstExpr)</code></returns>
        public override bool Equals(Expr other)
        {
            if (other is NamedConstantExpr item)
            {
                return Equals(item);
            }
            return false;
        }
        /// <summary>
        /// Checks for equality among <see cref="ConstExpr"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public bool Equals(NamedConstantExpr other)
        {
            return Symbol.Equals(other.Symbol);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295)*hc + Symbol.GetHashCode();
                return hc;
            }
        }
        #endregion

    }

    public sealed class VariableExpr : Expr, IEquatable<VariableExpr>
    {
        static readonly Dictionary<string, VariableExpr> defined = new Dictionary<string, VariableExpr>();
        public static IReadOnlyDictionary<string, VariableExpr> Defined => defined;

        public VariableExpr(string symbol)
        {
            Symbol = symbol;
            defined[symbol] = this;
        }

        public static implicit operator VariableExpr(string symbol) => Expr.Variable(symbol);
        public static explicit operator string(VariableExpr e) => e.Symbol;
        public string Symbol { get; }

        public override double Eval(params (string sym, double val)[] parameters)
        {
            foreach (var (sym, val) in parameters)
            {
                if (sym.Equals(Symbol))
                {
                    return val;
                }
            }
            if (Variables.ContainsKey(Symbol))
            {
                return Variables[Symbol];
            }
            return 0;
        }
        public override string ToString(string format, IFormatProvider provider)
        {
            return Symbol.ToString(provider);
        }
        public override Expr Substitute(Expr target, Expr expression)
        {
            return this;
        }

        public override Expr Partial(VariableExpr variable)
        {
            if (Equals(variable))
            {
                return 1;
            }
            return 0;
        }

        public VariableExpr Derivative(char dot = 'p')
        {
            var parts = Symbol.Split('_');
            parts[0] += dot;
            return Variable(string.Join(string.Empty, parts));
        }

        #region IEquatable Members
        public override bool Equals(object obj) => base.Equals(obj);

        /// <summary>
        /// Equality overrides from <see cref="Expr"/>
        /// </summary>
        /// <param name="other">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(VariableExpr)</code></returns>
        public override bool Equals(Expr other)
        {
            if (other is VariableExpr item)
            {
                return Equals(item);
            }
            return false;
        }

        /// <summary>
        /// Checks for equality among <see cref="VariableExpr"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public bool Equals(VariableExpr other)
        {
            return Symbol.Equals(other.Symbol);
        }
        /// <summary>
        /// Calculates the hash code for the <see cref="VariableExpr"/>
        /// </summary>
        /// <returns>The int hash value</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295)*hc + Symbol.GetHashCode();
                return hc;
            }
        }

        #endregion

    }

}
