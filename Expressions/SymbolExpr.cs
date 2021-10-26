using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace JA.Expressions
{
    public record SymbolExpr(string Name) : Expr
    {
        public static implicit operator SymbolExpr(string name) => new(name);
        public static implicit operator string(SymbolExpr symbol) => symbol.Name;
        public override int Rank => 0;
        protected internal override Expr Substitute(Expr variable, Expr value)
        {
            if (variable.IsSymbol(out string sym) && sym==Name)
            {
                return value;
            }
            return this;
        }
        /// <summary>
        /// Genarate a new symbol representing the derivative of this symbol.
        /// Adds the letter 'p' after the name of the symbol, but before any
        /// subscript denoted with '_'.
        /// </summary>
        /// <example>
        /// <list type="bullet">
        /// <item><code>"x".Dot() = "xp"</code></item>
        /// <item><code>"xh".Dot() = "xhp"</code></item>
        /// <item><code>"xp".Dot() = "xpp"</code></item>
        /// <item><code>"x_1".Dot() = "xp_1"</code></item>
        /// <item><code>"xh_C".Dot() = "xhp_C"</code></item>
        /// </list>
        /// </example>
        /// <returns>A symbol expression</returns>
        public SymbolExpr Dot()
        {
            var parts = Name.Split('_');
            parts[0] += 'p';
            return string.Join("_", parts);
        }
        protected internal override void FillSymbols(ref List<string> variables)
        {
            variables.Add(Name);
        }
        protected internal override void FillValues(ref List<double> values)
        {            
        }
        public override Expr PartialDerivative(SymbolExpr param) => param.Name == Name ? 1 : 0;
        public override IQuantity Eval(params (string sym, double val)[] parameters)
        {
            foreach (var (sym, val) in parameters)
            {
                if (sym.Equals(Name))
                {
                    return (Scalar)val;
                }
            }
            if (Variables.ContainsKey(Name))
            {
                return (Scalar)Variables[Name];
            }
            return Scalar.Zero;
        }
        protected internal override void Compile(ILGenerator gen, Dictionary<string, int> env)
        {
            if (env.ContainsKey(Name))
            {
                gen.Emit(OpCodes.Ldarg, env[Name]);
            }
            else if (Variables.ContainsKey(Name))
            {
                gen.Emit(OpCodes.Ldc_R8, Variables[Name]);
            }
            else
            {
                throw new ArgumentException($"Unspecified symbol {Name}", nameof(env));
            }
        }
        public override string ToString() => ToString("g");
        public override string ToString(string formatting, IFormatProvider provider)
        {
            return Name.ToString(provider);
        }

    }

}
