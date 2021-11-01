using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using static System.Math;

namespace JA
{
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Reflection;
    using System.Reflection.Emit;


    public abstract record Operation(string Identifier);

    public record ConstOp(string Identifier, double Value) : Operation(Identifier)
    {
        public static implicit operator ConstOp(string symbol) => KnownConstDictionary.Defined[symbol];

        [Op("pi")] public static readonly double PI = Math.PI;
        [Op("deg")] public static readonly double Deg = Math.PI/180;
        [Op("rad")] public static readonly double Rad = 180/Math.PI;
        [Op("rpm")] public static readonly double Rpm = Math.PI/30;
        [Op("e")] public static readonly double E = Math.E;
        [Op("Φ")] public static readonly double Phi = (1 + Math.Sqrt(5)) / 2;

        public static bool IsConst(string symbol, out ConstOp op)
        {
            if (KnownConstDictionary.Defined.Contains(symbol))
            {
                op = KnownConstDictionary.Defined[symbol];
                return true;
            }
            op = null;
            return false;
        }
    }

    public record UnaryOp(string Identifier, FArg1 Function, MethodInfo Method) : Operation(Identifier)
    {
        public string FunctionName { get => Method.Name; }

        public static implicit operator UnaryOp(string op) => KnownUnaryDictionary.Defined[op];

        public static UnaryOp FromMethod([CallerMemberName] string method = null)
        {
            return method.ToLowerInvariant();
        }

        [Op("+")] public static double Pos(double x) => x;
        [Op("-")] public static double Neg(double x) => -x;
        [Op("pi")] public static double Pi(double x) => PI * x;
        [Op("abs")] public static double Abs(double x) => Math.Abs(x);
        [Op("sign")] public static double Sign(double x) => Math.Sign(x);
        [Op("inv")] public static double Inv(double x) => 1 / x;
        [Op("sqr")] public static double Sqr(double x) => x * x;
        [Op("cub")] public static double Cub(double x) => x * x * x;
        [Op("exp")] public static double Exp(double x) => Math.Exp(x);
        [Op("ln")] public static double Ln(double x) => Math.Log(x);
        [Op("sqrt")] public static double Sqrt(double x) => Math.Sqrt(x);
        [Op("cbrt")] public static double Cbrt(double x) => Math.Cbrt(x);
        [Op("floor")] public static double Floor(double x) => Math.Floor(x);
        [Op("ceil")] public static double Ceiling(double x) => Math.Ceiling(x);
        [Op("round")] public static double Round(double x) => Math.Round(x);
        [Op("rnd")] public static double Random(double x) => ExpressionEx.RandomNumberGenerator.NextDouble() * x;
        [Op("sin")] public static double Sin(double x) => Math.Sin(x);
        [Op("cos")] public static double Cos(double x) => Math.Cos(x);
        [Op("tan")] public static double Tan(double x) => Math.Tan(x);
        [Op("sind")] public static double Sind(double x) => Math.Sin(x * PI / 180);
        [Op("cosd")] public static double Cosd(double x) => Math.Cos(x * PI / 180);
        [Op("tand")] public static double Tand(double x) => Math.Tan(x * PI / 180);
        [Op("asin")] public static double Asin(double x) => Math.Asin(x);
        [Op("acos")] public static double Acos(double x) => Math.Acos(x);
        [Op("atan")] public static double Atan(double x) => Math.Atan(x);
        [Op("asind")] public static double Asind(double x) => Math.Asin(x) * 180 / PI;
        [Op("acosd")] public static double Acosd(double x) => Math.Acos(x) * 180 / PI;
        [Op("atand")] public static double Atand(double x) => Math.Atan(x) * 180 / PI;
        [Op("sinh")] public static double Sinh(double x) => Math.Sinh(x);
        [Op("cosh")] public static double Cosh(double x) => Math.Cosh(x);
        [Op("tanh")] public static double Tanh(double x) => Math.Tanh(x);
        [Op("asinh")] public static double Asinh(double x) => Math.Asinh(x);
        [Op("acosh")] public static double Acosh(double x) => Math.Acosh(x);
        [Op("atanh")] public static double Atanh(double x) => Math.Atanh(x);

        public static bool IsUnary(string name, out UnaryOp op)
        {
            if (KnownUnaryDictionary.Defined.Contains(name))
            {
                op = KnownUnaryDictionary.Defined[name];
                return true;
            }
            op = null;
            return false;
        }

    }
    public record BinaryOp(string Identifier, FArg2 Function, MethodInfo Method) : Operation(Identifier)
    {
        public static implicit operator BinaryOp(string op) => KnownBinaryDictionary.Defined[op];
        public static BinaryOp FromMethod([CallerMemberName] string method = null)
        {
            return method.ToLowerInvariant();
        }

