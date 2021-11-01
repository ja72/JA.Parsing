using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using static System.Math;

namespace JA.Expressions
{
    public record AssignExpr(Expr Left, Expr Right) : Expr
    {
        static double Eq(double a, double b)
        {
            return a == b ? 1d : 0d;
        }
        public override int Rank => Max(Left.Rank, Right.Rank);
        protected internal override void Compile(ILGenerator gen, Dictionary<string, int> env)
        {
	        //// return (a == b) ? 1.0 : 0.0;

            //IL_0001: ldarg.0
            Left.Compile(gen, env);
            //IL_0002: ldarg.1
            Right.Compile(gen, env);
            Label IL_10 = gen.DefineLabel();
            Label IL_19 = gen.DefineLabel();

            //IL_0003: beq.s IL_0010
            gen.Emit(OpCodes.Beq_S, IL_10);
            //IL_0005: ldc.r8 0.0
            gen.Emit(OpCodes.Ldc_R8, 0.0);
            //IL_000e: br.s IL_0019
            gen.Emit(OpCodes.Br_S, IL_19);
            //IL_0010: ldc.r8 1
            gen.MarkLabel(IL_10);
            gen.Emit(OpCodes.Ldc_R8, 1.0);
            //IL_0019: 
            gen.MarkLabel(IL_19);

        }
        public override IQuantity Eval(params (string sym, double val)[] parameters)
        {
            return Rank switch
            {
                0 => (Scalar)Eq(Left.Eval(parameters).Value, Right.Eval(parameters).Value),
                1 => (Vector)Enumerable.Zip(Left.ToArray(), Right.ToArray())
                        .Select(pair => Eq(pair.First.Eval(parameters).Value,
                            pair.Second.Eval(parameters).Value)).ToArray(),
                2 => (Matrix)Enumerable.Zip(Left.ToArray(), Right.ToArray())
                        .Select(rows => Enumerable.Zip(rows.First.ToArray(), rows.Second.ToArray())
                            .Select(pair => Eq(pair.First.Eval(parameters).Value,
                                pair.Second.Eval(parameters).Value)).ToArray()).ToArray(),
                _ => throw new NotSupportedException("Rank > 2 is not supported."),
            };
        }
        protected internal override Expr Substitute(Expr variable, Expr value)
        {
            return Assign(Left.Substitute(variable, value), Right.Substitute(variable, value));
        }
        public override Expr PartialDerivative(VariableExpr param)
        {
            var x = Left;
            var y = Right;
            var xp = x.PartialDerivative(param);
            var yp = y.PartialDerivative(param);
            return Assign(xp, yp);
        }
        protected internal override void FillSymbols(ref List<string> variables)
        {
            Left.FillSymbols(ref variables);
            Right.FillSymbols(ref variables);
        }
        protected internal override void FillValues(ref List<double> values)
        {
            Left.FillValues(ref values);
            Right.FillValues(ref values);
        }
        public override string ToString() => ToString("g");

        public override string ToString(string formatting, IFormatProvider provider)
        {
            string largs = $"{Left.ToString(formatting, provider)}";
            string rargs = $"{Right.ToString(formatting, provider)}";
            return $"{largs}={rargs}";
        }
    }

}
