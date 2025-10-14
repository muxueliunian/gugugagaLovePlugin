using Newtonsoft.Json.Linq;
using Spectre.Console;
using System;
using System.Collections.Concurrent;
using System.Threading;
using UmamusumeResponseAnalyzer.Plugin;

[assembly: LoadInHostContext]

namespace RaceRewardTracker;

public class Plugin : IPlugin
{
    public string Name => "gugugagaLovePlugin";
    public string Author => "muxiulianNian";
    public Version Version => new(1, 2, 0);
    public string[] Targets => Array.Empty<string>();

    [PluginSetting]
    [PluginDescription("是否启用每日胡萝卜统计 默认 true")]
    public bool EnableCarrotCollection { get; set; } = true;

    [PluginSetting]
    [PluginDescription("每日胡萝卜统计文件(gugugagaMagicCarrot.json)的输出目录。留空=工作目录 PluginData/<插件名>；可填绝对或相对路径。")]
    public string JsonOutputDirectory { get; set; } = string.Empty;

    // 新增：粉丝统计功能开关与输出目录（默认关闭，避免改变旧行为） gugugaga!!!
    [PluginSetting]
    [PluginDescription("是否启用粉丝统计(采集 summary_user_info_array)。默认 false。")]
    public bool EnableFansCollection { get; set; } = false;

    [PluginSetting]
    [PluginDescription("粉丝快照(NDJSON)输出目录。留空=工作目录 PluginData/<插件名>；可填绝对或相对路径。")]
    public string FansOutputDirectory { get; set; } = string.Empty;

    private static readonly ConcurrentDictionary<string, SessionAggregate> _sessions = new();
    private static readonly object _fileLock = new();
    private static readonly TimeZoneInfo TzChina = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
    private static readonly HashSet<(int itemType, int itemId)> KnownGemKeys = new()
    {
        (90, 43),
        //宝石id，如果以后有新的id填进来，不过我觉得cy应该就一种
    };
    private const string DailyGemFileName = "gugugagaMagicCarrot.json";

    // 新增：粉丝快照文件锁与文件名
    private static readonly object _fansFileLock = new();
    private const string FansNdjsonName = "gugugagaFans.ndjson";

    // 胡萝卜计数相关
    private static int _currentCarrotCount = 0;
    private static readonly object _carrotCountLock = new();
    private static Thread? _refreshThread;
    private static volatile bool _refreshRunning = false;

    [PluginSetting]
    [PluginDescription("胡萝卜统计刷新间隔（秒），设置为0禁用定时刷新。默认5秒")]
    public int RefreshIntervalSeconds { get; set; } = 5;

    public void Initialize()
    {
        // 初始化时清理内存聚合，避免跨次运行污染
        _sessions.Clear();

        // 初始化胡萝卜计数
        _currentCarrotCount = GetTodayTotal();
        // 显示初始状态
        DisplayCarrotStatus(_currentCarrotCount);

        // 启动定时刷新线程
        StartRefreshThread();
    }

    public void Dispose()
    {
        // 停止刷新线程
        StopRefreshThread();
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

        // 新增：在不影响原有功能的情况下尝试采集粉丝快照（受开关控制）
        try
        {
            if (EnableFansCollection)
            {
                TryRecordFans(data);
            }
        }
        catch
        {
            // 防御性：采集异常不影响原有逻辑
        }
    }

