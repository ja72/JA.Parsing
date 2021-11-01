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
