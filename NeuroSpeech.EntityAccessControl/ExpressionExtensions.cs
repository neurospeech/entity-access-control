using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace NeuroSpeech.EntityAccessControl
{
    internal static class ExpressionExtensions
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(this Expression exp, ExpressionType nodeType, out T result)
            where T: Expression
        {
            if (exp.NodeType == nodeType)
            {
                result = (T)exp;
                return true;
            }
            result = null!;
            return false;
        }

    }
}
