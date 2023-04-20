using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeuroSpeech.EntityAccessControl.Tests.Model;
using Org.BouncyCastle.Crypto.Paddings;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.EntityAccessControl.Tests.Insert
{

    [TestClass]
    public class ExpressionMatches
    {

        [TestMethod]
        public void Match()
        {
            Expression<Func<Post, long>> e1 = e => e.Author.AccountID;
            Expression<Func<Post, long>> e2= x => x.Author.AccountID;

            Assert.AreEqual(e1.ToExpressionPath(), e2.ToExpressionPath());
        }

    }

    [TestClass]
    public class TypeTests
    {

        [TestMethod]
        public void Resolve()
        {
            var m1 = SingleInput<string>;
            var m2 = SingleInput<int>;
            var list = new List<Expression> {
                Expression.Lambda<Func<string,int>>(Expression.Constant(0), Expression.Parameter(typeof(string)))
            };

            var m = m1.Method.MatchArguments(null, list);
            AssertParameterMatches(m, m2);

        }


        [TestMethod]
        public void ResolveMested()
        {
            var m1 = NestedInput<string>;
            var m2 = NestedInput<int>;

            var l = Expression.Lambda<Func<int>>(Expression.Constant(0));

            var list = new List<Expression> {
                Expression.Lambda<Func<string,Func<int>>>(l, Expression.Parameter(typeof(string)))
            };

            var m = m1.Method.MatchArguments(null, list);
            AssertParameterMatches(m, m2);

        }


        public static void SingleInput<T>(Func<string, T> fx)  {
        }

        public static void NestedInput<T>(Func<string, Func<T>> fx)
        {
        }

        public static void AssertParameterMatches(MethodInfo m1, Delegate m)
        {
            var m2 = m.GetMethodInfo();
            Assert.AreEqual(m1.ReturnType, m2.ReturnType);

            var p1 = m1.GetParameters();
            var p2 = m2.GetParameters();
            Assert.AreEqual(p1.Length, p2.Length);
            for (int i = 0; i < p1.Length; i++)
            {
                Assert.AreEqual(p1[i].ParameterType, p2[i].ParameterType);
            }
        }

    }
}
