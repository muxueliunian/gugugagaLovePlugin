using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using UmamusumeResponseAnalyzer.Plugin;

[assembly: LoadInHostContext]

namespace RaceRewardTracker;

public class Plugin : IPlugin
{
    public string Name => "gugugagaLovePlugin";
    public string Author => "muxiulianNian";
    public Version Version => new(1, 0, 2);
    public string[] Targets => Array.Empty<string>();

    [PluginSetting]
    [PluginDescription("每日宝石汇总文件(gugugagaMagicCarrot.json)的输出目录。留空=工作目录 PluginData/<插件名>；可填绝对或相对路径。")]
    public string JsonOutputDirectory { get; set; } = string.Empty;

    private static readonly ConcurrentDictionary<string, SessionAggregate> _sessions = new();
    private static readonly object _fileLock = new();
    private static readonly TimeZoneInfo TzChina = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
    private static readonly HashSet<(int itemType, int itemId)> KnownGemKeys = new()
    {
        (90, 43),
        //宝石id，如果以后有新的id填进来，不过我觉得cy应该就一种
    };
    private const string DailyGemFileName = "gugugagaMagicCarrot.json";

    public void Initialize()
    {
        // 初始化时清理内存聚合，避免跨次运行污染
        _sessions.Clear();
    }

    public void Dispose()
    {
    }

    public Task UpdatePlugin(Spectre.Console.ProgressContext ctx)
    {
        // 空实现，暂时还没写更新途径，以后上传在线服务器或者GitHub可能会写，那是以后的事情 gugugaga！！！
        return Task.CompletedTask;
    }

    [Analyzer] // Response 分析器，签名应为 void Method(JObject obj)
    public void OnResponse(JObject obj)
    {
        var data = obj["data"] as JObject;
        if (data == null) return;

        var charaInfo = data["chara_info"] as JObject;
        var trainedCharaId = charaInfo?["trained_chara_id"]?.ToString() ?? string.Empty;
        var sessionKey = string.IsNullOrEmpty(trainedCharaId) ? "global" : trainedCharaId;

        // Venus/普通育成
        if (data["venus_data_set"] is JObject venus)
        {
            TryRecordRewards(sessionKey, venus);
        }
        if (data["sport_data_set"] is JObject sport)
        {
            TryRecordRewards(sessionKey, sport);
        }
        if (data["free_data_set"] is JObject free)
        {
            TryRecordRewards(sessionKey, free);
        }
        if (data["team_data_set"] is JObject team)
        {
            TryRecordRewards(sessionKey, team);
        }
        if (data["live_data_set"] is JObject live)
        {
            TryRecordRewards(sessionKey, live);
        }
        // 其他可能的数据集也可尝试解析 race_reward_info
        TryRecordRewards(sessionKey, data);
    }

    private void TryRecordRewards(string sessionKey, JObject container)
    {
        if (container["race_reward_info"] is not JObject rewardInfo) return;

        var items = ExtractRewards(rewardInfo);
        // 仅在顶层 data 容器打印（避免在各子 data_set 重复打印）
        bool isTopData = string.Equals(container.Path, "data", StringComparison.Ordinal);
        if (items.Count == 0)
        {
            if (isTopData)
            {
                var totalTodayNoGain = GetTodayTotal();
                Console.WriteLine($"今日已获得胡萝卜数量:{totalTodayNoGain} 🐧gugugaga!!!🐧");
            }
            return;
        }
    
        var agg = _sessions.GetOrAdd(sessionKey, _ => new SessionAggregate(sessionKey));
        agg.Add(items);
    
        // 统计本次获得的宝石数量并写入每日 JSON（东八区）
        var gainGem = items.Where(it => IsGem(it.item_type, it.item_id)).Sum(it => it.item_num);
        if (isTopData)
        {
            if (gainGem > 0)
            {
                var updated = AppendToDailyGemJson(gainGem);
                Console.WriteLine($"今日已获得胡萝卜数量:{updated} 🐧gugugaga!!!🐧");
            }
            else
            {
                var totalToday = GetTodayTotal();
                Console.WriteLine($"今日已获得胡萝卜数量:{totalToday} 🐧gugugaga!!!🐧");
            }
        }
    }
    
