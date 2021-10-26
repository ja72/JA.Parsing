using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JA.Expressions
{

    public class Function : IFormattable
    {
        public Function(string name, Expr body)
            : this(name, body, body.GetSymbols().ToArray())
        {
        }
        internal Function(string name, Expr body, params string[] parameters)
        {
            var sym = body.GetSymbols();
            var missing = parameters.Except(sym).ToArray();
            if (missing.Length>0)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters),
                    $"Missing {string.Join(",", missing)} from parameter list.");
            }
            Name = name;
            Body = body;
            Parameters = parameters;
        }

        public string Name { get; }
        public Expr Body { get; }
        public string[] Parameters { get; }
        public int Rank => Body.Rank;
        public Function TotalDerivative() => TotalDerivative($"{Name}'");
        public Function TotalDerivative(string name)
        {
            var paramsAndDots = new List<string>(Parameters);
            var body = Body.TotalDerivative(ref paramsAndDots);
            return new Function(name, body, paramsAndDots.ToArray());
        }
        public Function TotalDerivative(string name, params (string sym, Expr expr)[] parameters)
        {
            var paramsAndDots = new List<string>(Parameters);
            var body = Body.TotalDerivative(parameters, ref paramsAndDots);
            return new Function(name, body, paramsAndDots.ToArray());
        }
        public Function PartialDerivative(string variable)
        {
            var body = Body.PartialDerivative(variable);
            return new Function($"{Name}_{variable}", body, Parameters);
        }
        public Function Substitute(string name, params (string symbol, double values)[] parameters)
        {
            List<string> arg = new List<string>(Parameters);
            Expr expr = Body.Substitute(parameters);
            foreach (var (symbol, _) in parameters)
            {
                arg.Remove(symbol);
            }
            return new Function(name, expr, arg.ToArray());
        }
        public Function Substitute(string name, params (string symbol, Expr subExpression)[] parameters)
        {
            List<string> arg = new List<string>(Parameters);
            Expr expr = Body.Substitute(parameters);
            foreach (var (symbol, _) in parameters)
            {
                arg.Remove(symbol);
            }
            return new Function(name, expr, arg.ToArray());
        }

        #region Compiling
        public static explicit operator FArg0(Function function) => function.CompileArg0();
        public static explicit operator FArg1(Function function) => function.CompileArg1();
        public static explicit operator FArg2(Function function) => function.CompileArg2();
        public static explicit operator FArg3(Function function) => function.CompileArg3();
        public static explicit operator FArg4(Function function) => function.CompileArg4();
        public static explicit operator VArg0(Function function) => function.CompileVArg0();
        public static explicit operator VArg1(Function function) => function.CompileVArg1();
        public static explicit operator VArg2(Function function) => function.CompileVArg2();

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public FArg0 CompileArg0() => Compile<FArg0>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public FArg1 CompileArg1() => Compile<FArg1>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public FArg2 CompileArg2() => Compile<FArg2>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public FArg3 CompileArg3() => Compile<FArg3>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public FArg4 CompileArg4() => Compile<FArg4>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public VArg0 CompileVArg0() => Compile<VArg0>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public VArg1 CompileVArg1() => Compile<VArg1>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public VArg2 CompileVArg2() => Compile<VArg2>();        

        internal T Compile<T>()
        {
            var asm = AssemblyBuilder.DefineDynamicAssembly(
                    new AssemblyName("LCG"),
                    AssemblyBuilderAccess.RunAndCollect);

            var mod = asm.DefineDynamicModule("Lightweight");
            
            var tpe = mod.DefineType("Code");
            var ret = typeof(T).GetMethod("Invoke").ReturnType;

            var mtd = tpe.DefineMethod("Generation",
                MethodAttributes.Public | MethodAttributes.Static,
                // Return type
                ret,
                // Every of the Parameters.Count parameters is of type int as well
                Enumerable.Repeat(typeof(double), Parameters.Length).ToArray()
            );

            // Establish environment; we could also use a List<T> and use IndexOf
            // in the recursive compilation code to find the parameter's index.
            var env = new Dictionary<string, int>();
            int arg = 0;
            foreach (var par in Parameters)
            {
                env[par] = arg++;
            }

            // Compilation is requested for the Body, and a final "ret" instruction
            // is added to the IL generator.
            var gen = mtd.GetILGenerator();
            
            Body.Compile(gen, env);

            if (ret.IsAssignableTo(typeof(IQuantity)))
            {
                switch (Body.Rank)
                {
                    case 0:
                        var fsinfo = typeof(Scalar).GetMethod("op_Implicit", new[] { typeof(double) });
                        gen.EmitCall(OpCodes.Call, fsinfo, null);
                        break;
                    case 1:
                        var fvinfo = typeof(Vector).GetMethod("op_Implicit", new[] { typeof(double[]) });
                        gen.EmitCall(OpCodes.Call, fvinfo, null);
                        break;
                    case 2:
                        var fminfo = typeof(Matrix).GetMethod("op_Implicit", new[] { typeof(double[][]) });
                        gen.EmitCall(OpCodes.Call, fminfo, null);
                        break;
                    default:
                        throw new NotSupportedException("Rank > 2 is not supported.");
                }
            }
            gen.Emit(OpCodes.Ret);

            var res = tpe.CreateType();

            // Create a delegate of the specified type, referring to the generated
            // method which we always call "Generation" (see the DefineMethod call).
            return (T)(object)Delegate.CreateDelegate(
                typeof(T),
                res.GetMethod("Generation"));
        }

        #endregion

        #region Formatting
        public override string ToString() => ToString("g");
        public string ToString(string formatting) => ToString(formatting, null);
        public string ToString(string format, IFormatProvider provider)
        {
            return $"{Name}({string.Join(",", Parameters)})={Body.ToString(format, provider)}";
        }
        #endregion

    }
}
