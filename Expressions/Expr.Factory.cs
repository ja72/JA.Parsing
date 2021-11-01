using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JA.Expressions
{
    public partial record Expr
    {
        #region Constants        
        public static readonly NamedConstExpr Pi = new("pi");
        public static readonly NamedConstExpr E =  new("e");
        public static readonly NamedConstExpr Φ =  new("Φ");

        public static readonly ConstExpr Zero = 0;
        public static readonly ConstExpr One = 1;
        public static readonly Expr Deg = Pi/180;
        public static readonly Expr Rad = 180/Pi;
        public static readonly Expr Rpm = Pi/30;
        #endregion

        #region Factory
        public static implicit operator Expr(string expr) => Parse(expr);
        public static implicit operator Expr(double value) => Const(value);
        public static implicit operator Expr(Vector vector) => Vector(vector.Elements);
        public static implicit operator Expr(Matrix matrix) => Matrix(matrix.Elements);

        public static explicit operator string(Expr expression)
        {
            if (expression.IsSymbol(out string symbol))
            {
                return symbol;
            }
            throw new ArgumentException("Argument must be a " + nameof(VariableExpr) + " type.", nameof(expression));
        }
        public static explicit operator double(Expr expression)
        {
            if (expression.IsConstant(out double value, true))
            {
                return value;
            }
            throw new ArgumentException("Argument must be a " + nameof(ConstExpr) + " type.", nameof(expression));
        }

        public static ConstExpr Const(double value)
        {
            if (value == 0) return Zero;
            if (value == 1) return One;
            if (value == Math.PI) return Pi;
            if (value == Math.E) return E;
            return new ConstExpr(value);
        }
        public static Expr ConstOrVariable(string symbol)
        {
            if (ConstOp.IsConst(symbol, out var op))
            {
                return Const(op);
            }
            return Variable(symbol);

        }
        public static NamedConstExpr Const(ConstOp op) => new(op);
        public static NamedConstExpr Const(string symbol, double value)
        {
            if (ConstOp.IsConst(symbol, out var op))
            {
                if (op.Value != value)
                {
                    throw new ArgumentException($"Cannot change the value of the constant {symbol}");
                }
                return Const(op);
            }
            if (!parameters.ContainsKey(symbol))
            {
                parameters.Add(symbol, value);
            }
            else
            {
                parameters[symbol] = value;
            }
            return new NamedConstExpr(symbol, value);
        }

        public static VariableExpr Variable(string name) => new(name);

        public static Expr Unary(UnaryOp Op, Expr Argument)
        {
            if (Argument.IsConstant(out double value))
            {
                return Op.Function(value);
            }

            if (Argument.IsAssign(out var leftAsgn, out var rightAsgn))
            {
                return Assign(Unary(Op, leftAsgn), Unary(Op, rightAsgn));
            }

            if (Argument.IsArray(out var arrayExpr))
            {
                return Array(UnaryVectorOp(Op, arrayExpr));
            }

            if (Argument.IsUnary(out string argOp, out Expr argArg))
            {
                if (Op.Identifier=="-" && argOp=="-") return argArg;
                if (Op.Identifier=="inv" && argOp=="inv") return argArg;
                if (Op.Identifier=="ln" && argOp=="exp") return argArg;
                if (Op.Identifier=="exp" && argOp=="ln") return argArg;
                if (Op.Identifier=="sqrt" && argOp=="sqr") return Abs(argArg);
                if (Op.Identifier=="cbrt" && argOp=="cub") return argArg;
                if (Op.Identifier=="cub" && argOp=="cbrt") return argArg;
                if (Op.Identifier=="sin" && argOp=="asin") return argArg;
                if (Op.Identifier=="cos" && argOp=="acos") return argArg;
                if (Op.Identifier=="tan" && argOp=="atan") return argArg;
                if (Op.Identifier=="asin" && argOp=="sin") return argArg;
                if (Op.Identifier=="acos" && argOp=="cos") return argArg;
                if (Op.Identifier=="atan" && argOp=="tan") return argArg;
                if (Op.Identifier=="sinh" && argOp=="asinh") return argArg;
                if (Op.Identifier=="cosh" && argOp=="acosh") return argArg;
                if (Op.Identifier=="tanh" && argOp=="atanh") return argArg;
                if (Op.Identifier=="asinh" && argOp=="sinh") return argArg;
                if (Op.Identifier=="acosh" && argOp=="cosh") return argArg;
                if (Op.Identifier=="atanh" && argOp=="tanh") return argArg;
            }

            //if (Op.Identifier=="+") return Argument;
            switch (Op.Identifier)
            {
                case "+": return Argument;
                case "-": return Negate(Argument);
                case "inv": return 1/Argument;
                case "ln": return Ln(Argument);
                case "exp": return Exp(Argument);
                case "sqrt": return Sqrt(Argument);
                case "sqr": return Sqr(Argument);
                case "sin": return Sin(Argument);
                case "cos": return Cos(Argument);
                case "tan": return Tan(Argument);
                case "sind": return Sin(Deg*Argument);
                case "cosd": return Cos(Deg*Argument);
                case "tand": return Tan(Deg*Argument);
                case "sinh": return Sinh(Argument);
                case "cosh": return Cosh(Argument);
                case "tanh": return Tanh(Argument);
                case "asin": return Asin(Argument);
                case "acos": return Acos(Argument);
                case "atan": return Atan(Argument);
                case "asinh": return Asinh(Argument);
                case "acosh": return Acosh(Argument);
                case "atanh": return Atanh(Argument);
            }

            return new UnaryExpr(Op, Argument);
        }
        public static Expr Binary(BinaryOp Op, Expr Left, Expr Right)
        {
            if (Left.IsScalar() && Right.IsMatrix(out var rightMatrix))
            {
                Left = Matrix(Diagonal(rightMatrix.Length, Left));

                return Binary(Op, Left, Right);
            }
            if (Left.IsMatrix(out var leftMatrix) && Right.IsScalar())
            {
                Right = Matrix(Diagonal(leftMatrix.Length, Right));

                return Binary(Op, Left, Right);
            }
            if (IsVectorizable(Left, Right, out var leftArray, out var rightArray, true))
            {
                return Array(BinaryVectorOp(Op, leftArray, rightArray));
            }

            switch (Op.Identifier)
            {
                case "=": return Assign(Left, Right);
                case "+": return Add(Left, Right);
                case "-": return Subtract(Left, Right);
                case "*": return Multiply(Left, Right);
                case "/": return Divide(Left, Right);
                case "^": return Power(Left, Right);
                case "log" when Right.IsConstant(out var newBase): return Log(Left, newBase);
            }

            if (Left.IsConstant(out var leftValue) && Right.IsConstant(out var rightValue))
            {
                return Op.Function(leftValue, rightValue);
            }

            if (Left.IsAssign(out var leftAsgnLeft, out var leftAsgnRight)
                && Right.IsAssign(out var rightAsgnLeft, out var rightAsgnRight))
            {
                // (a=b) + (c=d) => (a+c) = (b+d)
                return Assign(
                    Binary(Op, leftAsgnLeft, rightAsgnLeft),
                    Binary(Op, leftAsgnRight, rightAsgnRight));
            }
            if (Left.IsAssign(out leftAsgnLeft, out leftAsgnRight))
            {
                // (a=b) + c => (a+c) = (b+c)
                return Assign(
                    Binary(Op, leftAsgnLeft, Right),
                    Binary(Op, leftAsgnRight, Right));
            }
            if (Right.IsAssign(out rightAsgnLeft, out rightAsgnRight))
            {
                // (a) + (c=d) => (a+c) = (a+d)
                return Assign(
                    Binary(Op, Left, rightAsgnLeft),
                    Binary(Op, Left, rightAsgnRight));
            }

            return new BinaryExpr(Op, Left, Right);
        }


        #endregion

    }
}
