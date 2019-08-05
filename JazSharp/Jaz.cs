﻿using JazSharp.Expectations;
using JazSharp.Reflection;
using JazSharp.SpyLogic;
using JazSharp.Spies;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using JazSharp.Testing.ExecutionContext;
using System.Threading;
using System.Text;

[assembly: InternalsVisibleTo("JazSharp.TestAdapter")]
[assembly: InternalsVisibleTo("JazSharp.Tests")]
[assembly: InternalsVisibleTo("JazSharp.ManualTest")]

namespace JazSharp
{
    public static class Jaz
    {
        internal static SemaphoreSlim CurrentTestSemaphore { get; } = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The context for the currently executing test. This can be used to add custom messages
        /// to the test's output.
        /// </summary>
        public static TestExecutionContext CurrentTest { get; internal set; }

        internal static void SetupTestExecutionContext(string testDescription, StringBuilder output)
        {
            SpyInfo.Clear();
            CurrentTest = new TestExecutionContext(testDescription, output);
        }

        internal static void ClearTestExecutionContext()
        {
            CurrentTest = null;
            SpyInfo.Clear();
        }

        public static Spy SpyOn(object @object, string nameOfMethod)
            => SpyOn(@object, nameOfMethod, null);

        public static Spy SpyOn(object @object, string nameOfMethod, Type[] overloadParameters)
        {
            if (@object == null)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            if (nameOfMethod == null)
            {
                throw new ArgumentNullException(nameof(nameOfMethod));
            }

            var methods =
                @object.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.Name == nameOfMethod)
                    .ToList();

            if (overloadParameters != null)
            {
                methods = methods.Where(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(overloadParameters)).ToList();
            }

            if (methods.Count == 0)
            {
                throw new ArgumentException("Name does not match any methods on the object.", nameof(nameOfMethod));
            }

            if (methods.Count > 1)
            {
                throw new ArgumentException("Name matches more than one methods.", nameof(nameOfMethod));
            }

            var spyInfo = SpyCreator.CreateSpy(methods[0], @object);

            return new Spy(spyInfo, @object);
        }

        public static Spy SpyOn(Type type, string nameOfMethod)
            => SpyOn(type, nameOfMethod, null);

        public static Spy SpyOn(Type type, string nameOfMethod, Type[] overloadParameters)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (nameOfMethod == null)
            {
                throw new ArgumentNullException(nameof(nameOfMethod));
            }

            var methods =
                type
                    .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.Name == nameOfMethod)
                    .ToList();

            if (overloadParameters != null)
            {
                methods = methods.Where(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(overloadParameters)).ToList();
            }

            if (methods.Count == 0)
            {
                throw new ArgumentException("Name does not match any static methods on the type.", nameof(nameOfMethod));
            }

            if (methods.Count > 1)
            {
                throw new ArgumentException("Name matches more than one static methods.", nameof(nameOfMethod));
            }

            var spyInfo = SpyCreator.CreateSpy(methods[0], string.Empty);

            return new Spy(spyInfo, string.Empty);
        }

        public static PropertySpy SpyOn<TObject, TProperty>(TObject @object, Expression<Func<TObject, TProperty>> property)
        {
            var propertyInfo = ExpressionHelper.GetPropertyFromExpression(property);
            return new PropertySpy(propertyInfo, @object);
        }

        public static PropertySpy SpyOn<TProperty>(Expression<Func<TProperty>> staticProperty)
        {
            var propertyInfo = ExpressionHelper.GetPropertyFromExpression(staticProperty);
            return new PropertySpy(propertyInfo, string.Empty);
        }

        public static AnyMatcher Any<T>()
        {
            return new AnyMatcher(typeof(T), false);
        }

        public static AnyMatcher InstanceOf<T>()
        {
            return new AnyMatcher(typeof(T), true);
        }

        public static object Invoke(Expression<Action> method)
        {
            if (method.Body is MethodCallExpression methodCall)
            {
                return InvokationHelper.InvokeMethodWithSpySupport(
                    methodCall.Method,
                    ExpressionHelper.GetValueFromExpression(methodCall.Object),
                    methodCall.Arguments.Select(ExpressionHelper.GetValueFromExpression).ToArray());
            }
            else
            {
                throw new ArgumentException("Method must only contain a method call.");
            }
        }
    }
}