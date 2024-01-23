namespace System.Diagnostics.CodeAnalysis;

#if !NET5_0_OR_GREATER
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
    AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method |
    AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    Inherited = false)]
public sealed class DynamicallyAccessedMembersAttribute : Attribute
{
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
    {
        MemberTypes = memberTypes;
    }

    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}
#endif

#if !NET5_0_OR_GREATER
[Flags]
public enum DynamicallyAccessedMemberTypes
{
    All = -1,
    None = 0,
    PublicParameterlessConstructor = 1,
    PublicConstructors = 3,
    NonPublicConstructors = 4,
    PublicMethods = 8,
    NonPublicMethods = 16,
    PublicFields = 32,
    NonPublicFields = 64,
    PublicNestedTypes = 128,
    NonPublicNestedTypes = 256,
    PublicProperties = 512,
    NonPublicProperties = 1024,
    PublicEvents = 2048,
    NonPublicEvents = 4096,
    Interfaces = 8192
}
#endif