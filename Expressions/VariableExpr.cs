using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Collections.ObjectModel;

namespace JA.Expressions
{
    public record VariableExpr(string Name) : Expr
    {
        public static implicit operator VariableExpr(string name) => new(name);
        public static implicit operator string(VariableExpr symbol) => symbol.Name;
        public override int Rank => 0;
        protected internal override Expr Substitute(Expr variable, Expr value)
        {            
            if (!Parameters.ContainsKey(Name))
            {
                // NOTE: will not substitute a named constant
                if (variable.IsSymbol(out string sym) && sym==Name)
                {
                    return value;
                }
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
        /// <item><code>"x".Derivative() = "xp"</code></item>
        /// <item><code>"xh".Derivative() = "xhp"</code></item>
        /// <item><code>"xp".Derivative() = "xpp"</code></item>
        /// <item><code>"x_1".Derivative() = "xp_1"</code></item>
        /// <item><code>"xh_C".Derivative() = "xhp_C"</code></item>
        /// </list>
        /// </example>
        /// <returns>A symbol expression</returns>
        public Expr Derivative()
        {
            if (IsConstant(out _))
            {
                return 0;
            }
            var parts = Name.Split('_');
            parts[0] += 'p';
            return string.Join("_", parts);
        }
        protected internal override void FillSymbols(ref List<string> variables)
        {
            if(!IsConstant(out _))
            {
                variables.Add(Name);
            }
        }
        protected internal override void FillValues(ref List<double> values)
        {
            if (IsConstant(out var value))
            {
                values.Add(value);
            }
        }
        public override Expr PartialDerivative(VariableExpr param) => param.Name == Name ? 1 : 0;
        public override IQuantity Eval(params (string sym, double val)[] parameters)
        {
            foreach (var (sym, val) in parameters)
            {
                if (sym.Equals(Name))
                {
                    return (Scalar)val;
                }
            }
            if (IsConstant(out var value))
            {
                return (Scalar)value;
            }
            throw new ArgumentException($"Missing parameter {Name}", nameof(parameters));
        }
        protected internal override void Compile(ILGenerator gen, Dictionary<string, int> env)
        {
            if (env.ContainsKey(Name))
            {
                gen.Emit(OpCodes.Ldarg, env[Name]);
            }
            else if (IsConstant(out var value))
            {
                gen.Emit(OpCodes.Ldc_R8, value);
            }
            else
            {
                throw new ArgumentException($"Missing parameter {Name}", nameof(env));
            }
        }
        public override string ToString() => ToString("g");
        public override string ToString(string formatting, IFormatProvider provider)
        {
            return Name.ToString(provider);
        }

    }

}
