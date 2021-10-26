using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using JA.Expressions;

namespace JA
{
    public delegate double FArg0();
    public delegate double FArg1(double arg1);
    public delegate double FArg2(double arg1, double arg2);
    public delegate double FArg3(double arg1, double arg2, double arg3);
    public delegate double FArg4(double arg1, double arg2, double arg3, double arg4);
    public delegate double[] VArg0();
    public delegate double[] VArg1(double arg1);
    public delegate double[] VArg2(double arg1, double arg2);

    public delegate IQuantity QArg0();
    public delegate IQuantity QArg1(double arg1);
    public delegate IQuantity QArg2(double arg1, double arg2);

    public interface IExpression : IFormattable
    {
        string[] GetSymbols(bool alphabetically = true);
        double[] GetValues();
        int Rank { get; }
        IQuantity Eval(params (string sym, double val)[] parameters);
        void Compile(ILGenerator gen, Dictionary<string, int> env);
        bool IsConstant(out double value);
        bool IsNamedConstant(out string symbol, out double value);
        bool IsNamedConstant(string symbol, out double value);
        bool IsSymbol(out string symbol);
        bool IsSymbol(string symbol);
    }

    public interface IExpression<TExpr> : IExpression where TExpr : IExpression<TExpr>
    {
        bool IsArray(out TExpr[] elements);
        bool IsBinary(out string operation, out TExpr left, out TExpr right);
        bool IsBinary(string operation, out TExpr left, out TExpr right);
        bool IsMatrix(out TExpr[][] elements);
        bool IsUnary(out string operation, out TExpr argument);
        bool IsUnary(string operation, out TExpr argument);
        TExpr Substitute(params (string sym, TExpr expr)[] knownValues);
        TExpr[] ToArray();
        TExpr[][] ToMatrix();
    }

    public static class ExpressionEx
    {
        internal static readonly Type mathType = typeof(Math);
        internal static readonly Random RandomNumberGenerator = new();

        /// <summary>
        /// Get the operand (string) associated with an enum field
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">The enum value</param>
        /// <returns>The string as defined with the following attribute <see cref="OpAttribute"/></returns>        
        public static string GetOperand<TEnum>(this TEnum value) where TEnum : struct, Enum
        {
            var name = Enum.GetName(value);
            var field = typeof(TEnum).GetField(name);
            if (field.GetCustomAttributes(typeof(OpAttribute), false).FirstOrDefault() is OpAttribute attr)
            {
                return attr.Operand;
            }
            return null;
        }
    }


}