    private void TryRecordRewards(string sessionKey, JObject container)
    {
        // 开关
        if (!EnableCarrotCollection) return;

        if (container["race_reward_info"] is not JObject rewardInfo) return;

        var items = ExtractRewards(rewardInfo);
        // 仅在顶层 data 容器打印（避免在各子 data_set 重复打印）
        bool isTopData = string.Equals(container.Path, "data", StringComparison.Ordinal);
        if (items.Count == 0)
        {
            if (isTopData)
            {
                var totalTodayNoGain = GetTodayTotal();
                UpdateCarrotCount(totalTodayNoGain); // 更新状态栏显示
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
                UpdateCarrotCount(updated); // 更新状态栏显示
            }
            else
            {
                var totalToday = GetTodayTotal();
                UpdateCarrotCount(totalToday); // 更新状态栏显示
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

    // 新增：粉丝数据文件采集。解析 summary_user_info_array 将关键信息写入JSON
    private void TryRecordFans(JObject data)
    {
        if (data["summary_user_info_array"] is not JArray summaryArray || summaryArray.Count == 0) return;

        var dateKey = DateTime.Now.ToString("yyyyMMdd");
        var baseDir = string.IsNullOrWhiteSpace(FansOutputDirectory)
            ? Path.Combine(".", "PluginData", Name)
            : FansOutputDirectory;
        Directory.CreateDirectory(baseDir);
        
        // 文件名改为按日期分文件的JSON格式
        var fileName = $"{dateKey}.json";
        var path = Path.Combine(baseDir, fileName);

        var newRecords = new Dictionary<string, JObject>(); // 改为string键，用于JSON
        int count = 0;

        foreach (var userInfo in summaryArray)
        {
            if (userInfo is not JObject u) continue;
            var viewerId = u["viewer_id"]?.ToObject<long?>();
            if (viewerId == null) continue;

            var name = u["name"]?.ToString() ?? "";
            var fan = u["fan"]?.ToObject<long?>() ?? 0;
            var comment = u["comment"]?.ToString() ?? "";
            var rankScore = u["rank_score"]?.ToObject<long?>() ?? 0;

            // 处理circle/社团信息
            long? circleId = null;
            string circleName = "";
            if (u["circle_info"] is JObject ci)
            {
                circleId = ci["circle_id"]?.ToObject<long?>();
                circleName = ci["name"]?.ToString() ?? "";
            }

            // 构造json数据结构记录
            var rec = new JObject
            {
                ["name"] = name,
                ["fan"] = fan,
                ["circle_name"] = circleName,
                ["ts"] = dateKey, // 简化为yyyymmdd格式
                ["viewer_id"] = viewerId.Value,
                ["comment"] = comment,
                ["rank_score"] = rankScore
            };
            if (circleId is long cid) rec["circle_id"] = cid;

            newRecords[viewerId.Value.ToString()] = rec; // 使用viewer_id字符串作为键
            count++;
        }

        if (count == 0) return;

        lock (_fansFileLock)
        {
            // 读取现有JSON数据（如果文件存在）
            var existingData = new JObject();
            if (File.Exists(path))
            {
                try
                {
                    var jsonText = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(jsonText))
                    {
                        existingData = JObject.Parse(jsonText);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"读取现有粉丝数据失败，将重新创建文件: {ex.Message}");
                    existingData = new JObject();
                }
            }

            // 合并数据：新数据覆盖旧数据
            foreach (var kvp in newRecords)
            {
                existingData[kvp.Key] = kvp.Value; // 使用viewer_id作为键，覆盖或新增
            }

            // 重写整个JSON文件
            var jsonOutput = existingData.ToString(Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(path, jsonOutput);
        }

        Console.WriteLine($"已采集社团粉丝数: {count} 条，文件: {fileName} 🐧gugugaga!!!🐧");
    }

    // 显示彩色胡萝卜状态（非阻塞）
    private static void DisplayCarrotStatus(int count)
    {
        // 根据数量不同显示不同颜色
        var color = count switch
        {
            >= 110 => "purple",      // 110以上：紫色
            >= 70 => "blue",         // 70-109：蓝色
            >= 40 => "red",          // 40-69：红色
            >= 10 => "green",        // 10-39：绿色
            _ => "grey"              // 0-9：灰色
        };

        // 输出彩色文本：今日已获得胡萝卜数量:数字 🐧
        AnsiConsole.MarkupLine($"今日育成时已获得胡萝卜数量:[{color}]{count}[/] 🐧");
    }

    // 更新胡萝卜计数（线程安全）并显示
    private static void UpdateCarrotCount(int newCount)
    {
        lock (_carrotCountLock)
        {
            _currentCarrotCount = newCount;
        }
        DisplayCarrotStatus(newCount);
    }

    // 启动定时刷新线程
    private void StartRefreshThread()
    {
        if (_refreshRunning || RefreshIntervalSeconds <= 0) return;

        _refreshRunning = true;
        _refreshThread = new Thread(() =>
        {
            while (_refreshRunning)
            {
                try
                {
                    // 等待指定间隔
                    Thread.Sleep(RefreshIntervalSeconds * 1000);

                    if (!_refreshRunning) break;

                    // 从文件读取最新数据并显示
                    int currentCount = GetTodayTotal();
                    lock (_carrotCountLock)
                    {
                        _currentCarrotCount = currentCount;
                    }
                    DisplayCarrotStatus(currentCount);
                }
                catch
                {
                    // 防止线程崩溃
                }
            }
        })
        {
            IsBackground = true,
            Name = "CarrotRefreshThread"
        };

        _refreshThread.Start();
    }

    // 停止定时刷新线程
    private static void StopRefreshThread()
    {
        _refreshRunning = false;
        _refreshThread?.Join(2000); // 等待最多2秒
    }

    private sealed class SessionAggregate(string key)
    {
        public string Key { get; } = key;
        public List<(int item_type, int item_id, int item_num, string source)> Items { get; } = new();
        public void Add(List<(int item_type, int item_id, int item_num, string source)> items) => Items.AddRange(items);
    }

    private static bool IsGem(int itemType, int itemId) => KnownGemKeys.Contains((itemType, itemId));
}