        public string FunctionName { get => Method.Name; }

        [Op("+")] public static double Add(double x, double y) => x + y;
        [Op("-")] public static double Sub(double x, double y) => x - y;
        [Op("*")] public static double Mul(double x, double y) => x * y;
        [Op("/")] public static double Div(double x, double y) => x / y;
        [Op("^")] public static double Pow(double x, double y) => Math.Pow(x, y);
        [Op("min")] public static double Max(double x, double y) => Math.Min(x, y);
        [Op("max")] public static double Min(double x, double y) => Math.Max(x, y);
        [Op("atan2")] public static double Atan2(double dy, double dx) => Math.Atan2(dy, dx);

        public static bool IsBinary(string name, out BinaryOp op)
        {
            if (KnownBinaryDictionary.Defined.Contains(name))
            {
                op = KnownBinaryDictionary.Defined[name];
                return true;
            }
            op = null;
            return false;
        }

    }

    public class KnownConstDictionary : KeyedCollection<string, ConstOp>
    {
        protected override string GetKeyForItem(ConstOp item)
        {
            return item.Identifier;
        }
        public static readonly KnownConstDictionary Defined =  GetDefined();
        static KnownConstDictionary GetDefined()
        {
            var dic = new KnownConstDictionary();
            var defn = typeof(ConstOp).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var info in defn)
            {
                if (info.FieldType == typeof(double))
                {
                    var attr = info.GetCustomAttribute<OpAttribute>();
                    if (attr != null)
                    {
                        var x = (double)info.GetValue(null);
                        dic.Add(new ConstOp(attr.Operand, x));
                    }
                }
            }
            return dic;
        }

        public bool ContainsKey(string key)
        {
            return base.Contains(key);
        }
        
        public IEnumerable<string> Keys { get => Items.Select((op)=>op.Identifier); }
        public IEnumerable<ConstOp> Values { get => Items; }
        public ConstOp[] ToArray() => Values.ToArray();
    }

    public class KnownUnaryDictionary : KeyedCollection<string, UnaryOp>
    {
        protected override string GetKeyForItem(UnaryOp item)
        {
            return item.Identifier;
        }
        public static readonly KnownUnaryDictionary Defined = GetDefined();
        static KnownUnaryDictionary GetDefined()
        {
            var dic = new KnownUnaryDictionary();
            var defn = typeof(UnaryOp).GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (var info in defn)
            {
                var args = info.GetParameters();
                if (args.Length == 1 && args.All((pi) => pi.ParameterType == typeof(double)))
                {
                    var attr = info.GetCustomAttribute<OpAttribute>();
                    if (attr != null)
                    {
                        var minfo = typeof(Math).GetMethod(info.Name, new[] { typeof(double) });
                        if (minfo == null || minfo.ReturnType != typeof(double))
                        {
                            minfo = info;
                        }
                        var f = (FArg1)Delegate.CreateDelegate(typeof(FArg1), minfo);
                        dic.Add(new UnaryOp(attr.Operand, f, info));
                    }
                }
            }
            return dic;
        }
        public bool ContainsKey(string key)
        {
            return base.Contains(key);
        }

        public IEnumerable<string> Keys { get => Items.Select((op) => op.Identifier); }
        public IEnumerable<UnaryOp> Values { get => Items; }

    }
    public class KnownBinaryDictionary : KeyedCollection<string, BinaryOp>
    {
        protected override string GetKeyForItem(BinaryOp item)
        {
            return item.Identifier;
        }
        public static readonly KnownBinaryDictionary Defined = GetDefined();
        static KnownBinaryDictionary GetDefined()
        {
            var dic = new KnownBinaryDictionary();
            var defn = typeof(BinaryOp).GetMethods( BindingFlags.Public | BindingFlags.Static );
            foreach (var info in defn)
            {
                var args = info.GetParameters();
                if (args.Length == 2 && args.All((pi) => pi.ParameterType == typeof(double)))
                {
                    var attr = info.GetCustomAttribute<OpAttribute>();
                    if (attr != null)
                    {
                        var minfo = typeof(Math).GetMethod(info.Name, new[] { typeof(double), typeof(double) });
                        if (minfo == null)
                        {
                            minfo = info;
                        }
                        var f = (FArg2)Delegate.CreateDelegate(typeof(FArg2), minfo);
                        dic.Add(new BinaryOp(attr.Operand, f, info));
                    }
                }
            }
            return dic;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class OpAttribute : Attribute
    {
        public OpAttribute(string operand)
        {
            Operand = operand;
        }

        public string Operand { get; }
    }
}
