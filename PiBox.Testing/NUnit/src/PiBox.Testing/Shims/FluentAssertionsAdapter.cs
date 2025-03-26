using System.Collections.Concurrent;
using NUnit.Framework;
using PiBox.Testing;
using PiBox.Testing.Utils;

// ReSharper disable once CheckNamespace
namespace FluentAssertions
{
    public static class FluentAssertionsCommonExtensions
    {
        public const string NUnitObsolescenceMssage = "Use NUnit 4.x Assert methods instead.";

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static T As<T>(this object obj) where T : class =>
            obj as T ?? throw new AssertionException("object is not of type expected");
    }

    public static class FluentAssertionsReferenceTypeExtensions
    {
        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static TestableEnumerable<T> Should<T>(this IEnumerable<T> enumerable) => new(enumerable);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static TestableKeyValueObjects<TKey, TValue> Should<TKey, TValue>(this Dictionary<TKey, TValue> dict) =>
            new(dict);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static TestableKeyValueObjects<TKey, TValue> Should<TKey, TValue>(this IDictionary<TKey, TValue> dict) =>
            new(dict);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static TestableKeyValueObjects<TKey, TValue> Should<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict) =>
            new(dict);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static TestableKeyValueObjects<TKey, TValue> Should<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dict) => new(dict);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static TestableReferenceType<object> Should(this object obj) => new(obj);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static TestableString Should(this string obj) => new(obj);
    }

    public static class FluentAssertionsValueTypeExtensions
    {
        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static TestableValueType<T> Should<T>(this T val) where T : struct => new(val);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static TestableGuid Should(this Guid guid) => new(guid);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static TestableBool Should(this bool boolean) => new(boolean);
    }

    public static class FluentAssertionsMetricMeasurementExtensions
    {
        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static TestableListMetricMeasurement Should(this List<MetricMeasurement> val) => new(val);
    }

    public abstract class TestableBase<T>
    {
        protected abstract T Should { get; }

        public Converted<TTarget> BeOfType<TTarget>()
        {
            Assert.That(Should, Is.TypeOf<TTarget>());
            return new Converted<TTarget>((TTarget)(object)Should);
        }

        public void BeOfType<TTarget>(string message) => Assert.That(Should, Is.TypeOf<TTarget>(), message);
        public void BeOfType(Type type) => Assert.That(Should, Is.TypeOf(type));

        public void BeOfType(Type type, string message) =>
            Assert.That(Should, Is.TypeOf(type), message);

        public class Converted<TTarget>(TTarget subject)
        {
            public TTarget Subject { get; } = subject;
        }
    }

    public abstract class TestableReferenceTypeBase<T> : TestableBase<T> where T : class
    {
        public void Be(T o) => Assert.That(Should, Is.EqualTo(o));
        public void Be(T o, string message) => Assert.That(Should, Is.EqualTo(o), message);

        public void NotBe(T o) => Assert.That(Should, Is.Not.EqualTo(o));
        public void NotBe(T o, string message) => Assert.That(Should, Is.Not.EqualTo(o), message);

        public void NotBeNull() => Assert.That(Should, Is.Not.Null);
        public void NotBeNull(string message) => Assert.That(Should, Is.Not.Null, message);

        public void BeNull() => Assert.That(Should, Is.Null);
        public void BeNull(string message) => Assert.That(Should, Is.Null, message);
    }

    public abstract class TestableValueTypeBase<T> : TestableBase<T> where T : struct
    {
        public void Be(T o) => Assert.That(Should, Is.EqualTo(o));
        public void Be(T o, string message) => Assert.That(Should, Is.EqualTo(o), message);

        public void NotBe(T o) => Assert.That(Should, Is.Not.EqualTo(o));
        public void NotBe(T o, string message) => Assert.That(Should, Is.Not.EqualTo(o), message);
    }

    public class TestableGuid(Guid should) : TestableValueTypeBase<Guid>
    {
        protected override Guid Should { get; } = should;

        public void NotBe(Guid guid) => Assert.That(Should, Is.Not.EqualTo(guid));
        public void NotBeEmpty() => Assert.That(Should, Is.Not.EqualTo(Guid.Empty));
    }

    public class TestableBool(bool should) : TestableValueTypeBase<bool>
    {
        protected override bool Should { get; } = should;

        public void BeFalse() => Assert.That(Should, Is.False);
        public void BeFalse(string message) => Assert.That(Should, Is.False, message);
        public void BeTrue() => Assert.That(Should, Is.True);
        public void BeTrue(string message) => Assert.That(Should, Is.True, message);
    }

    public class TestableValueType<T>(T should) : TestableValueTypeBase<T> where T : struct
    {
        protected override T Should { get; } = should;
    }

    public class TestableKeyValueObjects<TKey, TValue>(object should)
        : TestableEnumerable<KeyValuePair<TKey, TValue>>((IEnumerable<KeyValuePair<TKey, TValue>>)should)
    {
        public void ContainKey(TKey key) => Assert.That(Should, Does.ContainKey(key));
        public void ContainKey(TKey key, string message) => Assert.That(Should, Does.ContainKey(key), message);

        public void NotContainKey(TKey key) => Assert.That(Should, Does.Not.ContainKey(key));
        public void NotContainKey(TKey key, string message) => Assert.That(Should, Does.Not.ContainKey(key), message);

        public void ContainValue(TValue value) => Assert.That(Should, Does.ContainValue(value));
        public void ContainValue(TValue value, string message) => Assert.That(Should, Does.ContainValue(value), message);

        public void NotContainValue(TValue value) => Assert.That(Should, Does.Not.ContainValue(value));
        public void NotContainValue(TValue value, string message) => Assert.That(Should, Does.Not.ContainValue(value), message);
    }

    public class TestableEnumerable<T>(IEnumerable<T> should) : TestableReferenceTypeBase<IEnumerable<T>>
    {
        protected override IEnumerable<T> Should { get; } = should;

        public void NotBeEmpty() => Assert.That(Should.Any(), Is.True);
        public void NotBeEmpty(string message) => Assert.That(Should.Any(), Is.True, message);

        public void BeEmpty() => Assert.That(Should.Any(), Is.False);
        public void BeEmpty(string message) => Assert.That(Should.Any(), Is.False, message);

        public void BeNullOrEmpty() => Assert.That(Should.Count(), Should is null ? Is.Null : Is.EqualTo(0));

        public void BeNullOrEmpty(string message) =>
            Assert.That(Should.Count(), Should is null ? Is.Null : Is.EqualTo(0), message);

        public void HaveCount(int count) => Assert.That(Should.Count(), Is.EqualTo(count));
        public void HaveCount(int count, string message) => Assert.That(Should.Count(), Is.EqualTo(count), message);
        public void HaveCountGreaterThan(int count) => Assert.That(Should.Count(), Is.GreaterThan(count));
        public void HaveCountGreaterThan(int count, string message) => Assert.That(Should.Count(), Is.GreaterThan(count), message);
        public void HaveCountGreaterOrEqualTo(int count) => Assert.That(Should.Count(), Is.GreaterThanOrEqualTo(count));
        public void HaveCountGreaterOrEqualTo(int count, string message) => Assert.That(Should.Count(), Is.GreaterThanOrEqualTo(count), message);

#pragma warning disable NUnit2007
        public void BeEquivalentTo(IEnumerable<T> expectation, IEqualityComparer<T> comparer) =>
            Assert.That(true, Is.EqualTo(TestUtils.CompareSequences(Should, expectation, comparer)),
                "Sequences are not equivalent");

        public void BeEquivalentTo(IEnumerable<T> expectation, IEqualityComparer<T> comparer, string message) =>
            Assert.That(true, Is.EqualTo(TestUtils.CompareSequences(Should, expectation, comparer)), message);
#pragma warning restore NUnit2007

        public void NotBeNullOrEmpty()
        {
            if (Should != null)
                Assert.That(Should.Count(), Is.GreaterThan(0));
        }

        public void Satisfy(params Func<T, bool>[] func)
        {
            var list = Should.ToList();

            Assert.That(list, Has.Count.EqualTo(func.Length));

            var result = new bool[list.Count];

            foreach (var (f, i) in func.Select((x, i) => (x, i)))
                foreach (var s in list)
                    result[i] |= f(s);

            // ReSharper disable once ReplaceWithSingleCallToAny
            Assert.That(result.Where(r => !r).Any(), Is.False);
        }

        public void ContainSingle(Func<T, bool> predicate) =>
            Assert.That(Should.Where(predicate).SingleOrDefault(), Is.Not.Default);
        public void ContainSingle(Func<T, bool> predicate, string message) =>
            Assert.That(Should.Where(predicate).SingleOrDefault(), Is.Not.Default, message);

        // ReSharper disable once ReplaceWithSingleCallToAny
        public void AllSatisfy(Func<T, bool> func) => Assert.That(Should.Where(s => !func(s)).Any(), Is.False);

        public void Contain(T content) => Assert.That(Should, Does.Contain(content));
        public void Contain(T content, string messages) => Assert.That(Should, Does.Contain(content), messages);
        public void Contain(Func<T, bool> contentExpr) => Assert.That(Should.Any(contentExpr), Is.True);
        public void Contain(Func<T, bool> contentExpr, string message) =>
            Assert.That(Should.Any(contentExpr), Is.True, message);
        public void Contain(IEnumerable<T> containList) => Assert.That(Should, Is.SupersetOf(containList));
        public void Contain(IEnumerable<T> containList, string message) =>
            Assert.That(Should, Is.SupersetOf(containList), message);

        public void NotContain(T content) => Assert.That(Should, Does.Not.Contain(content));
        public void NotContain(T content, string messages) => Assert.That(Should, Does.Not.Contain(content), messages);
        public void NotContain(Func<T, bool> contentExpr) => Assert.That(Should.Any(contentExpr), Is.False);

        public void NotContain(Func<T, bool> contentExpr, string message) =>
            Assert.That(Should.Any(contentExpr), Is.False, message);

        public void Equal(IEnumerable<T> expected)
        {
            var listShould = Should.ToList();
            var listExpected = expected.ToList();

            Assert.That(listShould, Has.Count.EqualTo(listExpected.Count));

            for (var i = 0; i < listShould.Count; i++)
            {
                Assert.That(listShould[i], Is.EqualTo(listExpected[i]));
            }
        }
        public void Equal(IEnumerable<T> expected, string message)
        {
            var listShould = Should.ToList();
            var listExpected = expected.ToList();

            Assert.That(listShould, Has.Count.EqualTo(listExpected.Count), message);

            for (var i = 0; i < listShould.Count; i++)
            {
                Assert.That(listShould[i], Is.EqualTo(listExpected[i]), message);
            }
        }
    }

    public class TestableString(string should) : TestableReferenceType<string>(should)
    {
        public void BeEmpty() => Assert.That(Should, Is.Empty);
        public void BeEmpty(string message) => Assert.That(Should, Is.Empty, message);

        public void NotBeEmpty() => Assert.That(Should, Is.Not.Empty);
        public void NotBeEmpty(string message) => Assert.That(Should, Is.Not.Empty, message);

        public void Contain(string substr) => Assert.That(Should, Does.Contain(substr));
        public void Contain(string substr, string message) => Assert.That(Should, Does.Contain(substr), message);

        public void NotContain(string substr) => Assert.That(Should, Does.Not.Contain(substr));
        public void NotContain(string substr, string message) => Assert.That(Should, Does.Not.Contain(substr), message);

        public void StartWith(string substr) => Assert.That(Should, Does.StartWith(substr));
        public void StartWith(string substr, string message) => Assert.That(Should, Does.StartWith(substr), message);

        public void EndWith(string substr) => Assert.That(Should, Does.EndWith(substr));
        public void EndWith(string substr, string message) => Assert.That(Should, Does.EndWith(substr), message);

        public void Match(string match) => Assert.That(Should, Does.Match(match));
        public void Match(string match, string message) => Assert.That(Should, Does.Match(match), message);
    }

    public class TestableReferenceType<T>(T should) : TestableReferenceTypeBase<T> where T : class
    {
        protected override T Should { get; } = should;

        public void BeAssignableTo<TAssignable>() => Assert.That(Should, Is.AssignableTo<TAssignable>());

        public void BeSameAs(T expectation) => Assert.That(Should, Is.SameAs(expectation));
        public void BeSameAs(T expectation, string message) => Assert.That(Should, Is.SameAs(expectation), message);
        public void NotBeSameAs(T expectation) => Assert.That(Should, Is.Not.SameAs(expectation));
        public void NotBeSameAs(T expectation, string message) => Assert.That(Should, Is.Not.SameAs(expectation), message);

        public void BeEquivalentTo<R>(IEnumerable<R> expectation) => Assert.That(Should, Is.EquivalentTo(expectation));
        public void BeEquivalentTo<R>(IEnumerable<R> expectation, string message) => Assert.That(Should, Is.EquivalentTo(expectation), message);

        public void NotBeNullOrEmpty() => Assert.That(Should, Is.Not.Null.And.Not.Empty);

        public void Match(Func<T, bool> predicate) => Assert.That(predicate(Should), Is.True);
        public void Match(Func<T, bool> predicate, string message) => Assert.That(predicate(Should), Is.True, message);
    }

    public static class FluentAssertionsThrowExtensions
    {
        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static FluentAssertionsFuncSyncThrowHelper<T, R> Invoking<T, R>(this T obj, Func<T, R> predicate) =>
            new(obj, predicate);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static FluentAssertionsAsyncThrowHelper<T, Task> Invoking<T>(this T obj, Func<T, Task> predicate) =>
            new(obj, predicate);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static FluentAssertionsAsyncThrowHelper<T, Task<R>> Invoking<T, R>(this T obj,
            Func<T, Task<R>> predicate) =>
            new(obj, predicate);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static FluentAssertionsActionSyncThrowHelper<T> Invoking<T>(this T obj, Action<T> action) =>
            new(obj, action);

        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public static FluentAssertionsActionNoParameterThrowHelper Should(this Action action) => new(action);
    }

    public class FluentAssertionsActionNoParameterThrowHelper(Action action)
    {
        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public FluentAssertionsActionNoParameterThrowHelper Should() => this;

        public void NotThrow() => Assert.DoesNotThrow(() => action());
        public void NotThrow(string message) => Assert.DoesNotThrow(() => action(), message);

        public TException Throw<TException>() where TException : Exception => Assert.Catch<TException>(() => action());

        public TException Throw<TException>(string message) where TException : Exception =>
            Assert.Catch<TException>(() => action(), message);
    }

    public class FluentAssertionsActionSyncThrowHelper<T>(T obj, Action<T> action)
    {
        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public FluentAssertionsActionSyncThrowHelper<T> Should() => this;

        public void NotThrow() => Assert.DoesNotThrow(() => action(obj));
        public void NotThrow(string message) => Assert.DoesNotThrow(() => action(obj), message);

        public TException Throw<TException>() where TException : Exception =>
            Assert.Catch<TException>(() => action(obj));

        public TException Throw<TException>(string message) where TException : Exception =>
            Assert.Catch<TException>(() => action(obj), message);
    }

    public class FluentAssertionsFuncSyncThrowHelper<T, R>(T obj, Func<T, R> predicate)
    {
        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public FluentAssertionsFuncSyncThrowHelper<T, R> Should() => this;

        public Task NotThrowAsync()
        {
            Assert.DoesNotThrowAsync(() => Task.FromResult(predicate(obj)));
            return Task.CompletedTask;
        }

        public Task NotThrowAsync(string message)
        {
            Assert.DoesNotThrowAsync(() => Task.FromResult(predicate(obj)), message);
            return Task.CompletedTask;
        }

        public Task<TException> ThrowAsync<TException>() where TException : Exception =>
            Task.FromResult(Assert.CatchAsync<TException>(() => Task.FromResult(predicate(obj))));

        public void NotThrow() => Assert.DoesNotThrow(() => predicate(obj));
        public void NotThrow(string message) => Assert.DoesNotThrow(() => predicate(obj), message);

        public TException Throw<TException>() where TException : Exception =>
            Assert.Catch<TException>(() => predicate(obj));

        public TException Throw<TException>(string message) where TException : Exception =>
            Assert.Catch<TException>(() => predicate(obj), message);
    }

    public class FluentAssertionsAsyncThrowHelper<T, R>(T obj, Func<T, R> action) where R : Task
    {
        [Obsolete(FluentAssertionsCommonExtensions.NUnitObsolescenceMssage)]
        public FluentAssertionsAsyncThrowHelper<T, R> Should() => this;

        public Task NotThrowAsync()
        {
            Assert.DoesNotThrowAsync(() => action(obj));
            return Task.CompletedTask;
        }

        public Task NotThrowAsync(string message)
        {
            Assert.DoesNotThrowAsync(() => action(obj), message);
            return Task.CompletedTask;
        }

        public Task<TException> ThrowAsync<TException>() where TException : Exception =>
            Task.FromResult(Assert.CatchAsync<TException>(() => action(obj)));

        public Task<TException> ThrowAsync<TException>(string message) where TException : Exception =>
            Task.FromResult(Assert.CatchAsync<TException>(() => action(obj), message));
    }

    public class TestableListMetricMeasurement(List<MetricMeasurement> val) : TestableEnumerable<MetricMeasurement>(val)
    {
        public void ContainsMetric(long value) =>
            Assert.That(Should.Any(tuple => tuple.Measurement == value), Is.True);

        public void ContainsMetric(long value, string message) =>
            Assert.That(Should.Any(tuple => tuple.Measurement == value), Is.True, message);

        public void ContainsMetric(long value, string expectedTagKey, string expectedTagValue) =>
            Assert.That(Should.Any(tuple =>
                    tuple.Measurement == value
                    && tuple.Tags.Any(
                        x => x.Key == expectedTagKey
                             && x.Value == expectedTagValue)),
                Is.True);

        public void ContainsMetric(long value, string expectedTagKey, string expectedTagValue, string message) =>
            Assert.That(Should.Any(tuple =>
                    tuple.Measurement == value
                    && tuple.Tags.Any(
                        x => x.Key == expectedTagKey
                             && x.Value == expectedTagValue)),
                Is.True, message);
    }

    public class KvpStringObjectComparer : IEqualityComparer<KeyValuePair<string, object>>
    {
        public bool Equals(KeyValuePair<string, object> x, KeyValuePair<string, object> y) => x.Key == y.Key && Equals(x.Value, y.Value);
        public int GetHashCode(KeyValuePair<string, object> obj) => HashCode.Combine(obj.Key, obj.Value);
    }
}
