namespace EskaCMS.IntegrationDepartment.ServerFlows.Engine
{
    /// <summary>
    /// The JavaScript value kinds a <c>JToken?</c> stands for. <see cref="Object"/>,
    /// <see cref="Array"/> and <see cref="Date"/> are all JS objects (reference
    /// identity, <c>typeof === 'object'</c>); the split exists because the ports
    /// branch on <c>Array.isArray</c> and <c>instanceof Date</c>.
    /// </summary>
    internal enum JsType
    {
        Undefined,
        Null,
        Boolean,
        Number,
        String,
        Object,
        Array,
        Date,
    }
}
