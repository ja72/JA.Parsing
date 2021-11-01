using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JA.Expressions
{
    public partial record Expr
    {
        #region Operators
        public static Expr operator +(Expr a) => a;
        public static Expr operator -(Expr a) => Negate(a);
        public static Expr operator +(Expr a, Expr b) => Add(a, b);
        public static Expr operator -(Expr a, Expr b) => Subtract(a, b);
        public static Expr operator *(Expr a, Expr b)
        {
            if (a.IsArray(out _) && b.IsArray(out _))
            {
                return Product(a, b);
            }
            return Multiply(a, b);
        }

        public static Expr operator /(Expr a, Expr b)
        {
            if (a.IsArray(out _) && b.IsArray(out _))
            {
                return Solve(a, b);
            }
            return Divide(a, b);
        }

        public static Expr operator ^(Expr a, Expr b) => Power(a, b);
        #endregion


        #region Functions
        public static Expr Add(Expr Left, Expr Right)
        {
            if (IsVectorizable(Left, Right, out var a_array, out var b_array, true))
            {
                return Array(BinaryVectorOp(Add, a_array, b_array));
            }
            if (Left.IsConstant(out double x, true) && Right.IsConstant(out double y, true))
            {
                return x + y;
            }
            if (Left.IsConstant(out x, true) && x == 0)
            {
                return Right;
            }
            if (Right.IsConstant(out y, true) && y == 0)
            {
                return Left;
            }
            if (Left.Equals(Right))
            {
                return 2 * Left;
            }
            if (Left.IsUnary("-", out Expr argA)
                && Right.IsUnary("-", out Expr argB))
            {
                return -(argB + argA);
            }
            if (Right.IsUnary("-", out Expr argB2))
            {
                return Left - argB2;
            }
            if (Left.IsUnary("-", out Expr argA2))
            {
                return Right - argA2;
            }
            if (Left.IsFactor(out double a_factor, out var a_arg)
                && Right.IsFactor(out double b_factor, out var b_arg))
            {
                if (a_arg.Equals(b_arg))
                {
                    return (a_factor+b_factor)*a_arg;
                }
            }
            if (Left.IsBinary("+", out var a_add_left, out var a_add_right))
            {
                if (Right.IsBinary("+", out var b_add_left, out var b_add_right))
                {
                    return Sum(a_add_left, a_add_right, b_add_left, b_add_right);
                }
                if (Right.IsBinary("-", out var b_sub_left, out var b_sub_right))
                {
                    return Sum(a_add_left, a_add_right, b_sub_left, -b_sub_right);
                }
            }
            if (Left.IsBinary("-", out var a_sub_left, out var a_sub_right))
            {
                if (Right.IsBinary("+", out var b_add_left, out var b_add_right))
                {
                    return Sum(a_sub_left, -a_sub_right, b_add_left, b_add_right);
                }
                if (Right.IsBinary("-", out var b_sub_left, out var b_sub_right))
                {
                    return Sum(a_sub_left, -a_sub_right, b_sub_left, -b_sub_right);
                }
            }

            if (Right.IsBinary("*", out var b_left, out var b_right))
            {
                if (b_left.IsConstant(out double b_left_val))
                {
                    if (b_left_val<0)
                    {
                        return Subtract(Left, (-b_left_val)*b_right);
                    }
                }
            }

            return new BinaryExpr("+", Left, Right);
        }
        public static Expr Subtract(Expr Left, Expr Right)
        {
            if (IsVectorizable(Left, Right, out var a_array, out var b_array))
            {
                return Array(BinaryVectorOp(Subtract, a_array, b_array));
            }
            if (Left.IsConstant(out double x, true) && Right.IsConstant(out double y, true))
            {
                return x - y;
            }
            if (Left.IsConstant(out x, true))
            {
                if (x == 0)
                {
                    return -Right;
                }
            }
            if (Right.IsConstant(out y, true))
            {
                if (y == 0)
                {
                    return Left;
                }
            }
            if (Left.Equals(Right))
            {
                return 0;
            }
            if (Left.IsUnary("-", out Expr argA)
                && Right.IsUnary("-", out Expr argB))
            {
                return argB - argA;
            }
            if (Right.IsUnary("-", out Expr argB2))
            {
                return Left + argB2;
            }
            if (Left.IsUnary("-", out Expr argA2))
            {
                return -(argA2 + Right);
            }
            if (Left.IsFactor(out double a_factor, out var a_arg)
                && Right.IsFactor(out double b_factor, out var b_arg))
            {
                if (a_arg.Equals(b_arg))
                {
                    return (a_factor-b_factor)*a_arg;
                }
            }
            if (Left.IsBinary("+", out var a_add_left, out var a_add_right))
            {
                if (Right.IsBinary("+", out var b_add_left, out var b_add_right))
                {
                    return Sum(a_add_left, a_add_right, -b_add_left, -b_add_right);
                }
                if (Right.IsBinary("-", out var b_sub_left, out var b_sub_right))
                {
                    return Sum(a_add_left, a_add_right, -b_sub_left, b_sub_right);
                }
            }
            if (Left.IsBinary("-", out var a_sub_left, out var a_sub_right))
            {
                if (Right.IsBinary("+", out var b_add_left, out var b_add_right))
                {
                    return Sum(a_sub_left, -a_sub_right, -b_add_left, -b_add_right);
                }
                if (Right.IsBinary("-", out var b_sub_left, out var b_sub_right))
                {
                    return Sum(a_sub_left, -a_sub_right, -b_sub_left, b_sub_right);
                }
            }

            if (Right.IsBinary("*", out var b_left, out var b_right))
            {
                if (b_left.IsConstant(out double b_left_val))
                {
                    if (b_left_val<0)
                    {
                        return Add(Left, (-b_left_val)*b_right);
                    }
                }
            }
            return new BinaryExpr("-", Left, Right);
        }
        public static Expr Multiply(Expr Left, Expr Right)
        {
            if (IsVectorizable(Left, Right, out var a_array, out var b_array, true))
            {
                return Array(BinaryVectorOp(Multiply, a_array, b_array));
            }
            if (Left.IsUnary("sqrt", out Expr argA)
                && Right.IsUnary("sqrt", out Expr argB))
            {
                return Sqrt(argA * argB);
            }
            if (Left.IsConstant(out double x, true) && Right.IsConstant(out double y, true))
            {
                return x * y;
            }
            if (Left.IsConstant(out double a_const, true))
            {
                if (a_const == 0)
                {
                    return 0;
                }
                if (a_const == 1)
                {
                    return Right;
                }
            }
            if (Left.IsConstant(out a_const))
            {
                if (Right.IsBinary("*", out var b_mul_left, out var b_mul_right))
                {
                    if (b_mul_left.IsConstant(out double b_mul_left_const))
                    {
                        return (a_const * b_mul_left_const) * b_mul_right;
                    }
                    if (b_mul_right.IsConstant(out double b_mul_right_const))
                    {
                        return (a_const * b_mul_right_const) * b_mul_left;
                    }
                }
                if (Right.IsBinary("/", out var b_div_left, out var b_div_right))
                {
                    if (b_div_left.IsConstant(out double b_div_left_const))
                    {
                        return (a_const * b_div_left_const) / b_div_right;
                    }
                    if (b_div_right.IsConstant(out double b_div_right_const))
                    {
                        return (a_const / b_div_right_const) * b_div_left;
                    }
                }
                if (Math.Abs(a_const)<0.2)
                {
                    return Right/(1/a_const);
                }
            }
            if (Right.IsConstant(out double b_const, true))
            {
                if (b_const == 0)
                {
                    return 0;
                }
                if (b_const == 1)
                {
                    return Left;
                }
            }
            if (Right.IsConstant(out b_const))
            {
                if (Math.Abs(b_const)<1)
                {
                    return Left / (1/b_const);
                }
                else
                {
                    return b_const * Left;
                }
            }
            if (Left.IsFactor(out double a_factor, out var a_arg)
                && Right.IsFactor(out double b_factor, out var b_arg))
            {
                if (a_factor!=1 || b_factor!=1)
                {
                    return (a_factor*b_factor)*(a_arg*b_arg);
                }
            }
            if (Left.IsBinary(out var aop, out var a_left, out var a_right)
                && Right.IsBinary(out var bop, out var b_left, out var b_right))
            {
                if (aop=="/" && bop=="/")
                {
                    // (x/a)*(y/b) = (x*y)/(a*b)
                    return (a_left*b_left)/(a_right*b_right);
                }
            }
            if (Left.IsBinary(out aop, out a_left, out a_right))
            {
                if (aop=="*")
                {
                    if (a_left.IsConstant(out var a_left_const))
                    {
                        return a_left_const * (a_right  * Right);
                    }
                    if (a_right.IsConstant(out var a_right_const))
                    {
                        return a_right_const * (a_left * Right);
                    }
                }
                if (aop=="/")
                {
                    if (a_left.IsConstant(out var a_left_const))
                    {
                        return a_left_const*(Right/a_right);
                    }
                    if (a_right.IsConstant(out var a_right_const))
                    {
                        return (a_left*Right)/a_right_const;
                    }
                }
            }
            if (Right.IsBinary(out bop, out b_left, out b_right))
            {
                if (bop=="*")
                {
                    if (b_left.IsConstant(out var b_left_const))
                    {
                        return b_left_const * (Left*b_right);
                    }
                    if (b_right.IsConstant(out var b_right_const))
                    {
                        return b_right_const * (Left*b_left);
                    }
                }
                if (bop=="/")
                {
                    if (b_left.IsConstant(out var b_left_const))
                    {
                        return b_left_const * (Left/b_right);
                    }
                    if (b_right.IsConstant(out var b_right_const))
                    {
                        return (Left*b_left)/b_right_const;
                    }
                }
            }
            if (Left.Equals(Right))
            {
                return Left^2;
            }
            return new BinaryExpr("*", Left, Right);
        }

        public static Expr Divide(Expr Left, Expr Right)
        {
            if (IsVectorizable(Left, Right, out var a_array, out var b_array, true))
            {
                return Array(BinaryVectorOp(Divide, a_array, b_array));
            }
            if (Left.IsConstant(out double x) && Right.IsConstant(out double y))
            {
                return x / y;
            }
            if (Left.IsConstant(out double a_const, true))
            {
                if (a_const == 0)
                {
                    return 0;
                }
            }
            if (Left.IsConstant(out a_const))
            {
                if (a_const<0.2)
                {
                    return 1/((1/a_const)*Right);
                }
            }
            if (Right.IsConstant(out double b_const, true))
            {
                if (b_const == 0)
                {
                    return double.PositiveInfinity;
                }
                if (b_const == 1)
                {
                    return Left;
                }
            }
            if (Right.IsConstant(out b_const))
            {
                if (Left.IsBinary("*", out var a_mul_left, out var a_mul_right))
                {
                    if (a_mul_left.IsConstant(out var a_mul_left_const))
                    {
                        return (a_mul_left_const/b_const) * a_mul_right;
                    }
                    if (a_mul_right.IsConstant(out var a_mul_right_const))
                    {
                        return (a_mul_right_const/b_const) * a_mul_left;
                    }
                }
                if (Left.IsBinary("/", out var a_div_left, out var a_div_right))
                {
                    if (a_div_left.IsConstant(out var a_div_left_const))
                    {
                        return (a_div_left_const/b_const) / a_div_right;
                    }
                    if (a_div_right.IsConstant(out var a_div_right_const))
                    {
                        return a_div_left/(a_div_right_const*b_const);
                    }
                }
                if (Math.Abs(b_const)<0.2)
                {
                    return (1/b_const)*Left;
                }
            }
            if (Left.Equals(Right))
            {
                return 1;
            }
            if (Left.IsUnary("-", out Expr negA))
            {
                return Negate(negA / Right);
            }
            if (Right.IsUnary("-", out Expr negB))
            {
                return Negate(Left / negB);
            }
            if (Left.IsUnary("sqrt", out Expr argA) && argA == Right)
            {
                return 1 / Sqr(Right);
            }
            if (Right.IsUnary("sqrt", out Expr argB) && argB == Left)
            {
                return Sqr(Left);
            }
            if (Left.IsFactor(out double a_factor, out var a_arg)
                && Right.IsFactor(out double b_factor, out var b_arg))
            {
                if (a_factor!=1 && b_factor!=1)
                {
                    return (a_factor/b_factor)*(a_arg/b_arg);
                }
            }
            if (Left.IsBinary(out var aop, out var a_left, out var a_right)
                && Right.IsBinary(out var bop, out var b_left, out var b_right))
            {
                if (aop=="/" && bop=="/")
                {
                    return (a_left*b_right)/(a_right*b_left);
                }
            }
            if (Right.IsBinary(out bop, out b_left, out b_right))
            {
                if (bop=="*")
                {
                    if (b_left.IsFactor(out var b_left_factor, out var b_left_arg) && b_left_factor!=1)
                    {
                        return (1/b_left_factor)*(Left/(b_left_arg*b_right));
                    }
                    if (b_right.IsFactor(out var b_right_factor, out var b_right_arg) && b_right_factor!=1)
                    {
                        return (1/b_right_factor)*(Left/(b_left  *b_right_arg));
                    }
                }
                if (bop=="/")
                {
                    if (b_left.IsFactor(out var b_left_factor, out var b_left_arg) && b_left_factor!=1)
                    {
                        return (1/b_left_factor)*(Left*b_right/b_left_arg);
                    }
                    if (b_right.IsFactor(out var b_right_factor, out var b_right_arg) && b_right_factor!=1)
                    {
                        return (b_right_factor)*(Left*b_right_arg/b_left);
                    }
                }
            }
            return new BinaryExpr("/", Left, Right);
        }

        public static Expr Sum(params Expr[] terms)
        {
            List<double> numnbers = new();
            List<Expr> constants = new();
            List<Expr> expressions = new();
            for (int i = 0; i < terms.Length; i++)
            {
                if (terms[i].IsNamedConstant(out _, out _))
                {
                    constants.Add(terms[i]);
                }
                else if (terms[i].IsConstant(out var x, true))
                {
                    numnbers.Add(x);
                }
                else
                {
                    expressions.Add(terms[i]);
                }
            }
            if (constants.Count>1 || (constants.Count>0 && numnbers.Count>0))
            {
                foreach (var c in constants)
                {
                    if (c.IsConstant(out var x, true))
                    {
                        numnbers.Add(x);
                    }
                    else
                    {
                        throw new InvalidOperationException("Expected a constant here.");
                    }
                }
                constants.Clear();
            }
            else
            {
                expressions.AddRange(constants);
                constants.Clear();
            }

            List<Expr> collect = new();
            foreach (var grp in expressions.GroupBy((item) => item))
            {
                collect.Add(grp.Count() * grp.Key);
            }

            return collect.Aggregate((sum, expr) => sum+expr) + numnbers.Sum();
        }
        public static Expr Sum(Expr array)
        {
            if (array.IsArray(out var terms))
            {
                return Sum(terms);
            }
            return array;
        }
        public static Expr Power(Expr Left, Expr Right)
        {
            if (IsVectorizable(Left, Right, out var a_array, out var b_array))
            {
                return Array(BinaryVectorOp(Power, a_array, b_array));
            }
            if (Right.IsConstant(out var bConst))
            {
                switch (bConst)
                {
                    case 0:
                        return 1;
                    case 1:
                        return Left;
                    case -1:
                        return 1/Left;
                }
            }
            if (Left.IsConstant(out double aConst))
            {
                if (Right.IsConstant(out bConst))
                {
                    return Math.Pow(aConst, bConst);
                }
                return Exp(Math.Log(aConst) * Right);
            }
            return new BinaryExpr("^", Left, Right);
        }

        #endregion

    }
}
