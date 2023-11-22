using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;

namespace NeuroSpeech.EntityAccessControl
{
    public static class CastAs
    {
        public static string String(object n) => n.ToString()!;

        public static string String(int n) => n.ToString();

        internal static MethodInfo StringMethod = typeof(CastAs)
            .GetRuntimeMethod(nameof(String), new Type[] { typeof(int) })!;

        public static double Double(object n) => Convert.ToDouble(n);

        public static double Double(float? n) => n.HasValue ? n.Value : 0;
        
        public static double Double(float n) => n;

        public static bool? Nullable(bool v) => v;

        public static short? Nullable(short v) => v;
        public static int? Nullable(int v) => v;

        public static long? Nullable(long v) => v;
        public static ushort? Nullable(ushort v) => v;
        public static uint? Nullable(uint v) => v;

        public static ulong? Nullable(ulong v) => v;

        public static float? Nullable(float v) => v;
        public static double? Nullable(double v) => v;



        public static DateTime? Nullable(DateTime v) => v;

        public static DateTimeOffset? Nullable(DateTimeOffset v) => v;

        public static Guid? Nullable(Guid v) => v;

        public static Decimal? Nullable(decimal v) => v;

        internal static void Register(ModelBuilder modelBuilder)
        {
            foreach(var method in typeof(CastAs).GetMethods())
            {
                if (method.Name == "Nullable")
                {
                    modelBuilder.HasDbFunction(method)
                        .HasTranslation(a => a.ElementAt(0));
                }
            }
        }

        internal static MethodInfo DoubleFromFloatNullableMethod = typeof(CastAs)
            .GetRuntimeMethod(nameof(Double), new Type[] { typeof(float?) })!;
        internal static MethodInfo DoubleFromFloatMethod = typeof(CastAs)
            .GetRuntimeMethod(nameof(Double), new Type[] { typeof(float) })!;

    }
}
