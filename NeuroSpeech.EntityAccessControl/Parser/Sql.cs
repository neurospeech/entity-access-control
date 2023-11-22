using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace NeuroSpeech.EntityAccessControl
{
    public static class Sql
    {
        public static double? Least(double? l, double? r) => l < r ? l : r;
        public static long? Least(long? l, long? r) => l < r ? l : r;

        public static float? Least(float? l, float? r) => l < r ? l : r;

        public static int? Least(int? l, int? r) => l < r ? l : r;

        public static string Least(string l, string r) => l.CompareTo(r) < 0 ? l : r;

        public static double? Greatest(double? l, double? r) => l > r ? l : r;
        public static long? Greatest(long? l, long? r) => l > r ? l : r;

        public static float? Greatest(float? l, float? r) => l > r ? l : r;

        public static int? Greatest(int? l, int? r) => l > r ? l : r;

        public static string Greatest(string l, string r) => l.CompareTo(r) > 0 ? l : r;

        internal static void Register(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {

            var methods = typeof(Sql).GetMethods();
            foreach (var method in methods)
            {
                switch(method.Name)
                {
                    case nameof(Least):
                    case nameof(Greatest):
                        break;
                    default:
                        continue;
                }
                modelBuilder.HasDbFunction(method)
                    .HasName(method.Name)
                    .IsBuiltIn();
            }

        }
    }
}
