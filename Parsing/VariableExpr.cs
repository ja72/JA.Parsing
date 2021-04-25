using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace JA.Parsing
{
    public sealed class VariableExpr : Expr, IEquatable<VariableExpr>
    {
        static readonly Dictionary<string, VariableExpr> defined = new Dictionary<string, VariableExpr>();
        public static IReadOnlyDictionary<string, VariableExpr> Defined => defined;

        public VariableExpr(string symbol)
        {
            Symbol = symbol;
            defined[symbol] = this;
        }
        public override int ResultCount => 1;
        public static implicit operator VariableExpr(string symbol) => Variable(symbol);
        public static implicit operator string(VariableExpr e) => e.Symbol;

        protected internal override void AddVariables(List<VariableExpr> variables)
        {
            if (!variables.Contains(this))
            {
                variables.Add(this);
            }
        }
        protected internal override void Compile(ILGenerator generator, Dictionary<VariableExpr, int> envirnoment)
        {
            Debug.WriteLine($"Compile [{Symbol}]");
            generator.Emit(OpCodes.Ldarg, envirnoment[this]);
        }
        public string Symbol { get; }

        public override string ToString(string format, IFormatProvider provider)
        {
            return Symbol.ToString(provider);
        }
        public override Expr Substitute(VariableExpr target, Expr expression)
        {
            return target.Equals(this) ? expression : this;
        }

        public override Expr Partial(VariableExpr variable)
        {
            if (Equals(variable))
            {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Defines a new variable for the time rate of this variable.
        /// </summary>
        /// <param name="dot">
        /// The character to use instead of dot. Default is <c>'p'</c>.
        /// </param>
        /// <remarks>
        /// Appends a <c>'p'</c> character to the variable name. If the
        /// variable contains and underscore <c>'_'</c> the appending
        /// happens before the underscore.
        /// </remarks>
        /// <example>
        ///   <br />
        ///     <code>xp = x.Rate();</code>
        /// </example>
        public VariableExpr Rate(char dot = 'p')
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
