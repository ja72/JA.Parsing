using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using static System.Math;

namespace JA.Expressions
{
    public record BinaryExpr(BinaryOp Op, Expr Left, Expr Right) : Expr
    {
        public override int Rank => Max(Left.Rank, Right.Rank);
        protected internal override void Compile(ILGenerator gen, Dictionary<string, int> env)
        {
            //IL_0001: ldarg.0
            Left.Compile(gen, env);
            //IL_0002: ldarg.1
            Right.Compile(gen, env);
            switch (Op.Identifier)
            {
                case "+":
                    gen.Emit(OpCodes.Add);
                    break;
                case "*":
                    gen.Emit(OpCodes.Mul);
                    break;
                case "-":
                    gen.Emit(OpCodes.Sub);
                    break;
                case "/":
                    gen.Emit(OpCodes.Div);
                    break;
                default:
                    gen.Emit(OpCodes.Call, Op.Method);
                    break;
            }
        }
        public override IQuantity Eval(params (string sym, double val)[] parameters)
        {
            return Rank switch
            {
                0 => (Scalar)Op.Function(Left.Eval(parameters).Value, Right.Eval(parameters).Value),
                1 => (Vector)Enumerable.Zip(Left.ToArray(), Right.ToArray())
                        .Select(pair => Op.Function(pair.First.Eval(parameters).Value,
                            pair.Second.Eval(parameters).Value)).ToArray(),
                2 => (Matrix)Enumerable.Zip(Left.ToArray(), Right.ToArray())
                        .Select(rows => Enumerable.Zip(rows.First.ToArray(), rows.Second.ToArray())
                            .Select(pair => Op.Function(pair.First.Eval(parameters).Value,
                                pair.Second.Eval(parameters).Value)).ToArray()).ToArray(),
                _ => throw new NotSupportedException("Rank > 2 is not supported."),
            };
        }
        protected internal override Expr Substitute(Expr variable, Expr value)
        {
            return Binary(Op, Left.Substitute(variable, value), Right.Substitute(variable, value));
        }

        public override Expr PartialDerivative(VariableExpr param)
        {
            var x = Left;
            var y = Right;
            var xp = x.PartialDerivative(param);
            var yp = y.PartialDerivative(param);
            return Op.Identifier switch
            {
                "+" => xp + yp,
                "-" => xp - yp,
                "*" => y * xp + x * yp,
                "/" => (y * xp - x * yp) / Sqr(y),
                "^" => Power(x, y - 1) * (y * xp + x * Ln(x) * yp),
                _ => throw new NotSupportedException($"f'(x{Op.Identifier}y)"),
            };

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

            switch (Op.Identifier)
            {
                case "+":
                    {
                        return $"{largs}+{rargs}";
                    }
                case "-":
                    {
                        if (Right.IsBinary(out var rop, out _, out _))
                        {
                            if (rop=="+" || rop=="-")
                            {
                                rargs = $"({rargs})";
                            }
                        }
                        return $"{largs}-{rargs}";
                    }
                case "*":
                    {
                        if (Left.IsBinary(out var lop, out _, out _))
                        {
                            if (lop == "+" || lop=="-")
                            {
                                largs = $"({largs})";
                            }
                        }
                        if (Right.IsBinary(out var rop, out _, out _))
                        {
                            if (rop == "+" || rop=="-")
                            {
                                rargs = $"({rargs})";
                            }
                        }
                        return $"{largs}*{rargs}";
                    }
                case "/":
                    {
                        if (Left.IsBinary(out var lop, out _, out _))
                        {
                            if (lop == "+" || lop=="-" || lop=="/")
                            {
                                largs = $"({largs})";
                            }
                        }
                        if (Right.IsBinary(out var rop, out _, out _))
                        {
                            if (rop == "+" || rop=="-" || rop=="*" || rop=="/")
                            {
                                rargs = $"({rargs})";
                            }
                        }
                        return $"{largs}/{rargs}";
                    }
                case "^":
                    {
                        if (Left.IsBinary(out _, out _, out _))
                        {
                            largs = $"({largs})";
                        }

                        if (Right.IsBinary(out _, out _, out _))
                        {
                            rargs = $"({rargs})";
                        }
                        return $"{largs}^{rargs}";
                    }
                default:
                    {
                        return $"{Op.Identifier}({largs},{rargs})";
                    }
            }
        }
    }

}
