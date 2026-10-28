namespace EskaCMS.IntegrationDepartment.ServerFlows.Definition
{
    public enum FlowIssueSeverity
    {
        Error = 1,
        Warning = 2,
    }

    /// <summary>
    /// One problem found in a flow definition. Path points at the JSON node
    /// (e.g. "apiGroupList[0].apis[1].dataMapping[2]") so the admin page can show it next to the field.
    /// </summary>
    public sealed record FlowIssue(FlowIssueSeverity Severity, string Path, string Code, string Message)
    {
        public static FlowIssue Error(string path, string code, string message) => new(FlowIssueSeverity.Error, path, code, message);

        public static FlowIssue Warning(string path, string code, string message) => new(FlowIssueSeverity.Warning, path, code, message);
    }
}
