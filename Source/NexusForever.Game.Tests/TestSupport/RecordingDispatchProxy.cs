using System.Reflection;

namespace NexusForever.Game.Tests.TestSupport;

internal class RecordingDispatchProxy<T> : DispatchProxy where T : class
{
    private readonly Dictionary<string, object> propertyValues = [];
    private readonly Dictionary<string, object> methodReturnValues = [];

    public List<Invocation> Invocations { get; } = [];

    public static T Create(out RecordingDispatchProxy<T> proxy)
    {
        T instance = DispatchProxy.Create<T, RecordingDispatchProxy<T>>();
        proxy = (RecordingDispatchProxy<T>)(object)instance;
        return instance;
    }

    public IReadOnlyList<Invocation> GetInvocations(string methodName)
    {
        return Invocations
            .Where(i => i.MethodName == methodName)
            .ToList();
    }

    public void SetProperty(string name, object value)
    {
        propertyValues[name] = value;
    }

    public void SetMethodReturn(string name, object value)
    {
        methodReturnValues[name] = value;
    }

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        object[] invocationArgs = args?.ToArray() ?? [];
        Invocations.Add(new Invocation(targetMethod.Name, invocationArgs));

        if (targetMethod.IsSpecialName)
        {
            if (targetMethod.Name.StartsWith("get_", StringComparison.Ordinal)
                && propertyValues.TryGetValue(targetMethod.Name[4..], out object value))
                return value;

            if (targetMethod.Name.StartsWith("set_", StringComparison.Ordinal))
            {
                propertyValues[targetMethod.Name[4..]] = invocationArgs[0];
                return null;
            }
        }

        if (targetMethod.ReturnType == typeof(void))
            return null;

        if (methodReturnValues.TryGetValue(targetMethod.Name, out object returnValue))
            return returnValue;

        return targetMethod.ReturnType.IsValueType
            ? Activator.CreateInstance(targetMethod.ReturnType)
            : null;
    }

    internal readonly record struct Invocation(string MethodName, object[] Arguments);
}
