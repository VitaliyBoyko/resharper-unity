using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis
{
    public static class BurstCodeAnalysisUtil
    {
        [ContractAnnotation("null => false")]
        public static bool IsBurstPermittedType([CanBeNull] this IType type)
        {
            if (type == null)
                return false;
            
            return type.IsValueType() || type.IsPointerType() || type.IsOpenType;
        }

        [ContractAnnotation("null => false")]
        public static bool IsAccessedFromOpenType([CanBeNull] this IConditionalAccessExpression conditionalAccessExpression)
        {
            if (conditionalAccessExpression is IInvocationExpression)
            {
                conditionalAccessExpression =
                    conditionalAccessExpression.ConditionalQualifier as IConditionalAccessExpression;
            }
            
            if (conditionalAccessExpression == null)
                return false;

            return conditionalAccessExpression.ConditionalQualifier?.Type().IsOpenType ?? false;
        }

        public static bool IsBurstPermittedInvocation([NotNull] IInvocationExpression invocationExpression)
        {
            var invokedMethod = CallGraphUtil.GetCallee(invocationExpression) as IMethod;
            if (invokedMethod == null)
                return false;
            if (IsDebugLog(invokedMethod))
                return true;
            if (IsStringFormat(invokedMethod))
                return true;
            return false;
        }

        private static bool IsDebugLog([NotNull] IMethod method)
        {
            if (!method.IsStatic)
                return false;
            
            if (method.Parameters.Count != 1)
                return false;
            
            if (method.ShortName != "Log" && method.ShortName != "LogError" && method.ShortName != "LogWarning")
                return false;
            
            var clrTypeName = method.GetContainingType()?.GetClrName();
            if (clrTypeName == null)
                return false;

            if (!clrTypeName.Equals(KnownTypes.Debug))
                return false;
            
            var parameter = method.Parameters[0];
            if (!parameter.Type.IsObject())
                return false;
            
            return true;
        }

        private static bool IsStringFormat([NotNull] IMethod method)
        {
            if (!method.IsStatic)
                return false;
            
            if (method.ShortName != "Format")
                return false;
            
            var clrTypeName = method.GetContainingType()?.GetClrName();
            if (clrTypeName == null)
                return false;

            if (!clrTypeName.Equals(PredefinedType.STRING_FQN))
                return false;

            var parameters = method.Parameters;
            if (parameters.Count < 2)
                return false;
            
            if (!parameters[0].Type.IsString())
                return false;

            return true;
        }
        
        public static bool IsObjectMethodInvocation([CanBeNull] this IInvocationExpression invocation)
        {
            var function =
                invocation?.InvocationExpressionReference.Resolve().DeclaredElement as IFunction;
            if (function == null)
                return false;
            var containingType = function.GetContainingType();
            // GetHashCode permitted in burst only if no boxing happens i.e. calling base.GetHashCode
            // Equals is prohibited because it works through System.Object and require boxing, which 
            // Burst does not support
            if (containingType is IStruct && function is IMethod method && method.IsOverridesObjectGetHashCode())
                return false;
            var isValueTypeOrObject = containingType is IClass @class && (@class.IsSystemValueTypeClass() || @class.IsObjectClass());
            return isValueTypeOrObject || containingType is IStruct && function.IsOverride;
        }

        [ContractAnnotation("null => false")]
        public static bool IsReturnValueBurstProhibited([CanBeNull] this IFunction invokedMethod)
        {
            if (invokedMethod == null)
                return false;
            
            return (invokedMethod.IsStatic || invokedMethod.GetContainingType() is IStruct)
                   && invokedMethod.ReturnType.Classify == TypeClassification.REFERENCE_TYPE;
        }

        [ContractAnnotation("null => false")]
        public static bool HasBurstProhibitedArguments([CanBeNull] this IArgumentList argumentList)
        {
            if (argumentList == null)
                return false;
            
            foreach (var argument in argumentList.Arguments)
            {
                if (!(argument.MatchingParameter?.Type.IsBurstPermittedType() ?? true))
                    return true;
            }

            return false;
        }

        public static bool IsFunctionNode(ITreeNode node)
        {
            switch (node)
            {
                case IFunctionDeclaration _:
                case ICSharpClosure _:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsBurstContextBannedNode(ITreeNode node)
        {
            switch (node)
            {
                case IThrowStatement _:
                case IThrowExpression _:
                case IInvocationExpression invocationExpression
                    when CallGraphUtil.GetCallee(invocationExpression) is IMethod method && IsBurstDiscarded(method):
                case IFunctionDeclaration functionDeclaration when IsBurstContextBannedFunction(functionDeclaration.DeclaredElement):
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsBurstContextBannedFunction(IFunction function)
        {
            if (function.IsStatic || function.GetContainingTypeMember() is IStruct)
                return function is IMethod method && IsBurstDiscarded(method);
            return true;
        }

        private static bool IsBurstDiscarded(IMethod method)
        {
            var attributes = method.GetAttributeInstances(KnownTypes.BurstDiscardAttribute, AttributesSource.Self);
            
            return attributes.Count != 0;
        }
    }
}