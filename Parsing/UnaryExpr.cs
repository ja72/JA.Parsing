using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.ComponentModel;
using System.Reflection;

namespace JA.Parsing
{
    public enum UnaryOp
    {
        Undefined,
        [Description("+")] Identity,
        [Description("-")] Negate,
        [Description("inv")] Inverse,
        [Description("rnd")] Rnd,
        [Description("pi")] Pi,
        [Description("abs")] Abs,
        [Description("sign")] Sign,
        [Description("exp")] Exp,
        [Description("ln")] Log,
        [Description("log2")] Log2,
        [Description("log10")] Log10,
        [Description("sqr")] Sqr,
        [Description("cub")] Cub,
        [Description("sqrt")] Sqrt,
        [Description("cbrt")] Cbrt,
        [Description("floor")] Floor,
        [Description("ceil")] Ceiling,
        [Description("round")] Round,
        [Description("sin")] Sin,
        [Description("cos")] Cos,
        [Description("tan")] Tan,
        [Description("sinh")] Sinh,
        [Description("cosh")] Cosh,
        [Description("tanh")] Tanh,
        [Description("asin")] Asin,
        [Description("acos")] Acos,
        [Description("atan")] Atan,
        [Description("asinh")] Asinh,
        [Description("acosh")] Acosh,
        [Description("atanh")] Atanh,
    }

    public sealed class UnaryExpr : 
        Expr,
        IEquatable<UnaryExpr>
    {

        internal static readonly Dictionary<UnaryOp, Func<double, double>> functions = new Dictionary<UnaryOp, Func<double, double>>()
        {
            [UnaryOp.Identity]  = (x) => x,
            [UnaryOp.Negate]    = (x) => -x,
            [UnaryOp.Inverse]   = (x) =>1/x,
            [UnaryOp.Pi]        = (x) => Math.PI*x,
            [UnaryOp.Rnd]       = (x) => rng.NextDouble()*x,
            [UnaryOp.Abs]       = (x) => Math.Abs(x),
            [UnaryOp.Sign]      = (x) => Math.Sign(x),
            [UnaryOp.Exp]       = (x) => Math.Exp(x),
            [UnaryOp.Log]       = (x) => Math.Log(x),
            [UnaryOp.Log2]      = (x) => Math.Log(x,2),
            [UnaryOp.Log10]     = (x) => Math.Log(x, 10),
            [UnaryOp.Sqr]       = (x) => x * x,
            [UnaryOp.Cub]       = (x) => x * x * x,
            [UnaryOp.Sqrt]      = (x) => Math.Sqrt(x),
            [UnaryOp.Cbrt]      = (x) => Math.Pow(x, 1/3.0),
            [UnaryOp.Floor]     = (x) => Math.Floor(x),
            [UnaryOp.Ceiling]   = (x) => Math.Ceiling(x),
            [UnaryOp.Round]     = (x) => Math.Round(x),
            [UnaryOp.Sin]       = (x) => Math.Sin(x),
            [UnaryOp.Cos]       = (x) => Math.Cos(x),
            [UnaryOp.Tan]       = (x) => Math.Tan(x),
            [UnaryOp.Sinh]      = (x) => Math.Sinh(x),
            [UnaryOp.Cosh]      = (x) => Math.Cosh(x),
            [UnaryOp.Tanh]      = (x) => Math.Tanh(x),
            [UnaryOp.Asin]      = (x) => Math.Asin(x),
            [UnaryOp.Acos]      = (x) => Math.Acos(x),
            [UnaryOp.Atan]      = (x) => Math.Atan(x),
            [UnaryOp.Asinh]     = MathExtra.Asinh,
            [UnaryOp.Acosh]     = MathExtra.Acosh,
            [UnaryOp.Atanh]     = MathExtra.Atanh,
        };

        public UnaryExpr(UnaryOp op, Expr arg)
        {
            if (op==UnaryOp.Undefined)
            {
                throw new ArgumentException("Undefined operand.", nameof(op));
            }
            this.Op = op;
            this.Key = Parser.DescriptionAttr(op);
            try
            {
                this.Function = functions[op];
            }
            catch (KeyNotFoundException ex)
            {
                Debug.WriteLine(ex.ToString());
                throw new ArgumentException($"Operator {Key} not found.", nameof(op));
            }
            this.Argument = arg;
        }
        public string Key { get; }
        public UnaryOp Op { get; }
        public Expr Argument { get; }
        public Func<double, double> Function { get; }
        public override int ResultCount => Argument.ResultCount;

        protected internal override void AddVariables(List<VariableExpr> variables)
        {
            Argument.AddVariables(variables);
        }

        protected internal override void Compile(ILGenerator generator, Dictionary<VariableExpr, int> envirnoment)
        {
            if (ArrayExpr.IsVectorizable(Argument, out int count, out Expr[] argArray))
            {
                var vector = new Expr[count];
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = Unary(Op, argArray[i]);
                }
                FromArray(vector).Compile(generator, envirnoment);
                return;
            }

            Debug.WriteLine($"Compile {Op}(x)");
            if (Op==UnaryOp.Inverse)
            {
                generator.Emit(OpCodes.Ldc_R8, 1.0);
            }
            Argument.Compile(generator, envirnoment);
            switch (Op)
            {
                case UnaryOp.Identity:
                    break;
                case UnaryOp.Negate:
                    generator.Emit(OpCodes.Neg);
                    break;
                case UnaryOp.Inverse:
                    generator.Emit(OpCodes.Div);
                    break;
                case UnaryOp.Pi:
                    generator.Emit(OpCodes.Ldc_R8, Math.PI);
                    generator.Emit(OpCodes.Mul);
                    break;
                case UnaryOp.Rnd:
                    generator.Emit(OpCodes.Ldc_R8, rng.NextDouble());
                    generator.Emit(OpCodes.Mul);
                    break;
                case UnaryOp.Log2:
                    generator.Emit(OpCodes.Ldc_R8, 2.0);
                    generator.Emit(OpCodes.Call, GetOpMethod(BinaryOp.Log));
                    break;
                case UnaryOp.Log10:
                    generator.Emit(OpCodes.Call, GetOpMethod(UnaryOp.Log10));
                    break;
                case UnaryOp.Sqr:
                    generator.Emit(OpCodes.Dup);
                    generator.Emit(OpCodes.Mul);
                    break;
                case UnaryOp.Cub:
                    generator.Emit(OpCodes.Dup);
                    generator.Emit(OpCodes.Dup);
                    generator.Emit(OpCodes.Mul);
                    generator.Emit(OpCodes.Mul);
                    break;
                case UnaryOp.Cbrt:
                    generator.Emit(OpCodes.Ldc_R8, 1/3.0);
                    generator.Emit(OpCodes.Call, GetOpMethod(BinaryOp.Pow));
                    break;
                default:
                    var info = GetOpMethod(Op);
                    if (info != null)
                    {
                        generator.Emit(OpCodes.Call, info);
                        if (info.ReturnType != typeof(double))
                        {
                            generator.Emit(OpCodes.Conv_R8);
                        }
                    }
                    break;
            }
        }

