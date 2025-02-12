using UnityEngine;

namespace VRoidChinese;

public class VRoidChinese : MonoBehaviour
{
    public VRoidChineseLoader? loader;
    KeyCode refreshLangKey;
    KeyCode switchLangKey;
    private void Update()
    {
        if (loader?.DevMode?.Value == true)
        {
            if (loader.RefreshLangKey?.Value != null)
            {
                refreshLangKey = (KeyCode)(loader.RefreshLangKey?.Value)!;
                if (Input.GetKeyDown(refreshLangKey))
                {
                    loader.ToCN();
                }
            }
            if (loader.SwitchLangKey?.Value != null)
            {
                switchLangKey = (KeyCode)(loader.SwitchLangKey?.Value)!;
                if (Input.GetKeyDown(switchLangKey))
                {
                    if (loader.nowCN)
                    {
                        loader.ToEN();
                    }
                    else
                    {
                        loader.ToCN();
                    }
                }
            }
        }
    }

    private void OnGUI()
    {
        GUI.backgroundColor = Color.black;
        if (VRoidChineseLoader.ShowUpdateTip)
        {
            var rect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 150, 400, 300);
            rect = GUILayout.Window(1234, rect, ExceptionTipWindowFunc, "出现异常", GUILayout.ExpandHeight(true));
        }
    }

    public void ExceptionTipWindowFunc(int id)
    {
        GUI.backgroundColor = Color.white;
        GUI.contentColor = Color.black;
        GUILayout.Label("检查到汉化插件出现了异常, 可能是与新版本不兼容导致.");
        GUILayout.Label("可以前往 GitHub 查看汉化是否有更新.");
        GUILayout.Label("如果 GitHub 上未更新汉化, 可以到VRoid交流群找我反馈.");
        GUILayout.Label("汉化作者: xiaoye97");
        GUILayout.Label("GitHub: xiaoye97");
        GUILayout.Label("QQ: 1066666683");
        GUILayout.Label("B站: 宵夜97");
        GUILayout.Label("宵夜食堂: 528385469");
        GUILayout.Label("VRoid交流群: 684544577");
        GUILayout.Label("汉化插件官网: https://github.com/xiaoye97/VRoidChinese");
        GUILayout.Label(" ");
        if (VRoidChineseLoader.IsFallback)
        {
            GUI.contentColor = Color.red;
            GUILayout.Label("由于缺失词条导致异常, 已回退到英文.");
        }
        GUI.contentColor = Color.black;
        if (GUILayout.Button("确定"))
        {
            VRoidChineseLoader.ShowUpdateTip = false;
        }
    }
}
