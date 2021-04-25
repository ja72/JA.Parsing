using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;

namespace JA.Parsing
{
    public enum BinaryOp
    {
        Undefined,
        [Description("+")] Add,
        [Description("-")] Subtract,
        [Description("*")] Multiply,
        [Description("/")] Divide,
        [Description("max")] Max,
        [Description("min")] Min,
        [Description("pow")] Pow,
        [Description("sign")] Sign,
        [Description("log")] Log,
    }
    public sealed class BinaryExpr : 
        Expr,
        IEquatable<BinaryExpr>
    {
        internal static readonly Dictionary<BinaryOp, Func<double, double, double>> functions = new Dictionary<BinaryOp, Func<double, double, double>>()
        {
            [BinaryOp.Add] = (x, y) => x + y,
            [BinaryOp.Subtract] = (x, y) => x - y,
            [BinaryOp.Multiply] = (x, y) => x * y,
            [BinaryOp.Divide] = (x, y) => x / y,
            [BinaryOp.Min] = (x, y) => Math.Min(x, y),
            [BinaryOp.Max] = (x, y) => Math.Max(x, y),
            [BinaryOp.Pow] = (x, y) => Math.Pow(x, y),
            [BinaryOp.Sign] = MathExtra.Sign,
            [BinaryOp.Log] = (x,y) => Math.Log(x,y),
        };

        public BinaryExpr(BinaryOp op, Expr left, Expr right)
        {
            if (op==BinaryOp.Undefined)
            {
                throw new ArgumentException("Undefined operand.", nameof(op));
            }
            this.Op = op;
            this.Key = Parser.DescriptionAttr(op);
            
            try
            {
                Function = functions[op];
            }
            catch(KeyNotFoundException ex)
            {
                Debug.WriteLine(ex.ToString());
                throw new ArgumentException($"Operator {op} not found.", nameof(op));
            }
            this.Left = left;
            this.Right = right;
        }
        public string Key { get; }
        public BinaryOp Op { get; }
        public Expr Left { get; }
        public Expr Right { get; }
        public Func<double, double, double> Function { get; }
        public override int ResultCount => Math.Max(Left.ResultCount, Right.ResultCount);

        protected internal override void AddVariables(List<VariableExpr> variables)
        {
            Left.AddVariables(variables);
            Right.AddVariables(variables);
        }
        protected internal override void Compile(ILGenerator generator, Dictionary<VariableExpr, int> envirnoment)
        {
            if (ArrayExpr.IsVectorizable(Left, Right, out int count, out Expr[] leftArray, out Expr[] rightArray))
            {
                var vector = new Expr[count];
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = Binary(Op, leftArray[i], rightArray[i]);
                }
                FromArray(vector).Compile(generator, envirnoment);
                return;
            }
            Debug.WriteLine($"Compile {Op}(x,y)");
            Left.Compile(generator, envirnoment);
            Right.Compile(generator, envirnoment);

            switch (Op)
            {
                case BinaryOp.Add:
                    generator.Emit(OpCodes.Add);
                    break;
                case BinaryOp.Subtract:
                    generator.Emit(OpCodes.Sub);
                    break;
                case BinaryOp.Multiply:
                    generator.Emit(OpCodes.Mul);
                    break;
                case BinaryOp.Divide:
                    generator.Emit(OpCodes.Div);
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
            Expr x = Left, xp = Left.Partial(variable);
            Expr y = Right, yp = Right.Partial(variable);
            switch (Op)
            {
                case BinaryOp.Add: return xp+yp;
                case BinaryOp.Subtract: return xp-yp;
                case BinaryOp.Multiply: return xp*y+x*yp;
                case BinaryOp.Divide: return (y*xp-x*yp)/(y^2);
                case BinaryOp.Pow: return (x^(y-1))*(x*Log(x)*yp+y*xp);
                case BinaryOp.Min: return ((yp-xp)*Sign(x-y)+xp+yp)/2;
                case BinaryOp.Max: return ((xp-yp)*Sign(y-x)+xp+yp)/2;
                case BinaryOp.Sign: return xp*Sign(y/x);
                case BinaryOp.Log: return xp/(x*Log(y))-yp*Log(x)/(y*Log(y)^2);
                default:
                    throw new NotImplementedException($"Operator {Key} does not have slope defined.");
            }
        }
        public override Expr Substitute(VariableExpr target, Expr expression)
        {
            var left = target.Equals(Left) ? expression : Left.Substitute(target, expression);
            var right = target.Equals(Right) ? expression : Right.Substitute(target, expression);

            if (left.Equals(Left) && right.Equals(Right)) return this;

            return Binary(Op, left, right);
        }
        #region IEquatable Members
        public override bool Equals(object obj) => base.Equals(obj);
        /// <summary>
        /// Equality overrides from <see cref="Expr"/>
        /// </summary>
        /// <param name="other">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(BinaryOperatorExpr)</code></returns>
        public override bool Equals(Expr other)
        {
            if (other is BinaryExpr item)
            {
                return Equals(item);
            }
            return false;
        }

        /// <summary>
        /// Checks for equality among <see cref="BinaryExpr"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public bool Equals(BinaryExpr other)
        {
            return Op.Equals(other.Op)
                && Left.Equals(other.Left)
                && Right.Equals(other.Right);
        }
        /// <summary>
        /// Calculates the hash code for the <see cref="BinaryExpr"/>
        /// </summary>
        /// <returns>The int hash value</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295)*hc + Op.GetHashCode();
                hc = (-1521134295)*hc + Left.GetHashCode();
                hc = (-1521134295)*hc + Right.GetHashCode();
                return hc;
            }
        }

        #endregion

        #region Formatting
        public override string ToString(string format, IFormatProvider provider)
        {
            string left = Left.ToString(format, provider);
            string right = Right.ToString(format, provider);
            switch (Op)
            {
                case BinaryOp.Add:
                case BinaryOp.Subtract:
                case BinaryOp.Multiply:
                case BinaryOp.Divide:
                    return $"({left}{Key}{right})";
            }
            // Function
            return $"{Key}({left},{right})";
        }

        #endregion
    }

}
