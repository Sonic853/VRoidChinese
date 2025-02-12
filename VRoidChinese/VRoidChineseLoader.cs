using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Newtonsoft.Json;
using StandaloneWindowTitleChanger;
using UnityEngine;
using VRoid.UI.Messages;

namespace VRoidChinese;

[BepInPlugin("VRoid.Chinese", "VRoid汉化插件", "2.0")]
public class VRoidChineseLoader : BasePlugin
{
    /// <summary>
    /// 汉化文本存放路径
    /// </summary>
    public static string WorkDir = Path.Combine(Paths.PluginPath, "VRoidChinese");
    public static string TranslatePath = Path.Combine(WorkDir, "Chinese");
    public static string StringChinesePath = Path.Combine(TranslatePath, "StringChinese.txt");
    public static string MessagesChinesePath = Path.Combine(TranslatePath, "MessagesChinese.json");

    /// <summary>
    /// 启动汉化插件时是否Dump原文
    /// </summary>
    public ConfigEntry<bool>? OnStartDump;

    /// <summary>
    /// 出现空值时是否Dump合并文本
    /// </summary>
    public ConfigEntry<bool>? OnHasNullValueDump;

    /// <summary>
    /// 开发者模式
    /// </summary>
    public ConfigEntry<bool>? DevMode;

    /// <summary>
    /// 刷新UI快捷键
    /// </summary>
    public ConfigEntry<KeyCode>? RefreshLangKey;

    /// <summary>
    /// 切换中英文快捷键
    /// </summary>
    public ConfigEntry<KeyCode>? SwitchLangKey;

    /// <summary>
    /// 是否有空值, 有空值则需要Dump
    /// </summary>
    public static bool HasNullValue;

    /// <summary>
    /// 英文原文Messages
    /// </summary>
    public string ENMessage = "";

    /// <summary>
    /// 英文原文String
    /// </summary>
    public string ENString = "";

    /// <summary>
    /// 英文原文String的字典形式
    /// </summary>
    public Dictionary<string, string> ENStringDict = new();

    /// <summary>
    /// 合并文本Messages
    /// </summary>
    public string MergeMessage = "";

    /// <summary>
    /// 合并文本String
    /// </summary>
    public string MergeString = "";

    /// <summary>
    /// 是否显示提示
    /// </summary>
    public static bool ShowUpdateTip;

    /// <summary>
    /// 是否进行了回退
    /// </summary>
    public static bool IsFallback;

    /// <summary>
    /// 当前是否为中文
    /// </summary>
    public bool nowCN;
    public override void Load()
    {
        if (!Directory.Exists(TranslatePath))
        {
            Directory.CreateDirectory(TranslatePath);
        }
        // 读取配置
        OnStartDump = Config.Bind("config", "OnStartDump", false, "当启动时进行转储 (原词条)");
        OnHasNullValueDump = Config.Bind("config", "OnHasNullValueDump", false, "当缺失词条时进行转储 (合并后词条)");
        DevMode = Config.Bind("config", "DevMode", false, "汉化者开发模式");
        RefreshLangKey = Config.Bind("config", "RefreshLangKey", KeyCode.F10, "[仅限开发模式] 刷新语言快捷键");
        SwitchLangKey = Config.Bind("config", "SwitchLangKey", KeyCode.F11, "[仅限开发模式] 切换语言快捷键");

        var vRoidChinese = AddComponent<VRoidChinese>();
        vRoidChinese.loader = this;
        Harmony.CreateAndPatchAll(typeof(VRoidChinese));
    }

