using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using NServiceBus.Pipeline;

namespace PipelineOptimizations.Step2;

static class BehaviorExtensions
{
    public static Func<TRootContext, Task> CreatePipelineExecutionFuncWithSmugglingFor<TRootContext>(this IBehavior[] behaviors)
        where TRootContext : IBehaviorContext
    {
        return (Func<TRootContext, Task>)behaviors.CreatePipelineExecutionExpression();
    }

    /// <code>
    /// rootContext
    ///    => behavior1.Invoke(rootContext,
    ///       context1 => behavior2.Invoke(context1,
    ///        ...
    ///          context{N} => behavior{N}.Invoke(context{N},
    ///             context{N+1} => TaskEx.Completed))
    /// </code>
    public static Delegate CreatePipelineExecutionExpression(this IBehavior[] behaviors, List<Expression> expressions = null)
    {
        Delegate lambdaExpression = null;
        var length = behaviors.Length - 1;
        // We start from the end of the list know the lambda expressions deeper in the call stack in advance
        for (var i = length; i >= 0; i--)
        {
            var currentBehavior = behaviors[i];
            var behaviorInterfaceType = currentBehavior.GetType().GetInterfaces().FirstOrDefault(t => t.GetGenericArguments().Length == 2 && t.FullName.StartsWith("NServiceBus.Pipeline.IBehavior"));
            if (behaviorInterfaceType == null)
            {
                throw new InvalidOperationException("Behaviors must implement IBehavior<TInContext, TOutContext>");
            }
            var methodInfo = behaviorInterfaceType.GetMethods().FirstOrDefault();
            if (methodInfo == null)
            {
                throw new InvalidOperationException("Behaviors must implement IBehavior<TInContext, TOutContext> and provide an invocation method.");
            }

            var genericArguments = behaviorInterfaceType.GetGenericArguments();
            var inContextType = genericArguments[0];
                
            var inContextParameter = Expression.Parameter(inContextType, $"context{i}");

            if (i == length)
            {
                if (currentBehavior is IPipelineTerminator)
                {
                    inContextType = typeof(PipelineTerminator<>.ITerminatingContext).MakeGenericType(inContextType);
                }
                var doneDelegate = CreateDoneDelegate(inContextType, i);
                lambdaExpression = CreateBehaviorCallDelegate(methodInfo, inContextParameter, currentBehavior.GetType(), doneDelegate, i, expressions);
                continue;
            }

            lambdaExpression = CreateBehaviorCallDelegate(methodInfo, inContextParameter, currentBehavior.GetType(), lambdaExpression, i, expressions);
        }

        return lambdaExpression;
    }

    /// <code>
    /// context{i} => behavior.Invoke(context{i}, context{i+1} => previous)
    /// </code>>
    static Delegate CreateBehaviorCallDelegate(MethodInfo methodInfo, ParameterExpression outerContextParam, Type behaviorType, Delegate previous, int i, List<Expression> expressions = null)
    {
        PropertyInfo extensionProperty = typeof(IExtendable).GetProperty("Extensions");
        Expression extensionPropertyExpression = Expression.Property(outerContextParam, extensionProperty);
        PropertyInfo behaviorsProperty = typeof(ContextBag).GetProperty("Behaviors", BindingFlags.Instance | BindingFlags.NonPublic);
        Expression behaviorsPropertyExpression = Expression.Property(extensionPropertyExpression, behaviorsProperty);
        Expression indexerPropertyExpression = Expression.ArrayIndex(behaviorsPropertyExpression, Expression.Constant(i));
        Expression castToBehavior = Expression.Convert(indexerPropertyExpression, behaviorType);
        Expression body = Expression.Call(castToBehavior, methodInfo, outerContextParam, Expression.Constant(previous));
        var lambdaExpression = Expression.Lambda(body, outerContextParam);
        expressions?.Add(lambdaExpression);
        return lambdaExpression.CompileFast();
    }

    /// <code>
    /// context{i} => return TaskEx.CompletedTask;
    /// </code>>
    static Delegate CreateDoneDelegate(Type inContextType, int i)
    {
        var innerContextParam = Expression.Parameter(inContextType, $"context{i + 1}");
        return Expression.Lambda(Expression.Constant(Task.CompletedTask), innerContextParam).CompileFast();
    }
}