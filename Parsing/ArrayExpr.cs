using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

namespace JA.Parsing
{
    public sealed class ArrayExpr : Expr, IEquatable<ArrayExpr>
    {
        public ArrayExpr(params Expr[] elements)
        {
            Elements=elements;
        }

        public Expr[] Elements { get; }

        public override int ResultCount => Elements.Length;

        #region Methods

        protected internal override void Compile(ILGenerator generator, Dictionary<VariableExpr, int> envirnoment)
        {
            Debug.WriteLine($"Compile Array[{Elements.Length}]");
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

        public override bool Equals(Expr other)
        {
            if (other is ArrayExpr array)
            {
                return Equals(array);
            }
            return false;
        }

        protected internal override void AddVariables(List<VariableExpr> variables)
        {
            foreach (var item in Elements)
            {
                item.AddVariables(variables);
            }
        }

        public override Expr Substitute(VariableExpr target, Expr expression)
        {
            return new ArrayExpr(Elements.Select((item) => item.Substitute(target, expression)).ToArray());
        }

        internal static bool IsVectorizable(Expr argument, out int count, out Expr[] argArray)
        {
            if (argument.IsArray(out argArray))
            {
                count = argArray.Length;
                return true;
            }
            count = 1;
            argArray = null;
            return false;
        }

        internal static bool IsVectorizable(Expr left, Expr right, out int count, out Expr[] leftArray, out Expr[] rightArray)
        {
            if (left.IsArray(out leftArray) && right.IsArray(out rightArray))
            {
                int lcount = leftArray.Length;
                int rcount = rightArray.Length;
                if (lcount < rcount)
                {
                    var temp = new Expr[rcount];
                    int index = 0;
                    while (index<temp.Length)
                    {
                        Array.Copy(leftArray, 0, temp, index, Math.Min(lcount, temp.Length-index));
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
                        Array.Copy(rightArray, 0, temp, index, Math.Min(rcount, temp.Length-index));
                        index += rcount;
                    }
                    rightArray = temp;
                    rcount = temp.Length;
                }
                count = Math.Max(lcount, rcount);
                return true;
            }
            else if (left.IsArray(out leftArray))
            {
                rightArray = new Expr[leftArray.Length];
                for (int i = 0; i < rightArray.Length; i++)
                {
                    rightArray[i] = right;
                }
                return IsVectorizable(leftArray, rightArray, out count, out leftArray, out rightArray);
            }
            else if (right.IsArray(out rightArray))
            {
                leftArray = new Expr[rightArray.Length];
                for (int i = 0; i < leftArray.Length; i++)
                {
                    leftArray[i] = left;
                }
                return IsVectorizable(leftArray, rightArray, out count, out leftArray, out rightArray);
            }
            // both scalar
            count = 1;
            leftArray = null;
            rightArray = null;
            return false;
        }

        /// <summary>
        /// Get the partial derivative of the expression with respect to a variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// </example>
        public override Expr Partial(VariableExpr variable)
        {
            return new ArrayExpr(Elements.Select((item) => item.Partial(variable)).ToArray());
        }

        #endregion

        #region Formatting
        public override string ToString(string format, IFormatProvider provider)
        {
            return $"[{string.Join(",", Elements.Select((item) => item.ToString(format, provider)))}]";
        }
        #endregion

        #region IEquatable Members
        /// <summary>
        /// Equality overrides from <see cref="Expr"/>
        /// </summary>
        /// <param name="obj">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(UnaryOperatorExpr)</code></returns>
        public override bool Equals(object obj)
        {
            if (obj is ArrayExpr array)
            {
                return Equals(array);
            }
            return false;
        }

        /// <summary>
        /// Checks for equality among <see cref="ArrayExpr"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public bool Equals(ArrayExpr other)
        {
            return Enumerable.SequenceEqual(Elements, other.Elements);
        }
        /// <summary>
        /// Calculates the hash code for the <see cref="ArrayExpr"/>
        /// </summary>
        /// <returns>The int hash value</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hc = -1817952719;
                for (int i = 0; i < Elements.Length; i++)
                {
                    hc = (-1521134295)*hc + Elements[i].GetHashCode();
                }
                return hc;
            }
        }

        #endregion

    }

}