    /// <summary>
    /// 备份原文
    /// </summary>
    public void Backup()
    {
        Log.LogInfo("开始备份原文...");
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        ENMessage = JsonConvert.SerializeObject(Messages.All["en"], Formatting.Indented);
        var enDict = Messages.s_localeStringDictionary["en"];
        var sb = new StringBuilder();
        foreach (var kv in enDict)
        {
            ENStringDict.Add(kv.Key, kv.Value);
            string value = kv.Value.Replace("\r\n", "\\r\\n");
            sb.AppendLine($"{kv.Key}={value}");
        }
        ENString = sb.ToString();
        sw.Stop();
        Log.LogInfo($"备份耗时 {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 转储词条
    /// </summary>
    public void DumpOri()
    {
        Log.LogInfo("开始 Dump 原文...");
        File.WriteAllText(Path.Combine(TranslatePath, $"DumpMessages_en_{Application.version}.json"), ENMessage);
        File.WriteAllText(Path.Combine(TranslatePath, $"DumpString_en_{Application.version}.txt"), ENString);
    }

    /// <summary>
    /// Dump合并后的文本
    /// </summary>
    public void DumpMerge()
    {
        Debug.Log("开始 Dump Merge Messages...");
        var messages = JsonConvert.DeserializeObject<Messages>(MergeMessage);
        var messagesStr = JsonConvert.SerializeObject(messages, Formatting.Indented);
        File.WriteAllText(Path.Combine(TranslatePath, $"DumpMergeMessages.json"), messagesStr);
        Debug.Log("开始 Dump Merge String...");
        var strDict = Messages.s_localeStringDictionary["en"];
        var sb = new StringBuilder();
        foreach (var kv in strDict)
        {
            var value = kv.Value.Replace("\r\n", "\\r\\n");
            sb.AppendLine($"{kv.Key}={value}");
        }
        File.WriteAllText(Path.Combine(TranslatePath, $"DumpMergeString.txt"), sb.ToString());
    }

    /// <summary>
    /// 开始汉化
    /// </summary>
    public void ToCN()
    {
        HasNullValue = false;
        Log.LogInfo("----------开始汉化----------");
        FixString();
        FixMessages();
        Log.LogInfo("刷新界面...");
        try
        {
            Messages.OnMessagesLanguageChange.Invoke();
            nowCN = true;
            Log.LogInfo("----------汉化完成----------");
        }
        catch (Exception e)
        {
            Log.LogInfo($"刷新界面出现异常: {e.Message}\n{e.StackTrace}");
            IsFallback = true;
            ToEN();
        }
    }

    /// <summary>
    /// 切换到英文原文
    /// </summary>
    public void ToEN()
    {
        Log.LogInfo("切换到英文...");
        Messages.s_localeDictionary["en"] = JsonConvert.DeserializeObject<Messages>(ENMessage);
        Messages.OnMessagesLanguageChange.Invoke();
        foreach (var kv in ENStringDict)
        {
            Messages.s_localeStringDictionary["en"][kv.Key] = kv.Value;
        }
        nowCN = false;
    }

    /// <summary>
    /// 汉化Messages
    /// </summary>
    public void FixMessages()
    {
        Log.LogInfo("开始汉化 Messages...");
        if (File.Exists(Path.Combine(TranslatePath, "MessagesChinese.json")))
        {
            Log.LogInfo("检测到 Messages 汉化文件, 开始读取文件...");
            string json;
            try
            {
                json = File.ReadAllText(Path.Combine(TranslatePath, "MessagesChinese.json"));
            }
            catch (Exception e)
            {
                Log.LogError($"读取 Messages 汉化文件出现异常: {e.Message}\n{e.StackTrace}");
                return;
            }
            Log.LogInfo("合并软件原有英文和 Messages 汉化文件...");
            try
            {

                var ori = new JSONObject(ENMessage);
                var cnJson = new JSONObject(json);
                MergeJson(ori, cnJson);
                var sortJson = SortJson(ori);
                MergeMessage = sortJson.ToString();
            }
            catch (Exception e)
            {
                Log.LogError($"合并软件原有英文和 Messages 汉化文件出现异常: {e.Message}\n{e.StackTrace}");
                return;
            }
            Log.LogInfo("开始解析合并后文件...");
            Messages? cn = null;
            try
            {
                cn = JsonConvert.DeserializeObject<Messages>(MergeMessage);
            }
            catch (Exception e)
            {
                Log.LogError($"解析合并后文件出现异常: {e.Message}\n{e.StackTrace}");
                return;
            }
            if (HasNullValue)
            {
                Log.LogWarning("有缺失的词条, 需要通知汉化作者进行更新.");
                if (OnHasNullValueDump?.Value == true)
                {
                    DumpMerge();
                }
            }
            Log.LogInfo("开始将中文 Messages 对象替换到英文对象...");
            try
            {
                Messages.s_localeDictionary["en"] = cn;
            }
            catch (Exception e)
            {
                Log.LogError($"将中文 Messages 对象替换到英文对象出现异常:{e.Message}\n{e.StackTrace}");
                return;
            }
            Log.LogInfo("Messages 汉化完毕.");
        }
        else
        {
            Log.LogError($"未检测到 Messages 汉化文件{TranslatePath}/MessagesChinese.json,请检查安装.");
        }
    }

    /// <summary>
    /// 汉化常规文本
    /// </summary>
    public void FixString()
    {
        Log.LogInfo("开始汉化常规文本...");
        if (File.Exists(Path.Combine(TranslatePath, "StringChinese.txt")))
        {
            Log.LogInfo("检测到 String 汉化文件, 开始读取文件...");
            string[] lines;
            try
            {
                lines = File.ReadAllLines(Path.Combine(TranslatePath, "StringChinese.txt"));
            }
            catch (Exception e)
            {
                Log.LogError($"读取 String 汉化文件出现异常: {e.Message}\n{e.StackTrace}");
                return;
            }
            Log.LogInfo("开始解析 String 汉化文件...");
            var strDict = Messages.s_localeStringDictionary["en"];
            try
            {
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var kv = line.Split(['='], 2);
                        if (kv.Length == 2)
                        {
                            strDict[kv[0]] = kv[1].Replace("\\r\\n", "\r\n");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError($"解析 String 汉化文件出现异常: {e.Message}\n{e.StackTrace}");
                return;
            }
            Log.LogInfo("String 汉化完毕.");
        }
        else
        {
            Log.LogError($"未检测到 String 汉化文件 {WorkDir}/StringChinese.txt, 请检查安装.");
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(StandaloneWindowTitle), "Change")]
    public static bool WindowTitlePatch(ref string newTitle)
    {
        newTitle += $" 汉化插件 by 宵夜97 (开源免费)";
        return true;
    }

    /// <summary>
    /// 合并游戏英文数据和从文本读取的中文数据
    /// </summary>
    public void MergeJson(JSONObject baseJson, JSONObject modJson)
    {
        var keys = new List<string>();
        foreach (var k in baseJson.keys)
        {
            keys.Add(k);
        }
        foreach (var k in keys)
        {
            if (modJson.HasField(k))
            {
                if (baseJson[k].IsString)
                {
                    baseJson.SetField(k, modJson[k]);
                }
                else if (baseJson[k].IsObject)
                {
                    MergeJson(baseJson[k], modJson[k]);
                }
            }
            else
            {
                // 没有字段, 添加到通知
                HasNullValue = true;
                Log.LogWarning($"检测到缺失的词条 {k}:{baseJson[k]}");
            }
        }
    }

    /// <summary>
    /// 根据Key排序Json
    /// </summary>
    public static JSONObject SortJson(JSONObject baseJson)
    {
        if (baseJson.type == JSONObject.Type.OBJECT)
        {
            var keys = new List<string>(baseJson.keys);
            keys.Sort();
            var obj = new JSONObject(JSONObject.Type.OBJECT);
            foreach (var key in keys)
            {
                obj.SetField(key, baseJson[key]);
            }
            return obj;
        }
        else
        {
            return baseJson;
        }
    }
}
