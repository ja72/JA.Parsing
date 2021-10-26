using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace JA.Expressions
{
    public record UnaryExpr(UnaryOp Op, Expr Argument) : Expr
    {
        public override int Rank => Argument.Rank;
        protected internal override Expr Substitute(Expr variable, Expr value)
        {
            return Unary(Op, Argument.Substitute(variable, value));
        }
        public override IQuantity Eval(params (string sym, double val)[] parameters)
        {
            return Rank switch
            {
                0 => (Scalar)Op.Function(Argument.Eval(parameters).Value),
                1 => (Vector)Argument.ToArray().Select(arg => Op.Function(arg.Eval(parameters).Value)).ToArray(),
                2 => (Matrix)Argument.ToMatrix().Select(row =>
                    row.Select(arg => Op.Function(arg.Eval(parameters).Value)).ToArray()).ToArray(),
                _ => throw new NotSupportedException("Rank > 2 is not supported."),
            };
        }
        protected internal override void Compile(ILGenerator gen, Dictionary<string, int> env)
        {
            Argument.Compile(gen, env);
            switch (Op.Identifier)
            {
                case "+":
                    gen.Emit(OpCodes.Nop);
                    break;
                case "-":
                case "neg":
                    gen.Emit(OpCodes.Neg);
                    break;
                case "sign":
                    gen.Emit(OpCodes.Call, Op.Method);
                    gen.Emit(OpCodes.Conv_R8);
                    break;
                case "inv":
                    gen.Emit(OpCodes.Stloc_0);
                    gen.Emit(OpCodes.Ldc_R8, 1.0);
                    gen.Emit(OpCodes.Ldloc_0);
                    gen.Emit(OpCodes.Div);
                    break;
                case "pi":
                    gen.Emit(OpCodes.Ldc_R8, Math.PI);
                    gen.Emit(OpCodes.Mul);
                    break;
                default:
                    gen.Emit(OpCodes.Call, Op.Method);
                    break;
            }
        }
        public override Expr PartialDerivative(SymbolExpr param)
        {
            var x = Argument;
            var xp = x.PartialDerivative(param);
            var pi = KnownConstDictionary.Defined["pi"].Value;
            var deg = pi/180;
            return Op.Identifier switch
            {
                "pi" => pi*xp,
                "abs" => Sign(x) * xp,
                "sign" => 0,
                "floor" => 0,
                "ceil" => 0,
                "round" => 0,
                "rnd" => double.NaN,
                "sqr" => 2*x*xp,
                "cub" => 3*Sqr(x)*xp,
                "sqrt" => xp / (2 * this),
                "cbrt" => xp / (3 * Sqr(this)),
                "-" => -xp,
                "neg" => -xp,
                "inv" => -xp / (x ^ 2),
                "exp" => Exp(x) * xp,
                "ln" => xp / x,
                "sin" => Cos(x)*xp,
                "cos" => -Sin(x)*xp,
                "tan" => 1/(Cos(x)^2)*xp,
                "asin" => xp/Sqrt(1-Sqr(x)),
                "acos" => -xp/Sqrt(1-Sqr(x)),
                "atan" => xp/(1+Sqr(x)),
                "sind" => deg*Cos(deg*x)*xp,
                "cosd" => -deg*Sin(deg*x)*xp,
                "tand" => deg/(Cos(deg*x)^2)*xp,
                "asind" => deg*xp/Sqrt(1-Sqr(deg*x)),
                "acosd" => -deg*xp/Sqrt(1-Sqr(deg*x)),
                "atand" => deg*xp/(1+Sqr(deg*x)),
                "sinh" => Cosh(x)*xp,
                "cosh" => Sinh(x)*xp,
                "tanh" => 1/(Cosh(x)^2)*xp,
                "asinh" => xp/Sqrt(Sqr(x)+1),
                "acosh" => xp/Sqrt(Sqr(x)-1),
                "atanh" => xp/(1-Sqr(x)),
                // TODO: Fill derivatives
                _ => throw new NotSupportedException($"f'({Op.Identifier}(x))"),
            };
        }
        protected internal override void FillSymbols(ref List<string> variables)
        {
            Argument.FillSymbols(ref variables);
        }
        protected internal override void FillValues(ref List<double> values)
        {
            Argument.FillValues(ref values);
        }

        public override string ToString() => ToString("g");
        public override string ToString(string formatting, IFormatProvider provider)
        {
            if (Op.Identifier=="-" || Op.Identifier=="+")
            {
                var args = Argument.IsBinary(out _, out _, out _) ?
                $"({Argument.ToString(formatting, provider)})" :
                $"{Argument.ToString(formatting, provider)}";
                return $"{Op.Identifier}{args}";
            }
            return $"{Op.Identifier}({Argument.ToString(formatting, provider)})";
        }
    }

}
