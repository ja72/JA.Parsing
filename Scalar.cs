using System;

namespace JA
{
    public record Scalar(double Value) : IQuantity
    {
        public static readonly Scalar Zero = 0;
        public static implicit operator Scalar(double value) => new(value);
        public static implicit operator double(Scalar scalar) => scalar.Value;
        public int Rank { get => 0; }
        double IQuantity.Value { get => Value; }
        double[] IQuantity.Array { get => new[] { Value }; }
        double[][] IQuantity.Array2 { get => new double[][] { new[] { Value } }; }

        #region Formatting
        public override string ToString() => ToString("g");
        public string ToString(string formatting) => ToString(formatting, null);
        public string ToString(string formatting, IFormatProvider formatProvider) => Value.ToString(formatting, formatProvider);
        #endregion

        #region Algebra
        public static Scalar Add(Scalar A, Scalar B)
        {
            return A.Value + B.Value;
        }
        public static Scalar Subtract(Scalar A, Scalar B)
        {
            return A.Value - B.Value;
        }

        public static Scalar Negate(Scalar A)
        {
            return -A.Value;
        }
        public static Scalar Multiply(Scalar A, Scalar B)
        {
            return A.Value * B.Value;
        }
        public static Scalar Divide(Scalar A, Scalar B)
        {
            return A.Value / B.Value;
        }
        #endregion

        #region Operators
        public static Scalar operator -(Scalar a) => Negate(a);
        public static Scalar operator +(Scalar a, Scalar b) => Add(a, b);
        public static Scalar operator -(Scalar a, Scalar b) => Subtract(a, b);
        public static Scalar operator *(Scalar a, Scalar b) => Multiply(a, b);
        public static Scalar operator /(Scalar a, Scalar b) => Divide(a, b);
        #endregion



    }

}
