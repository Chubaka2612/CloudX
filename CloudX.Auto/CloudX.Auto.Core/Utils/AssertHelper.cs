﻿using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using CloudX.Auto.Core.Extensions;
using AssertionException = CloudX.Auto.Core.Exceptions.AssertionException;

namespace CloudX.Auto.Core.Utils
{
    public static class AssertHelper
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(AssertHelper));

        public static void ThrowFail(string message)
        {
            throw new AssertionException(message);
        }

        private static void AssertAction(string messageToThrow, bool result, bool shouldLogMessage = true)
        {
            if (!result)
            {
                if (shouldLogMessage)
                {
                    Logger.Error(messageToThrow);
                }
                ThrowFail(messageToThrow);
            }
        }

        public static void IsTrue(bool condition, string message = default, bool shouldLogMessage = true)
        {
            if (!string.IsNullOrEmpty(message) && shouldLogMessage)
            {
                Logger.Info(message);
            }
            var msg = string.IsNullOrEmpty(message) ? "" : message;
            AssertAction($"Check the condition is true: '{msg}' failed", condition);
        }


        public static void IsFalse(bool condition, string message = default, bool shouldLogMessage = true)
        {
            if (!string.IsNullOrEmpty(message) && shouldLogMessage)
            {
                Logger.Info(message);
            }
            var msg = string.IsNullOrEmpty(message) ? "" : message;
            AssertAction($"Check the condition is false: {msg} failed", !condition);
        }

        public static void CollectionContains<T>(IList<T> set, IList<T> subSet, string message = default)
        {
            var result = subSet.All(i => set.Contains(i));
            AssertAction($"Check that collection '{string.Join(", ", set)}' contains '{string.Join(", ", subSet)}' failed", result);

            if (!string.IsNullOrEmpty(message))
            {
                Logger.Info(message + $"\nSet: {set.ToJoinString()}" +
                            $"\nSubset  : {subSet.ToJoinString()}");
            }
        }

        public static void CollectionNotContains<T>(IList<T> set, IList<T> subSet, string message = default)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Logger.Info(message);
            }
            var result = subSet.Any(i => set.Contains(i));
            AssertAction($"Check that collection '{string.Join(", ", set)}' doesn't contain '{string.Join(", ", subSet)}' failed", !result);
        }

        public static void CollectionEquals<T>(IList<T> actual, IList<T> expected, string message = default)
        {
        
            var first = actual as T[] ?? actual.ToArray();
            var second = expected as T[] ?? expected.ToArray();
            var result = first.OrderBy(i => i).SequenceEqual(second.OrderBy(i => i));
            AssertAction($"Check that collection '{string.Join(", ", first)}' equals to '{string.Join(", ", second)}'",
                result); 
            Logger.Info(message + $"\nExpected: {expected.ToJoinString()}" +
                                     $"\nActual  : {actual.ToJoinString()}");
            
        }

        public static void CollectionIsNotEmpty<T>(IList<T> set, string message = default)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Logger.Info(message);
            }
            var result = set.Any();
            AssertAction($"Check that collection '{string.Join(", ", set)}' is not empty", result);
        }

        public static void NotNull<T>(T actual, string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Logger.Info(message);
            }
            var result = actual != null;
            AssertAction($"Check that object {actual?.GetType().Name} is not null failed", result);
        }

        public static void IsNull<T>(T actual, string message = default)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Logger.Info(message);
            }
            var result = actual == null;
            AssertAction("Check that object is null failed", result);
        }

        public static void IsNotNull<T>(T actual, string message = default)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Logger.Info(message);
            }
            var result = actual != null;
            AssertAction("Check that object is not null failed", result);
        }

        public static void AssertScope(params Action[] executables)
        {
            var failures = executables.Select(executable =>
            {
                try
                {
                    executable.Invoke();
                    return null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }).Where(failure => failure != null).ToList();

            if (failures.Any())
            {
                throw new AggregateException(failures);
            }
        }

        public static void AreEquals<T>(T actual, T expected, string message = default)
        {
            Logger.Info(message  +
                $"\nExpected: {expected}" +
                $"\nActual  : {actual}");
            bool result;
            if (actual == null)
            {
                if (expected == null || expected is string str && string.IsNullOrEmpty(str))
                {
                    return;
                }

                ThrowFail($"Check that 'null' equals to '{expected}' failed");
                return;
            }

            if (expected == null)
            {
                if (actual is string str && string.IsNullOrEmpty(str))
                {
                    return;
                }

                ThrowFail($"Check that 'null' equals to '{actual}' failed");
            }

            if (typeof(T) == typeof(string))
            {
                result = actual.ToString().Equals(expected.ToString(), StringComparison.OrdinalIgnoreCase);
                AssertAction($"Check that '{actual}' equals to '{expected}' failed", result);
                return;
            }

            result = actual.Equals(expected);
            AssertAction($"Check that '{actual}' equals to '{expected}' failed", result);
        }

        public static void Equals<TKey, TValue>(Dictionary<TKey, TValue> actual, Dictionary<TKey, TValue> expected, string message = default)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Logger.Info(message);
            }

            IEqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;

            AssertScope(() =>
            {
                foreach (var entry in expected)
                {
                    Logger.Debug($"Check key: '{entry.Key}', value: '{entry.Value}' presence");
                    var result = actual.Any(y => y.Key.Equals(entry.Key) && valueComparer.Equals(entry.Value, y.Value));
                    AssertAction("Check that dictionaries are equal", result);
                }
            });
        }

        public static string ShouldContain(this string actualString, string expectedString)
        {
            IsTrue(actualString.Contains(expectedString), $"Check string '{actualString}' contains '{expectedString}'");
            return actualString;
        }

        public static string ShouldNotContain(this string actualString, string expectedString)
        {
            IsFalse(actualString.Contains(expectedString), $"Check string '{actualString}' doesn't contain '{expectedString}'");
            return actualString;
        }

        public static void DatesAreEqualWithOffset(DateTimeOffset expectedDate, DateTimeOffset actualDate, int secondsOffset = 120, string message = default)
        {
            Logger.Info(message +
                        $"\nExpected: {expectedDate};" +
                        $"\nActual  : {actualDate}");

            var difference = Math.Abs(expectedDate.Second - actualDate.Second);
            var result = difference < secondsOffset;

            AssertAction($"Check that data '{actualDate}' equals to '{expectedDate}' failed", result);
        }

        public static void NumbersAreEqualWithOffset<T>(T expectedNumber, T actualNumber, int offset = 120, string message = default)
        {
            Logger.Info(message +
                        $"\nExpected: {expectedNumber};" +
                        $"\nActual  : {actualNumber}");

            if (!typeof(T).IsNumeric())
            {
                throw new ArgumentException("The type of numbers must be numeric.");
            }

            dynamic difference = Math.Abs(Convert.ToDouble(expectedNumber) - Convert.ToDouble(actualNumber));
            dynamic result = difference < offset;

            AssertAction($"Check that number '{actualNumber}' equals to '{expectedNumber}' failed", result);
        }

    }
}
