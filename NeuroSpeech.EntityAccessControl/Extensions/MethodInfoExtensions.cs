#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("NeuroSpeech.EntityAccessControl.Tests")]

namespace NeuroSpeech.EntityAccessControl
{
    internal static class MethodInfoExtensions
    {
        private static int IndexOf<T>(this T[] items, T item)
        {
            return Array.IndexOf(items, item);
        }

        internal static MethodInfo MatchArguments(this MethodInfo input, Expression? target, List<Expression> args)
        {
            if(target != null)
            {
                var al = new List<Expression> { target };
                al.AddRange(args);
                args = al;
            }

            if(input.ArgmentsMatch(args))
            {
                return input;
            }

            // must be a generic...
            var gm = input.GetGenericMethodDefinition();
            var inputParameters = gm.GetParameters();
            var inputGenericArgs = gm.GetGenericArguments();
            var gtypes = new Type[inputGenericArgs.Length];

            var i = 0;
            foreach(var ip in inputParameters)
            {
                var arg = args[i++];
                var ipt = ip.ParameterType;
                if (ipt.IsGenericParameter)
                {
                    var index = inputGenericArgs.IndexOf(ipt);
                    if (gtypes[index] != null)
                        continue;
                    gtypes[index] = args[0].Type;
                    continue;
                }

                if (!ipt.ContainsGenericParameters)
                    continue;

                ResolveDelegateType(inputGenericArgs, gtypes, ipt, arg.Type);

            }

            return gm.MakeGenericMethod(gtypes);
        }

        private static void ResolveDelegateType(
            Type[] inputGenericArgs, 
            Type[] gtypes, 
            Type ipt, Type argType)
        {
            var i = 0;
            foreach(var ip in ipt.GenericTypeArguments)
            {
                var argInputType = argType.GenericTypeArguments[i++];
                var index = inputGenericArgs.IndexOf(ip);
                if (index != -1)
                {
                    if (gtypes[index] != null)
                        continue;
                    gtypes[index] = argInputType;
                    continue;
                }
                if (!ipt.ContainsGenericParameters)
                    continue;
                ResolveDelegateType(inputGenericArgs, gtypes, ip, argInputType);
            }
        }

        internal static bool ArgmentsMatch(this MethodInfo input, List<Expression> args)
        {
            var mps = input.GetParameters();
            int index = 0;
            foreach (var item in args)
            {
                var p = mps[index++];
                if (!p.ParameterType.IsAssignableFrom(item.Type))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
