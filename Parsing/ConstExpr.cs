using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Reflection.Emit;

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
        public override int ResultCount => 1;
        protected internal override void AddVariables(List<VariableExpr> variables)
        {
            // nothing to add
        }
        protected internal override void Compile(ILGenerator generator, Dictionary<VariableExpr, int> envirnoment)
        {
            Debug.WriteLine($"Compile {Value}");
            generator.Emit(OpCodes.Ldc_R8, Value);
        }
        public override string ToString(string format, IFormatProvider provider)
        {
            return Value.ToString(format, provider);
        }

        public override Expr Substitute(VariableExpr target, Expr expression)
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

}
