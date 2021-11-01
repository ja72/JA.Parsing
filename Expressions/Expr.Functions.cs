using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JA.Expressions
{
    public partial record Expr
    {
        #region Unary Functions
        public static Expr Negate(Expr Argument)
        {
            if (Argument.IsArray(out Expr[] a_array))
            {
                return Array(UnaryVectorOp(Negate, a_array));
            }
            if (Argument.IsConstant(out double x))
            {
                return -x;
            }
            if (Argument.IsUnary("-", out Expr argA))
            {
                return argA;
            }
            return new UnaryExpr("-", Argument);
        }

        public static Expr Abs(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Abs, a_array));
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            if (Argument.IsUnary("-", out Expr a_arg))
            {
                return Abs(a_arg);
            }
            if (Argument.IsUnary("sqrt", out _))
            {
                return Argument;
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Sign(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Sign, a_array));
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Sqr(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Sqr, a_array));
            }
            if (Argument.IsConstant(out double x))
            {
                return x * x;
            }
            if (Argument.IsUnary("sqrt", out Expr a_arg))
            {
                return a_arg;
            }
            return Argument * Argument;
        }
        public static Expr Sqrt(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Sqrt, a_array));
            }
            if (Argument.IsBinary("*", out Expr argA, out Expr argB))
            {
                if (argA.Equals(argB))
                {
                    return Abs(argA);
                }
            }
            if (Argument.IsUnary("sqr", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Exp(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Exp, a_array));
            }
            if (Argument.IsUnary("log", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Log(Expr Argument, double newBase)
        {
            return Ln(Argument)/Math.Log(newBase);
        }
        public static Expr Ln(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Ln, a_array));
            }
            if (Argument.IsUnary("exp", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Sin(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Sin, a_array));
            }
            if (Argument.IsUnary("asin", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Cos(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Cos, a_array));
            }
            if (Argument.IsUnary("acos", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Tan(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Tan, a_array));
            }
            if (Argument.IsUnary("atan", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Sinh(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Sinh, a_array));
            }
            if (Argument.IsUnary("asinh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Cosh(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Cosh, a_array));
            }
            if (Argument.IsUnary("acosh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Tanh(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Tanh, a_array));
            }
            if (Argument.IsUnary("atanh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Asin(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Asin, a_array));
            }
            if (Argument.IsUnary("sin", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Acos(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Acos, a_array));
            }
            if (Argument.IsUnary("cos", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Atan(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Atan, a_array));
            }
            if (Argument.IsUnary("tan", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Asinh(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Asinh, a_array));
            }
            if (Argument.IsUnary("sinh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Acosh(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Acosh, a_array));
            }
            if (Argument.IsUnary("cosh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }
        public static Expr Atanh(Expr Argument)
        {
            if (Argument.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(Atanh, a_array));
            }
            if (Argument.IsUnary("tanh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (Argument.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, Argument);
        }

        #endregion
    }
}
