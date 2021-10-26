using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace JA.Expressions
{

    public record ValueExpr(double Value) : Expr
    {
        public static implicit operator ValueExpr(double value) => new(value);
        public static implicit operator double(ValueExpr expression) => expression.Value;
        public override int Rank => 0;
        protected internal override void Compile(ILGenerator gen, Dictionary<string, int> env)
        {
            gen.Emit(OpCodes.Ldc_R8, Value);
        }
        public override IQuantity Eval(params (string sym, double val)[] parameters)
        {
            return (Scalar)Value;
        }
        protected internal override Expr Substitute(Expr variable, Expr value) => this;
        public override Expr PartialDerivative(SymbolExpr param) => 0;
        protected internal override void FillSymbols(ref List<string> variables)
        {
        }
        protected internal override void FillValues(ref List<double> values)
        {
            values.Add(Value);
        }

        public override string ToString() => ToString("g");
        public override string ToString(string formatting, IFormatProvider provider)
        {
            return Value.ToString(formatting, provider);
        }
    }
    public record NamedValueExpression(string Name, double Value) : ValueExpr(Value)
    {
        public NamedValueExpression(ConstOp Op) : this(Op.Identifier, Op.Value) { }
        public static implicit operator NamedValueExpression((string name, double value) arg) 
            => new(arg.name, arg.value);
        protected internal override Expr Substitute(Expr variable, Expr value)
        {
            if (variable.IsSymbol(out string sym) && Name==sym)
            {
                return value;
            }
            return this;
        }
        public override string ToString() => ToString("g");
        public override string ToString(string formatting, IFormatProvider provider)
        {
            return Name.ToString(provider);
        }
    }

}
