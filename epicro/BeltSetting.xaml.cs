using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using epicro.Helpers;

namespace epicro
{
    /// <summary>
    /// BeltSetting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BeltSetting : Window
    {
        public BeltSetting()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txt_Hero.Text      = SettingsManager.Current.HeroNum.ToString();
            txt_Bag.Text       = SettingsManager.Current.BagNum.ToString();
            cbb_BeltNum.Text   = SettingsManager.Current.BeltNum;
            txt_BeltSpeed.Text = SettingsManager.Current.BeltSpeed.ToString();

            cb_save.IsChecked           = SettingsManager.Current.SaveEnabled;
            cb_pickup.IsChecked         = SettingsManager.Current.PickupEnabled;
            cb_heroselect.IsChecked     = SettingsManager.Current.HeroSelectEnabled;
            cb_preventChatbox.IsChecked = SettingsManager.Current.PreventChatboxEnter;
        }

        private void btn_beltSave_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txt_Hero.Text, out _) || !int.TryParse(txt_Bag.Text, out _))
            {
                MessageBox.Show("영웅과 창고는 숫자만 입력해주세요.");
                return;
            }

            if (!double.TryParse(txt_BeltSpeed.Text, out _))
            {
                MessageBox.Show("벨트 속도는 숫자(초)로 입력해주세요.");
                return;
            }
            BeltSetting_Save();
            this.Close();
        }

        private void BeltSetting_Save()
        {
            if (int.TryParse(txt_Hero.Text, out int heroNum))
                SettingsManager.Current.HeroNum = heroNum;

            if (int.TryParse(txt_Bag.Text, out int bagNum))
                SettingsManager.Current.BagNum = bagNum;

            SettingsManager.Current.BeltNum = cbb_BeltNum.Text;

            if (double.TryParse(txt_BeltSpeed.Text, out double beltSpeed))
                SettingsManager.Current.BeltSpeed = beltSpeed;

            SettingsManager.Current.SaveEnabled         = cb_save.IsChecked           == true;
            SettingsManager.Current.PickupEnabled       = cb_pickup.IsChecked         == true;
            SettingsManager.Current.HeroSelectEnabled   = cb_heroselect.IsChecked     == true;
            SettingsManager.Current.PreventChatboxEnter = cb_preventChatbox.IsChecked == true;

            SettingsManager.Save();
        }

        private void btn_beltClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
