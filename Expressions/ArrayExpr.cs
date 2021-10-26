using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

namespace JA.Expressions
{
    public record ArrayExpr : Expr
    {
        public ArrayExpr(Expr[] elements)
        {
            Elements=elements;
        }
        public ArrayExpr(Expr[][] elements)
        {
            Elements=elements.Select((item)=>Array(item)).ToArray();
        }

        public Expr[] Elements { get; }
        public override int Rank => Elements.Max((item)=>item.Rank)+1;        

        #region Methods

        public override IQuantity Eval(params (string sym, double val)[] parameters)
        {
            return Rank switch
            {
                1 => (Vector)(Elements.Select(item => item.Eval(parameters).Value).ToArray()),
                2 => (Matrix)(Elements.Select((item) => item.ToArray().Select(col => col.Eval(parameters).Value).ToArray()).ToArray()),
                _ => throw new NotSupportedException("Ranks more than 2 are not supported"),
            };
        }

        protected internal override void Compile(ILGenerator generator, Dictionary<string, int> envirnoment)
        {
            if (Rank==1)
            {
                generator.Emit(OpCodes.Ldc_I4, Elements.Length);
                generator.Emit(OpCodes.Newarr, typeof(double));
                for (int i = 0; i < Elements.Length; i++)
                {
                    generator.Emit(OpCodes.Dup);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    Elements[i].Compile(generator, envirnoment);
                    generator.Emit(OpCodes.Stelem_R8);
                }
            }
            else if (Rank==2)
            {
                generator.Emit(OpCodes.Ldc_I4, Elements.Length);
                generator.Emit(OpCodes.Newarr, typeof(double[]));
                for (int i = 0; i < Elements.Length; i++)
                {
                    generator.Emit(OpCodes.Dup);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    Elements[i].Compile(generator, envirnoment);
                    generator.Emit(OpCodes.Stelem_Ref);
                }
            }
            else
            {
                throw new NotSupportedException("Ranks more than 2 are not supported");
            }
        }


        protected internal override Expr Substitute(Expr variable, Expr value)
        {
            return new ArrayExpr(Elements.Select((item) => item.Substitute(variable, value)).ToArray());
        }
        internal static bool IsVectorizable(ref Expr[] leftArray, ref Expr[] rightArray, out int count)
        {
            int lcount = leftArray.Length;
            int rcount = rightArray.Length;
            if (lcount < rcount)
            {
                var temp = new Expr[rcount];
                int index = 0;
                while (index<temp.Length)
                {
                    System.Array.Copy(leftArray, 0, temp, index, Math.Min(lcount, temp.Length-index));
                    index += lcount;
                }
                leftArray = temp;
                lcount = temp.Length;
            }
            if (lcount > rcount)
            {
                var temp = new Expr[lcount];
                var index = 0;
                while (index<temp.Length)
                {
                    System.Array.Copy(rightArray, 0, temp, index, Math.Min(rcount, temp.Length-index));
                    index += rcount;
                }
                rightArray = temp;
                rcount = temp.Length;
            }
            count = Math.Max(lcount, rcount);
            return true;
        }
        internal static bool IsVectorizable(Expr left, Expr right, out int count, out Expr[] leftArray, out Expr[] rightArray)
        {
            if (left.IsArray(out leftArray) && right.IsArray(out rightArray))
            {
                return IsVectorizable(ref leftArray, ref rightArray, out count);
            }
            else if (left.IsArray(out leftArray))
            {
                rightArray = new Expr[leftArray.Length];
                for (int i = 0; i < rightArray.Length; i++)
                {
                    rightArray[i] = right;
                }
                //return IsVectorizable(leftArray, rightArray, out count, out leftArray, out rightArray);
                return IsVectorizable(ref leftArray, ref rightArray, out count);
            }
            else if (right.IsArray(out rightArray))
            {
                leftArray = new Expr[rightArray.Length];
                for (int i = 0; i < leftArray.Length; i++)
                {
                    leftArray[i] = left;
                }
                //return IsVectorizable(leftArray, rightArray, out count, out leftArray, out rightArray);
                return IsVectorizable(ref leftArray, ref rightArray, out count);
            }
            // both scalar
            count = 1;
            leftArray = null;
            rightArray = null;
            return false;
        }

        public override Expr PartialDerivative(SymbolExpr symbol)
        {
            return new ArrayExpr(Elements.Select((item) => item.PartialDerivative(symbol)).ToArray());
        }

        protected internal override void FillSymbols(ref List<string> variables)
        {
            for (int i = 0; i < Elements.Length; i++)
            {
                Elements[i].FillSymbols(ref variables);
            }
        }
        protected internal override void FillValues(ref List<double> values)
        {
            for (int i = 0; i < Elements.Length; i++)
            {
                Elements[i].FillValues(ref values);
            }
        }

        #endregion

        #region Formatting
        public override string ToString() => ToString("g");
        public override string ToString(string format, IFormatProvider provider)
        {
            return $"[{string.Join(",", Elements.Select((item) => item.ToString(format, provider)))}]";
        }
        #endregion

    }

}