        public override Expr Partial(VariableExpr variable)
        {
            Expr x = Argument, xp = Argument.Partial(variable);
            switch (Op)
            {
                case UnaryOp.Identity: return xp;
                case UnaryOp.Negate: return -xp;
                case UnaryOp.Inverse: return -xp/(x^2);
                case UnaryOp.Rnd: return 0;
                case UnaryOp.Pi: return Math.PI*xp;
                case UnaryOp.Abs: return Sign(x)*xp;
                case UnaryOp.Sign: return 0;
                case UnaryOp.Exp: return Exp(x)*xp;
                case UnaryOp.Log: return xp/x;
                case UnaryOp.Log2: return xp/(Math.Log(2)*x);
                case UnaryOp.Log10: return xp/(Math.Log(10)*x);
                case UnaryOp.Sqr: return x*(2*xp);
                case UnaryOp.Cub: return (x^2)*(3*xp);
                case UnaryOp.Sqrt: return xp/(2*Sqrt(x));
                case UnaryOp.Cbrt: return xp/(3*(x^(2.0/3)));
                case UnaryOp.Floor: return 0;
                case UnaryOp.Ceiling: return 0;
                case UnaryOp.Round: return 0;
                case UnaryOp.Sin: return xp*Cos(x);
                case UnaryOp.Cos: return -xp*Sin(x);
                case UnaryOp.Tan: return 2*xp/(1+Cos(2*x));
                case UnaryOp.Sinh: return xp*Cosh(x);
                case UnaryOp.Cosh: return xp*Sinh(x);
                case UnaryOp.Tanh: return xp/(0.5*(1+Cosh(2*x)));
                case UnaryOp.Asin: return xp/Sqrt(1-(x^2));
                case UnaryOp.Acos: return -xp/Sqrt(1-(x^2));
                case UnaryOp.Atan: return xp/(1+(x^2));
                case UnaryOp.Asinh: return xp/Sqrt((x^2)+1);
                case UnaryOp.Acosh: return xp/Sqrt((x^2)-1);
                case UnaryOp.Atanh: return xp/(1-(x^2));
                default:
                    throw new NotImplementedException($"Operator {Key} does not have slope defined.");
            }
        }
        public override Expr Substitute(VariableExpr target, Expr expression)
        {
            var argument = target.Equals(Argument) ? expression : Argument.Substitute(target, expression);

            if (argument.Equals(Argument)) return this;

            return Unary(Op, argument);
        }

        #region IEquatable Members
        public override bool Equals(object obj) => base.Equals(obj);
        /// <summary>
        /// Equality overrides from <see cref="Expr"/>
        /// </summary>
        /// <param name="other">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(UnaryOperatorExpr)</code></returns>
        public override bool Equals(Expr other)
        {
            if (other is UnaryExpr item)
            {
                return Equals(item);
            }
            return false;
        }

        /// <summary>
        /// Checks for equality among <see cref="UnaryExpr"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public bool Equals(UnaryExpr other)
        {
            return Op.Equals(other.Op)
                && Argument.Equals(other.Argument);
        }        
        /// <summary>
        /// Calculates the hash code for the <see cref="UnaryOperatorExpr"/>
        /// </summary>
        /// <returns>The int hash value</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295)*hc + Op.GetHashCode();
                hc = (-1521134295)*hc + Argument.GetHashCode();
                return hc;
            }
        }

        #endregion

        #region Formatting
        public override string ToString(string format, IFormatProvider provider)
        {
            var arg = Argument.ToString(format, provider);
            if (Op == UnaryOp.Identity)
            {
                return arg;
            }
            if (Op == UnaryOp.Negate)
            {
                return $"({Key}{arg})";
            }
            if (Argument.IsBinary(out _, out _, out _))
            {
                // parens already included in binary expr
                return $"{Key}{arg}";
            }
            if (Argument.IsUnary(UnaryOp.Negate, out _))
            {
                // parens already included in negate expr
                return $"{Key}{arg}";
            }
            // parens is needed as function call
            return $"{Key}({arg})";
        }

        #endregion
    }


}
