using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TopPicksPlus.Logic;

namespace TopPicksPlus.Storage;

/// <summary>
/// How the four files are written (D24, D88, D117): System.Text.Json, camelCase names and enum values, indented, UTF-8 with no
/// byte-order mark; times as UTC ISO 8601 ending in <c>Z</c>; UK days as <c>YYYY-MM-DD</c>; a value that is none is left out
/// (and a missing or null value reads back as none). Every file is an object whose <c>version</c> comes first.
/// </summary>
internal static class Json
{
    public static readonly JsonSerializerOptions Options = Build();

    private static JsonSerializerOptions Build()
    {
        var o = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Disallow,
        };
        o.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        o.Converters.Add(new UtcDateTimeConverter());
        return o;
    }

    public static readonly UTF8Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>UTF-8 text; a byte-order mark (Notepad adds one) is dropped. Invalid UTF-8 throws (the file is damaged).</summary>
    public static string Decode(byte[] bytes)
    {
        var text = Utf8.GetString(bytes);
        return text.Length > 0 && text[0] == '\uFEFF' ? text.Substring(1) : text;
    }

    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Read<T>(string text)
        where T : class => JsonSerializer.Deserialize<T>(text, Options) ?? throw new JsonException("empty file");

    /// <summary>The file's <c>version</c>, or null when it isn't a JSON object with a whole-number version of at least 1.</summary>
    public static int? Version(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;
            if (!doc.RootElement.TryGetProperty("version", out var v) || v.ValueKind != JsonValueKind.Number || !v.TryGetInt32(out var n))
                return null;
            return n >= 1 ? n : (int?)null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>A UTC time, whatever kind it arrived as (an unspecified time is taken to be UTC already).</summary>
    public static DateTime Utc(DateTime t) =>
        t.Kind == DateTimeKind.Local ? t.ToUniversalTime() : DateTime.SpecifyKind(t, DateTimeKind.Utc);

    public static string Day(DateTime day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static DateTime ParseDay(string? text) =>
        DateTime.ParseExact(text ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);
}

/// <summary>Writes every time as UTC ISO 8601 with a trailing <c>Z</c>; reads ISO 8601 and keeps it as UTC (D117).</summary>
internal sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (text == null || text.Length < 20 || text[10] != 'T' || !text.EndsWith("Z", StringComparison.Ordinal))
            throw new JsonException("a time must be UTC ISO 8601, ending in Z");
        return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Json.Utc(value).ToString(Format, CultureInfo.InvariantCulture));
}

/// <summary>managed.json (data.md): <c>{ "version": 1, "entries": [ ... ] }</c>.</summary>
internal sealed class ManagedFile : IDataFile
{
    public int Version { get; set; } = DataFolder.CurrentVersion;
    public List<ManagedRow> Entries { get; set; } = new List<ManagedRow>();

    public static ManagedFile From(IEnumerable<ManagedEntry> entries) =>
        new ManagedFile { Entries = entries.Select(ManagedRow.From).ToList() };

    public IReadOnlyList<ManagedEntry> ToEntries() =>
        (Entries ?? throw new JsonException("entries missing")).Select(r => (r ?? throw new JsonException("empty entry")).ToEntry()).ToList();
}

/// <summary>One managed.json entry, keyed by <c>folder</c> (D26, D28, D90, D101).</summary>
internal sealed class ManagedRow
{
    public string? Folder { get; set; }
    public long? SourceId { get; set; }
    public TitleKind Kind { get; set; }
    public EntryState State { get; set; }
    public int? Rank { get; set; }
    public DateTime? RankDateUtc { get; set; }
    public bool Adopted { get; set; }

    /// <summary>Video path (relative, <c>\</c>-separated) → source item ID; null for an adopted .strm that matched nothing.</summary>
    public Dictionary<string, long?>? Items { get; set; }

    public List<string>? Files { get; set; }

    public static ManagedRow From(ManagedEntry e) => new ManagedRow
    {
        Folder = e.Folder,
        SourceId = e.SourceId,
        Kind = e.Kind,
        State = e.State,
        Rank = e.Rank,
        RankDateUtc = e.RankDateUtc.HasValue ? Json.Utc(e.RankDateUtc.Value) : (DateTime?)null,
        Adopted = e.Adopted,
        Items = e.Items.ToDictionary(kv => kv.Key, kv => kv.Value),
        Files = e.Files.ToList(),
    };

    /// <summary>
    /// Back to Logic's record. The folder is kept exactly as written, even if it is empty or unsafe: the D37 checks refuse it and
    /// the report names it (C45), so a bad entry is never dropped silently.
    /// </summary>
    public ManagedEntry ToEntry()
    {
        var items = new Dictionary<string, long?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in Items ?? new Dictionary<string, long?>())
            items[kv.Key] = kv.Value;
        var files = (Files ?? new List<string>()).Select(f => f ?? string.Empty).ToList();
        return new ManagedEntry(Folder ?? string.Empty, SourceId, Kind, State, Rank, RankDateUtc, Adopted, items, files);
    }
}

/// <summary>plays.json (data.md): <c>{ "version": 1, "plays": [ ... ], "importMarker": { ... } }</c>.</summary>
internal sealed class PlaysFile : IDataFile
{
    public int Version { get; set; } = DataFolder.CurrentVersion;
    public List<PlayRow> Plays { get; set; } = new List<PlayRow>();
    public ImportMarker? ImportMarker { get; set; }

    public static PlaysFile From(PlayLog log) =>
        new PlaysFile { Plays = log.Plays.Select(PlayRow.From).ToList(), ImportMarker = log.Marker };

    public PlayLog ToLog()
    {
        var plays = (Plays ?? throw new JsonException("plays missing")).Select(r => (r ?? throw new JsonException("empty play")).ToPlay()).ToList();
        if (ImportMarker != null && string.IsNullOrEmpty(ImportMarker.RunId))
            throw new JsonException("import marker without a runId");
        return new PlayLog(plays, ImportMarker);
    }
}

/// <summary>One play (data.md). <c>dayUk</c> is a plain <c>YYYY-MM-DD</c>.</summary>
internal sealed class PlayRow
{
    public string? UserId { get; set; }
    public long SourceId { get; set; }
    public long? EpisodeId { get; set; }
    public string? DayUk { get; set; }
    public double Minutes { get; set; }
    public bool MarkedPlayed { get; set; }
    public double? RuntimeMinutes { get; set; }
    public PlayOrigin Origin { get; set; }

    public static PlayRow From(Play p) => new PlayRow
    {
        UserId = p.UserId,
        SourceId = p.SourceId,
        EpisodeId = p.EpisodeId,
        DayUk = Json.Day(p.DayUk),
        Minutes = p.Minutes,
        MarkedPlayed = p.MarkedPlayed,
        RuntimeMinutes = p.RuntimeMinutes,
        Origin = p.Origin,
    };

    public Play ToPlay()
    {
        if (string.IsNullOrEmpty(UserId))
            throw new JsonException("play without a userId");
        return new Play(UserId!, SourceId, EpisodeId, Json.ParseDay(DayUk), Minutes, MarkedPlayed, RuntimeMinutes, Origin);
    }
}
