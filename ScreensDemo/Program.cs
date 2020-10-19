using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace ScreensDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            MutiScreenManagerDriver.Do();

            Console.ReadLine();
        }


    }
    /// <summary>
    /// ScreenManager 使用示例
    /// </summary>
    public class MutiScreenManagerDriver
    {
        public static void Do()
        {
            var sm = new MutiScreenManager(); // 1.创建对象
            sm.LoadConfigFile(Path.Combine(Application.StartupPath, "screen.json")); // 2.读取配置文件
            if (!sm.ScreenConfigInspect()) // 3.比较兼容性
            {
                Console.WriteLine($"不兼容");
                var rst = sm.ShowConfigDialog(); // 4.a.显示配置管理对话框
                Console.WriteLine($"{rst}");
            }
            else
            {
                Console.WriteLine($"兼容");
                // 4.b.读取屏幕配置
                var primaryScreenNo = sm.GetScreenIndexByScreenType(ScreenType.Primary); // 读取主控屏索引
                var promptScreenNo = sm.GetScreenIndexByScreenType(ScreenType.Prompt);   // 读取提示屏索引
                var eblackboardScreenNo = sm.GetScreenIndexByScreenType(ScreenType.EBlackboard); // 读取电子白板屏索引
                Console.WriteLine($"primaryScreenNo={primaryScreenNo}, promptScreenNo={promptScreenNo}, eblackboardScreenNo={eblackboardScreenNo}");
            }
        }
    }

    /// <summary>
    /// 屏幕管理类
    /// </summary>
    sealed public class MutiScreenManager
    {
        private const string PRIMARY_SCREEN = "主控屏";
        private const string PROMPT_SCREEN = "提示屏";
        private const string EBLACKBOARD_SCREEN = "白板屏";
        private JObject _configFileContent = null; // 配置文件内容缓存

        /// <summary>
        /// 判断当前屏幕配置与配置文件是否兼容
        /// </summary>
        /// <returns>true: 兼容  false: 不兼容</returns>
        public bool ScreenConfigInspect()
        {
            #region 定义变量
            var activeScreenData = new List<ScreenData>(4); // 活动屏幕配置
            var configScreenData = new List<ScreenData>(4); // 配置文件屏幕配置
            var validNickNameCollection = new List<string> { PRIMARY_SCREEN, PROMPT_SCREEN, EBLACKBOARD_SCREEN }; // 配置文件中的定义，只读不可更改。
            #endregion
            #region 读取活动屏幕配置(A)
            foreach (var s in Screen.AllScreens)
            {
                activeScreenData.Add(new ScreenData { DeviceName = s.DeviceName, NickName = string.Empty });
            }
            #endregion
            #region 读取配置文件屏幕配置(C)
            var screens = _configFileContent["screens"] as JArray;
            foreach (JObject s in screens)
            {
                configScreenData.Add(new ScreenData { DeviceName = s["DisplayName"].ToString(), NickName = s["NickName"].ToString() });
            }
            #endregion
            #region 输出数据到屏幕
            OutputMessage($"== 活动屏幕配置(A) ==");
            foreach (var data in activeScreenData)
            {
                OutputMessage($"{data.DeviceName}");
            }
            OutputMessage($"== 配置文件屏幕配置(C) ==");
            foreach (var data in configScreenData)
            {
                OutputMessage($"{data.DeviceName}, {data.NickName}");
            }
            #endregion
            #region 比较两个配置是否兼容
            // (A)中的每一项，(C)中都包含。并且，(A)中的每一项，对应的(C)中的NickName都被指定为(主控屏|提示屏|白板屏)中的一项
            foreach (var data in activeScreenData)
            {
                if (!configScreenData.Any(x => x.DeviceName == data.DeviceName
                    && validNickNameCollection.Any(v => v == x.NickName)))
                {
                    // (A)中的某一项，(C)中不包含。说明(C)中存在某一项配置错误
                    // 或者 (A)中的每一项，(C)中都包含。但对应的(C)中的NickName不合法
                    return false; // 不兼容
                }
            }
            #endregion

            return true; // 兼容
        }

        /// <summary>
        /// 装置配置文件
        /// </summary>
        /// <param name="configFileName"></param>
        /// <returns>返回JObejct对象</returns>
        public void LoadConfigFile(string configFileName) => _configFileContent = JObject.Parse(File.ReadAllText(configFileName)); 

        /// <summary>
        /// 根据屏幕类型查找索引号
        /// </summary>
        /// <param name="screenType"></param>
        /// <returns></returns>
        public int GetScreenIndexByScreenType(ScreenType screenType)
        {
            var screens = _configFileContent["screens"] as JArray;
            foreach (JObject s in screens)
            {
                if ((screenType == ScreenType.Primary && s["NickName"].ToString() == PRIMARY_SCREEN)
                    || (screenType == ScreenType.Prompt && s["NickName"].ToString() == PROMPT_SCREEN)
                    || (screenType == ScreenType.EBlackboard && s["NickName"].ToString() == EBLACKBOARD_SCREEN))
                {
                    for (var i = 0; i < Screen.AllScreens.Length; i++)
                    {
                        if (s["DisplayName"].ToString() == Screen.AllScreens[i].DeviceName)
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }
        /// <summary>
        /// 显示配置屏幕对话框
        /// </summary>
        /// <returns></returns>
        public DialogResult ShowConfigDialog()
        {
            var cfgPrimaryScreenDeviceName = string.Empty;
            var cfgPromptScreenDeviceName = string.Empty;
            var cfgEBlackboardScreenDeviceName = string.Empty;

            var screens = _configFileContent["screens"] as JArray;
            foreach (JObject s in screens)
            {
                var nickName = s["NickName"].ToString();
                if (nickName == PRIMARY_SCREEN)
                {
                    cfgPrimaryScreenDeviceName = s["DisplayName"].ToString();
                }
                else if (nickName == PROMPT_SCREEN)
                {
                    cfgPromptScreenDeviceName = s["DisplayName"].ToString();
                }
                else if (nickName == EBLACKBOARD_SCREEN)
                {
                    cfgEBlackboardScreenDeviceName = s["DisplayName"].ToString();
                }
            }
            var datasourcePrimaryScreen = new List<string>(4);
            var datasourcePromptScreen = new List<string>(4);
            var datasourceEBlackboardScreen = new List<string>(4);
            foreach(var s in Screen.AllScreens)
            {
                datasourcePrimaryScreen.Add(s.DeviceName);
                datasourcePromptScreen.Add(s.DeviceName);
                datasourceEBlackboardScreen.Add(s.DeviceName);
            }
            var frm = new Form { Text = "屏幕管理器", Width = 340, Height = 220, StartPosition = FormStartPosition.CenterScreen };
            var colTypeOffsetX = 20;
            var colComboBoxOffsetX = 120;
            var buttonOffsetX = 140;
            var buttonOffsetY = 140;
            var rowOffsetY = 20;
            var rowHeight = 30;
            var fontHeight = frm.Font.Height;
            var lblPrimaryScreen = new Label { Text = "主控屏", Left = colTypeOffsetX, Top = rowOffsetY };
            var lblPromptScreen = new Label { Text = "提示屏", Left = colTypeOffsetX, Top = rowOffsetY + rowHeight };
            var lblEBlackboardScreen = new Label { Text = "电子白板屏", Left = colTypeOffsetX, Top = rowOffsetY + rowHeight + rowHeight };
            var cbPrimaryScreen = new ComboBox { Left = colComboBoxOffsetX, Top = rowOffsetY, Width = 180, DataSource = datasourcePrimaryScreen };
            var cbPromptScreen = new ComboBox { Left = colComboBoxOffsetX, Top = rowOffsetY + rowHeight, Width = 180, DataSource = datasourcePromptScreen };
            var cbEBlackboardScreen = new ComboBox { Left = colComboBoxOffsetX, Top = rowOffsetY + rowHeight + rowHeight, Width = 180, DataSource = datasourceEBlackboardScreen };
            var lblErrorMessage = new Label { Text = "", ForeColor=Color.Red, Left = colTypeOffsetX, Top = rowOffsetY + rowHeight + rowHeight + rowHeight, Width = 300 };

            var btnOK = new Button { Text = "OK", Left = buttonOffsetX, Top = buttonOffsetY };
            var btnCancel = new Button { Text = "Cancel", Left = buttonOffsetX + new Button().Width + 10, Top = buttonOffsetY };
            frm.Controls.AddRange(new Control[] { lblPrimaryScreen, lblPromptScreen, lblEBlackboardScreen, cbPrimaryScreen, cbPromptScreen, cbEBlackboardScreen, lblErrorMessage, btnOK, btnCancel });
            // 显示错误提示
            Action<string> aShowErrorMessage = new Action<string>((errorMessage) => { lblErrorMessage.Text = $"错误：{errorMessage}"; });
            // 检查配置
            Action aCheckError = new Action(()=> {
                if (!Screen.AllScreens.Any(x => x.DeviceName == cbPrimaryScreen.Text))
                {
                    lblPrimaryScreen.ForeColor = Color.Red;
                }
                else
                {
                    lblPrimaryScreen.ForeColor = Color.Black;
                }
                if (!Screen.AllScreens.Any(x => x.DeviceName == cbPromptScreen.Text))
                {
                    lblPromptScreen.ForeColor = Color.Red;
                }
                else
                {
                    lblPromptScreen.ForeColor = Color.Black;
                }
                if (!Screen.AllScreens.Any(x => x.DeviceName == cbEBlackboardScreen.Text))
                {
                    lblEBlackboardScreen.ForeColor = Color.Red;
                }
                else
                {
                    lblEBlackboardScreen.ForeColor = Color.Black;
                }
            });
            cbPrimaryScreen.LostFocus += (o, ex)=> aCheckError();
            cbPromptScreen.LostFocus += (o, ex) => aCheckError();
            cbEBlackboardScreen.LostFocus += (o, ex) => aCheckError();
            frm.Load += (o, ex) =>
            {
                frm.AcceptButton = btnOK;
                frm.CancelButton = btnCancel;

                cbPrimaryScreen.Text = cfgPrimaryScreenDeviceName;
                cbPromptScreen.Text = cfgPromptScreenDeviceName;
                cbEBlackboardScreen.Text = cfgEBlackboardScreenDeviceName;
                aCheckError();
                btnOK.Click += (ook, eok) =>
                {
                    aCheckError();
                    if (!Screen.AllScreens.Any(x=>x.DeviceName == cbPrimaryScreen.Text))
                    {
                        // 主控屏配置错误
                        aShowErrorMessage("“主控屏配置错误”。");

                        return;
                    }
                    if (!Screen.AllScreens.Any(x => x.DeviceName == cbPromptScreen.Text))
                    {
                        // 提示屏配置错误
                        aShowErrorMessage("“提示屏配置错误”。");

                        return;
                    }
                    if (!Screen.AllScreens.Any(x => x.DeviceName == cbEBlackboardScreen.Text))
                    {
                        // 电子白板屏配置错误
                        aShowErrorMessage("“电子白板屏配置错误”。");

                        return;
                    }

                    // 检查通过
                    frm.DialogResult = DialogResult.OK;
                };
                btnCancel.Click += (ocancel, ecancel) =>
                {
                    frm.DialogResult = DialogResult.Cancel;
                };
            };

            return frm.ShowDialog();
        }

        public static void OutputMessage(string message)
        {
            Console.WriteLine(message);
        }
        /// <summary>
        /// 屏幕数据定义
        /// </summary>
        sealed class ScreenData
        {
            public string DeviceName { get; set; } // 设备名
            public string NickName { get; set; } // 别名(主控屏|提示屏|白板屏|string.Empty)
        }
    }

    /// <summary>
    /// 定义屏幕类型
    /// </summary>
    public enum ScreenType
    {
        Primary, // 主控屏
        Prompt, // 提示屏
        EBlackboard, // (电子)白板屏
        Unknow, // 未知
    }
}
