using System.Collections.Concurrent;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Localization;
using EskaCMS.Infrastructure.Data;
using EskaCMS.IntegrationDepartment.ServerFlows.Definition;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.IntegrationDepartment.ServerFlows.Services
{
    /// <summary>
    /// One of the site's languages (SitesCultures). Id is what pages send as their cultureId
    /// header; Language is the code's language part ("ar" for "ar-SA").
    /// </summary>
    public sealed record SiteLanguage(long Id, string Code, string Language, string Prefix, bool IsDefault);

    /// <summary>The sentences for one caller: the caller's language, and the site's translations in it by key.</summary>
    public sealed record CallerTexts(SiteLanguage? Language, IReadOnlyDictionary<string, string> Site)
    {
        /// <summary>The sentence for <paramref name="key"/> (see <see cref="FlowTexts.Pick"/>); <paramref name="own"/> are the call's and the flow's.</summary>
        public string Get(string key, params IReadOnlyDictionary<string, string>?[] own) =>
            FlowTexts.Pick(key, Language?.Language, Site.TryGetValue(key, out var text) ? text : null, own);

        /// <summary>The sentence with its values filled in.</summary>
        public string Get(CallerText text) => FlowTexts.Format(Get(text.Key), text.Args);
    }

    /// <summary>
    /// The site's languages and the sentences callers get, both from DCMS itself:
    ///  - the languages are the site's cultures; the default is the one whose address has no
    ///    prefix, else the first one (the order DCMS lists them in);
    ///  - the sentences are the site's Translations keys "ServerFlows.{key}" (see FlowTexts),
    ///    translated per language in the Admin's Translations screen. A value still equal to its
    ///    key is the placeholder DCMS writes for a new key, not a translation;
    ///  - a key the site does not have yet is added the way the pages add a key they miss (every
    ///    language, value = the key), so it shows up in Translations by itself: the "ServerFlows.*"
    ///    keys, and the keys a flow's author wrote. A key made of what another system answered is
    ///    only looked up, never added.
    /// Both are read once and kept for a few minutes: a change shows within that time. Only sites
    /// that exist are served (an unknown site id gets nothing, and leaves nothing behind).
    /// </summary>
    public sealed class ServerFlowTexts
    {
        /// <summary>What the Localization keys of these sentences start with.</summary>
        public const string KeyPrefix = "ServerFlows.";

        private static readonly TimeSpan Keep = TimeSpan.FromMinutes(5);

        /// <summary>How soon a site id that is not known makes the list of sites be read again.</summary>
        private static readonly TimeSpan LookAgainForSites = TimeSpan.FromSeconds(30);

        /// <summary>The longest key DCMS's Translations take as written.</summary>
        private const int MaxKeyLength = 450;

        private readonly IServiceScopeFactory _scopes;
        private readonly IMemoryCache _cache;

        /// <summary>Sites whose own keys were checked in this process (existing sites only).</summary>
        private readonly ConcurrentDictionary<long, bool> _ownKeysChecked = new();

        /// <summary>One at a time in this process: adding keys, reading the list of sites.</summary>
        private readonly SemaphoreSlim _gate = new(1, 1);

        private HashSet<long> _siteIds = new();
        private DateTimeOffset _siteIdsRead = DateTimeOffset.MinValue;

        public ServerFlowTexts(IServiceScopeFactory scopes, IMemoryCache cache)
        {
            _scopes = scopes;
            _cache = cache;
        }

        /// <summary>
        /// Whether a site with this id exists. The site ids are read at most every few minutes (sooner
        /// for an id not seen yet, at most every 30 s), so a caller naming any site id costs nothing.
        /// </summary>
        public async Task<bool> SiteExistsAsync(long siteId, CancellationToken ct = default)
        {
            if (siteId <= 0)
                return false;
            var age = DateTimeOffset.UtcNow - _siteIdsRead;
            var known = _siteIds.Contains(siteId);
            if (known ? age < Keep : age < LookAgainForSites)
                return known;

            await _gate.WaitAsync(ct);
            try
            {
                if (DateTimeOffset.UtcNow - _siteIdsRead >= LookAgainForSites)
                {
                    using var scope = _scopes.CreateScope();
                    var ids = await scope.ServiceProvider.GetRequiredService<IRepository<Site>>().Query().AsNoTracking().Select(s => s.Id).ToListAsync(ct);
                    _siteIds = ids.ToHashSet();
                    _siteIdsRead = DateTimeOffset.UtcNow;
                }
                return _siteIds.Contains(siteId);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>The site's languages, default first.</summary>
        public async Task<IReadOnlyList<SiteLanguage>> LanguagesAsync(long siteId, CancellationToken ct = default)
        {
            var key = $"ServerFlows:languages:{siteId}";
            if (_cache.TryGetValue(key, out IReadOnlyList<SiteLanguage>? hit) && hit != null)
                return hit;

            using var scope = _scopes.CreateScope();
            var rows = await scope.ServiceProvider.GetRequiredService<IRepository<SitesCultures>>().Query().AsNoTracking()
                .Where(c => c.SiteId == siteId && c.Status != EStatus.Deleted)
                .OrderBy(c => c.Id)
                .Select(c => new { c.Id, c.CultureId, c.CulturePrefix })
                .ToListAsync(ct);

            var defaultId = rows.FirstOrDefault(r => string.IsNullOrWhiteSpace(r.CulturePrefix))?.Id ?? rows.FirstOrDefault()?.Id;
            IReadOnlyList<SiteLanguage> list = rows
                .Select(r => new SiteLanguage(r.Id, r.CultureId ?? "", FlowTexts.LanguageOf(r.CultureId), r.CulturePrefix ?? "", r.Id == defaultId))
                .OrderByDescending(l => l.IsDefault)
                .ToList();
            _cache.Set(key, list, Keep);
            return list;
        }

        /// <summary>The site language with this id; the site's default one for 0 or an id it does not have.</summary>
        public async Task<SiteLanguage?> LanguageAsync(long siteId, long cultureId, CancellationToken ct = default)
        {
            var languages = await LanguagesAsync(siteId, ct);
            return languages.FirstOrDefault(l => l.Id == cultureId) ?? languages.FirstOrDefault(l => l.IsDefault);
        }

        /// <summary>The site's default language id (0 when the site has none).</summary>
        public async Task<long> DefaultCultureIdAsync(long siteId, CancellationToken ct = default) =>
            (await LanguageAsync(siteId, 0, ct))?.Id ?? 0;

        /// <summary>The sentences for a caller in this site language (the default one for 0).</summary>
        public async Task<CallerTexts> ForAsync(long siteId, long cultureId, CancellationToken ct = default)
        {
            await AddOwnKeysAsync(siteId, ct);
            var language = await LanguageAsync(siteId, cultureId, ct);
            return new CallerTexts(language, language == null ? new Dictionary<string, string>() : await SiteTextsAsync(siteId, language.Id, ct));
        }

        /// <summary>The site's translations in one language, by key without the prefix ("failed", ...).</summary>
        public async Task<IReadOnlyDictionary<string, string>> SiteTextsAsync(long siteId, long cultureId, CancellationToken ct = default)
        {
            var key = $"ServerFlows:texts:{siteId}:{cultureId}";
            if (_cache.TryGetValue(key, out IReadOnlyDictionary<string, string>? hit) && hit != null)
                return hit;

            using var scope = _scopes.CreateScope();
            var rows = await scope.ServiceProvider.GetRequiredService<IRepository<ResourcesValue>>().Query().AsNoTracking()
                .Where(v => v.CultureId == cultureId && v.Key.SiteId == siteId && v.Key.Key.StartsWith(KeyPrefix))
                // A translation for the whole site before one made for a single page.
                .OrderBy(v => v.SlugInfoId == null ? 0 : 1).ThenBy(v => v.Id)
                .Select(v => new { v.Key.Key, v.Value })
                .ToListAsync(ct);

            var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var name = row.Key[KeyPrefix.Length..];
                var value = row.Value?.Trim();
                if (name.Length == 0 || string.IsNullOrEmpty(value) || string.Equals(value, row.Key.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
                texts.TryAdd(name, value);
            }
            _cache.Set(key, (IReadOnlyDictionary<string, string>)texts, Keep);
            return texts;
        }

        /// <summary>
        /// A sentence from the site's Translations by its key, in this site language: a call's own
        /// error message with "Translate the message". Null when the key has no translation there
        /// (no value, or the placeholder: the key itself). <paramref name="component"/> prefers the
        /// key of that Translations component. <paramref name="authorKey"/>: a key the flow's author
        /// wrote; one the site does not have yet is added, as the pages add a key they miss, and its
        /// answer is kept a few minutes. Any other key (made of what the other side answered) is only
        /// looked up.
        /// </summary>
        public async Task<string?> TranslateAsync(long siteId, long cultureId, string key, string? component, bool authorKey, CancellationToken ct = default)
        {
            key = (key ?? "").Trim();
            if (key.Length == 0 || key.Length > MaxKeyLength)
                return null;
            component = string.IsNullOrWhiteSpace(component) ? null : component.Trim();

            var cacheKey = $"ServerFlows:translation:{siteId}:{cultureId}:{component}:{key}";
            if (authorKey && _cache.TryGetValue(cacheKey, out string? hit))
                return hit;

            string? text;
            bool known;
            using (var scope = _scopes.CreateScope())
            {
                var rows = await scope.ServiceProvider.GetRequiredService<IRepository<ResourcesValue>>().Query().AsNoTracking()
                    .Where(v => v.CultureId == cultureId && v.Key.SiteId == siteId && v.Key.Key == key
                        && (v.Key.Component == null || v.Key.Component == component))
                    // the component's own key, then a translation for the whole site before one for a single page
                    .OrderBy(v => v.Key.Component == null ? 1 : 0).ThenBy(v => v.SlugInfoId == null ? 0 : 1).ThenBy(v => v.Id)
                    .Select(v => v.Value)
                    .ToListAsync(ct);
                text = rows.Select(v => v?.Trim()).FirstOrDefault(v => !string.IsNullOrEmpty(v) && !string.Equals(v, key, StringComparison.OrdinalIgnoreCase));
                known = rows.Count > 0 || await scope.ServiceProvider.GetRequiredService<IRepository<ResourcesKey>>().Query().AsNoTracking()
                    .AnyAsync(k => k.SiteId == siteId && k.Key == key, ct);
            }
            if (!known && component == null && authorKey)
                await AddKeysAsync(siteId, new[] { key }, ct);

            if (authorKey)
                _cache.Set(cacheKey, text, Keep);
            return text;
        }

        /// <summary>
        /// The site's "ServerFlows.*" keys, added once per process when the site lacks some (a site
        /// without languages yet is looked at again next time).
        /// </summary>
        public async Task AddOwnKeysAsync(long siteId, CancellationToken ct = default)
        {
            if (siteId <= 0 || _ownKeysChecked.ContainsKey(siteId))
                return;
            if (await AddKeysAsync(siteId, FlowTexts.Defaults.Keys.Select(k => KeyPrefix + k).ToList(), ct))
                _ownKeysChecked[siteId] = true;
        }

        /// <summary>
        /// Adds the site-wide keys the site does not have, with a value for each of its languages equal
        /// to the key: what DCMS writes for a key the pages miss (LocalizationServices.AddLocalization).
        /// One at a time in this process, and looked for again inside the lock, so a key is not added
        /// twice (a site-wide key has no component, and a unique index does not stop two of those);
        /// when another server adds it at the same moment, theirs is kept. False when the site has no
        /// language to add them in yet.
        /// </summary>
        private async Task<bool> AddKeysAsync(long siteId, IReadOnlyCollection<string> keys, CancellationToken ct)
        {
            await _gate.WaitAsync(ct);
            try
            {
                using var scope = _scopes.CreateScope();
                var sp = scope.ServiceProvider;
                var keyRepository = sp.GetRequiredService<IRepository<ResourcesKey>>();
                var wanted = keys.ToList();
                var have = await keyRepository.Query().AsNoTracking()
                    .Where(k => k.SiteId == siteId && wanted.Contains(k.Key))
                    .Select(k => k.Key)
                    .ToListAsync(ct);
                var missing = wanted.Where(k => !have.Contains(k, StringComparer.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (missing.Count == 0)
                    return true;

                var cultures = (await LanguagesAsync(siteId, ct)).Select(l => l.Id).ToList();
                if (cultures.Count == 0)
                    return false;
                try
                {
                    var added = missing.Select(k => new ResourcesKey { Key = k, SiteId = siteId, Component = null, GroupId = null, IsShared = true }).ToList();
                    foreach (var key in added)
                        keyRepository.Add(key);
                    await keyRepository.SaveChangesAsync();

                    var valueRepository = sp.GetRequiredService<IRepository<ResourcesValue>>();
                    foreach (var key in added)
                        foreach (var culture in cultures)
                            valueRepository.Add(new ResourcesValue { KeyId = key.Id, CultureId = culture, Value = key.Key, SlugInfoId = null });
                    await valueRepository.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Another server added them at the same moment: theirs are the site's keys now.
                }
                return true;
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