    private int AppendToDailyGemJson(int delta)
    {
        var baseDir = string.IsNullOrWhiteSpace(JsonOutputDirectory)
            ? Path.Combine(".", "PluginData", Name)
            : JsonOutputDirectory;
        Directory.CreateDirectory(baseDir);
    
        var path = Path.Combine(baseDir, DailyGemFileName);
    
        lock (_fileLock)
        {
            JObject obj;
            if (File.Exists(path))
            {
                try
                {
                    var txt = File.ReadAllText(path);
                    obj = string.IsNullOrWhiteSpace(txt) ? new JObject() : JObject.Parse(txt);
                }
                catch
                {
                    // 文件损坏或结构不符时重置
                    obj = new JObject();
                }
            }
            else
            {
                obj = new JObject();
            }
    
            var now8 = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TzChina);
            var dateKey = now8.ToString("yyyyMMdd"); // 以东八区日期为键
            var current = obj[dateKey]?.ToObject<int?>() ?? 0;
            var updated = current + delta;
            obj[dateKey] = updated;
    
            File.WriteAllText(path, obj.ToString(Newtonsoft.Json.Formatting.Indented));
            return updated;
        }
    }

    private int GetTodayTotal()
    {
        var baseDir = string.IsNullOrWhiteSpace(JsonOutputDirectory)
            ? Path.Combine(".", "PluginData", Name)
            : JsonOutputDirectory;
        Directory.CreateDirectory(baseDir);
        var path = Path.Combine(baseDir, DailyGemFileName);

        lock (_fileLock)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return 0;
                }
                var txt = File.ReadAllText(path);
                var obj = string.IsNullOrWhiteSpace(txt) ? new JObject() : JObject.Parse(txt);
                var now8 = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TzChina);
                var dateKey = now8.ToString("yyyyMMdd");
                return obj[dateKey]?.ToObject<int?>() ?? 0;
            }
            catch
            {
                // 文件损坏或格式不符时按 0 处理
                return 0;
            }
        }
    }
    // 未使用的 MatchKey 方法
    // private static bool MatchKey(string keys, int itemType, int itemId)
    // {
    //     if (string.IsNullOrWhiteSpace(keys)) return false;
    //     var token = $"{itemType}:{itemId}";
    //     foreach (var k in keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    //     {
    //         if (string.Equals(k, token, StringComparison.Ordinal)) return true;
    //     }
    //     return false;
    // }

    private static List<(int item_type, int item_id, int item_num, string source)> ExtractRewards(JObject rewardInfo)
    {
        var list = new List<(int, int, int, string)>();
        void addFromToken(JToken? tok, string source)
        {
            if (tok is not JArray arr) return;
            foreach (var e in arr)
            {
                var it = e["item_type"]?.ToObject<int?>();
                var id = e["item_id"]?.ToObject<int?>();
                var num = e["item_num"]?.ToObject<int?>();
                if (it is int t && id is int i && num is int n)
                {
                    list.Add((t, i, n, source));
                }
            }
        }
        addFromToken(rewardInfo["race_reward"], "race_reward");
        addFromToken(rewardInfo["race_reward_bonus"], "race_reward_bonus");
        addFromToken(rewardInfo["race_reward_plus_bonus"], "race_reward_plus_bonus");
        addFromToken(rewardInfo["race_reward_bonus_win"], "race_reward_bonus_win");
        return list;
    }

    private sealed class SessionAggregate(string key)
    {
        public string Key { get; } = key;
        public List<(int item_type, int item_id, int item_num, string source)> Items { get; } = new();
        public void Add(List<(int item_type, int item_id, int item_num, string source)> items) => Items.AddRange(items);
    }

    private static bool IsGem(int itemType, int itemId) => KnownGemKeys.Contains((itemType, itemId));
}