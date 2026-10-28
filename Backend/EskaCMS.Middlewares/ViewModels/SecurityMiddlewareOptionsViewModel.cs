using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Middlewares.ViewModels
{
    public class SecurityMiddlewareOptions
    {
        public const string SectionName = "SecurityMiddleware";

        /// <summary>
        /// Master switch — set to false to bypass the entire middleware pipeline.
        /// Replaces the old "SecuirtyPenetration" key (typo fixed).
        /// </summary>
        public bool Enabled { get; set; } = false;

        public CorsOptions Cors { get; set; } = new();
        public ActiveSessionOptions ActiveSession { get; set; } = new();
        public UserDataAuthorizationOptions UserDataAuthorization { get; set; } = new();
        public CspOptions Csp { get; set; } = new(); 

    }
    public class CspOptions
    {
        public bool Enabled { get; set; } = false;
        public List<string> ScriptSources { get; set; } = new();
        public List<string> StyleSources { get; set; } = new();
        public List<string> ImageSources { get; set; } = new();
        public List<string> FontSources { get; set; } = new();
        public List<string> ConnectSources { get; set; } = new();
        public List<string> DefaultSources { get; set; } = new();
        public bool AllowUnsafeInlineScripts { get; set; } = false;
        public bool AllowUnsafeInlineStyles { get; set; } = false;
        public string FrameAncestors { get; set; } = "none";
    }

    public class CorsOptions
    {
      
        public bool Enabled { get; set; } = false;

      
        public List<string> StrictSites { get; set; } = new();
    }

    public class ActiveSessionOptions
    {
       
        public List<string> ExcludedPaths { get; set; } = new();
    }

    public class UserDataAuthorizationOptions
    {
       
        public List<string> ProtectedEndpoints { get; set; } = new();
    }
}
